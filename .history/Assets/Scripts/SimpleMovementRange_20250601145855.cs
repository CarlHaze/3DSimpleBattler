using UnityEngine;
using System.Collections.Generic;

public class SimpleMovementRange : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask unitLayerMask = -1;
    public int defaultMovementRange = 3;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private Camera gameCamera;
    private GameObject selectedUnit;
    private UnitPlacementManager placementManager;
    private SimpleUnitSelector unitSelector;
    private GridOverlayManager gridManager;
    private SimpleHeightCheck heightChecker;
    
    // Store original grid materials
    private Dictionary<GameObject, Material> originalGridMaterials = new Dictionary<GameObject, Material>();
    private List<GameObject> highlightedGridLines = new List<GameObject>();
    
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
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, unitLayerMask))
        {
            GameObject clickedObject = hit.collider.gameObject;
            UnitGridInfo unitInfo = clickedObject.GetComponent<UnitGridInfo>();
            
            if (unitInfo != null && clickedObject.CompareTag("Player"))
            {
                SelectUnit(clickedObject);
            }
            else
            {
                SimpleMessageLog.Log("Cannot select enemy units!");
            }
        }
        else
        {
            DeselectUnit();
        }
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
        HideMovementRange();
    }
    
    void ShowMovementRange(UnitGridInfo unitInfo)
    {
        HideMovementRange(); // Clear existing highlights
        
        Vector2Int unitPos = unitInfo.gridPosition;
        GameObject unitGround = unitInfo.groundObject;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Showing movement range from {unitPos} on {unitGround.name}");
        }
        
        // Get all grid line objects from the GridOverlayManager
        List<GameObject> allGridLines = GetAllGridLines();
        
        // Check each grid position within movement range
        for (int x = unitPos.x - defaultMovementRange; x <= unitPos.x + defaultMovementRange; x++)
        {
            for (int y = unitPos.y - defaultMovementRange; y <= unitPos.y + defaultMovementRange; y++)
            {
                Vector2Int checkPos = new Vector2Int(x, y);
                
                // Skip the unit's current position
                if (checkPos == unitPos) continue;
                
                // Calculate Manhattan distance (movement cost)
                int distance = Mathf.Abs(x - unitPos.x) + Mathf.Abs(y - unitPos.y);
                if (distance > defaultMovementRange) continue;
                
                // Check if this position is valid to move to
                if (IsValidMovePosition(checkPos, unitGround))
                {
                    HighlightGridPosition(checkPos, unitGround);
                }
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Highlighted {highlightedGridLines.Count} grid positions");
        }
    }
    
    bool IsValidMovePosition(Vector2Int gridPos, GameObject groundObject)
    {
        // Check if position is within grid bounds
        if (!gridManager.IsValidGridPosition(gridPos, groundObject))
            return false;
        
        // Check if position is occupied by another unit
        if (placementManager.IsTileOccupied(groundObject, gridPos))
            return false;
        
        // Check if position is reachable (height check)
        if (heightChecker != null && !heightChecker.IsPositionReachable(gridPos, groundObject))
            return false;
        
        return true;
    }
    
    void HighlightGridPosition(Vector2Int gridPos, GameObject groundObject)
    {
        // Find grid lines that correspond to this position
        string gridParentName = $"Grid_{groundObject.name}";
        Transform gridParent = gridManager.transform.Find(gridParentName);
        
        if (gridParent == null)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"Could not find grid parent: {gridParentName}");
            }
            return;
        }
        
        // Highlight the grid cell by changing nearby grid line colors
        HighlightGridCell(gridParent, gridPos);
    }
    
    void HighlightGridCell(Transform gridParent, Vector2Int gridPos)
    {
        // Find the grid lines that form this cell
        string[] lineNames = {
            $"GridLine_V_{gridPos.x}",      // Left vertical line
            $"GridLine_V_{gridPos.x + 1}",  // Right vertical line
            $"GridLine_H_{gridPos.y}",      // Bottom horizontal line
            $"GridLine_H_{gridPos.y + 1}"   // Top horizontal line
        };
        
        foreach (string lineName in lineNames)
        {
            Transform lineTransform = gridParent.Find(lineName);
            if (lineTransform != null)
            {
                GameObject lineObj = lineTransform.gameObject;
                LineRenderer lr = lineObj.GetComponent<LineRenderer>();
                
                if (lr != null)
                {
                    // Store original material if not already stored
                    if (!originalGridMaterials.ContainsKey(lineObj))
                    {
                        originalGridMaterials[lineObj] = lr.material;
                    }
                    
                    // Create blue material for highlighting
                    Material blueMaterial = new Material(lr.material);
                    blueMaterial.color = Color.blue;
                    lr.material = blueMaterial;
                    
                    highlightedGridLines.Add(lineObj);
                }
            }
        }
    }
    
    List<GameObject> GetAllGridLines()
    {
        List<GameObject> gridLines = new List<GameObject>();
        
        // Get all grid parent objects from GridOverlayManager
        for (int i = 0; i < gridManager.transform.childCount; i++)
        {
            Transform child = gridManager.transform.GetChild(i);
            if (child.name.StartsWith("Grid_"))
            {
                // Get all line children
                for (int j = 0; j < child.childCount; j++)
                {
                    Transform lineChild = child.GetChild(j);
                    if (lineChild.name.StartsWith("GridLine_"))
                    {
                        gridLines.Add(lineChild.gameObject);
                    }
                }
            }
        }
        
        return gridLines;
    }
    
    void HideMovementRange()
    {
        // Restore original materials
        foreach (GameObject lineObj in highlightedGridLines)
        {
            if (lineObj != null && originalGridMaterials.ContainsKey(lineObj))
            {
                LineRenderer lr = lineObj.GetComponent<LineRenderer>();
                if (lr != null)
                {
                    lr.material = originalGridMaterials[lineObj];
                }
            }
        }
        
        highlightedGridLines.Clear();
    }
    
    // Public getters
    public GameObject GetSelectedUnit() => selectedUnit;
    public bool HasSelectedUnit() => selectedUnit != null;
}