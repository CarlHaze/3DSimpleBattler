# Terrain Height Detection System Setup Guide

## Overview
This system makes units properly follow terrain height, climb walls, and move smoothly across varied terrain instead of passing through objects.

## What You Get
- **Accurate height detection** using raycast technology
- **Units climb onto walls and elevated surfaces** instead of passing through
- **Smooth movement animations** with climbing arcs and falling physics
- **Height-based movement validation** prevents impossible moves
- **Automatic terrain following** for all unit placement and movement

## Setup Instructions

### 1. Add TerrainHeightDetector to Scene

**Option A: Add to existing GameObject**
1. Select your main game manager GameObject
2. Add Component → Scripts → Terrain Height Detector

**Option B: Create new GameObject**
1. Create Empty GameObject → name it "HeightManager"
2. Add Component → Scripts → Terrain Height Detector

### 2. Configure Height Detection Settings

In the TerrainHeightDetector inspector:

**Height Detection Settings:**
- **Raycast Start Height**: 20f (how high above to start raycasting)
- **Max Raycast Distance**: 50f (maximum raycast distance)
- **Ground Layer Mask**: Set to your Ground layer
- **Height Tolerance**: 0.1f (precision for height comparison)
- **Debug Raycasts**: ✓ Check to see raycast lines in scene view

**Movement Validation:**
- **Max Step Height**: 1.5f (maximum height units can step up)
- **Allow Climbing**: ✓ Check to enable climbing onto surfaces
- **Climb Check Radius**: 0.3f (radius for checking climb surfaces)

### 3. Verify Your Ground Object Setup

**Important Requirements:**
1. **Your ground prefab MUST have a Collider component**
2. **The collider should match the visual mesh geometry**
3. **Set the ground object to the "Ground" layer**

**To check:**
1. Select your ground prefab in the scene
2. Verify it has a MeshCollider or other collider component
3. Verify the collider is set to match the mesh (not convex if using MeshCollider)
4. Verify the layer is set to "Ground"

### 4. Test the System

1. **Enter Play Mode**
2. **Press P** to enter placement mode
3. **Place units on different terrain heights** - they should snap to correct elevations
4. **Select a unit and try moving** - movement highlights should respect height limits
5. **Move units onto elevated surfaces** - they should smoothly climb up
6. **Move units off elevated surfaces** - they should fall down with physics

## How It Works

### Raycast Height Detection
- Shoots rays downward from above each grid position
- Finds the exact ground height at that location
- Caches results for performance
- Works with complex terrain meshes

### Movement Validation
- **Step Height Check**: Units can step up to 1.5 units high
- **Climbing System**: Units can climb onto surfaces within limits
- **Fall Detection**: Units can fall down (with limits)
- **Path Validation**: Ensures units can actually reach target positions

### Animation System
- **Climbing**: Arcs upward with extra height for realistic climbing motion
- **Falling**: Accelerated downward motion with physics
- **Normal Movement**: Smooth interpolation across flat surfaces
- **Speed Adjustment**: Slower when climbing, faster when falling

## Troubleshooting

### Units Still Pass Through Walls
1. **Check Ground Object Collider**: Ensure your ground prefab has a collider
2. **Verify Layer Setup**: Make sure ground is on "Ground" layer
3. **Check Layer Mask**: Verify TerrainHeightDetector is looking at correct layers
4. **Enable Debug Raycasts**: Turn on debug to see if raycasts are hitting

### Units Don't Follow Height
1. **Check Console Warnings**: Look for "TerrainHeightDetector not found" messages
2. **Verify Component**: Ensure TerrainHeightDetector is in scene and active
3. **Check Raycast Settings**: Increase raycast start height if terrain is very tall
4. **Test Height Cache**: Clear height cache if terrain has changed

### Movement Highlights Wrong
1. **Check Max Step Height**: Adjust if your terrain has higher/lower steps
2. **Verify Allow Climbing**: Enable if you want units to climb walls
3. **Test Grid Bounds**: Ensure grid positions are within ground object bounds

### Performance Issues
1. **Height caching** automatically improves performance
2. **Reduce raycast distance** if you don't need long-range detection
3. **Disable debug options** in final builds

## Advanced Configuration

### Custom Step Heights
```csharp
// Get reference to height detector
TerrainHeightDetector heightDetector = FindFirstObjectByType<TerrainHeightDetector>();

// Check max step height
float maxStep = heightDetector.MaxStepHeight;

// Check if climbing is enabled
bool canClimb = heightDetector.AllowClimbing;
```

### Manual Height Queries
```csharp
// Get height at specific world position
float height = heightDetector.GetGroundHeightAtWorldPosition(worldPos);

// Get complete ground position for grid coordinate
Vector3 groundPos = heightDetector.GetGroundPositionAtGridPosition(gridPos, groundObject);

// Check if position is walkable
bool walkable = heightDetector.IsPositionWalkable(worldPos);

// Check if movement is possible
bool canMove = heightDetector.CanMoveToPosition(fromPos, toPos);
```

### Clear Height Cache
```csharp
// Call this if your terrain changes at runtime
heightDetector.ClearHeightCache();
```

## Animation Features

### Climbing Animation
- **Arc Motion**: Units follow realistic climbing arcs
- **Speed Reduction**: 30% slower when climbing
- **Extra Height**: Adds visual arc for believable motion

### Falling Animation  
- **Acceleration**: Falls accelerate naturally
- **Speed Increase**: 20% faster when falling
- **Realistic Physics**: Follows gravity-like motion

### Normal Movement
- **Smooth Interpolation**: Maintains existing smooth movement
- **Height Following**: Automatically adjusts to terrain contours

## System Benefits

1. **Realistic Movement**: Units behave naturally with terrain
2. **Visual Polish**: Smooth animations for all height transitions  
3. **Automatic Integration**: Works with existing unit systems
4. **Performance Optimized**: Height caching reduces computation
5. **Flexible Configuration**: Easy to adjust for different game styles
6. **Debug Support**: Visual tools for troubleshooting

The height detection system is now ready! Your units will automatically follow terrain height and smoothly climb or fall as needed.