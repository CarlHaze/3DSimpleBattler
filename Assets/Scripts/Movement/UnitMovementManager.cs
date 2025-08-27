using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class UnitMovementManager : MonoBehaviour
{
    [Header("Movement Settings")]
    public int movementRange = 3;
    public float moveSpeed = 2f;
    public LayerMask groundLayerMask = 1;
    
    [Header("Visual Feedback")]
    public Color validMoveColor = Color.blue;
    public Color blockedMoveColor = Color.red;
    public Material movementHighlightMaterial;
    
    private Camera gameCamera;
    private GridOverlayManager gridManager;
    private UnitPlacementManager placementManager;
    private SimpleHeightCheck heightChecker;
    private SimpleUnitSelector unitSelector;
    private SimpleUnitOutline outlineController;
    private TerrainHeightDetector terrainHeightDetector;
    private HeightAwarePathfinder pathfinder;
    
    // Current selection state
    private GameObject selectedUnit;
    private List<GameObject> movementHighlights = new List<GameObject>();
    private List<Vector2Int> validMovePositions = new List<Vector2Int>();
    private GameObject currentGroundObject;
    
    // Movement animation
    private bool isMoving = false;
    
    // Movement mode state
    private bool inMovementMode = false;

    void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();

        gridManager = FindFirstObjectByType<GridOverlayManager>();
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        heightChecker = FindFirstObjectByType<SimpleHeightCheck>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        outlineController = FindFirstObjectByType<SimpleUnitOutline>();
        terrainHeightDetector = FindFirstObjectByType<TerrainHeightDetector>();
        pathfinder = FindFirstObjectByType<HeightAwarePathfinder>();

        if (gridManager == null)
            Debug.LogError("GridOverlayManager not found!");
        if (placementManager == null)
            Debug.LogError("UnitPlacementManager not found!");
        if (heightChecker == null)
            Debug.LogError("SimpleHeightCheck not found!");
        if (outlineController == null)
            Debug.LogWarning("SimpleUnitOutline not found - unit outlines will not work!");
        if (terrainHeightDetector == null)
            Debug.LogWarning("TerrainHeightDetector not found - units may not follow terrain height properly!");
        if (pathfinder == null)
            Debug.LogWarning("HeightAwarePathfinder not found - units may clip through elevated terrain!");
        
    }
    
    void Update()
    {
        // Only handle movement input when in movement mode
        if (!isMoving && inMovementMode)
        {
            HandleMovementInput();
        }
    }
    
    bool AllUnitsPlaced()
    {
        if (unitSelector == null) return true;
        return unitSelector.IsInitialPlacementComplete();
    }
    
    void HandleMovementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                // Try to move to the clicked position
                if (selectedUnit != null && TryMoveToPosition(hit.point, clickedObject))
                {
                    return; // Movement handled
                }
            }
        }
        
        // Exit movement mode with right click or escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            ExitMovementMode();
        }
    }
    
    // New methods for ActionMenu integration
    public void SetSelectedUnit(GameObject unit, bool enterMovementMode = false)
    {
        // Clear previous selection
        ClearMovementHighlights();
        
        selectedUnit = unit;
        inMovementMode = enterMovementMode;
        
        if (unit != null)
        {
            // Add outline to selected unit
            if (outlineController != null)
            {
                outlineController.SetSelectedUnit(selectedUnit);
            }
            
            // Only show movement range if entering movement mode
            if (enterMovementMode)
            {
                UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
                if (unitInfo != null)
                {
                    currentGroundObject = unitInfo.groundObject;
                    ShowMovementRange(unitInfo.gridPosition, unitInfo.groundObject);
                    SimpleMessageLog.Log($"Movement mode activated for {unit.name}");
                }
            }
            else
            {
                SimpleMessageLog.Log($"Selected {unit.name}");
            }
        }
    }
    
    public void ClearSelection()
    {
        DeselectUnit();
    }
    
    public void ExitMovementMode()
    {
        // Store selected unit before clearing for potential re-selection
        GameObject unitToReselect = selectedUnit;
        
        inMovementMode = false;
        ClearMovementHighlights();
        validMovePositions.Clear();
        SimpleMessageLog.Log("Exited movement mode");
        
        // Re-show action menu if we exited during combat for a player unit
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        
        if (turnManager != null && actionMenu != null && unitToReselect != null &&
            turnManager.GetCurrentPhase() == BattlePhase.Combat && 
            turnManager.IsPlayerTurn() && unitToReselect == turnManager.GetCurrentUnit())
        {
            Debug.Log($"Re-selecting unit {unitToReselect.name} after exiting movement mode");
            actionMenu.SelectUnit(unitToReselect);
        }
    }
    
    void DeselectUnit()
    {
        if (selectedUnit == null) return;
        
        // Remove outline from deselected unit
        if (outlineController != null)
        {
            outlineController.ClearSelectedUnit();
        }
        
        SimpleMessageLog.Log("Unit deselected");
        selectedUnit = null;
        currentGroundObject = null;
        inMovementMode = false;
        ClearMovementHighlights();
        validMovePositions.Clear();
    }
    
    void ShowMovementRange(Vector2Int unitPosition, GameObject groundObject)
    {
        validMovePositions.Clear();
        
        // Use pathfinder to get reachable positions if available
        if (pathfinder != null)
        {
            List<Vector2Int> reachablePositions = pathfinder.GetReachablePositions(unitPosition, groundObject, movementRange);
            
            foreach (Vector2Int pos in reachablePositions)
            {
                if (IsBasicValidMovePosition(pos, groundObject, false)) // Don't use pathfinding here since we already got reachable positions
                {
                    validMovePositions.Add(pos);
                    CreateMovementHighlight(pos, groundObject, true);
                }
            }
            
            // Also show some blocked positions for visual feedback
            ShowBlockedPositions(unitPosition, groundObject);
        }
        else
        {
            // Fallback to old grid-based method
            for (int x = -movementRange; x <= movementRange; x++)
            {
                for (int z = -movementRange; z <= movementRange; z++)
                {
                    // Skip the unit's current position
                    if (x == 0 && z == 0) continue;
                    
                    // Use Manhattan distance for movement range
                    int distance = Mathf.Abs(x) + Mathf.Abs(z);
                    if (distance > movementRange) continue;
                    
                    Vector2Int checkPos = unitPosition + new Vector2Int(x, z);
                    
                    // Check if position is valid and not occupied
                    if (IsValidMovePosition(checkPos, groundObject))
                    {
                        validMovePositions.Add(checkPos);
                        CreateMovementHighlight(checkPos, groundObject, true);
                    }
                    else if (gridManager.IsValidGridPosition(checkPos, groundObject))
                    {
                        // Show red highlight for blocked but valid grid positions
                        CreateMovementHighlight(checkPos, groundObject, false);
                    }
                }
            }
        }
    }
    
    void ShowBlockedPositions(Vector2Int unitPosition, GameObject groundObject)
    {
        // Show some blocked positions within range for visual context
        for (int x = -movementRange; x <= movementRange; x++)
        {
            for (int z = -movementRange; z <= movementRange; z++)
            {
                // Skip the unit's current position
                if (x == 0 && z == 0) continue;
                
                // Use Manhattan distance for movement range
                int distance = Mathf.Abs(x) + Mathf.Abs(z);
                if (distance > movementRange) continue;
                
                Vector2Int checkPos = unitPosition + new Vector2Int(x, z);
                
                // Only show if it's a valid grid position but not reachable
                if (gridManager.IsValidGridPosition(checkPos, groundObject) && !validMovePositions.Contains(checkPos))
                {
                    // Check if it's blocked by something visible (enemies, other units)
                    bool blockedByUnits = placementManager.IsTileOccupied(groundObject, checkPos) || IsEnemyAtPosition(checkPos, groundObject);
                    if (blockedByUnits)
                    {
                        // This position is blocked by units/enemies, show as red
                        CreateMovementHighlight(checkPos, groundObject, false);
                    }
                }
            }
        }
    }
    
    bool IsValidMovePosition(Vector2Int gridPos, GameObject groundObject)
    {
        // Check if position is within grid bounds
        if (!gridManager.IsValidGridPosition(gridPos, groundObject))
            return false;
        
        // Check if position is occupied by units
        if (placementManager.IsTileOccupied(groundObject, gridPos))
            return false;
        
        // Check if position is occupied by enemies
        if (IsEnemyAtPosition(gridPos, groundObject))
            return false;
        
        // Check if position is reachable (height restrictions)
        if (heightChecker != null && !heightChecker.IsPositionReachable(gridPos, groundObject))
            return false;
        
        // NEW: Use terrain height detector for more accurate height validation
        if (terrainHeightDetector != null && selectedUnit != null)
        {
            UnitGridInfo unitInfo = selectedUnit.GetComponent<UnitGridInfo>();
            if (unitInfo != null)
            {
                // Check if unit can move from current position to target position based on terrain height
                if (!terrainHeightDetector.CanMoveFromGridToGrid(unitInfo.gridPosition, gridPos, groundObject))
                {
                    return false;
                }
            }
        }
        
        // Check if there's a clear path to this position (no enemies blocking the way)
        if (selectedUnit != null && !HasClearPathToPosition(gridPos, groundObject))
            return false;
        
        return true;
    }
    
    bool IsBasicValidMovePosition(Vector2Int gridPos, GameObject groundObject, bool checkPathfinding = true)
    {
        // Check if position is within grid bounds
        if (!gridManager.IsValidGridPosition(gridPos, groundObject))
            return false;
        
        // Check if position is occupied by units
        if (placementManager.IsTileOccupied(groundObject, gridPos))
            return false;
        
        // Check if position is occupied by enemies
        if (IsEnemyAtPosition(gridPos, groundObject))
            return false;
        
        // Check if position is reachable (height restrictions)
        if (heightChecker != null && !heightChecker.IsPositionReachable(gridPos, groundObject))
            return false;
        
        // NOTE: Pathfinding validation is handled separately in ShowMovementRange for performance
        // Don't do individual pathfinding checks here as it's too expensive
        
        return true;
    }
    
    bool IsEnemyAtPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float checkRadius = gridManager.gridSize * 0.4f;
        
        Collider[] colliders = Physics.OverlapSphere(worldPos, checkRadius);
        
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool HasClearPathToPosition(Vector2Int targetPos, GameObject groundObject)
    {
        if (selectedUnit == null) return true;
        
        UnitGridInfo unitInfo = selectedUnit.GetComponent<UnitGridInfo>();
        if (unitInfo == null) return true;
        
        Vector2Int startPos = unitInfo.gridPosition;
        Vector2Int moveVector = targetPos - startPos;
        
        // For moves within movement range, be more permissive
        int manhattanDistance = Mathf.Abs(moveVector.x) + Mathf.Abs(moveVector.y);
        
        // For short movements (within 2 squares), only block if trying to move directly through an enemy
        if (manhattanDistance <= 2)
        {
            return !IsDirectlyThroughEnemy(startPos, targetPos, groundObject);
        }
        
        // For longer movements, use more strict pathfinding
        return HasDirectPath(startPos, targetPos, groundObject);
    }
    
    bool IsDirectlyThroughEnemy(Vector2Int startPos, Vector2Int targetPos, GameObject groundObject)
    {
        Vector2Int moveVector = targetPos - startPos;
        
        // If it's a single step move, it's never "through" an enemy
        if (Mathf.Abs(moveVector.x) <= 1 && Mathf.Abs(moveVector.y) <= 1)
        {
            return false;
        }
        
        // For multi-step moves, check if we're moving directly through an enemy's position
        // Use a simple midpoint check for 2-step moves
        if (Mathf.Abs(moveVector.x) == 2 && moveVector.y == 0)
        {
            // Moving 2 squares horizontally
            Vector2Int midPoint = new Vector2Int(startPos.x + moveVector.x / 2, startPos.y);
            return IsEnemyAtPosition(midPoint, groundObject);
        }
        else if (Mathf.Abs(moveVector.y) == 2 && moveVector.x == 0)
        {
            // Moving 2 squares vertically
            Vector2Int midPoint = new Vector2Int(startPos.x, startPos.y + moveVector.y / 2);
            return IsEnemyAtPosition(midPoint, groundObject);
        }
        
        // For diagonal 2-step moves, only block if enemy is directly in the path
        if (Mathf.Abs(moveVector.x) == 2 && Mathf.Abs(moveVector.y) == 2)
        {
            Vector2Int midPoint = new Vector2Int(startPos.x + moveVector.x / 2, startPos.y + moveVector.y / 2);
            return IsEnemyAtPosition(midPoint, groundObject);
        }
        
        // For other diagonal moves, don't block based on enemies in the path
        return false;
    }
    
    bool HasDirectPath(Vector2Int startPos, Vector2Int targetPos, GameObject groundObject)
    {
        // Calculate the movement vector
        Vector2Int moveVector = targetPos - startPos;
        int absX = Mathf.Abs(moveVector.x);
        int absY = Mathf.Abs(moveVector.y);
        
        // For single-step movements, just check if the target is clear
        if (absX <= 1 && absY <= 1)
        {
            return true; // Single step movements are always allowed if target is clear
        }
        
        // For longer movements, use a more permissive approach
        // Check if we can move in the general direction without being completely blocked
        
        // For diagonal movement, check both axis components
        if (absX > 0 && absY > 0)
        {
            return HasDiagonalPath(startPos, targetPos, groundObject);
        }
        
        // For straight line movement, use the original line check
        List<Vector2Int> pathCells = GetLinePath(startPos, targetPos);
        
        // Check each cell in the path (excluding start and end)
        for (int i = 1; i < pathCells.Count - 1; i++)
        {
            Vector2Int checkPos = pathCells[i];
            
            // If there's an enemy blocking this cell, path is not clear
            if (IsEnemyAtPosition(checkPos, groundObject))
            {
                return false;
            }
        }
        
        return true;
    }
    
    bool HasDiagonalPath(Vector2Int startPos, Vector2Int targetPos, GameObject groundObject)
    {
        // For diagonal movement, we're more permissive
        // Check if either the horizontal or vertical component path is clear
        
        Vector2Int moveVector = targetPos - startPos;
        int stepX = moveVector.x > 0 ? 1 : (moveVector.x < 0 ? -1 : 0);
        int stepY = moveVector.y > 0 ? 1 : (moveVector.y < 0 ? -1 : 0);
        
        // Check if we can move step by step without being completely surrounded
        Vector2Int currentPos = startPos;
        
        while (currentPos != targetPos)
        {
            // Try to move towards target
            Vector2Int nextPos = currentPos;
            
            // Prioritize the longer axis for movement
            if (Mathf.Abs(targetPos.x - currentPos.x) >= Mathf.Abs(targetPos.y - currentPos.y))
            {
                if (currentPos.x != targetPos.x)
                    nextPos.x += stepX;
                else if (currentPos.y != targetPos.y)
                    nextPos.y += stepY;
            }
            else
            {
                if (currentPos.y != targetPos.y)
                    nextPos.y += stepY;
                else if (currentPos.x != targetPos.x)
                    nextPos.x += stepX;
            }
            
            // If the direct step is blocked, try alternative diagonal movement
            if (IsEnemyAtPosition(nextPos, groundObject))
            {
                // Try moving the other axis first
                Vector2Int altPos = currentPos;
                if (nextPos.x != currentPos.x && nextPos.y != currentPos.y)
                {
                    // Try horizontal first
                    Vector2Int horizontalFirst = new Vector2Int(currentPos.x + stepX, currentPos.y);
                    Vector2Int verticalFirst = new Vector2Int(currentPos.x, currentPos.y + stepY);
                    
                    if (!IsEnemyAtPosition(horizontalFirst, groundObject) || !IsEnemyAtPosition(verticalFirst, groundObject))
                    {
                        // At least one alternative path is available
                        currentPos = !IsEnemyAtPosition(horizontalFirst, groundObject) ? horizontalFirst : verticalFirst;
                        continue;
                    }
                }
                
                // If we can't find an alternative, this path is blocked
                return false;
            }
            
            currentPos = nextPos;
        }
        
        return true;
    }
    
    List<Vector2Int> GetLinePath(Vector2Int start, Vector2Int end)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        
        int x0 = start.x, y0 = start.y;
        int x1 = end.x, y1 = end.y;
        
        int dx = Mathf.Abs(x1 - x0);
        int dy = Mathf.Abs(y1 - y0);
        
        int x = x0;
        int y = y0;
        
        int n = 1 + dx + dy;
        int x_inc = (x1 > x0) ? 1 : -1;
        int y_inc = (y1 > y0) ? 1 : -1;
        int error = dx - dy;
        
        dx *= 2;
        dy *= 2;
        
        for (; n > 0; --n)
        {
            path.Add(new Vector2Int(x, y));
            
            if (error > 0)
            {
                x += x_inc;
                error -= dy;
            }
            else
            {
                y += y_inc;
                error += dx;
            }
        }
        
        return path;
    }
    
    void CreateMovementHighlight(Vector2Int gridPos, GameObject groundObject, bool isValid)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        highlight.name = $"MovementHighlight_{gridPos.x}_{gridPos.y}_{(isValid ? "Valid" : "Invalid")}";
        highlight.transform.position = worldPos;
        highlight.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        
        // Remove collider to prevent physics interactions with units
        Destroy(highlight.GetComponent<Collider>());
        
        // Set material and color
        Renderer renderer = highlight.GetComponent<Renderer>();
        if (movementHighlightMaterial != null)
        {
            Material mat = new Material(movementHighlightMaterial);
            mat.color = isValid ? validMoveColor : blockedMoveColor;
            renderer.material = mat;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = isValid ? validMoveColor : blockedMoveColor;
            renderer.material = mat;
        }
        
        movementHighlights.Add(highlight);
    }
    
    bool TryMoveToPosition(Vector3 worldClickPos, GameObject clickedObject)
    {
        if (selectedUnit == null || currentGroundObject == null)
            return false;
        
        // Check if we clicked on a valid movement highlight (old method)
        if (clickedObject != null && clickedObject.name.StartsWith("MovementHighlight_") && clickedObject.name.Contains("Valid"))
        {
            // Convert world position to grid position
            Vector2Int targetGridPos = gridManager.WorldToGridPosition(worldClickPos, currentGroundObject);
            
            // Perform the move
            MoveUnit(targetGridPos);
            return true;
        }
        
        // New method: check if we clicked on ground and if that position is a valid move
        if (clickedObject != null && (clickedObject.layer == LayerMask.NameToLayer("Ground") || clickedObject.layer == LayerMask.NameToLayer("Grid")))
        {
            Vector2Int targetGridPos = gridManager.WorldToGridPosition(worldClickPos, clickedObject);
            
            // Check if this position is in our valid move list
            if (validMovePositions.Contains(targetGridPos))
            {
                currentGroundObject = clickedObject;
                MoveUnit(targetGridPos);
                return true;
            }
        }
        
        return false;
    }
    
    void MoveUnit(Vector2Int targetGridPos)
    {
        UnitGridInfo unitInfo = selectedUnit.GetComponent<UnitGridInfo>();
        if (unitInfo == null) return;
        
        // Check if unit has enough MP for this move
        Character character = selectedUnit.GetComponent<Character>();
        if (character?.Stats != null)
        {
            // Calculate movement cost (1 MP per tile moved)
            int distance = Mathf.Abs(targetGridPos.x - unitInfo.gridPosition.x) + Mathf.Abs(targetGridPos.y - unitInfo.gridPosition.y);
            int moveCost = Mathf.Max(1, distance); // At least 1 MP per move
            
            if (!character.Stats.CanSpendMP(moveCost))
            {
                SimpleMessageLog.Log($"{character.CharacterName} doesn't have enough Move Points! Need {moveCost}, have {character.Stats.CurrentMP}");
                return;
            }
            
            // Spend the move points
            character.Stats.SpendMP(moveCost);
            Debug.Log($"{character.CharacterName} spent {moveCost} MP for movement - remaining: {character.Stats.CurrentMP}");
        }
        
        // Clear old position in placement manager
        placementManager.SetTileOccupied(unitInfo.groundObject, unitInfo.gridPosition, false);
        
        // Calculate new world position using terrain height detection
        Vector3 targetWorldPos;
        if (terrainHeightDetector != null)
        {
            targetWorldPos = terrainHeightDetector.GetGroundPositionAtGridPosition(targetGridPos, currentGroundObject);
        }
        else
        {
            targetWorldPos = gridManager.GridToWorldPosition(targetGridPos, currentGroundObject);
        }
        
        // Add height offset for unit
        float heightOffset = CalculateUnitHeightOffset(selectedUnit);
        targetWorldPos.y += heightOffset;
        
        // Use pathfinder to get the actual path if available
        if (pathfinder != null)
        {
            List<Vector2Int> path = pathfinder.FindPath(unitInfo.gridPosition, targetGridPos, currentGroundObject, movementRange);
            if (path.Count > 0)
            {
                // Follow the pathfinding route
                StartCoroutine(AnimateAlongPath(selectedUnit, path, currentGroundObject));
            }
            else
            {
                // Fallback to direct movement
                StartCoroutine(AnimateHeightAwareMovement(selectedUnit, targetWorldPos, unitInfo.gridPosition, targetGridPos));
            }
        }
        else
        {
            // Fallback to direct movement
            StartCoroutine(AnimateHeightAwareMovement(selectedUnit, targetWorldPos, unitInfo.gridPosition, targetGridPos));
        }
        
        // Update unit's grid info
        unitInfo.gridPosition = targetGridPos;
        unitInfo.groundObject = currentGroundObject;
        
        // Mark new position as occupied
        placementManager.SetTileOccupied(currentGroundObject, targetGridPos, true, selectedUnit);
        
        
        // Clear movement mode and highlights after successful move
        ExitMovementMode();
        
        // Keep unit selected and show action menu after movement for player units
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Combat && 
            turnManager.IsPlayerTurn() && selectedUnit == turnManager.GetCurrentUnit())
        {
            // Re-select the unit to show the action menu after a small delay
            Invoke(nameof(ReselectUnitAfterMove), 0.1f);
        }
    }
    
    IEnumerator AnimateMovement(GameObject unit, Vector3 targetPosition)
    {
        isMoving = true;
        Vector3 startPosition = unit.transform.position;
        float journeyTime = Vector3.Distance(startPosition, targetPosition) / moveSpeed;
        float elapsedTime = 0;
        
        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / journeyTime;
            
            // Smooth movement curve
            fractionOfJourney = Mathf.SmoothStep(0, 1, fractionOfJourney);
            
            unit.transform.position = Vector3.Lerp(startPosition, targetPosition, fractionOfJourney);
            yield return null;
        }
        
        // Ensure unit ends up exactly at target
        unit.transform.position = targetPosition;
        isMoving = false;
    }
    
    IEnumerator AnimateHeightAwareMovement(GameObject unit, Vector3 targetPosition, Vector2Int startGridPos, Vector2Int endGridPos)
    {
        isMoving = true;
        Vector3 startPosition = unit.transform.position;
        
        // Calculate if this is a height transition
        float heightDifference = targetPosition.y - startPosition.y;
        bool isClimbing = heightDifference > 0.5f; // Climbing if more than 0.5 units up
        bool isFalling = heightDifference < -0.5f; // Falling if more than 0.5 units down
        
        float journeyTime = Vector3.Distance(startPosition, targetPosition) / moveSpeed;
        
        // Adjust journey time for height transitions
        if (isClimbing)
        {
            journeyTime *= 1.3f; // Slower when climbing
        }
        else if (isFalling)
        {
            journeyTime *= 0.8f; // Faster when falling
        }
        
        float elapsedTime = 0;
        
        while (elapsedTime < journeyTime)
        {
            elapsedTime += Time.deltaTime;
            float fractionOfJourney = elapsedTime / journeyTime;
            
            Vector3 currentPosition;
            
            if (isClimbing)
            {
                // Climbing animation: arc upward
                float arcHeight = Mathf.Abs(heightDifference) * 0.3f + 0.5f; // Add extra height for arc
                float horizontalProgress = Mathf.SmoothStep(0, 1, fractionOfJourney);
                float verticalProgress = Mathf.Sin(fractionOfJourney * Mathf.PI); // Arc shape
                
                Vector3 horizontalPos = Vector3.Lerp(
                    new Vector3(startPosition.x, startPosition.y, startPosition.z),
                    new Vector3(targetPosition.x, startPosition.y, targetPosition.z),
                    horizontalProgress
                );
                
                float currentHeight = Mathf.Lerp(startPosition.y, targetPosition.y, horizontalProgress) + 
                                    (verticalProgress * arcHeight);
                
                currentPosition = new Vector3(horizontalPos.x, currentHeight, horizontalPos.z);
            }
            else if (isFalling)
            {
                // Falling animation: slight arc downward
                float horizontalProgress = Mathf.SmoothStep(0, 1, fractionOfJourney);
                float fallProgress = fractionOfJourney * fractionOfJourney; // Accelerating fall
                
                Vector3 horizontalPos = Vector3.Lerp(
                    new Vector3(startPosition.x, startPosition.y, startPosition.z),
                    new Vector3(targetPosition.x, startPosition.y, targetPosition.z),
                    horizontalProgress
                );
                
                float currentHeight = Mathf.Lerp(startPosition.y, targetPosition.y, fallProgress);
                currentPosition = new Vector3(horizontalPos.x, currentHeight, horizontalPos.z);
            }
            else
            {
                // Normal movement: smooth interpolation
                float smoothProgress = Mathf.SmoothStep(0, 1, fractionOfJourney);
                currentPosition = Vector3.Lerp(startPosition, targetPosition, smoothProgress);
            }
            
            unit.transform.position = currentPosition;
            yield return null;
        }
        
        // Ensure unit ends up exactly at target
        unit.transform.position = targetPosition;
        isMoving = false;
    }
    
    IEnumerator AnimateAlongPath(GameObject unit, List<Vector2Int> path, GameObject groundObject)
    {
        isMoving = true;
        
        // Convert path to world positions with proper heights
        List<Vector3> worldPath = new List<Vector3>();
        worldPath.Add(unit.transform.position); // Start from current position
        
        float heightOffset = CalculateUnitHeightOffset(unit);
        
        foreach (Vector2Int gridPos in path)
        {
            Vector3 worldPos;
            if (terrainHeightDetector != null)
            {
                worldPos = terrainHeightDetector.GetGroundPositionAtGridPosition(gridPos, groundObject);
            }
            else
            {
                worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
            }
            worldPos.y += heightOffset;
            worldPath.Add(worldPos);
        }
        
        // Animate along each segment of the path
        for (int i = 1; i < worldPath.Count; i++)
        {
            Vector3 startPos = worldPath[i - 1];
            Vector3 endPos = worldPath[i];
            
            // Calculate movement time for this segment
            float distance = Vector3.Distance(startPos, endPos);
            float segmentTime = distance / moveSpeed;
            
            // Check if this is a height transition
            float heightDifference = endPos.y - startPos.y;
            bool isClimbing = heightDifference > 0.5f;
            bool isFalling = heightDifference < -0.5f;
            
            // Adjust time for height transitions
            if (isClimbing)
            {
                segmentTime *= 1.3f; // Slower when climbing
            }
            else if (isFalling)
            {
                segmentTime *= 0.8f; // Faster when falling
            }
            
            // Animate this segment
            float elapsedTime = 0;
            while (elapsedTime < segmentTime)
            {
                elapsedTime += Time.deltaTime;
                float progress = elapsedTime / segmentTime;
                
                Vector3 currentPosition;
                
                if (isClimbing)
                {
                    // Climbing animation with arc
                    float arcHeight = Mathf.Abs(heightDifference) * 0.3f + 0.5f;
                    float horizontalProgress = Mathf.SmoothStep(0, 1, progress);
                    float verticalProgress = Mathf.Sin(progress * Mathf.PI);
                    
                    Vector3 horizontalPos = Vector3.Lerp(
                        new Vector3(startPos.x, startPos.y, startPos.z),
                        new Vector3(endPos.x, startPos.y, endPos.z),
                        horizontalProgress
                    );
                    
                    float currentHeight = Mathf.Lerp(startPos.y, endPos.y, horizontalProgress) + 
                                        (verticalProgress * arcHeight);
                    
                    currentPosition = new Vector3(horizontalPos.x, currentHeight, horizontalPos.z);
                }
                else if (isFalling)
                {
                    // Falling animation with acceleration
                    float horizontalProgress = Mathf.SmoothStep(0, 1, progress);
                    float fallProgress = progress * progress; // Accelerating fall
                    
                    Vector3 horizontalPos = Vector3.Lerp(
                        new Vector3(startPos.x, startPos.y, startPos.z),
                        new Vector3(endPos.x, startPos.y, endPos.z),
                        horizontalProgress
                    );
                    
                    float currentHeight = Mathf.Lerp(startPos.y, endPos.y, fallProgress);
                    currentPosition = new Vector3(horizontalPos.x, currentHeight, horizontalPos.z);
                }
                else
                {
                    // Normal movement
                    float smoothProgress = Mathf.SmoothStep(0, 1, progress);
                    currentPosition = Vector3.Lerp(startPos, endPos, smoothProgress);
                }
                
                unit.transform.position = currentPosition;
                yield return null;
            }
            
            // Ensure we end exactly at the target position for this segment
            unit.transform.position = endPos;
        }
        
        isMoving = false;
    }
    
    void ClearMovementHighlights()
    {
        foreach (GameObject highlight in movementHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }
        movementHighlights.Clear();
    }
    
    float CalculateUnitHeightOffset(GameObject unit)
    {
        Renderer unitRenderer = unit.GetComponent<Renderer>();
        if (unitRenderer != null)
        {
            return unitRenderer.bounds.size.y * 0.5f;
        }
        
        Collider unitCollider = unit.GetComponent<Collider>();
        if (unitCollider != null)
        {
            return unitCollider.bounds.size.y * 0.5f;
        }
        
        return 1f; // Default height
    }
    
    void ReselectUnitAfterMove()
    {
        // Re-select the unit to show the action menu
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null && selectedUnit != null)
        {
            actionMenu.SelectUnit(selectedUnit);
        }
    }
    
    void OnDestroy()
    {
        ClearMovementHighlights();
    }
    
    // Public getters for other systems
    public GameObject GetSelectedUnit()
    {
        return selectedUnit;
    }
    
    public bool IsUnitSelected()
    {
        return selectedUnit != null;
    }
    
    public bool IsMoving()
    {
        return isMoving;
    }
    
    public bool IsInMovementMode()
    {
        return inMovementMode;
    }
}