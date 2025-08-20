# SimpleBattler - Codebase Summary

## Overview
SimpleBattler is a Unity-based tactical grid combat system featuring unit placement, movement, enemy spawning, and an isometric camera system. The project implements a complete tactical game framework with physics-aware unit interactions.

## Core Systems

### 1. Camera System
**File**: `SimpleTacticsCameraController.cs`

**Features**:
- **WASD/Arrow Key Panning**: Move camera around the battlefield
- **Mouse Drag Panning**: Middle mouse button drag for intuitive camera control
- **Edge Panning**: Camera moves when mouse reaches screen edges
- **Mouse Wheel Zoom**: Adjusts camera height (5-30 units)
- **Q/E Rotation**: Rotate camera around Y-axis with 45° snap increments
- **R/F Tilt**: Adjust camera angle (30-90° range, 90° = straight down)
- **T Reset**: Return camera to starting position and rotation
- **Space/Ctrl Vertical Pan**: Additional height control independent of zoom
- **Boundary System**: Optional map bounds to constrain camera movement
- **Smooth Rotation**: Interpolated rotation transitions for polished feel

**Key Technical Details**:
- Starting position: X=90° (looking straight down), customizable initial state
- Gimbal lock prevention through angle normalization
- Transform-relative movement based on current camera rotation
- Height-sensitive mouse drag sensitivity

### 2. Unit Placement System
**File**: `UnitPlacementManager.cs`

**Features**:
- **P Key Toggle**: Enter/exit placement mode
- **Grid-Based Placement**: Snap units to grid positions on ground objects
- **Visual Feedback**: Blue highlights for valid positions, red for invalid
- **Collision Detection**: Prevents placement on occupied tiles or obstructed areas
- **Height Validation**: Integration with height checking system
- **Unit Limit Enforcement**: Prevents exceeding maximum unit count
- **Multi-Surface Support**: Works across multiple ground objects

**Technical Implementation**:
- Dual layer support: Ground layer (3) and Grid layer (7)
- Tile occupation tracking via dictionary system
- Capsule cast obstruction detection
- Real-time preview highlighting during placement mode

### 3. Unit Movement System
**File**: `UnitMovementManager.cs` (referenced in GridMovementHighlighter.cs)

**Features**:
- **Click-to-Select**: Choose units for movement
- **Range-Based Movement**: Configurable movement range per unit
- **Enemy Collision**: Red highlights where enemies block movement
- **Line-of-Sight Pathfinding**: Uses Bresenham's algorithm for clear paths
- **Diagonal Movement**: Full 8-directional movement support
- **Height Awareness**: Respects elevation differences

**Movement Validation**:
- Manhattan distance calculation for movement cost
- Enemy detection via sphere overlap and tag checking
- Ground walkability validation
- Height difference limitations (1.5 units max)

### 4. Enemy Spawning System
**File**: `EnemySpawner.cs`

**Features**:
- **E Key Spawning**: Manual enemy spawn trigger
- **Automatic Spawning**: Optional spawn on scene start
- **Distance-Based Placement**: Minimum 2-grid spacing from units/other enemies
- **Smart Positioning**: Finds valid positions across all ground surfaces
- **Collision Avoidance**: Prevents spawning in obstructed areas

**Spawn Algorithm**:
1. Collect all valid positions across ground objects
2. Shuffle positions for randomization
3. Apply distance constraints from existing units
4. Spawn enemies with proper grid registration

### 5. Height Validation System
**File**: `SimpleHeightCheck.cs`

**Features**:
- **Maximum Height Limits**: Configurable absolute height restrictions
- **Surface Detection**: Raycast-based ground validation
- **Multi-Layer Support**: Works with both Ground and Grid layers

### 6. Grid Overlay System
**File**: `GridOverlayManager.cs`

**Features**:
- **Visual Grid Lines**: Shows tactical grid on ground surfaces
- **Multi-Surface Support**: Adapts to different ground object sizes
- **Layer Integration**: Detects Ground layer objects for grid display

