# Unit Outline System Setup Guide

## What You Get
- **Green pulsing outline** for selected units  
- **No custom shaders required** - works with any Unity version
- **Customizable colors** and animation settings
- **Simple and reliable** mesh duplication approach

## Setup Instructions

### 1. Add SimpleUnitOutline to Scene

**Option A: Add to existing GameObject**
1. Select your main game manager GameObject
2. Add Component → Scripts → Simple Unit Outline

**Option B: Create new GameObject**
1. Create Empty GameObject → name it "OutlineManager"
2. Add Component → Scripts → Simple Unit Outline

### 2. Configure Settings

In the SimpleUnitOutline inspector:

**Outline Settings:**
- **Outline Color**: Green (0, 1, 0, 1) - color of the outline
- **Outline Width**: 0.02 (how much bigger the outline is)
- **Animate Outline**: ✓ Checked for pulsing effect
- **Pulse Speed**: 2.0 (how fast the pulse animation)
- **Min/Max Scale**: 0.98 to 1.05 (pulse scale range)

### 3. Test the System

1. **Enter Play Mode**
2. **Press P** to enter placement mode
3. **Place some units** on valid grid positions
4. **Click on a unit** - should see green pulsing outline
5. **Click another unit** - outline moves to new unit
6. **Right-click or Escape** - outline disappears

## How It Works

### Selection Integration
- When you click a unit in `UnitMovementManager`, it automatically adds an outline
- Deselecting removes the outline
- Only one unit can have a selection outline at a time

### Mesh Duplication Technology
- Creates a slightly larger copy of the unit's mesh
- Colors it with a solid color material
- Parents it to the original unit and scales it for pulsing
- No shaders required - works with any Unity version

### Simple and Reliable
- Uses standard Unity materials (Unlit/Color)
- No render pipeline dependencies
- Easy to customize and debug

## Customization Options

### Change Outline Colors
```csharp
// In code
outlineController.SetOutlineColor(unit, Color.red);

// Or in inspector
selectedOutlineColor = Color.blue;
```

### Adjust Outline Width
```csharp
// In code
outlineController.SetOutlineWidth(unit, 0.015f);

// Or in inspector
outlineWidth = 0.012f;
```

### Disable Animation
```csharp
// In inspector
animateOutline = false;
```

## Troubleshooting

### Outline Not Visible
1. Check that UnitOutlineController is in the scene
2. Verify the Custom/UnitOutline shader compiled correctly
3. Check outline width isn't too small (try 0.015)
4. Ensure unit has Renderer components

### Performance Issues
1. Reduce outline width
2. Disable animation on many units
3. Check unit polygon count (outlines add geometry)

### Shader Errors
1. Check Console for compilation errors
2. Ensure shader is in Assets/Shaders/ folder
3. Try reimporting the shader file

## Advanced Usage

### Manual Outline Control
```csharp
// Get reference to outline controller
UnitOutlineController outline = FindFirstObjectByType<UnitOutlineController>();

// Add custom outline
outline.SetSelectedUnit(myUnit);

// Remove outline
outline.ClearSelectedUnit();

// Check if unit has outline
bool hasOutline = outline.IsUnitOutlined(myUnit);
```

### Future Turn System Integration
```csharp
// Set active unit for turn system
outline.SetActiveUnit(currentTurnUnit);

// Clear when turn ends
outline.ClearActiveUnit();
```

## System Benefits

1. **Visual Clarity**: Clear indication of selected units
2. **Performance**: Efficient shader-based rendering
3. **Flexibility**: Easy to customize colors and animation
4. **Integration**: Works seamlessly with existing selection system
5. **Extensible**: Ready for turn systems and team colors
6. **Clean**: Automatically manages material creation/cleanup

The outline system is now ready to use! Selected units will automatically get a green pulsing outline when clicked.