# Game Essentials for Unity

A comprehensive, production-ready Unity package providing essential game development systems following industry best practices. This package includes Event Systems, UI Management, Sound Management, Save Systems, Notifications, and more.

## Features

- **Event System**: Type-safe, generic event management system for decoupled communication
- **UI Manager**: Robust UI lifecycle management with stacking and transition support
- **Sound Manager**: Advanced audio system with pooling, mixing, and 3D spatial audio
- **Save System**: Modular save/load system with slot management
- **Notification System**: Queue-based notification display system
- **Runtime Data Hub**: Persistent data storage with flexible providers
- **FSM (Finite State Machine)**: Event-driven state management
- **Developer Console**: In-game debugging console

## Installation

### Via Unity Package Manager (Recommended)

1. Open Unity Package Manager (`Window > Package Manager`)
2. Click the `+` button and select `Add package from git URL`
3. Enter: `https://github.com/Kusa-Unity-Game-Dev/Essentials_Unity_GameDev.git`

### Via manifest.json

Add this to your `Packages/manifest.json`:

```json
{
  "dependencies": {
    "com.kusabb.essentials": "https://github.com/Kusa-Unity-Game-Dev/Essentials_Unity_GameDev.git#1.3.9"
  }
}
```

## Requirements

- Unity 6000.0 or higher
- .NET Standard 2.1

## Quick Start

### Event System

```csharp
using BB.Framework;

// Define your event data type
public class PlayerData
{
    public int health;
    public int score;
}

// Create event manager instance
private EventManager<PlayerData> eventManager = new EventManager<PlayerData>();

// Subscribe to events
eventManager.AddListener("PlayerDamaged", OnPlayerDamaged);

// Dispatch events
PlayerData data = new PlayerData { health = 80, score = 100 };
eventManager.DispatchEvent("PlayerDamaged", data);

// Unsubscribe when done
eventManager.RemoveListener("PlayerDamaged", OnPlayerDamaged);

private void OnPlayerDamaged(PlayerData data)
{
    Debug.Log($"Player health: {data.health}");
}
```

### UI Manager

```csharp
using BB.Framework;

// Your UI class should inherit from UIBase
public class MainMenuUI : UIBase
{
    protected override void OnCanvasShowBegin() 
    { 
        // Animation starts here
    }
    
    protected override void OnCanvasShowEnd() 
    { 
        // UI fully visible, enable interactions
    }
    
    protected override void OnCanvasHideBegin() 
    { 
        // Start hide animation
    }
    
    protected override void OnCanvasHideEnd() 
    { 
        // UI fully hidden
    }
}

// Show/Hide UI
UIManager.Instance.ShowUI(mainMenuUI);
UIManager.Instance.HideUI(mainMenuUI);
UIManager.Instance.HideAllUI();
```

### Sound Manager

```csharp
using BB.Framework;

// Play 2D sound
SoundManager.PlaySound("ButtonClick");

// Play 3D positional sound
SoundManager.PlayAt("Explosion", transform.position);

// Play music with crossfade
SoundManager.PlayMusic("BackgroundMusic", crossfade: 1.0f);

// Volume control (0.0 to 1.0)
SoundManager.SetMasterVolume(0.8f);
SoundManager.SetMusicVolume(0.7f);
SoundManager.SetSfxVolume(0.9f);

// Stop all sound effects
SoundManager.StopAllSfx();
```

### Save System

```csharp
using BB.Framework;

// Create a save slot
SaveSlotManager.Instance.CreateGameSlot("SaveSlot1");

// Register a save module
CurrencySaveModule currencyModule = new CurrencySaveModule();
SaveManager.Instance.RegisterModule(currencyModule);

// Save/Load specific module
SaveManager.Instance.SaveModule("SaveSlot1", ESaveModule.ECurrency);
SaveManager.Instance.LoadModule("SaveSlot1", ESaveModule.ECurrency);

// Save/Load all modules
SaveManager.Instance.SaveAllModule("SaveSlot1");
SaveManager.Instance.LoadAllModule("SaveSlot1");

// Retrieve module data
var currency = SaveManager.Instance.GetModule<CurrencySaveModule>(ESaveModule.ECurrency);
if (currency != null)
{
    currency.AddCoins(50);
}
```

### Notification System

```csharp
using BB.Framework;

// Create notification data
NotificationData notification = new NotificationData
{
    message = "Achievement Unlocked!",
    type = NotificationType.Success,
    duration = 3.0f
};

// Display notification
NotificationManager.s_Instance.ShowNotification(notification);
```

### Runtime Data Hub

```csharp
using BB.Framework;

// Access the service
var store = RuntimeDataHub.Service;

// Store and retrieve values
store.SetInt("PlayerLevel", 5);
int level = store.GetInt("PlayerLevel", defaultValue: 1);

// Persist data
store.SavePersistent();
store.LoadPersistent();
```

## Architecture Patterns

This package follows several industry-standard patterns:

- **Singleton Pattern**: Used for manager classes to ensure single instance
- **Observer Pattern**: Implemented via the EventManager for loose coupling
- **Object Pooling**: Used in SoundManager for efficient audio source management
- **Factory Pattern**: Used in save system for module creation
- **State Pattern**: FSM implementation for game state management

## Best Practices

1. **Always unsubscribe from events** when objects are destroyed to prevent memory leaks
2. **Use object pooling** for frequently instantiated objects
3. **Implement proper cleanup** in OnDestroy methods
4. **Use DontDestroyOnLoad** carefully to avoid duplicates
5. **Validate data** before saving to prevent corruption
6. **Handle null references** defensively throughout your code

## Performance Considerations

- Sound pooling reduces GC allocations
- Event system uses dictionaries for O(1) lookup
- UI stacking minimizes instantiation overhead
- Save system supports modular loading for faster startup

## Contributing

Contributions are welcome! Please feel free to submit pull requests or open issues for bugs and feature requests.

## License

MIT License - see [LICENSE](LICENSE) file for details

## Author

**Kunal Sagar**
- GitHub: [@kusaflow](https://github.com/kusaflow)

## Support

For questions and support, please open an issue on the [GitHub repository](https://github.com/Kusa-Unity-Game-Dev/Essentials_Unity_GameDev).

## Documentation

- **[API Reference](API_REFERENCE.md)** - Complete API documentation for all classes and methods
- **[Best Practices Guide](BEST_PRACTICES.md)** - Industry-standard best practices and common patterns
- **[CHANGELOG](CHANGELOG.md)** - Version history and release notes

## Version History

See [CHANGELOG.md](CHANGELOG.md) for version history and release notes.