## Layer System

### Physics Layers
- **Layer 3 (Ground)**: Primary walking surfaces and collision detection
- **Layer 6 (Units)**: Player units with physics bodies
- **Layer 7 (Grid)**: Grid objects separated from unit physics to prevent bouncing
- **Layer 8 (groundLayer)**: Additional ground layer support

### Collision Matrix
- Units layer does NOT interact with Grid layer (prevents physics bouncing)
- Ground layer maintains collision with units for walking surface detection
- Grid layer used purely for visual overlay and object detection

## Tags System
- **Player**: Player-controlled units
- **Enemy**: AI-controlled enemy units
- **Obstacle**: Impassable terrain features
- **Wall**: Vertical blocking elements
- **Unwalkable**: Areas that cannot be traversed

## Key Technical Patterns

### 1. Component Communication
- Cross-component references via `FindFirstObjectByType<>()`
- Grid info components (UnitGridInfo, EnemyGridInfo) for position tracking
- Centralized managers for system coordination

### 2. Grid Mathematics
- **World ↔ Grid Conversion**: `GridToWorldPosition()` and `WorldToGridPosition()`
- **Grid Size**: Configurable unit size for tactical positioning
- **Multi-Surface Grids**: Each ground object maintains its own grid coordinate system

### 3. Physics Integration
- **Raycast Ground Detection**: Validates walkable surfaces
- **Sphere Overlap**: Enemy and obstruction detection
- **Capsule Cast**: Vertical space validation for unit placement
- **Layer Masking**: Selective physics interactions

### 4. State Management
- **Placement Mode**: Boolean state with visual feedback
- **Unit Selection**: Single-unit selection with movement highlighting
- **Camera State**: Position and rotation memory for reset functionality

## File Structure
```
Assets/Scripts/
├── SimpleTacticsCameraController.cs     # Complete camera control system
├── UnitPlacementManager.cs              # Unit placement and grid management
├── GridMovementHighlighter.cs           # Movement visualization (mostly disabled)
├── EnemySpawner.cs                      # Enemy spawn management
├── SimpleHeightCheck.cs                # Height validation system
└── GridOverlayManager.cs               # Visual grid display

ProjectSettings/
└── TagManager.asset                     # Layer and tag definitions
```

## Recent Improvements

### Camera System Enhancements
- Fixed tilt direction issues (F key now properly clamps at 90° straight-down view)
- Added starting position memory for accurate reset functionality
- Improved rotation clamping to prevent camera flipping
- Enhanced gimbal lock prevention

### Physics System Fixes
- Separated Grid and Units layers to prevent unit bouncing
- Removed colliders from movement highlights to eliminate physics interference
- Maintained ground detection while preventing unwanted physics interactions

### Movement System Updates
- Added comprehensive enemy collision detection
- Implemented line-of-sight pathfinding with diagonal support
- Enhanced movement validation with height and walkability checks

### Placement System Improvements
- Added unit limit enforcement with user feedback
- Integrated height checking for elevation-aware placement
- Enhanced visual feedback with color-coded highlighting

## Usage Instructions

### Basic Controls
- **P**: Toggle unit placement mode
- **E**: Spawn enemies
- **WASD/Arrows**: Pan camera
- **Q/E**: Rotate camera
- **R/F**: Tilt camera up/down
- **T**: Reset camera to starting position
- **Mouse Wheel**: Zoom in/out
- **Middle Mouse Drag**: Pan camera
- **Escape**: Cancel current mode

### Tactical Gameplay
1. Press **P** to enter placement mode
2. Click on blue highlighted areas to place units
3. Click units to select and see movement range
4. Blue areas show valid moves, red areas show blocked/invalid moves
5. Press **E** to spawn enemies for tactical encounters

This codebase provides a solid foundation for tactical grid-based combat games with room for expansion into combat mechanics, AI behaviors, and advanced tactical features.