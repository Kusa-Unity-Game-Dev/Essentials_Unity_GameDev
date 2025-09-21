using System;
using System.Collections.Generic;

namespace BB.Framework
{
    public sealed class Disposable : IDisposable
    {
        private Action _onDispose;
        public Disposable(Action onDispose) { _onDispose = onDispose; }
        public void Dispose() { _onDispose?.Invoke(); _onDispose = null; }
    }

    /// <summary>Collects multiple disposables and disposes them together.</summary>
    public sealed class CompositeDisposable : IDisposable
    {
        private readonly List<IDisposable> _list = new();
        private bool _disposed;

        public void Add(IDisposable d)
        {
            if (_disposed) { d.Dispose(); return; }
            _list.Add(d);
        }

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            foreach (var d in _list) d.Dispose();
            _list.Clear();
        }
    }
}