using UnityEngine;
using System.Collections.Generic;

public class UnitSelectionSystem : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask unitLayerMask = -1;
    public Material movementRangeMaterial;
    public Color movementRangeColor = Color.blue;
    public float highlightHeight = 0.02f;
    
    [Header("Movement Settings")]
    public int defaultMovementRange = 3;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private Camera gameCamera;
    private GameObject selectedUnit;
    private UnitPlacementManager placementManager;
    private SimpleUnitSelector unitSelector;
    private GridOverlayManager gridManager;
    
    // Movement range visualization
    private List<GameObject> movementHighlights = new List<GameObject>();
    private List<Vector2Int> validMovePositions = new List<Vector2Int>();
    
    // Events
    public System.Action<GameObject> OnUnitSelected;
    public System.Action OnUnitDeselected;
    
    void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        
        if (movementRangeMaterial == null)
        {
            CreateDefaultMovementMaterial();
        }
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
        
        if (enableDebugLogs)
        {
            Debug.Log("Trying to select unit...");
        }
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, unitLayerMask))
        {
            GameObject clickedObject = hit.collider.gameObject;
            UnitGridInfo unitInfo = clickedObject.GetComponent<UnitGridInfo>();
            
            if (enableDebugLogs)
            {
                Debug.Log($"Hit object: {clickedObject.name}, Has UnitGridInfo: {unitInfo != null}, Tag: {clickedObject.tag}");
            }
            
            if (unitInfo != null)
            {
                // Check if it's a player unit (not enemy)
                if (clickedObject.CompareTag("Player"))
                {
                    SelectUnit(clickedObject);
                }
                else
                {
                    if (enableDebugLogs)
                    {
                        Debug.Log($"Unit tag is '{clickedObject.tag}', expected 'Player'");
                    }
                    SimpleMessageLog.Log("Cannot select enemy units!");
                }
            }
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log("No unit hit by raycast");
            }
            // Clicked on empty space
            DeselectUnit();
        }
    }
    
    public void SelectUnit(GameObject unit)
    {
        if (unit == selectedUnit) return; // Already selected
        
        if (enableDebugLogs)
        {
            Debug.Log($"Selecting unit: {unit.name}");
        }
        
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
            
            if (enableDebugLogs)
            {
                Debug.Log($"Unit grid position: {unitInfo.gridPosition}, Ground object: {unitInfo.groundObject.name}");
            }
            
            // Calculate and show movement range
            CalculateMovementRange(unitInfo);
            ShowMovementRange();
            
            OnUnitSelected?.Invoke(unit);
        }
    }
    
    public void DeselectUnit()
    {
        if (selectedUnit == null) return;
        
        SimpleMessageLog.Log("Unit deselected");
        
        selectedUnit = null;
        
        // Clear valid positions when deselecting
        validMovePositions.Clear();
        HideMovementRange();
        
        OnUnitDeselected?.Invoke();
    }
    
    void CalculateMovementRange(UnitGridInfo unitInfo)
    {
        validMovePositions.Clear();
        
        Vector2Int unitGridPos = unitInfo.gridPosition;
        GameObject groundObject = unitInfo.groundObject;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Calculating movement range from position {unitGridPos} on {groundObject.name}");
        }
        
        // Use flood fill algorithm to find all reachable positions within movement range
        Queue<MovementNode> toCheck = new Queue<MovementNode>();
        HashSet<string> visited = new HashSet<string>();
        
        // Start from unit's current position
        toCheck.Enqueue(new MovementNode { gridPos = unitGridPos, groundObject = groundObject, movementCost = 0 });
        
        while (toCheck.Count > 0)
        {
            MovementNode current = toCheck.Dequeue();
            string posKey = GetPositionKey(current.gridPos, current.groundObject);
            
            if (visited.Contains(posKey)) continue;
            visited.Add(posKey);
            
            // Add this position to valid moves (except starting position)
            if (current.movementCost > 0)
            {
                validMovePositions.Add(current.gridPos);
                
                if (enableDebugLogs && validMovePositions.Count <= 5) // Only log first few to avoid spam
                {
                    Debug.Log($"Added valid move position: {current.gridPos} (cost: {current.movementCost})");
                }
            }
            
            // Check adjacent positions if we haven't reached max movement
            if (current.movementCost < defaultMovementRange)
            {
                CheckAdjacentPositions(current, toCheck, visited);
            }
        }
        
        Debug.Log($"Found {validMovePositions.Count} valid movement positions");
        
        if (enableDebugLogs)
        {
            Debug.Log($"Valid positions list size after calculation: {validMovePositions.Count}");
        }
    }
    
    void CheckAdjacentPositions(MovementNode current, Queue<MovementNode> toCheck, HashSet<string> visited)
    {
        Vector2Int[] directions = {
            new Vector2Int(1, 0),   // Right
            new Vector2Int(-1, 0),  // Left
            new Vector2Int(0, 1),   // Up
            new Vector2Int(0, -1),  // Down
        };
        
        foreach (Vector2Int direction in directions)
        {
            Vector2Int newPos = current.gridPos + direction;
            
            // Check if position is valid and not occupied
            if (IsValidMovePosition(newPos, current.groundObject))
            {
                string posKey = GetPositionKey(newPos, current.groundObject);
                if (!visited.Contains(posKey))
                {
                    toCheck.Enqueue(new MovementNode 
                    { 
                        gridPos = newPos, 
                        groundObject = current.groundObject, 
                        movementCost = current.movementCost + 1 
                    });
                }
            }
            
            // Also check same position on other ground objects
            CheckPositionOnOtherGrounds(newPos, current.movementCost + 1, toCheck, visited);
        }
    }
    
    void CheckPositionOnOtherGrounds(Vector2Int gridPos, int movementCost, Queue<MovementNode> toCheck, HashSet<string> visited)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & placementManager.groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                if (IsValidMovePosition(gridPos, obj))
                {
                    string posKey = GetPositionKey(gridPos, obj);
                    if (!visited.Contains(posKey))
                    {
                        toCheck.Enqueue(new MovementNode 
                        { 
                            gridPos = gridPos, 
                            groundObject = obj, 
                            movementCost = movementCost 
                        });
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
        
        // Check if position is occupied
        if (placementManager.IsTileOccupied(groundObject, gridPos))
            return false;
        
        // Check if position is reachable (height check)
        SimpleHeightCheck heightChecker = FindFirstObjectByType<SimpleHeightCheck>();
        if (heightChecker != null && !heightChecker.IsPositionReachable(gridPos, groundObject))
            return false;
        
        return true;
    }
    
    string GetPositionKey(Vector2Int gridPos, GameObject groundObject)
    {
        return $"{groundObject.GetInstanceID()}_{gridPos.x}_{gridPos.y}";
    }
    
    void ShowMovementRange()
    {
        HideMovementRange(); // Clear existing highlights
        
        if (enableDebugLogs)
        {
            Debug.Log($"Showing movement range for {validMovePositions.Count} positions");
        }
        
        foreach (Vector2Int gridPos in validMovePositions)
        {
            // Find which ground object this position belongs to
            GameObject groundObj = FindGroundObjectForPosition(gridPos);
            if (groundObj != null)
            {
                Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
                worldPos.y += highlightHeight;
                
                if (enableDebugLogs)
                {
                    Debug.Log($"Creating highlight at world pos: {worldPos} for grid pos: {gridPos}");
                }
                
                GameObject highlight = CreateMovementHighlight(worldPos);
                movementHighlights.Add(highlight);
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Could not find ground object for position {gridPos}");
                }
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Created {movementHighlights.Count} movement highlights");
        }
    }
    
    GameObject FindGroundObjectForPosition(Vector2Int gridPos)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & placementManager.groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                if (gridManager.IsValidGridPosition(gridPos, obj))
                {
                    return obj;
                }
            }
        }
        
        return null;
    }
    
    GameObject CreateMovementHighlight(Vector3 position)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        highlight.name = "MovementHighlight";
        highlight.transform.position = position;
        highlight.transform.localScale = new Vector3(0.8f, 0.01f, 0.8f);
        
        // Remove collider
        Destroy(highlight.GetComponent<Collider>());
        
        // Set material
        Renderer renderer = highlight.GetComponent<Renderer>();
        renderer.material = movementRangeMaterial;
        
        return highlight;
    }
    
    void HideMovementRange()
    {
        if (enableDebugLogs)
        {
            Debug.Log($"Hiding movement range. Current highlights: {movementHighlights.Count}, valid positions: {validMovePositions.Count}");
        }
        
        foreach (GameObject highlight in movementHighlights)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        movementHighlights.Clear();
        
        // DON'T clear valid positions here - we might need them
        // validMovePositions.Clear();
    }
    
    void CreateDefaultMovementMaterial()
    {
        movementRangeMaterial = new Material(Shader.Find("Standard"));
        movementRangeMaterial.SetFloat("_Mode", 3); // Transparent
        movementRangeMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        movementRangeMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        movementRangeMaterial.SetInt("_ZWrite", 0);
        movementRangeMaterial.DisableKeyword("_ALPHATEST_ON");
        movementRangeMaterial.EnableKeyword("_ALPHABLEND_ON");
        movementRangeMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        movementRangeMaterial.renderQueue = 3000;
        
        Color color = movementRangeColor;
        color.a = 0.6f;
        movementRangeMaterial.color = color;
    }
    
    // Public getters
    public GameObject GetSelectedUnit() => selectedUnit;
    public bool HasSelectedUnit() => selectedUnit != null;
    public List<Vector2Int> GetValidMovePositions() => new List<Vector2Int>(validMovePositions);
}

[System.Serializable]
public class MovementNode
{
    public Vector2Int gridPos;
    public GameObject groundObject;
    public int movementCost;
}