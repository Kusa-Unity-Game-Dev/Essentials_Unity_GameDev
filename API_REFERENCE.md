# API Reference

Complete API reference for the Game Essentials package.

## Table of Contents

1. [EventManager](#eventmanager)
2. [UIManager](#uimanager)
3. [SoundManager](#soundmanager)
4. [SaveManager](#savemanager)
5. [NotificationManager](#notificationmanager)
6. [FSM (Finite State Machine)](#fsm-finite-state-machine)
7. [RuntimeDataHub](#runtimedatahub)
8. [UIBase](#uibase)

---

## EventManager

Generic event management system for type-safe event dispatching.

### Constructor

```csharp
EventManager<T>()
```

Creates a new EventManager instance for events with data type `T`.

### Methods

#### AddListener

```csharp
void AddListener(string eventName, Action<T> listener)
```

Registers a listener callback for a specific event.

**Parameters:**
- `eventName` - The unique identifier for the event
- `listener` - The callback to invoke when the event is dispatched

**Exceptions:**
- `ArgumentException` - Thrown when eventName is null or empty
- `ArgumentNullException` - Thrown when listener is null

**Example:**
```csharp
eventManager.AddListener("PlayerDamaged", OnPlayerDamaged);
```

#### RemoveListener

```csharp
void RemoveListener(string eventName, Action<T> listener)
```

Removes a previously registered listener for a specific event.

**Parameters:**
- `eventName` - The event identifier
- `listener` - The callback to remove

#### DispatchEvent

```csharp
void DispatchEvent(string eventName, T eventData)
```

Dispatches an event to all registered listeners.

**Parameters:**
- `eventName` - The event identifier
- `eventData` - The data to pass to all listeners

#### RemoveAllEvents

```csharp
void RemoveAllEvents()
```

Removes all registered event listeners. Use with caution.

#### GetListenerCount

```csharp
int GetListenerCount(string eventName)
```

Gets the number of listeners registered for a specific event.

**Returns:** The number of listeners, or 0 if none registered

#### HasListeners

```csharp
bool HasListeners(string eventName)
```

Checks if any listeners are registered for a specific event.

**Returns:** True if at least one listener is registered

---

## UIManager

Centralized UI management system with lifecycle and stacking support.

### Properties

#### Instance

```csharp
static UIManager Instance { get; }
```

Gets the singleton instance of the UIManager.

#### uiScreens

```csharp
List<UIBase> uiScreens
```

List of currently visible UI screens.

#### uiLastShownScreens

```csharp
List<UIBase> uiLastShownScreens
```

History of recently shown UI screens (limited to 10).

#### ui_stackedScreens

```csharp
List<UIBase> ui_stackedScreens
```

Stack of UI screens that were hidden but can be restored.

### Methods

#### ShowUI

```csharp
void ShowUI(UIBase uiScreen, float delay = 0)
```

Shows a UI screen with optional delay.

**Parameters:**
- `uiScreen` - The UI screen to show
- `delay` - Optional delay before showing the UI

#### HideUI

```csharp
void HideUI(UIBase uiScreen)
```

Hides a UI screen.

**Parameters:**
- `uiScreen` - The UI screen to hide

#### HideAllUI

```csharp
void HideAllUI()
```

Hides all currently visible UI screens.

#### RegisterUI

```csharp
void RegisterUI(UIBase uiScreen)
```

Registers a UI screen as currently active/visible.

#### UnregisterUI

```csharp
void UnregisterUI(UIBase uiScreen, bool addToStack = false)
```

Unregisters a UI screen and optionally manages UI stacking.

**Parameters:**
- `uiScreen` - The UI screen to unregister
- `addToStack` - If true, adds this UI to the stack for later restoration

#### ClearAllList_s

```csharp
static void ClearAllList_s()
```

Clears all UI tracking lists. Use with caution.

---

## SoundManager

Advanced audio management system with pooling and spatial audio support.

### Properties

#### I

```csharp
static SoundManager I { get; }
```

Gets the singleton instance of the SoundManager.

### Static Methods

#### PlaySound

```csharp
static void PlaySound(string id)
```

Plays a 2D sound effect.

**Parameters:**
- `id` - The sound identifier from SoundData

**Example:**
```csharp
SoundManager.PlaySound("ButtonClick");
```

#### PlayAt

```csharp
static void PlayAt(string id, Vector3 position)
```

Plays a 3D positional sound.

**Parameters:**
- `id` - The sound identifier
- `position` - World position for the sound

**Example:**
```csharp
SoundManager.PlayAt("Explosion", transform.position);
```

#### PlayMusic

```csharp
static void PlayMusic(string id, float crossfade = -1f)
```

Plays background music with crossfade.

**Parameters:**
- `id` - The music identifier (must be marked as `isMusic` in SoundData)
- `crossfade` - Crossfade duration in seconds (-1 uses default)

#### StopAllSfx

```csharp
static void StopAllSfx()
```

Stops all currently playing sound effects (does not affect music).

#### SetMasterVolume

```csharp
static void SetMasterVolume(float linear01)
```

Sets the master volume level.

**Parameters:**
- `linear01` - Volume from 0.0 (silent) to 1.0 (full)

#### SetMusicVolume

```csharp
static void SetMusicVolume(float linear01)
```

Sets the music volume level.

#### SetSfxVolume

```csharp
static void SetSfxVolume(float linear01)
```

Sets the sound effects volume level.

---

## SaveManager

Modular save/load system with slot management.

### Properties

#### Instance

```csharp
static SaveManager Instance { get; }
```

Gets the singleton instance of the SaveManager.

### Methods

#### RegisterModule

```csharp
void RegisterModule(SaveDataModule module)
```

Registers a save data module with the system.

**Parameters:**
- `module` - The module to register

#### UnregisterModule

```csharp
void UnregisterModule(ESaveModule moduleType)
```

Unregisters a save data module from the system.

#### SaveModule

```csharp
void SaveModule(string slotName, ESaveModule moduleType)
```

Saves a specific module to the specified save slot.

**Parameters:**
- `slotName` - The name of the save slot
- `moduleType` - The type of module to save

#### SaveAllModule

```csharp
void SaveAllModule(string slotName)
```

Saves all registered modules to the specified save slot.

#### LoadModule

```csharp
void LoadModule(string slotName, ESaveModule moduleType)
```

Loads a specific module from the specified save slot.

#### LoadAllModule

```csharp
void LoadAllModule(string slotName)
```

Loads all registered modules from the specified save slot.

#### GetModule<T>

```csharp
T GetModule<T>(ESaveModule moduleType) where T : SaveDataModule
```

Retrieves a registered save module for direct access.

**Type Parameters:**
- `T` - The specific SaveDataModule type

**Returns:** The module instance, or null if not found

#### IsModuleRegistered

```csharp
bool IsModuleRegistered(ESaveModule moduleType)
```

Checks if a module is registered with the system.

#### GetRegisteredModuleCount

```csharp
int GetRegisteredModuleCount()
```

Gets the number of registered modules.

---

## NotificationManager

Queue-based notification system for sequential display.

### Properties

#### s_Instance

```csharp
static NotificationManager s_Instance { get; }
```

Gets the singleton instance of the NotificationManager.

### Methods

#### ShowNotification

```csharp
void ShowNotification(NotificationData data)
```

Queues a notification for display.

**Parameters:**
- `data` - The notification data to display

**Example:**
```csharp
NotificationData notification = new NotificationData
{
    message = "Achievement Unlocked!",
    type = NotificationType.Success,
    duration = 3.0f
};
NotificationManager.s_Instance.ShowNotification(notification);
```

#### GetQueuedNotificationCount

```csharp
int GetQueuedNotificationCount()
```

Gets the number of notifications currently in the queue.

#### ClearQueue

```csharp
void ClearQueue()
```

Clears all pending notifications from the queue.

#### IsDisplayingNotification

```csharp
bool IsDisplayingNotification()
```

Checks if a notification is currently being displayed.

---

## FSM (Finite State Machine)

Global event management for game state changes.

### Static Methods

#### AddListener_s

```csharp
static void AddListener_s(string eventName, Action<string> listener)
```

Adds a listener for a specific event (static method).

**Parameters:**
- `eventName` - The event identifier
- `listener` - The callback to invoke

#### RemoveListener_s

```csharp
static void RemoveListener_s(string eventName, Action<string> listener)
```

Removes a listener for a specific event (static method).

#### DispatchEvent_s

```csharp
static void DispatchEvent_s(string eventName, string eventData)
```

Dispatches an event to all registered listeners (static method).

**Parameters:**
- `eventName` - The event identifier
- `eventData` - The event data to pass to listeners

#### GetEventManager

```csharp
static EventManager<string> GetEventManager()
```

Gets the underlying EventManager for advanced usage.

### Instance Methods

#### AddEventListener

```csharp
void AddEventListener(string eventName, Action<string> listener)
```

Adds a listener for a specific event (instance method).

#### RemoveEventListener

```csharp
void RemoveEventListener(string eventName, Action<string> listener)
```

Removes a listener for a specific event (instance method).

#### DispatchEvent

```csharp
void DispatchEvent(string eventName, string eventData)
```

Dispatches an event to all registered listeners (instance method).

---

## RuntimeDataHub

Persistent data storage with flexible providers.

### Properties

#### Service

```csharp
static IValueStore Service { get; }
```

Gets the global IValueStore service instance.

### Configuration

Configure in Inspector:
- `dontDestroyOnLoad` - Persist across scenes
- `autoLoadOnAwake` - Load saved data on startup
- `persistenceConfig` - Custom persistence provider configuration

### IValueStore Methods

The `Service` property provides access to IValueStore methods for storing and retrieving values.

---

## UIBase

Abstract base class for all UI screens.

### Properties

#### IsVisible

```csharp
bool IsVisible { get; }
```

Gets whether this UI is currently visible.

#### isAllowedToStack

```csharp
bool isAllowedToStack
```

Determines if this UI can be added to the stack when hidden.

### Protected Fields

#### m_canvas

```csharp
Canvas m_canvas
```

Reference to the Canvas component.

#### m_graphicRaycaster

```csharp
GraphicRaycaster m_graphicRaycaster
```

Reference to the GraphicRaycaster component.

#### m_initAnimationTime

```csharp
float m_initAnimationTime
```

Duration of show animation in seconds (default: 0.4).

#### m_outroAnimationTime

```csharp
float m_outroAnimationTime
```

Duration of hide animation in seconds (default: 0.4).

### Abstract Methods

Must be implemented in derived classes:

#### OnCanvasShowBegin

```csharp
protected abstract void OnCanvasShowBegin()
```

Called when the UI starts to show. Implement show animations here.

#### OnCanvasShowEnd

```csharp
protected abstract void OnCanvasShowEnd()
```

Called when the UI is fully shown and ready for interaction.

#### OnCanvasHideBegin

```csharp
protected abstract void OnCanvasHideBegin()
```

Called when the UI starts to hide. Implement hide animations here.

#### OnCanvasHideEnd

```csharp
protected abstract void OnCanvasHideEnd()
```

Called when the UI is fully hidden and disabled.

### Public Methods

#### _UIBaseShowUI

```csharp
void _UIBaseShowUI(float delay = 0.0f)
```

Internal method to show the UI. Called by UIManager.

**Note:** Do not call directly - use `UIManager.ShowUI()` instead.

#### _UIBaseHideUI

```csharp
void _UIBaseHideUI(float delay = 0.0f)
```

Internal method to hide the UI. Called by UIManager.

**Note:** Do not call directly - use `UIManager.HideUI()` instead.

#### _UIBaseHideUI_AfterStacking

```csharp
void _UIBaseHideUI_AfterStacking(float delay = 0.0f)
```

Internal method to hide the UI and add it to the stack.

---

## Example Implementation

### Complete UI Screen Example

```csharp
using BB.Framework;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuUI : UIBase
{
    [SerializeField] private Button playButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private CanvasGroup canvasGroup;
    
    private void Awake()
    {
        playButton.onClick.AddListener(OnPlayClicked);
        settingsButton.onClick.AddListener(OnSettingsClicked);
    }
    
    protected override void OnCanvasShowBegin()
    {
        // Start fade in
        StartCoroutine(FadeIn());
        SoundManager.PlaySound("MenuAppear");
    }
    
    protected override void OnCanvasShowEnd()
    {
        // Enable interactions
        Debug.Log("Main menu ready");
    }
    
    protected override void OnCanvasHideBegin()
    {
        // Start fade out
        StartCoroutine(FadeOut());
    }
    
    protected override void OnCanvasHideEnd()
    {
        // Cleanup
        Debug.Log("Main menu hidden");
    }
    
    private void OnPlayClicked()
    {
        SoundManager.PlaySound("ButtonClick");
        UIManager.Instance.HideUI(this);
        // Load game scene
    }
    
    private void OnSettingsClicked()
    {
        SoundManager.PlaySound("ButtonClick");
        // Show settings UI
    }
    
    private IEnumerator FadeIn()
    {
        float time = 0;
        while (time < m_initAnimationTime)
        {
            time += Time.unscaledDeltaTime;
            canvasGroup.alpha = time / m_initAnimationTime;
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
            canvasGroup.alpha = 1 - (time / m_outroAnimationTime);
            yield return null;
        }
        canvasGroup.alpha = 0;
    }
}
```

---

For more examples and best practices, see [BEST_PRACTICES.md](BEST_PRACTICES.md).
