using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;

public class SoundManager : MonoBehaviour
{
    public static SoundManager I { get; private set; }

    [Header("Catalog + Mixer")]
    [SerializeField] private SoundData soundData;
    [SerializeField] private AudioMixer mixer;               // assign a mixer with Master/Music/SFX groups
    [SerializeField] private string masterVolParam = "MasterVol";
    [SerializeField] private string musicVolParam  = "MusicVol";
    [SerializeField] private string sfxVolParam    = "SfxVol";

    [Header("Pooling")]
    [SerializeField] private AudioSource audioSourcePrefab;  // lightweight prefab (no clip), output set to SFX group by default
    [SerializeField] private Transform sfxPoolParent;
    [SerializeField] private int initialPoolSize = 12;
    [SerializeField] private int maxPoolSize = 32;

    [Header("Music")]
    [SerializeField] private AudioSource musicA;
    [SerializeField] private AudioSource musicB;
    [SerializeField] private float defaultMusicCrossfade = 0.5f;

    // Lookups
    private Dictionary<string, Sound> _byId = new();
    private readonly List<AudioSource> _pool = new();
    private readonly Queue<AudioSource> _free = new();
    private readonly Dictionary<string, float> _cooldowns = new();             // id -> nextPlayableTime
    private readonly Dictionary<string, int> _liveCounts = new();              // id -> activeSourcesCount
    private readonly Dictionary<string, List<AudioSource>> _activeById = new(); // optional: for debugging/limits
    private bool _usingA = true;

    void Awake()
    {
        if (I && I != this) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        // Cache sounds
        if (soundData)
        {
            foreach (var s in soundData.sounds)
                if (!string.IsNullOrEmpty(s.id))
                    _byId[s.id] = s;
        }

        // Build pool
        PrewarmPool();

        // Ensure music sources output to Music bus (can set via inspector)
        if (musicA) musicA.playOnAwake = false;
        if (musicB) musicB.playOnAwake = false;
    }

    void OnDestroy()
    {
        if (I == this) I = null;
    }

    #region Public API

    public static void PlaySound(string id) => I?.Play2D(id);
    public static void PlayAt(string id, Vector3 position) => I?.Play3D(id, position);
    public static void StopAllSfx() => I?.StopAllSfxInternal();
    public static void PlayMusic(string id, float crossfade = -1f) => I?.PlayMusicInternal(id, crossfade);

    public static void SetMasterVolume(float linear01) => I?.SetMixerLinear(I.masterVolParam, linear01);
    public static void SetMusicVolume(float linear01)  => I?.SetMixerLinear(I.musicVolParam, linear01);
    public static void SetSfxVolume(float linear01)    => I?.SetMixerLinear(I.sfxVolParam, linear01);

    #endregion

    #region Core play

    void Play2D(string id)
    {
        if (!TryGetSound(id, out var s)) return;
        if (!CanPlay(s)) return;

        var src = GetFreeSource();
        ConfigureSource(src, s, is3D:false, position:Vector3.zero);
        StartClip(src, s);
    }

    void Play3D(string id, Vector3 pos)
    {
        if (!TryGetSound(id, out var s)) return;
        if (!CanPlay(s)) return;

        var src = GetFreeSource();
        ConfigureSource(src, s, is3D:true, position:pos);
        StartClip(src, s);
    }

    void StartClip(AudioSource src, Sound s)
    {
        var clip = s.GetRandomClip();
        if (!clip) { ReturnToPool(src); return; }

        src.clip = clip;
        src.volume = s.volume;
        src.pitch = s.GetRandomPitch();
        src.loop = s.loop;

        // Instance count & cooldown
        IncCount(s.id);
        if (s.cooldownSeconds > 0f)
            _cooldowns[s.id] = Time.unscaledTime + s.cooldownSeconds;

        src.Play();
        if (!s.loop) StartCoroutine(ReturnWhenDone(src, s.id, clip.length / Mathf.Max(0.01f, Mathf.Abs(src.pitch))));
        else TrackActive(id:s.id, src, add:true);
    }

    IEnumerator ReturnWhenDone(AudioSource src, string id, float duration)
    {
        // Uses unscaled time so UI-pause doesn’t stall cleanup
        float t = 0f;
        while (t < duration && src.isPlaying) { t += Time.unscaledDeltaTime; yield return null; }
        TrackActive(id, src, add:false);
        DecCount(id);
        ReturnToPool(src);
    }

    void StopAllSfxInternal()
    {
        foreach (var src in _pool)
        {
            if (!src) continue;
            if (src == musicA || src == musicB) continue;
            src.Stop();
        }
        foreach (var kv in _liveCounts.Keys) _liveCounts[kv] = 0;
        foreach (var list in _activeById.Values) list.Clear();
        _free.Clear();
        foreach (var src in _pool) if (src && !src.isPlaying) _free.Enqueue(src);
    }

