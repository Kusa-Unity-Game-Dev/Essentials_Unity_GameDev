using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BB.Framework
{
    /// <summary>
    /// Concrete implementation of IValueStore (typed blackboard with scopes, observers, persistence).
    /// </summary>
    public sealed class ValueStore : IValueStore
    {
        private readonly Dictionary<string, Entry> _entries = new(128);
        private readonly IPersistenceProvider _persistence;

        // Scene/Match cleanup flags
        private bool _lifecycleHooked;

        public ValueStore(IPersistenceProvider provider) { _persistence = provider; }

        #region Public API
        public void Set<T>(ValueKey<T> key, T value, ValueScope scope = ValueScope.Session, bool persist = false)
        {
            if (!_entries.TryGetValue(key.Path, out var e))
                e = _entries[key.Path] = new Entry(scope, persist);

            e.Set(value);
            if (persist || e.Persist) SavePersistent();
        }

        public bool TryGet<T>(ValueKey<T> key, out T value)
        {
            if (_entries.TryGetValue(key.Path, out var e) && e.TryGet(out value)) return true;
            value = default; return false;
        }

        public T GetOrDefault<T>(ValueKey<T> key, T fallback = default)
        {
            return TryGet(key, out T v) ? v : fallback;
        }

        public bool Remove(string keyPath)
        {
            var removed = _entries.Remove(keyPath);
            if (removed) SavePersistent();
            return removed;
        }

        public IDisposable Subscribe<T>(ValueKey<T> key, Action<T> onChanged, bool invokeImmediately = false)
        {
            if (!_entries.TryGetValue(key.Path, out var e))
                e = _entries[key.Path] = new Entry(ValueScope.Session, persist: false);

            var token = e.Subscribe(onChanged);
            if (invokeImmediately && e.TryGet(out T current))
                onChanged(current);

            return token;
        }

        public async Task<T> Await<T>(ValueKey<T> key, Func<T, bool> predicate = null, float timeoutSeconds = -1f)
        {
            var tcs = new TaskCompletionSource<T>();
            float start = Time.unscaledTime;

            IDisposable sub = null;
            sub = Subscribe(key, v =>
            {
                if (predicate == null || predicate(v))
                {
                    sub.Dispose();
                    tcs.TrySetResult(v);
                }
            }, invokeImmediately: true);

            if (timeoutSeconds > 0f)
            {
                while (Time.unscaledTime - start < timeoutSeconds && !tcs.Task.IsCompleted)
                    await Task.Yield();
                if (!tcs.Task.IsCompleted)
                {
                    sub.Dispose();
                    throw new TimeoutException($"Await({key}) timed out after {timeoutSeconds}s");
                }
            }

            return await tcs.Task;
        }

        public void ClearScope(ValueScope scope)
        {
            var toRemove = ListPool<string>.Get();
            foreach (var kv in _entries)
                if (kv.Value.Scope == scope) toRemove.Add(kv.Key);

            foreach (var k in toRemove) _entries.Remove(k);
            ListPool<string>.Release(toRemove);
            SavePersistent();
        }

        public void ClearAll()
        {
            _entries.Clear();
            SavePersistent();
        }
        #endregion

        #region Persistence
        public void LoadPersistent()
        {
            if (!_persistence.TryLoad(out var blob)) return;

            foreach (var (path, json) in blob)
            {
                var wrap = JsonUtility.FromJson<SerializedWrapper>(json);
                var e = new Entry(wrap.scope, persist: true);
                e.SetFromJson(wrap.type, wrap.payload);
                _entries[path] = e;
            }
        }

        public void SavePersistent()
        {
            var blob = new Dictionary<string, string>();
            foreach (var (path, e) in _entries)
            {
                if (!e.Persist) continue;
                var wrap = e.ToSerializedWrapper();
                blob[path] = JsonUtility.ToJson(wrap);
            }
            _persistence.Save(blob);
        }

        [Serializable] private class SerializedWrapper
        {
            public ValueScope scope;
            public string type;
            public string payload;
        }
        #endregion

        #region Lifecycle hooks (scene/match)
        public void HookLifecycle()
        {
            if (_lifecycleHooked) return;
            _lifecycleHooked = true;

            SceneManager.activeSceneChanged += OnActiveSceneChanged;
        }

        private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
        {
            ClearScope(ValueScope.Scene);
        }
        
        #endregion

        #region Entry (single value + observer list)
        private sealed class Entry
        {
            public readonly ValueScope Scope;
            public readonly bool Persist;

            private object _boxed;         // stored value
            private string _typeName;      // AssemblyQualifiedName for persistence
            private event Action<object> _changed;

            public Entry(ValueScope scope, bool persist) { Scope = scope; Persist = persist; }

            public void Set<T>(T value)
            {
                _boxed = value;
                _typeName = typeof(T).AssemblyQualifiedName;
                _changed?.Invoke(_boxed);
            }

            public bool TryGet<T>(out T v)
            {
                if (_boxed is T ok) { v = ok; return true; }
                v = default; return false;
            }

            public IDisposable Subscribe<T>(Action<T> onChanged)
            {
                void Handler(object boxed)
                {
                    if (boxed is T t) onChanged(t);
                }
                _changed += Handler;
                return new Disposable(() => _changed -= Handler);
            }

            public SerializedWrapper ToSerializedWrapper()
            {
                // Only JSON-serializable types should be persisted.
                return new SerializedWrapper
                {
                    scope = Scope,
                    type = _typeName,
                    payload = _boxed != null ? JsonUtility.ToJson(new Box(_boxed)) : string.Empty
                };
            }

            public void SetFromJson(string typeName, string payload)
            {
                _typeName = typeName;
                if (string.IsNullOrEmpty(payload)) { _boxed = null; return; }
                var box = JsonUtility.FromJson<Box>(payload);
                _boxed = box.Unbox(typeName);
            }

            [Serializable] private class Box
            {
                public string type;
                public string json;

                public Box(object obj)
                {
                    type = obj.GetType().AssemblyQualifiedName;
                    json = JsonUtility.ToJson(obj);
                }
                public object Unbox(string outerTypeName)
                {
                    var t = Type.GetType(type ?? outerTypeName);
                    return JsonUtility.FromJson(json, t);
                }
            }
        }
        #endregion

        #region Small list pool to reduce GC on ClearScope
        private static class ListPool<T>
        {
            private static readonly Stack<List<T>> Pool = new();
            public static List<T> Get() => Pool.Count > 0 ? Pool.Pop() : new List<T>(32);
            public static void Release(List<T> list) { list.Clear(); Pool.Push(list); }
        }
        #endregion
    }
}
