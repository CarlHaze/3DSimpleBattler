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
    private UnitOutlineController outlineController;
    
    // Current selection state
    private GameObject selectedUnit;
    private List<GameObject> movementHighlights = new List<GameObject>();
    private List<Vector2Int> validMovePositions = new List<Vector2Int>();
    private GameObject currentGroundObject;
    
    // Movement animation
    private bool isMoving = false;

    void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();

        gridManager = FindFirstObjectByType<GridOverlayManager>();
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        heightChecker = FindFirstObjectByType<SimpleHeightCheck>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        outlineController = FindFirstObjectByType<UnitOutlineController>();

        if (gridManager == null)
            Debug.LogError("GridOverlayManager not found!");
        if (placementManager == null)
            Debug.LogError("UnitPlacementManager not found!");
        if (heightChecker == null)
            Debug.LogError("SimpleHeightCheck not found!");
        if (outlineController == null)
            Debug.LogWarning("UnitOutlineController not found - unit outlines will not work!");
        
    }
    
    void Update()
    {
        // Handle both unit selection and movement
        if (!isMoving)
        {
            // Only allow selection when all units are placed
            if (!AllUnitsPlaced()) return;
            
            HandleInput();
        }
    }
    
    bool AllUnitsPlaced()
    {
        if (unitSelector == null) return true;
        return unitSelector.GetUnitsPlaced() >= unitSelector.GetMaxUnits();
    }
    
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                // First check if we clicked on a movement highlight
                if (selectedUnit != null && TryMoveToPosition(hit.point, clickedObject))
                {
                    return; // Movement handled, don't process unit selection
                }
                
                // If no movement, try to select a unit
                HandleUnitSelection(clickedObject);
            }
        }
        
        // Deselect with right click or escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectUnit();
        }
    }
    
    void HandleUnitSelection(GameObject clickedObject)
    {
        // Check if clicked object is a player unit
        if (clickedObject.CompareTag("Player"))
        {
            SelectUnit(clickedObject);
        }
        else
        {
            DeselectUnit();
        }
    }
    
    void SelectUnit(GameObject unit)
    {
        // If same unit, deselect
        if (selectedUnit == unit)
        {
            DeselectUnit();
            return;
        }
        
        // Clear previous selection
        ClearMovementHighlights();
        
        selectedUnit = unit;
        
        // Add outline to selected unit
        if (outlineController != null)
        {
            outlineController.SetSelectedUnit(selectedUnit);
        }
        
        // Get unit's grid info
        UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
        if (unitInfo != null)
        {
            currentGroundObject = unitInfo.groundObject;
            ShowMovementRange(unitInfo.gridPosition, unitInfo.groundObject);
            SimpleMessageLog.Log($"Selected {unit.name}");
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
        ClearMovementHighlights();
        validMovePositions.Clear();
    }
    
    void ShowMovementRange(Vector2Int unitPosition, GameObject groundObject)
    {
        validMovePositions.Clear();
        
        // Calculate all positions within movement range
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
        
        // Check if there's a clear path to this position (no enemies blocking the way)
        if (selectedUnit != null && !HasClearPathToPosition(gridPos, groundObject))
            return false;
        
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
        
        // Clear old position in placement manager
        placementManager.SetTileOccupied(unitInfo.groundObject, unitInfo.gridPosition, false);
        
        // Calculate new world position
        Vector3 targetWorldPos = gridManager.GridToWorldPosition(targetGridPos, currentGroundObject);
        
        // Add height offset for unit
        float heightOffset = CalculateUnitHeightOffset(selectedUnit);
        targetWorldPos.y += heightOffset;
        
        // Start movement animation
        StartCoroutine(AnimateMovement(selectedUnit, targetWorldPos));
        
        // Update unit's grid info
        unitInfo.gridPosition = targetGridPos;
        unitInfo.groundObject = currentGroundObject;
        
        // Mark new position as occupied
        placementManager.SetTileOccupied(currentGroundObject, targetGridPos, true, selectedUnit);
        
        
        // Clear selection and highlights
        DeselectUnit();
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
}