    #endregion

    #region Music

    void PlayMusicInternal(string id, float crossfade)
    {
        if (!TryGetSound(id, out var s) || !s.isMusic) { Debug.LogWarning($"Music id '{id}' missing or not marked isMusic."); return; }
        var clip = s.GetRandomClip();
        if (!clip) return;
        if (crossfade < 0f) crossfade = defaultMusicCrossfade;

        var from = _usingA ? musicA : musicB;
        var to   = _usingA ? musicB : musicA;
        _usingA = !_usingA;

        to.clip = clip;
        to.outputAudioMixerGroup = s.mixerGroup;
        to.loop = true;
        to.volume = 0f;
        to.pitch = s.GetRandomPitch();
        to.Play();

        StartCoroutine(Crossfade(from, to, crossfade, s.volume));
    }

    IEnumerator Crossfade(AudioSource from, AudioSource to, float dur, float targetVol)
    {
        float t = 0f;
        float startFrom = from ? from.volume : 0f;
        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            float k = (dur <= 0f) ? 1f : Mathf.Clamp01(t / dur);
            if (from) from.volume = Mathf.Lerp(startFrom, 0f, k);
            if (to)   to.volume   = Mathf.Lerp(0f, targetVol, k);
            yield return null;
        }
        if (from) from.Stop();
        if (to)   to.volume = targetVol;
    }

    #endregion

    #region Helpers

    void PrewarmPool()
    {
        for (int i = 0; i < initialPoolSize; i++)
            _free.Enqueue(CreateSource());
    }

    AudioSource CreateSource()
    {
        var src = Instantiate(audioSourcePrefab, sfxPoolParent);
        src.playOnAwake = false;
        src.clip = null;
        src.loop = false;
        _pool.Add(src);
        return src;
    }

    AudioSource GetFreeSource()
    {
        if (_free.Count > 0) return _free.Dequeue();
        if (_pool.Count < maxPoolSize) return CreateSource();
        // Steal the least important source (lowest priority, quietest, or oldest) — here we just pick the first idle fallback:
        foreach (var s in _pool) if (s && !s.isPlaying) return s;
        // As a last resort, reuse any:
        return _pool[0];
    }

    void ReturnToPool(AudioSource src)
    {
        if (!src) return;
        src.clip = null;
        src.transform.localPosition = Vector3.zero;
        _free.Enqueue(src);
    }

    void ConfigureSource(AudioSource src, Sound s, bool is3D, Vector3 position)
    {
        src.outputAudioMixerGroup = s.mixerGroup;
        src.spatialBlend = is3D ? Mathf.Clamp01(s.spatialBlend) : 0f;
        src.transform.position = position;
        src.rolloffMode = s.rolloff;
        src.minDistance = s.minDistance;
        src.maxDistance = s.maxDistance;
        src.priority = s.isMusic ? 64 : 128; // music higher priority
        src.dopplerLevel = 0f; // mobile-friendly
    }

    bool TryGetSound(string id, out Sound s)
    {
        if (string.IsNullOrEmpty(id)) { s = null; return false; }
        if (_byId.TryGetValue(id, out s)) return true;
        Debug.LogWarning($"Sound id '{id}' not found.");
        return false;
    }

    bool CanPlay(Sound s)
    {
        // Cooldown
        if (_cooldowns.TryGetValue(s.id, out var nextTime) && Time.unscaledTime < nextTime)
            return false;

        // Max instances
        if (s.maxInstances > 0 && _liveCounts.TryGetValue(s.id, out var live) && live >= s.maxInstances)
            return false;

        return true;
    }

    void IncCount(string id)
    {
        _liveCounts.TryGetValue(id, out var v);
        _liveCounts[id] = v + 1;
        TrackActive(id, null, add:true); // keep the entry alive
    }

    void DecCount(string id)
    {
        if (_liveCounts.TryGetValue(id, out var v))
            _liveCounts[id] = Mathf.Max(0, v - 1);
    }

    void TrackActive(string id, AudioSource src, bool add)
    {
        if (!_activeById.TryGetValue(id, out var list))
            _activeById[id] = list = new List<AudioSource>(8);

        if (add)
        {
            if (src && !list.Contains(src)) list.Add(src);
        }
        else
        {
            if (src) list.Remove(src);
        }
    }

    // Map 0..1 linear to mixer dB (-80..0)
    void SetMixerLinear(string param, float linear)
    {
        if (!mixer || string.IsNullOrEmpty(param)) return;
        // avoid -inf: clamp to small positive
        float lin = Mathf.Clamp(linear, 0.0001f, 1f);
        float db = Mathf.Log10(lin) * 20f; // 1->0 dB, 0.5->-6 dB, etc.
        mixer.SetFloat(param, db);
    }

    #endregion
}