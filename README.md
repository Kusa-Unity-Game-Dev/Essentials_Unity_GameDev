# Essentials Unity Game Dev

Essential modules for Unity game development including UI management, event systems, notifications, and more.

## Features

### UI Manager System
A robust UI management system with **automatic layering and sorting** capabilities:

- **Automatic Layer Management**: No more manual canvas sort order management
- **6 Predefined Layers**: Background, Main, Popup, Overlay, System, Debug
- **Priority System**: 0-9 priority slots within each layer
- **Helper Methods**: BringToFront(), SendToBack(), layer-specific operations
- **UIGroup**: Manage multiple related UIs as a single unit
- **Custom Editors**: Visual controls in Unity Inspector
- **Backward Compatible**: Existing code works without changes

For detailed documentation, see [Runtime/UIManager/README.md](Runtime/UIManager/README.md)

### Other Modules
- Event System
- Notification System  
- Save System
- Sound Manager
- FSM (Finite State Machine)
- Developer Console
- Runtime Data Management

## Installation

Add this package to your Unity project via the Package Manager using the Git URL.

## Usage

### Quick Start - UI Layering

```csharp
public class MyUI : UIBase
{
    private void Awake()
    {
        // Set this UI to Popup layer with priority 5
        SetLayerAndPriority(UILayer.Popup, 5);
    }
    
    protected override void OnCanvasShowBegin() { }
    protected override void OnCanvasShowEnd() { }
    protected override void OnCanvasHideBegin() { }
    protected override void OnCanvasHideEnd() { }
}

// Show/hide the UI
UIManager.Instance.ShowUI(myUI);
UIManager.Instance.HideUI(myUI);
```

## Documentation

- [UI Manager System Documentation](Runtime/UIManager/README.md) - Complete guide to the UI system

## Requirements

- Unity 6000.0 or later

## License

See [LICENSE](LICENSE) for details.