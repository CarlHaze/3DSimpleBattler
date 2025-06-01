using UnityEngine;
using System.Collections.Generic;

public class GridMovementHighlighter : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask unitLayerMask = -1;
    public int movementRange = 3;
    public Color movementColor = Color.blue;
    public float highlightAlpha = 0.5f;
    
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
        
        int highlightCount = 0;
        
        // Check each position within movement range using Manhattan distance
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
                
                // Check if this position is valid to move to
                if (IsValidMovePosition(checkPos, unitGround))
                {
                    CreateMovementHighlight(checkPos, unitGround);
                    highlightCount++;
                }
            }
        }
        
        if (enableDebugLogs)
        {
            Debug.Log($"Created {highlightCount} movement highlights");
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
    
    void CreateMovementHighlight(Vector2Int gridPos, GameObject groundObject)
    {
        // Get world position for this grid cell
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Create a simple quad to fill the grid square
        GameObject highlight = CreateQuad();
        highlight.name = $"MovementHighlight_{gridPos.x}_{gridPos.y}";
        
        // Position it at the grid cell
        highlight.transform.position = new Vector3(worldPos.x, worldPos.y + 0.01f, worldPos.z);
        highlight.transform.localScale = new Vector3(gridManager.gridSize * 0.9f, 1f, gridManager.gridSize * 0.9f);
        
        // Create material with our movement color
        Material highlightMaterial = new Material(Shader.Find("Standard"));
        highlightMaterial.SetFloat("_Mode", 3); // Transparent
        highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        highlightMaterial.SetInt("_ZWrite", 0);
        highlightMaterial.DisableKeyword("_ALPHATEST_ON");
        highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
        highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        highlightMaterial.renderQueue = 3000;
        
        Color color = movementColor;
        color.a = highlightAlpha;
        highlightMaterial.color = color;
        
        Renderer renderer = highlight.GetComponent<Renderer>();
        renderer.material = highlightMaterial;
        
        movementHighlights.Add(highlight);
    }
    
    GameObject CreateQuad()
    {
        GameObject quad = new GameObject("Quad");
        
        MeshFilter meshFilter = quad.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = quad.AddComponent<MeshRenderer>();
        
        // Create a simple quad mesh
        Mesh mesh = new Mesh();
        
        Vector3[] vertices = new Vector3[4]
        {
            new Vector3(-0.5f, 0, -0.5f), // Bottom left
            new Vector3(0.5f, 0, -0.5f),  // Bottom right
            new Vector3(-0.5f, 0, 0.5f),  // Top left
            new Vector3(0.5f, 0, 0.5f)    // Top right
        };
        
        int[] triangles = new int[6]
        {
            0, 2, 1,  // First triangle
            2, 3, 1   // Second triangle
        };
        
        Vector2[] uv = new Vector2[4]
        {
            new Vector2(0, 0),
            new Vector2(1, 0),
            new Vector2(0, 1),
            new Vector2(1, 1)
        };
        
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
        
        meshFilter.mesh = mesh;
        
        return quad;
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