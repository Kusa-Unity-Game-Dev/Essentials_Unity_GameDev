namespace BB.Framework
{

/// <summary>
/// Manages DOTween Pro DOTweenAnimation clips on this GameObject (optionally children),
/// provides play/stop by ID/group, exclusive playback, pooling-safe cleanup, and runtime controls.
/// </summary>
public class DOTweenProAnimatorManager : MonoBehaviour
{
    [Serializable]
    public class AnimGroup
    {
        [Tooltip("Human-friendly name you’ll use in code, e.g. \"In\", \"Out\", \"Punch\", \"Win\".")]
        public string id = "In";
        
        [Tooltip("Assign specific DOTweenAnimation clips for this group (optional). " +
                 "If empty, manager will auto-collect any DOTweenAnimation on this object whose id matches the group id.")]
        public DOTweenAnimation[] clips;

        [Tooltip("Play this group on Start.")]
        public bool playOnStart = false;

        [Tooltip("If true, when playing this group, other groups will be stopped first.")]
        public bool exclusive = false;

        [Tooltip("After play completes, rewind to start (without playing).")]
        public bool rewindOnComplete = false;

        [Tooltip("UnityEvent fired when ALL clips in this group complete (after last completes).")]
        public UnityEvent onGroupComplete;
    }

    [Header("Scan Settings")]
    [Tooltip("Also search child objects for DOTweenAnimation components.")]
    public bool includeChildren = false;

    [Tooltip("If true, set tweens to use independent update (ignore Time.timeScale).")]
    public bool independentUpdate = true;

    [Header("Lifecycle")]
    [Tooltip("Kill all tweens on Disable. (Pooling-friendly)")]
    public bool killOnDisable = true;

    [Tooltip("On Disable, if not killing, pause instead.")]
    public bool pauseOnDisable = false;

    [Tooltip("Rewind all tweens when disabled (after Pause/Kill).")]
    public bool rewindOnDisable = true;

    [Tooltip("Kill all tweens on Destroy (safety).")]
    public bool killOnDestroy = true;

    [Header("Groups")]
    public AnimGroup[] groups;
    public DOTweenAnimation[] m_clips;

    // cache: id -> list of clips
    private readonly Dictionary<string, List<DOTweenAnimation>> _map = new();

    private readonly Dictionary<string, int> _pendingCompletions = new();

    
    
#if UNITY_EDITOR

    [Button("Gather All DOTweenAnimation Clips")]
    private void GatherAllClips()
    {
        // Gather all DOTweenAnimation components
        m_clips = includeChildren
            ? GetComponentsInChildren<DOTweenAnimation>(true)
            : GetComponents<DOTweenAnimation>();
    }

#endif
    
    
    void Awake()
    {
        BuildCache();
        ApplyIndependentUpdate(independentUpdate);

    }

    void Start()
    {
        // auto-play groups
        foreach (AnimGroup g in groups)
        {
            if (g.playOnStart)
            {
                if (g.exclusive) PlayExclusive(g.id);
                else Play(g.id);
            }
        }
    }

    void OnDisable()
    {
        if (killOnDisable) KillAll();
        else if (pauseOnDisable) PauseAll();

        if (rewindOnDisable) RewindAll();
    }

    void OnDestroy()
    {
        if (killOnDestroy) KillAll();
    }

    private void BuildCache()
    {
        _map.Clear();

        // Build initial empty lists for declared groups
        foreach (AnimGroup g in groups)
        {
            if (!_map.ContainsKey(g.id))
                _map[g.id] = new List<DOTweenAnimation>();
        }

        // Assign clips to groups based on either explicit assignment or DOTweenAnimation.id
        foreach (AnimGroup g in groups)
        {
            List<DOTweenAnimation> list = _map[g.id];

            // If group has explicit clip array, add them
            if (g.clips != null && g.clips.Length > 0)
            {
                foreach (DOTweenAnimation c in g.clips)
                    TryAddClip(list, c);
            }
        }

        // For remaining clips not explicitly assigned, slot by matching id
        foreach (DOTweenAnimation clip in m_clips)
        {
            if (clip == null) continue;
            // If this clip was already added explicitly, skip
            if (IsAlreadyMapped(clip)) continue;

            string clipId = clip.id ?? string.Empty;
            if (_map.TryGetValue(clipId, out var list))
            {
                TryAddClip(list, clip);
            }
        }

        // Hook group completion counters
        foreach (AnimGroup g in groups)
        {
            HookGroupCompletion(g);
        }
    }

    private bool IsAlreadyMapped(DOTweenAnimation clip)
    {
        foreach (var kv in _map)
            if (kv.Value.Contains(clip))
                return true;
        return false;
    }

    private static void TryAddClip(List<DOTweenAnimation> list, DOTweenAnimation c)
    {
        if (c != null && !list.Contains(c))
            list.Add(c);
    }

    private void HookGroupCompletion(AnimGroup g)
    {
        if (!_map.TryGetValue(g.id, out var list)) return;
        // remove previously attached callbacks
        foreach (var c in list)
        {
            // Ensure tween exists
            if (c.tween != null)
                c.tween.onComplete = null;
        }

        if (list.Count == 0) return;

        // Each time we Play the group we’ll set the counter to the number of active tweens.
        _pendingCompletions[g.id] = 0;
    }

    public void Play(string id)
    {
        if (!_map.TryGetValue(id, out var list) || list.Count == 0) return;

        // If group is marked exclusive, stop others
        var g = GetGroup(id);
        if (g != null && g.exclusive)
            StopAll(exceptId: id);

        int active = 0;
        foreach (var clip in list)
        {
            if (clip == null) continue;

            EnsureTweenExists(clip);

            // state reset
            clip.tween.SetUpdate(independentUpdate);
            clip.tween.timeScale = 1f; // reset any previous override
            clip.DORestart(); // safe restart from beginning
            active++;
        }

        // set pending completion count
        if (g != null)
        {
            _pendingCompletions[id] = active;
            // hook onComplete callbacks for this run
            foreach (var clip in list)
            {
                if (clip == null || clip.tween == null) continue;

                // Remove any previous handler for safety then add new
                clip.tween.onComplete -= () => HandleClipComplete(id, g);
                clip.tween.onComplete += () => HandleClipComplete(id, g);
            }
        }
    }

    public void PlayExclusive(string id)
    {
        StopAll(exceptId: id);
        Play(id);
    }

    public void Pause(string id)
    {
        if (!_map.TryGetValue(id, out var list)) return;
        foreach (var clip in list)
            if (clip?.tween != null) clip.tween.Pause();
    }

    public void Stop(string id, bool rewind = false)
    {
        if (!_map.TryGetValue(id, out var list)) return;
        foreach (var clip in list)
        {
            if (clip?.tween == null) continue;
            if (rewind) clip.tween.Rewind();
            clip.tween.Kill(false);
        }
    }

    public void PauseAll()
    {
        foreach (var list in _map.Values)
            foreach (var clip in list)
                if (clip?.tween != null) clip.tween.Pause();
    }

    public void RewindAll()
    {
        foreach (var list in _map.Values)
            foreach (var clip in list)
                if (clip?.tween != null) clip.tween.Rewind();
    }

    public void KillAll()
    {
        foreach (var list in _map.Values)
            foreach (var clip in list)
                if (clip?.tween != null) clip.tween.Kill(false);
    }

    public void StopAll(string exceptId = null, bool rewindOthers = false)
    {
        foreach (var kv in _map)
        {
            if (!string.IsNullOrEmpty(exceptId) && kv.Key == exceptId) continue;

            foreach (var clip in kv.Value)
            {
                if (clip?.tween == null) continue;
                if (rewindOthers) clip.tween.Rewind();
                clip.tween.Kill(false);
            }
        }
    }

    public void SetIndependentUpdate(bool value)
    {
        independentUpdate = value;
        ApplyIndependentUpdate(value);
    }

    public void SetGroupTimeScale(string id, float timeScale = 1f)
    {
        if (!_map.TryGetValue(id, out var list)) return;
        foreach (var clip in list)
            if (clip?.tween != null) clip.tween.timeScale = Mathf.Max(0f, timeScale);
    }

    public void SetAllTimeScale(float timeScale = 1f)
    {
        foreach (var list in _map.Values)
            foreach (var clip in list)
                if (clip?.tween != null) clip.tween.timeScale = Mathf.Max(0f, timeScale);
    }

    private void ApplyIndependentUpdate(bool value)
    {
        foreach (var list in _map.Values)
            foreach (var clip in list)
                if (clip?.tween != null) clip.tween.SetUpdate(value);
    }

    private AnimGroup GetGroup(string id)
    {
        foreach (var g in groups)
            if (g.id == id) return g;
        return null;
    }

    private static void EnsureTweenExists(DOTweenAnimation anim)
    {
        if (anim.tween == null || !anim.tween.active)
        {
            // Recreate with current inspector values
            anim.CreateTween();
        }
    }

    private void HandleClipComplete(string id, AnimGroup g)
    {
        if (!_pendingCompletions.ContainsKey(id)) return;

        _pendingCompletions[id] = Mathf.Max(0, _pendingCompletions[id] - 1);
        if (_pendingCompletions[id] == 0)
        {
            if (g.rewindOnComplete)
            {
                // Rewind all in this group
                if (_map.TryGetValue(id, out var list))
                    foreach (var clip in list)
                        if (clip?.tween != null) clip.tween.Rewind();
            }

            g.onGroupComplete?.Invoke();
        }
    }
}

/**usage example:

    // Play “In”
    animManager.Play("In");

    // Play “Out” and stop everything else first
    animManager.PlayExclusive("Out");

    // Pause or kill everything (e.g., when hiding or pooling)
    animManager.PauseAll();
    animManager.KillAll();

    // Respect UI should animate during Time.timeScale = 0 (pause screens)
    animManager.SetIndependentUpdate(true);

*/
    
    
}