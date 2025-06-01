using UnityEngine;
using System.Collections.Generic;

public class GridMovementHighlighter : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask unitLayerMask = -1;
    public int movementRange = 3;
    public Color validMoveColor = Color.blue;
    public Color invalidMoveColor = Color.red;
    public float highlightAlpha = 0.7f;
    
    [Header("Elevation Settings")]
    public float maxHeightDifference = 1.5f; // Maximum height difference allowed for movement
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private Camera gameCamera;
    private GameObject selectedUnit;
    private UnitPlacementManager placementManager;
    private SimpleUnitSelector unitSelector;
    private GridOverlayManager gridManager;
    private SimpleHeightCheck heightChecker;
    
    // Store created highlight squares
    private List<GameObject> movementHighlights = new List<GameObject>();
    
    void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        heightChecker = FindFirstObjectByType<SimpleHeightCheck>();
    }
    
    void Update()
    {
        // Only allow selection when all units are placed
        if (!AllUnitsPlaced()) return;
        
        HandleInput();
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
            TrySelectUnit();
        }
        
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeselectUnit();
        }
    }
    
    void TrySelectUnit()
    {
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Use a different approach - check for units without affecting them
        List<GameObject> allUnits = FindAllPlayerUnits();
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity))
        {
            // Check if we hit a player unit
            foreach (GameObject unit in allUnits)
            {
                if (hit.collider.gameObject == unit && unit.CompareTag("Player"))
                {
                    SelectUnit(unit);
                    return;
                }
            }
        }
        
        // If we get here, deselect
        DeselectUnit();
    }
    
    List<GameObject> FindAllPlayerUnits()
    {
        List<GameObject> playerUnits = new List<GameObject>();
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.CompareTag("Player") && obj.GetComponent<UnitGridInfo>() != null)
            {
                playerUnits.Add(obj);
            }
        }
        
        return playerUnits;
    }
    
    public void SelectUnit(GameObject unit)
    {
        if (unit == selectedUnit) return;
        
        // Deselect previous unit
        if (selectedUnit != null)
        {
            DeselectUnit();
        }
        
        selectedUnit = unit;
        UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
        
        if (unitInfo != null)
        {
            SimpleMessageLog.Log($"Selected {unit.name}");
            ShowMovementRange(unitInfo);
        }
    }
    
    public void DeselectUnit()
    {
        if (selectedUnit == null) return;
        
        SimpleMessageLog.Log("Unit deselected");
        selectedUnit = null;
        ClearMovementHighlights();
    }
    
    void ShowMovementRange(UnitGridInfo unitInfo)
    {
        ClearMovementHighlights();
        
        Vector2Int unitPos = unitInfo.gridPosition;
        GameObject unitGround = unitInfo.groundObject;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Showing movement range from {unitPos} on {unitGround.name}");
        }
        
        int validCount = 0;
        int invalidCount = 0;
        
        // Get the unit's current world position for height comparison
        Vector3 unitWorldPos = gridManager.GridToWorldPosition(unitPos, unitGround);
        
        // Only check positions on the SAME ground object as the unit
        for (int x = unitPos.x - movementRange; x <= unitPos.x + movementRange; x++)
        {
            for (int y = unitPos.y - movementRange; y <= unitPos.y + movementRange; y++)
            {
                Vector2Int checkPos = new Vector2Int(x, y);
                
                // Skip the unit's current position
                if (checkPos == unitPos) continue;
                
                // Calculate Manhattan distance (movement cost)
                int distance = Mathf.Abs(x - unitPos.x) + Mathf.Abs(y - unitPos.y);
                if (distance > movementRange) continue;
                
                // Check if this grid position exists on the ground object
                if (gridManager.IsValidGridPosition(checkPos, unitGround))
                {
                    // Get the world position for this grid cell
                    Vector3 checkWorldPos = gridManager.GridToWorldPosition(checkPos, unitGround);
                    
                    // Check if position is occupied first
                    bool isOccupied = IsPositionOccupied(checkPos, unitGround);
                    bool isWalkable = IsPositionWalkable(checkWorldPos);
                    bool isReachableHeight = IsHeightReachable(unitWorldPos, checkWorldPos);
                    
                    // Show highlight if position is within grid bounds
                    if (isOccupied)
                    {
                        // Position is occupied - show red
                        CreateMovementHighlight(checkPos, unitGround, false);
                        invalidCount++;
                    }
                    else if (!isWalkable)
                    {
                        // Position is not walkable (obstacle, hole, etc.) - show red
                        CreateMovementHighlight(checkPos, unitGround, false);
                        invalidCount++;
                    }
                    else if (!isReachableHeight)
                    {
                        // Position is too high/low to reach - show red
                        CreateMovementHighlight(checkPos, unitGround, false);
                        invalidCount++;
                    }
                    else
                    {
                        // Position is valid - show blue
                        CreateMovementHighlight(checkPos, unitGround, true);
                        validCount++;
                    }
                }
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Created {validCount} valid (blue) and {invalidCount} invalid (red) highlights");
        }
    }
    
    bool IsPositionOccupied(Vector2Int gridPos, GameObject groundObject)
    {
        // Check if position is occupied by another player unit
        if (placementManager != null && placementManager.IsTileOccupied(groundObject, gridPos))
            return true;
        
        // Check if position is occupied by an enemy
        if (IsEnemyAtPosition(gridPos, groundObject))
            return true;
        
        return false;
    }
    
    bool IsEnemyAtPosition(Vector2Int gridPos, GameObject groundObject)
    {
        // Get the world position for this grid cell
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Check for enemies in a small radius around this position
        float checkRadius = gridManager.gridSize * 0.4f;
        Collider[] colliders = Physics.OverlapSphere(worldPos, checkRadius);
        
        foreach (Collider col in colliders)
        {
            // Simple tag check for enemies
            if (col.CompareTag("Enemy"))
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Found enemy {col.name} at grid position {gridPos}");
                }
                return true;
            }
        }
        
        return false;
    }
    
    bool IsPositionWalkable(Vector3 worldPos)
    {
        // Cast a ray down from above to check what's below this position
        Vector3 rayStart = worldPos + Vector3.up * 5f; // Start 5 units above
        RaycastHit hit;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f))
        {
            // Check if we hit something that's not walkable
            GameObject hitObject = hit.collider.gameObject;
            
            // If it's on the Ground layer, it's walkable
            if (hitObject.layer == LayerMask.NameToLayer("Ground"))
                return true;
            
            // Check for specific tags that indicate unwalkable areas
            if (hitObject.CompareTag("Obstacle") || hitObject.CompareTag("Wall") || hitObject.CompareTag("Unwalkable"))
                return false;
            
            // If it has a renderer and is solid, consider the material/height
            Renderer renderer = hitObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                // If the height difference between the hit point and the expected grid position is too large,
                // it might be an obstacle or elevated terrain
                float heightDiff = Mathf.Abs(hit.point.y - worldPos.y);
                if (heightDiff > 0.5f) // Adjust this threshold as needed
                {
                    return false; // Too much height difference, treat as obstacle
                }
            }
        }
        else
        {
            // No ground found - this is a hole or void
            return false;
        }
        
        return true;
    }
    
    bool IsHeightReachable(Vector3 fromPos, Vector3 toPos)
    {
        float heightDifference = Mathf.Abs(toPos.y - fromPos.y);
        return heightDifference <= maxHeightDifference;
    }
    
    bool IsValidMovePosition(Vector2Int gridPos, GameObject groundObject)
    {
        // This method is now broken down into separate checks above
        // Keep it for compatibility with height checker if it exists
        if (heightChecker != null && !heightChecker.IsPositionReachable(gridPos, groundObject))
            return false;
        
        return true;
    }
    
    void CreateMovementHighlight(Vector2Int gridPos, GameObject groundObject, bool isValid)
    {
        // Get world position for this grid cell
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // FIXED: Use a simple cylinder instead of a custom quad to avoid shader issues
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        highlight.name = $"MovementHighlight_{gridPos.x}_{gridPos.y}_{(isValid ? "Valid" : "Invalid")}";
        
        // Remove the collider so it doesn't interfere with clicking
        Destroy(highlight.GetComponent<Collider>());
        
        // Position it slightly above the ground surface
        highlight.transform.position = new Vector3(worldPos.x, worldPos.y + 0.05f, worldPos.z);
        highlight.transform.localScale = new Vector3(gridManager.gridSize * 0.8f, 0.02f, gridManager.gridSize * 0.8f);
        
        // FIXED: Use Unlit/Color shader which is more reliable for solid colors
        Material highlightMaterial = new Material(Shader.Find("Unlit/Color"));
        
        // Set color based on validity
        Color color = isValid ? validMoveColor : invalidMoveColor;
        color.a = highlightAlpha;
        highlightMaterial.color = color;
        
        // Make it transparent if needed
        if (highlightAlpha < 1f)
        {
            highlightMaterial.SetFloat("_Mode", 3);
            highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            highlightMaterial.SetInt("_ZWrite", 0);
            highlightMaterial.DisableKeyword("_ALPHATEST_ON");
            highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
            highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            highlightMaterial.renderQueue = 3000;
        }
        
        Renderer renderer = highlight.GetComponent<Renderer>();
        renderer.material = highlightMaterial;
        
        movementHighlights.Add(highlight);
        
        if (enableDebugLogs && movementHighlights.Count <= 5) // Only log first few
        {
            Debug.Log($"Created {(isValid ? "VALID" : "INVALID")} highlight at {worldPos} for grid {gridPos} on {groundObject.name}");
        }
    }
    
    void ClearMovementHighlights()
    {
        foreach (GameObject highlight in movementHighlights)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        movementHighlights.Clear();
    }
    
    // Public getters
    public GameObject GetSelectedUnit() => selectedUnit;
    public bool HasSelectedUnit() => selectedUnit != null;
}