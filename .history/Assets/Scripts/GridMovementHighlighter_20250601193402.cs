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
                
                // Only check the SAME ground object the unit is on
                if (gridManager.IsValidGridPosition(checkPos, unitGround))
                {
                    // Check height difference
                    Vector3 checkWorldPos = gridManager.GridToWorldPosition(checkPos, unitGround);
                    float heightDifference = Mathf.Abs(checkWorldPos.y - unitWorldPos.y);
                    
                    if (heightDifference <= maxHeightDifference && IsValidMovePosition(checkPos, unitGround))
                    {
                        // Valid move - show blue
                        CreateMovementHighlight(checkPos, unitGround, true);
                        validCount++;
                    }
                    else
                    {
                        // Invalid move (occupied, unreachable, or too high) - show red
                        CreateMovementHighlight(checkPos, unitGround, false);
                        invalidCount++;
                    }
                }
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Created {validCount} valid (blue) and {invalidCount} invalid (red) highlights");
        }
    }
    
    bool IsValidMovePosition(Vector2Int gridPos, GameObject groundObject)
    {
        // Check if position is occupied by another unit
        if (placementManager != null && placementManager.IsTileOccupied(groundObject, gridPos))
            return false;
        
        // Check if position is reachable (height check)
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