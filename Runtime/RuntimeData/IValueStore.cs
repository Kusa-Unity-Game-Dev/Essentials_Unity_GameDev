using System;
using System.Threading.Tasks;

namespace BB.Framework
{
    public enum ValueScope 
    { 
        // Core persistence levels
        Persistent,     // Saved across game sessions (user preferences, achievements)
        Session,        // Current game session only
    
        // Game flow scopes
        Campaign,       // Entire campaign/story mode
        Chapter,        // Individual chapter/episode
        Level,          // Single level/stage
        Scene,          // Current scene
    
        // Gameplay scopes
        Match,          // Single match/round (multiplayer)
        Wave,           // Enemy wave or phase
        Turn,           // Turn-based gameplay
        Checkpoint,     // Between checkpoints
    
        // System scopes
        Menu,           // UI menu context
        Dialogue,       // During dialogue sequences
        Cutscene,       // During cutscenes
        Pause,          // Pause menu state
    
        // Temporary scopes
        Frame,          // Single frame (very temporary)
        Immediate,      // Immediate cleanup
    
        // Network scopes
        Server,         // Server-side data
        Client,         // Client-side only
        Lobby           // Multiplayer lobby
    }
    
    
    public interface IValueStore
    {
        void Set<T>(ValueKey<T> key, T value, ValueScope scope = ValueScope.Session, bool persist = false);
        bool TryGet<T>(ValueKey<T> key, out T value);
        T GetOrDefault<T>(ValueKey<T> key, T fallback = default);

        bool Remove(string keyPath);
        bool Remove<T>(ValueKey<T> key) => Remove(key.Path);

        IDisposable Subscribe<T>(ValueKey<T> key, Action<T> onChanged, bool invokeImmediately = false);

        Task<T> Await<T>(ValueKey<T> key, Func<T, bool> predicate = null, float timeoutSeconds = -1f);

        void ClearScope(ValueScope scope);
        void ClearAll();

        // Persistence (usually called by hub)
        void LoadPersistent();
        void SavePersistent();
    }
}