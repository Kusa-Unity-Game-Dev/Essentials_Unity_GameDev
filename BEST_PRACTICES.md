# Best Practices Guide

This document outlines industry-standard best practices for using the Game Essentials package effectively in your Unity projects.

## Table of Contents

1. [Event System Best Practices](#event-system-best-practices)
2. [UI Management Best Practices](#ui-management-best-practices)
3. [Sound Management Best Practices](#sound-management-best-practices)
4. [Save System Best Practices](#save-system-best-practices)
5. [Memory Management](#memory-management)
6. [Performance Optimization](#performance-optimization)
7. [Code Organization](#code-organization)

## Event System Best Practices

### Always Unsubscribe from Events

**Problem:** Memory leaks occur when objects are destroyed but event listeners remain registered.

**Solution:** Always unsubscribe in `OnDestroy()`:

```csharp
private EventManager<PlayerData> eventManager;

private void OnEnable()
{
    eventManager.AddListener("PlayerDamaged", OnPlayerDamaged);
}

private void OnDestroy()
{
    // Critical: Always unsubscribe to prevent memory leaks
    eventManager?.RemoveListener("PlayerDamaged", OnPlayerDamaged);
}
```

### Use Descriptive Event Names

**Bad:**
```csharp
eventManager.DispatchEvent("e1", data);
eventManager.DispatchEvent("update", data);
```

**Good:**
```csharp
eventManager.DispatchEvent("PlayerHealthChanged", data);
eventManager.DispatchEvent("EnemyDefeated", data);
```

### Consider Using Constants for Event Names

```csharp
public static class GameEvents
{
    public const string PLAYER_DAMAGED = "PlayerDamaged";
    public const string LEVEL_COMPLETE = "LevelComplete";
    public const string GAME_PAUSED = "GamePaused";
}

// Usage
eventManager.DispatchEvent(GameEvents.PLAYER_DAMAGED, data);
```

### Handle Exceptions in Event Listeners

The EventManager catches exceptions internally, but you should still handle errors gracefully:

```csharp
private void OnPlayerDamaged(PlayerData data)
{
    try
    {
        if (data == null)
        {
            Debug.LogError("Received null player data");
            return;
        }
        
        // Process event...
    }
    catch (Exception ex)
    {
        Debug.LogError($"Error handling PlayerDamaged event: {ex.Message}");
    }
}
```

## UI Management Best Practices

### Initialize UI References Early

```csharp
public class MainMenuUI : UIBase
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    
    private void Awake()
    {
        // Initialize button listeners early
        playButton?.onClick.AddListener(OnPlayClicked);
        settingsButton?.onClick.AddListener(OnSettingsClicked);
    }
    
    protected override void OnCanvasShowBegin()
    {
        // Trigger show animations
    }
}
```

### Use UI Stacking Wisely

```csharp
// Good: Allows returning to previous UI
public void ShowSettings()
{
    // This will stack the main menu
    mainMenuUI.isAllowedToStack = true;
    UIManager.Instance.ShowUI(settingsUI);
}

// Good: Prevents stacking for full-screen UIs
public void ShowGameOver()
{
    gameOverUI.isAllowedToStack = false;
    UIManager.Instance.HideAllUI();
    UIManager.Instance.ShowUI(gameOverUI);
}
```

### Implement Smooth Transitions

```csharp
public class FadeUI : UIBase
{
    [SerializeField] private CanvasGroup canvasGroup;
    
    protected override void OnCanvasShowBegin()
    {
        // Fade in
        StartCoroutine(FadeIn());
    }
    
    protected override void OnCanvasHideBegin()
    {
        // Fade out
        StartCoroutine(FadeOut());
    }
    
    private IEnumerator FadeIn()
    {
        float time = 0;
        while (time < m_initAnimationTime)
        {
            time += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, time / m_initAnimationTime);
            yield return null;
        }
        canvasGroup.alpha = 1;
    }
    
    private IEnumerator FadeOut()
    {
        float time = 0;
        while (time < m_outroAnimationTime)
        {
            time += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, time / m_outroAnimationTime);
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}
```

## Sound Management Best Practices

### Organize Sounds in ScriptableObjects

```csharp
// Create a SoundData asset: Assets/Create/kusa/GameAudio/SoundData
// Configure all your sounds in one place with proper IDs
```

### Use Appropriate Audio Types

```csharp
// 2D Sounds (UI, Music)
SoundManager.PlaySound("ButtonClick");
SoundManager.PlayMusic("MenuTheme");

// 3D Sounds (World Audio)
SoundManager.PlayAt("Footstep", transform.position);
SoundManager.PlayAt("Explosion", explosionPosition);
```

### Implement Volume Settings

```csharp
public class AudioSettings : MonoBehaviour
{
    public void OnMasterVolumeChanged(float value)
    {
        // value should be 0-1 from UI slider
        SoundManager.SetMasterVolume(value);
        PlayerPrefs.SetFloat("MasterVolume", value);
    }
    
    public void OnMusicVolumeChanged(float value)
    {
        SoundManager.SetMusicVolume(value);
        PlayerPrefs.SetFloat("MusicVolume", value);
    }
    
    public void OnSfxVolumeChanged(float value)
    {
        SoundManager.SetSfxVolume(value);
        PlayerPrefs.SetFloat("SfxVolume", value);
    }
    
    private void Start()
    {
        // Load saved settings
        SoundManager.SetMasterVolume(PlayerPrefs.GetFloat("MasterVolume", 0.8f));
        SoundManager.SetMusicVolume(PlayerPrefs.GetFloat("MusicVolume", 0.7f));
        SoundManager.SetSfxVolume(PlayerPrefs.GetFloat("SfxVolume", 0.8f));
    }
}
```

### Configure Cooldowns for Frequent Sounds

In your SoundData ScriptableObject, set cooldown values to prevent audio spam:
- Footsteps: 0.1-0.2 seconds
- Weapon fire: 0.05-0.1 seconds
- UI clicks: 0.05 seconds

## Save System Best Practices

### Create Modular Save Data

```csharp
public class PlayerProgressModule : SaveDataModule
{
    public int level = 1;
    public int experience = 0;
    public List<string> unlockedAbilities = new List<string>();
    
    public override void SaveOnDemand(string slotPath)
    {
        // Validate data before saving
        if (level < 1) level = 1;
        
        string json = JsonUtility.ToJson(this);
        string filePath = Path.Combine(slotPath, "player_progress.json");
        File.WriteAllText(filePath, json);
    }
    
    public override void LoadOnDemand(string slotPath)
    {
        string filePath = Path.Combine(slotPath, "player_progress.json");
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            JsonUtility.FromJsonOverwrite(json, this);
        }
        else
        {
            Debug.LogWarning("No save file found, using defaults");
        }
    }
}
```

### Implement Auto-Save

```csharp
public class AutoSaveController : MonoBehaviour
{
    [SerializeField] private float autoSaveInterval = 300f; // 5 minutes
    private float timeSinceLastSave = 0;
    
    private void Update()
    {
        timeSinceLastSave += Time.deltaTime;
        
        if (timeSinceLastSave >= autoSaveInterval)
        {
            SaveGame();
            timeSinceLastSave = 0;
        }
    }
    
    private void SaveGame()
    {
        try
        {
            SaveManager.Instance.SaveAllModule("AutoSave");
            Debug.Log("Auto-save completed");
        }
        catch (Exception ex)
        {
            Debug.LogError($"Auto-save failed: {ex.Message}");
        }
    }
    
    private void OnApplicationQuit()
    {
        // Save on quit
        SaveGame();
    }
}
```

### Validate Save Data

```csharp
public void LoadGame(string slotName)
{
    try
    {
        SaveManager.Instance.LoadAllModule(slotName);
        
        // Validate loaded data
        var playerData = SaveManager.Instance.GetModule<PlayerProgressModule>(ESaveModule.EPlayerProgress);
        if (playerData != null)
        {
            // Ensure data integrity
            if (playerData.level < 1 || playerData.level > 100)
            {
                Debug.LogError("Invalid level data, resetting to 1");
                playerData.level = 1;
            }
        }
    }
    catch (Exception ex)
    {
        Debug.LogError($"Failed to load game: {ex.Message}");
        // Load default/new game
    }
}
```

## Memory Management

### Clean Up Resources

```csharp
public class ResourceManager : MonoBehaviour
{
    private void OnDestroy()
    {
        // Unsubscribe from events
        EventManager?.RemoveAllEvents();
        
        // Clear UI references
        UIManager.ClearAllList_s();
        
        // Stop all sounds if needed
        SoundManager.StopAllSfx();
        
        // Clear caches
        Resources.UnloadUnusedAssets();
    }
}
```

### Use Object Pooling

The SoundManager already implements pooling. Apply the same pattern to frequently instantiated objects:

```csharp
public class BulletPool : MonoBehaviour
{
    [SerializeField] private GameObject bulletPrefab;
    [SerializeField] private int poolSize = 50;
    
    private Queue<GameObject> pool = new Queue<GameObject>();
    
    private void Start()
    {
        for (int i = 0; i < poolSize; i++)
        {
            GameObject bullet = Instantiate(bulletPrefab);
            bullet.SetActive(false);
            pool.Enqueue(bullet);
        }
    }
    
    public GameObject GetBullet()
    {
        if (pool.Count > 0)
        {
            GameObject bullet = pool.Dequeue();
            bullet.SetActive(true);
            return bullet;
        }
        
        // Pool exhausted, create new
        return Instantiate(bulletPrefab);
    }
    
    public void ReturnBullet(GameObject bullet)
    {
        bullet.SetActive(false);
        pool.Enqueue(bullet);
    }
}
```

## Performance Optimization

### Minimize FindObjectOfType Usage

**Bad:**
```csharp
void Update()
{
    SoundManager manager = FindObjectOfType<SoundManager>();
    manager.PlaySound("Update");
}
```

**Good:**
```csharp
// Cache manager references
private SoundManager soundManager;

void Start()
{
    soundManager = SoundManager.I;
}

void SomeMethod()
{
    soundManager.PlaySound("Action");
}
```

### Use Static Methods Appropriately

```csharp
// Good: Direct static access
SoundManager.PlaySound("Click");
UIManager.Instance.ShowUI(menuUI);
SaveManager.Instance.SaveModule("Slot1", ESaveModule.ECurrency);
```

### Batch Operations

```csharp
// Bad: Multiple individual saves
SaveManager.Instance.SaveModule("Slot1", ESaveModule.ECurrency);
SaveManager.Instance.SaveModule("Slot1", ESaveModule.EInventory);
SaveManager.Instance.SaveModule("Slot1", ESaveModule.EProgress);

// Good: Single batch save
SaveManager.Instance.SaveAllModule("Slot1");
```

## Code Organization

### Namespace Usage

Always use the BB.Framework namespace:

```csharp
using BB.Framework;

namespace YourGame.Systems
{
    public class GameController : MonoBehaviour
    {
        // Your code
    }
}
```

### Separate Concerns

```csharp
// Good: Separate controllers for different concerns
public class UIController : MonoBehaviour { }
public class AudioController : MonoBehaviour { }
public class GameStateController : MonoBehaviour { }

// Bad: One giant controller doing everything
public class GameManager : MonoBehaviour 
{
    // 1000+ lines of mixed responsibilities
}
```

### Use Interfaces for Abstraction

```csharp
public interface IDamageable
{
    void TakeDamage(float amount);
    float GetHealth();
}

public class Player : MonoBehaviour, IDamageable
{
    private float health = 100f;
    
    public void TakeDamage(float amount)
    {
        health -= amount;
        FSM.DispatchEvent_s("PlayerDamaged", health.ToString());
    }
    
    public float GetHealth() => health;
}
```

## Testing

### Test Singleton Initialization

```csharp
[Test]
public void UIManager_Singleton_IsNotNull()
{
    Assert.IsNotNull(UIManager.Instance);
}

[Test]
public void SoundManager_Singleton_IsNotNull()
{
    Assert.IsNotNull(SoundManager.I);
}
```

### Test Event System

```csharp
[Test]
public void EventManager_DispatchesEvent()
{
    var eventManager = new EventManager<string>();
    bool eventReceived = false;
    
    eventManager.AddListener("TestEvent", (data) => {
        eventReceived = true;
    });
    
    eventManager.DispatchEvent("TestEvent", "test");
    
    Assert.IsTrue(eventReceived);
}
```

## Common Pitfalls to Avoid

1. **Not unsubscribing from events** → Memory leaks
2. **Using FindObjectOfType in Update()** → Performance issues
3. **Not validating save data** → Corrupt saves
4. **Playing too many sounds simultaneously** → Audio chaos
5. **Not handling null references** → Crashes
6. **Forgetting DontDestroyOnLoad for managers** → Lost references
7. **Not using UI stacking** → Poor user experience
8. **Hardcoding values instead of using constants** → Maintenance nightmare

## Conclusion

Following these best practices will help you build robust, maintainable, and performant Unity games using the Game Essentials package. Always prioritize code clarity, proper error handling, and memory management.

For more information, see the [README.md](README.md) and [CHANGELOG.md](CHANGELOG.md).
