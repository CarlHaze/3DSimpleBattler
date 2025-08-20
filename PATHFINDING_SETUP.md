# Height-Aware Pathfinding System Setup Guide

## Problem Solved
This system prevents units from clipping through elevated terrain when moving to distant positions. Instead of taking direct routes through walls and elevated surfaces, units will now pathfind around obstacles using the shortest valid route.

## What You Get
- **A* pathfinding algorithm** that respects height differences and obstacles
- **Smart obstacle avoidance** around elevated terrain and walls
- **Diagonal movement support** with corner-cutting prevention
- **Smooth multi-segment animations** following the calculated path
- **Visual feedback** showing truly reachable vs blocked positions

## Setup Instructions

### 1. Add HeightAwarePathfinder to Scene

**Option A: Add to existing GameObject**
1. Select your main game manager GameObject
2. Add Component → Scripts → Height Aware Pathfinder

**Option B: Create new GameObject**
1. Create Empty GameObject → name it "PathfindingManager"
2. Add Component → Scripts → Height Aware Pathfinder

### 2. Configure Pathfinding Settings

In the HeightAwarePathfinder inspector:

**Pathfinding Settings:**
- **Enable Diagonal Movement**: ✓ Checked (allows 8-directional movement)
- **Diagonal Cost**: 1.414 (cost of diagonal moves - √2)
- **Straight Cost**: 1.0 (cost of cardinal moves)
- **Max Search Nodes**: 1000 (prevents infinite loops)
- **Debug Pathfinding**: ✓ Check to see pathfinding logs

**Height Constraints:**
- **Max Climb Height**: 1.5f (maximum height units can climb)
- **Max Fall Height**: 3.0f (maximum height units can fall)
- **Height Penalty**: 2.0f (cost multiplier for height changes)

### 3. Required Dependencies

**Ensure these components are in your scene:**
- ✅ **TerrainHeightDetector** (for height detection)
- ✅ **GridOverlayManager** (for grid calculations)
- ✅ **UnitPlacementManager** (for obstacle detection)

### 4. Test the System

1. **Create a test scenario** with elevated terrain and walls
2. **Place a unit** on lower ground
3. **Select the unit** and observe movement highlights
4. **Try to move 2-3 spaces** to a position behind elevated terrain
5. **The unit should path around** obstacles instead of clipping through

## How It Works

### A* Pathfinding Algorithm
- **Open/Closed Sets**: Tracks explored and unexplored nodes
- **G Cost**: Distance from start position
- **H Cost**: Heuristic distance to goal (Manhattan or diagonal distance)
- **F Cost**: Total cost (G + H) used for node selection
- **Path Reconstruction**: Traces back from goal to start

### Height-Aware Movement Validation
- **Height Constraints**: Respects max climb/fall limits
- **Corner Cutting Prevention**: Blocks diagonal movement through walls
- **Obstacle Detection**: Checks for units, enemies, and terrain blockers
- **Movement Range Limits**: Ensures paths don't exceed unit movement range

### Visual Feedback System
- **Blue Highlights**: Positions reachable via pathfinding
- **Red Highlights**: Positions blocked by obstacles or height
- **Smart Range Display**: Only shows truly accessible positions

### Animation System
- **Multi-Segment Movement**: Follows calculated path step by step
- **Height Transitions**: Climbing arcs and falling physics per segment
- **Speed Adjustments**: Slower climbing, faster falling per path segment

## Example Scenarios

### Scenario 1: Wall Blocking
```
[U] = Unit, [W] = Wall, [T] = Target

Before: Unit tries direct path U → T, clips through wall
U . W . T

After: Unit paths around wall
U → → ↑ → T
    . W . ↑
```

### Scenario 2: Elevated Terrain
```
[U] = Unit (low), [H] = High ground, [T] = Target (low)

Before: Unit clips through high ground
U . H . T

After: Unit paths around elevated area
U → → → ↓ T
    . H H .
```

### Scenario 3: Diagonal Obstacles
```
[U] = Unit, [O] = Obstacle, [T] = Target

Before: Unit cuts through corner
U . O
. O T

After: Unit avoids corner cutting
U → . O
↓ . O
T ← ← .
```

## Performance Optimization

### Built-in Optimizations
- **Node limit**: Prevents infinite search loops
- **Early termination**: Stops when goal is reached
- **Efficient data structures**: Uses dictionaries and lists for fast lookup
- **Reachable position caching**: Pre-calculates valid movement range

### Performance Settings
- **Max Search Nodes**: Reduce for faster pathfinding on large maps
- **Movement Range**: Smaller ranges = faster calculations
- **Diagonal Movement**: Disable for simpler 4-directional pathfinding

## Troubleshooting

### Units Still Clip Through Terrain
1. **Check component setup**: Ensure HeightAwarePathfinder is in scene
2. **Verify dependencies**: Confirm TerrainHeightDetector is working
3. **Check console warnings**: Look for "pathfinder not found" messages
4. **Test height detection**: Ensure terrain has proper colliders

### Movement Highlights Wrong
1. **Verify grid bounds**: Check GridOverlayManager is detecting terrain correctly
2. **Check height limits**: Adjust max climb/fall heights for your terrain
3. **Enable debug logs**: Turn on pathfinding debug to see path calculations

### Performance Issues
1. **Reduce max search nodes** (try 500 instead of 1000)
2. **Disable diagonal movement** for simpler pathfinding
3. **Reduce movement range** on units
4. **Optimize terrain colliders** (use simpler collision meshes)

### No Path Found
1. **Check movement range**: Target might be too far away
2. **Verify terrain accessibility**: Ensure path exists within height limits
3. **Check for obstacles**: Remove temporary blockers between start and goal
4. **Enable debug pathfinding**: See detailed path search information

## Advanced Configuration

### Custom Movement Costs
```csharp
// Access pathfinder reference
HeightAwarePathfinder pathfinder = FindFirstObjectByType<HeightAwarePathfinder>();

// Adjust costs for different terrain types
// Higher values = more expensive to traverse
```

### Manual Pathfinding
```csharp
// Find specific path
List<Vector2Int> path = pathfinder.FindPath(startPos, endPos, groundObject, maxRange);

// Check if position is reachable
bool canReach = pathfinder.IsPositionReachable(startPos, targetPos, groundObject, maxRange);

// Get all reachable positions
List<Vector2Int> reachable = pathfinder.GetReachablePositions(startPos, groundObject, maxRange);
```

## System Benefits

1. **Realistic Movement**: Units navigate terrain naturally
2. **Visual Clarity**: Players see exactly where units can move
3. **Strategic Depth**: Terrain becomes tactically important
4. **Performance Optimized**: Efficient pathfinding algorithms
5. **Flexible Configuration**: Easy to adjust for different game styles
6. **Automatic Integration**: Works with existing movement systems

The pathfinding system is now ready! Units will intelligently navigate around obstacles and elevated terrain instead of clipping through them.