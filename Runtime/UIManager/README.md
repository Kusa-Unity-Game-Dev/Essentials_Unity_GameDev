# UI Manager System

## Overview
The UI Manager system provides a robust framework for managing UI elements in Unity with automatic layering and sorting capabilities.

## Features

### Automatic Layering System
The UI system now includes automatic layer management, eliminating the need to manually set canvas sort orders.

#### UI Layers
Six predefined layers are available:

- **Background (0)**: Main menu backgrounds, static backdrop elements
- **Main (100)**: Primary game HUD, main screens, core UI elements
- **Popup (200)**: Dialog boxes, popup windows, modal screens
- **Overlay (300)**: Tooltips, notifications, floating elements
- **System (400)**: Loading screens, transitions, system-level UI
- **Debug (500)**: Debug overlays, developer tools (always on top)

Each layer has 10 priority slots (0-9) for fine-grained control within the layer.

### Key Components

#### UIBase
Abstract base class for all UI elements with built-in layering support.

**Properties:**
- `Layer`: Gets/sets the UI layer (automatically updates sort order)
- `LayerPriority`: Gets/sets priority within the layer (0-9, higher = front)
- `SortOrder`: Gets the calculated canvas sort order (read-only)
- `IsVisible`: Indicates if the UI is currently visible

**Methods:**
- `BringToFront()`: Brings UI to the front of its current layer
- `SendToBack()`: Sends UI to the back of its current layer
- `SetLayerAndPriority(UILayer, int)`: Sets layer and priority in one call

#### UIManager
Singleton manager for controlling all UI elements.

**Methods:**
- `ShowUI(UIBase, float)`: Shows a UI element with optional delay
- `HideUI(UIBase)`: Hides a UI element
- `HideAllUI()`: Hides all active UI elements
- `HideAllUIInLayer(UILayer)`: Hides all UI in a specific layer
- `GetUIInLayer(UILayer)`: Returns all visible UI in a layer
- `GetTopUIInLayer(UILayer)`: Returns the topmost UI in a layer

## Usage Examples

### Basic Usage - Setting UI Layer in Inspector
```csharp
// Your custom UI class
public class MainMenuUI : UIBase
{
    // Set layer to Main and priority to 0 in the inspector
    // The canvas sort order will automatically be 100
    
    protected override void OnCanvasShowBegin() { }
    protected override void OnCanvasShowEnd() { }
    protected override void OnCanvasHideBegin() { }
    protected override void OnCanvasHideEnd() { }
}
```

### Changing Layer at Runtime
```csharp
// Move UI to popup layer
myUI.Layer = UILayer.Popup;

// Set both layer and priority
myUI.SetLayerAndPriority(UILayer.Overlay, 5);
```

### Managing UI Priority
```csharp
// Bring dialog to front of its layer
dialogUI.BringToFront();

// Send notification to back of its layer
notificationUI.SendToBack();

// Set specific priority (0-9)
tooltipUI.LayerPriority = 8;
```

### Layer-Specific Operations
```csharp
// Hide all popups
UIManager.Instance.HideAllUIInLayer(UILayer.Popup);

// Get all UI elements in main layer
List<UIBase> mainScreens = UIManager.Instance.GetUIInLayer(UILayer.Main);

// Get the topmost overlay
UIBase topOverlay = UIManager.Instance.GetTopUIInLayer(UILayer.Overlay);
```

### Complete Example
```csharp
public class PauseMenuUI : UIBase
{
    private void Awake()
    {
        // Configure this as a popup with high priority
        SetLayerAndPriority(UILayer.Popup, 5);
    }
    
    protected override void OnCanvasShowBegin()
    {
        // Bring to front when showing
        BringToFront();
    }
    
    protected override void OnCanvasShowEnd()
    {
        // UI is fully visible and interactive
        Debug.Log($"Pause menu shown at sort order: {SortOrder}");
    }
    
    protected override void OnCanvasHideBegin()
    {
        // Starting to hide
    }
    
    protected override void OnCanvasHideEnd()
    {
        // UI is now hidden
    }
}

// Using the pause menu
UIManager.Instance.ShowUI(pauseMenuUI);
```

## Migration Guide

### For Existing Projects
The layering system is **backward compatible**. Existing UI elements will:
- Default to `UILayer.Main` with priority 0 (sort order 100)
- Continue to work without modifications
- Can adopt the new system incrementally

### Upgrading Existing UI
1. Open your UI prefab/scene
2. Select the GameObject with your UIBase-derived component
3. Set the desired **UI Layer** in the inspector
4. Optionally set **Layer Priority** (0-9)
5. The canvas sort order is now managed automatically

## Best Practices

1. **Choose Appropriate Layers**: Use the layer that best describes the UI's purpose
2. **Reserve Priority Slots**: Leave room for dynamic UI by not filling all 10 priority slots
3. **Use BringToFront Sparingly**: Overuse can lead to unpredictable ordering
4. **System Layer**: Reserve for critical system UI like loading screens
5. **Debug Layer**: Keep for development tools that should always be visible

## Sort Order Calculation

The final canvas sort order is calculated as:
```
sortOrder = layerBaseValue + priority
```

Examples:
- Main layer (100) + priority 0 = sort order 100
- Main layer (100) + priority 5 = sort order 105
- Popup layer (200) + priority 3 = sort order 203
- Overlay layer (300) + priority 9 = sort order 309

This ensures UI elements are correctly layered while maintaining flexibility within each layer.

## Troubleshooting

### UI Not Appearing in Correct Order
- Check the Layer setting in inspector
- Verify LayerPriority is set correctly (0-9)
- Ensure Canvas component is present on the GameObject

### Multiple UIs Fighting for Top Position
- Assign different priorities within the same layer
- Consider if UIs belong in different layers
- Use BringToFront() only when needed

### Sort Order Not Updating
- Ensure changes are made at runtime after Start()
- Verify the Canvas component reference is set
- Check if multiple Canvas components exist in hierarchy
