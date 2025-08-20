# Pathfinding Performance Fixes

## Issues Fixed

### 1. Performance Problem - Excessive Pathfinding Calls
**Problem**: Selecting a unit caused hundreds of pathfinding calculations, causing lag and console spam

**Solution**: 
- Optimized movement range calculation to use `GetReachablePositions()` once instead of individual pathfinding calls
- Disabled debug logging by default (can be re-enabled in HeightAwarePathfinder inspector)
- Removed redundant pathfinding validation in `IsBasicValidMovePosition`

### 2. Unit Placement Issue - Units Going to Center
**Problem**: Units were being placed incorrectly, possibly at world origin or center of map

**Solution**:
- Fixed height detection to use grid manager primarily, then adjust height
- Added debug logging to identify placement coordinates
- Ensured proper grid-to-world conversion

## Quick Fixes to Apply

### Step 1: Disable Debug Logging
1. Select the GameObject with **HeightAwarePathfinder** component
2. In the inspector, **uncheck "Debug Pathfinding"**
3. This will stop the console spam

### Step 2: Test Unit Placement
1. Press **P** to enter placement mode
2. Click on a grid square to place a unit
3. Check the console for debug message: `"Placing unit at grid (x,y) -> world (x,y,z) on GroundName"`
4. Verify the world position looks reasonable (not 0,0,0 or center of map)

### Step 3: Test Unit Selection
1. Click on a placed unit
2. Should see movement highlights appear quickly without console spam
3. Movement range should only show truly reachable positions

## If Issues Persist

### Unit Placement Still Wrong
If units are still going to wrong positions, check:
1. **GridOverlayManager** is working correctly
2. **Ground object bounds** are set properly
3. **Grid size** matches your terrain scale

Add this debug code to GridOverlayManager.cs in `GridToWorldPosition()`:
```csharp
Vector3 result = GetActualGroundPosition(gridPos, groundObject);
Debug.Log($"Grid {gridPos} -> World {result} on {groundObject.name}");
return result;
```

### Performance Still Slow
If selection is still slow:
1. **Reduce movement range** on units (try 2 instead of 3)
2. **Disable diagonal movement** in HeightAwarePathfinder
3. **Reduce max search nodes** to 500 in HeightAwarePathfinder

### Pathfinding Not Working
If units still clip through terrain:
1. **Check TerrainHeightDetector** is in scene and configured
2. **Verify ground object has colliders** that match the mesh
3. **Check layer setup** - ground objects should be on "Ground" layer

## Settings to Verify

### HeightAwarePathfinder Settings
- **Debug Pathfinding**: ❌ Unchecked (to stop console spam)
- **Enable Diagonal Movement**: ✅ Checked
- **Max Search Nodes**: 500-1000 (lower = faster)
- **Max Climb Height**: 1.5f (adjust for your terrain)

### TerrainHeightDetector Settings  
- **Debug Raycasts**: ❌ Unchecked
- **Show Debug Spheres**: ❌ Unchecked
- **Ground Layer Mask**: Set to "Ground" layer

### UnitMovementManager Settings
- **Movement Range**: 3 (or less for better performance)
- **Move Speed**: 2f (adjust to taste)

The system should now work smoothly without performance issues or incorrect unit placement!