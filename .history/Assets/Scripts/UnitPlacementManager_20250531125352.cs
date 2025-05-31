using UnityEngine;
using System.Collections.Generic;

public class UnitPlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public GameObject playerPrefab;
    public KeyCode activationKey = KeyCode.P;
    public LayerMask groundLayerMask = 1;
    public LayerMask obstructionLayerMask = -1;
    public float unitHeightOffset = 0f;
    public bool exitModeAfterPlacement = true;
    
    [Header("Visual Feedback")]
    public Material highlightMaterial;
    public Color validPlacementColor = Color.blue;
    public Color invalidPlacementColor = Color.red;
    
    [Header("Grid Highlighting")]
    public GameObject gridHighlightPrefab;
    
    [HideInInspector] public GridOverlayManager gridManager;
    private Camera gameCamera;
    private bool inPlacementMode = false;
    private GameObject highlightObject;
    private Vector2Int currentGridPos = Vector2Int.one * -1;
    private GameObject currentGroundObject;
    
    // Simple tile occupation tracking
    private Dictionary<string, bool> tileOccupation = new Dictionary<string, bool>();
    private Dictionary<string, GameObject> tileUnits = new Dictionary<string, GameObject>();
    
    private SimpleUnitSelector unitSelector;

    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        gameCamera = Camera.main;
        
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        if (gridManager == null)
        {
            Debug.LogError("GridOverlayManager not found!");
        }
        
        if (gridHighlightPrefab == null)
        {
            CreateDefaultHighlight();
        }

        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        
        InitializeTileSystem();
    }
    
    void InitializeTileSystem()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                InitializeGroundTiles(obj);
            }
        }
        
        Debug.Log($"Initialized {tileOccupation.Count} tiles as empty");
    }
    
    void InitializeGroundTiles(GameObject groundObj)
    {
        Renderer renderer = groundObj.GetComponent<Renderer>();
        if (renderer == null) return;
        
        Bounds bounds = renderer.bounds;
        Vector3 size = bounds.size;
        int gridCountX = Mathf.CeilToInt(size.x / gridManager.gridSize);
        int gridCountZ = Mathf.CeilToInt(size.z / gridManager.gridSize);
        
        for (int x = 0; x < gridCountX; x++)
        {
            for (int z = 0; z < gridCountZ; z++)
            {
                string tileKey = GetTileKey(groundObj, new Vector2Int(x, z));
                tileOccupation[tileKey] = false;
            }
        }
    }
    
    string GetTileKey(GameObject groundObj, Vector2Int gridPos)
    {
        return $"{groundObj.GetInstanceID()}_{gridPos.x}_{gridPos.y}";
    }
    
    bool IsTileOccupied(GameObject groundObj, Vector2Int gridPos)
    {
        string tileKey = GetTileKey(groundObj, gridPos);
        
        if (tileOccupation.ContainsKey(tileKey))
        {
            return tileOccupation[tileKey];
        }
        
        return true; // If tile not found, treat as occupied for safety
    }
    
    void SetTileOccupied(GameObject groundObj, Vector2Int gridPos, bool occupied, GameObject unit = null)
    {
        string tileKey = GetTileKey(groundObj, gridPos);
        tileOccupation[tileKey] = occupied;
        
        if (occupied && unit != null)
        {
            tileUnits[tileKey] = unit;
            Debug.Log($"Tile {tileKey} set to OCCUPIED");
        }
        else
        {
            if (tileUnits.ContainsKey(tileKey))
                tileUnits.Remove(tileKey);
            Debug.Log($"Tile {tileKey} set to EMPTY");
        }
    }
    
    bool IsPositionObstructed(Vector2Int gridPos, GameObject groundObj)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
        float heightOffset = CalculateUnitHeightOffset();
        
        Vector3 startPos = worldPos;
        Vector3 endPos = worldPos + Vector3.up * (heightOffset * 2f);
        float capsuleRadius = 0.4f;
        
        RaycastHit[] hits = Physics.CapsuleCastAll(
            startPos, 
            endPos, 
            capsuleRadius, 
            Vector3.up, 
            0.1f, 
            obstructionLayerMask
        );
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != groundObj)
            {
                Debug.Log($"Obstruction detected: {hit.collider.gameObject.name} at {gridPos}");
                return true;
            }
        }
        
        Collider[] overlapping = Physics.OverlapSphere(worldPos + Vector3.up * heightOffset, capsuleRadius, obstructionLayerMask);
        
        foreach (Collider col in overlapping)
        {
            if (col.gameObject != groundObj)
            {
                Debug.Log($"Overlap obstruction detected: {col.gameObject.name} at {gridPos}");
                return true;
            }
        }
        
        return false;
    }
    
    void Update()
    {
        ProcessInput();
        
        if (inPlacementMode)
        {
            HandlePlacementPreview();
        }
    }
    
    void ProcessInput()
    {
        if (Input.GetKeyDown(activationKey))
        {
            ToggleMode();
        }
        
        if (inPlacementMode && Input.GetMouseButtonDown(0))
        {
            AttemptUnitPlacement();
        }
        
        if (inPlacementMode && (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape)))
        {
            ExitMode();
        }
    }
    
    void ToggleMode()
    {
        inPlacementMode = !inPlacementMode;
        
        if (inPlacementMode)
        {
            Debug.Log("Placement mode activated.");
        }
        else
        {
            ExitMode();
        }
    }
    
    void ExitMode()
    {
        inPlacementMode = false;
        HideHighlight();
        Debug.Log("Placement mode deactivated.");
    }
    
    void HandlePlacementPreview()
    {
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, groundLayerMask))
        {
            GameObject groundObj = hit.collider.gameObject;
            
            if (IsValidGroundObject(groundObj))
            {
                Vector2Int gridPos = gridManager.WorldToGridPosition(hit.point, groundObj);
                
                if (gridPos != currentGridPos || groundObj != currentGroundObject)
                {
                    RefreshHighlight(gridPos, groundObj);
                    currentGridPos = gridPos;
                    currentGroundObject = groundObj;
                }
            }
            else
            {
                HideHighlight();
            }
        }
        else
        {
            HideHighlight();
        }
    }
    
    void RefreshHighlight(Vector2Int gridPos, GameObject groundObj)
    {
        bool isValidPosition = gridManager.IsValidGridPosition(gridPos, groundObj);
        bool isOccupied = IsTileOccupied(groundObj, gridPos);
        bool isObstructed = IsPositionObstructed(gridPos, groundObj);
        bool isValid = isValidPosition && !isOccupied && !isObstructed;
        
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
        ShowHighlight(worldPos, isValid);
    }
    
    void ShowHighlight(Vector3 position, bool isValid)
    {
        if (highlightObject == null)
        {
            highlightObject = Instantiate(gridHighlightPrefab);
        }
        
        highlightObject.SetActive(true);
        highlightObject.transform.position = position;
        
        Renderer highlightRenderer = highlightObject.GetComponent<Renderer>();
        if (highlightRenderer != null && highlightMaterial != null)
        {
            Material mat = new Material(highlightMaterial);
            mat.color = isValid ? validPlacementColor : invalidPlacementColor;
            highlightRenderer.material = mat;
        }
    }
    
    void HideHighlight()
    {
        if (highlightObject != null)
        {
            highlightObject.SetActive(false);
        }
        currentGridPos = Vector2Int.one * -1;
        currentGroundObject = null;
    }
    
    void AttemptUnitPlacement()
    {
        if (currentGroundObject == null) return;
        
        Vector2Int gridPos = currentGridPos;
        GameObject groundObj = currentGroundObject;
        
        bool isValidPosition = gridManager.IsValidGridPosition(gridPos, groundObj);
        bool isOccupied = IsTileOccupied(groundObj, gridPos);
        bool isObstructed = IsPositionObstructed(gridPos, groundObj);
        
        Debug.Log($"Placement check - Valid: {isValidPosition}, Occupied: {isOccupied}, Obstructed: {isObstructed}");
        
        if (isValidPosition && !isOccupied && !isObstructed)
        {
            SetTileOccupied(groundObj, gridPos, true);
            CreateUnit(gridPos, groundObj);
            
            if (exitModeAfterPlacement)
            {
                ExitMode();
            }
        }
        else
        {
            if (!isValidPosition)
                Debug.Log("BLOCKED: Outside grid bounds!");
            else if (isOccupied)
                Debug.Log("BLOCKED: Tile occupied!");
            else if (isObstructed)
                Debug.Log("BLOCKED: Position obstructed!");
        }
    }
    
    void CreateUnit(Vector2Int gridPos, GameObject groundObj)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
        float heightOffset = CalculateUnitHeightOffset();
        worldPos.y += heightOffset;
        
        GameObject newUnit = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        SetTileOccupied(groundObj, gridPos, true, newUnit);
        
        UnitGridInfo unitInfo = newUnit.AddComponent<UnitGridInfo>();
        unitInfo.gridPosition = gridPos;
        unitInfo.groundObject = groundObj;
        unitInfo.placementManager = this;
        
        Debug.Log($"Unit created at {gridPos}");
    }
    
    float CalculateUnitHeightOffset()
    {
        if (unitHeightOffset != 0f)
        {
            return unitHeightOffset;
        }
        
        if (playerPrefab != null)
        {
            Renderer prefabRenderer = playerPrefab.GetComponent<Renderer>();
            if (prefabRenderer != null)
            {
                return prefabRenderer.bounds.size.y * 0.5f;
            }
            
            Collider prefabCollider = playerPrefab.GetComponent<Collider>();
            if (prefabCollider != null)
            {
                return prefabCollider.bounds.size.y * 0.5f;
            }
        }
        
        return 1f;
    }
    
    bool IsValidGroundObject(GameObject obj)
    {
        return ((1 << obj.layer) & groundLayerMask) != 0;
    }
    
    void CreateDefaultHighlight()
    {
        gridHighlightPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gridHighlightPrefab.name = "GridHighlight";
        gridHighlightPrefab.transform.localScale = new Vector3(0.9f, 0.05f, 0.9f);
        
        Destroy(gridHighlightPrefab.GetComponent<Collider>());
        
        if (highlightMaterial == null)
        {
            highlightMaterial = new Material(Shader.Find("Standard"));
            highlightMaterial.SetFloat("_Mode", 3);
            highlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            highlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            highlightMaterial.SetInt("_ZWrite", 0);
            highlightMaterial.DisableKeyword("_ALPHATEST_ON");
            highlightMaterial.EnableKeyword("_ALPHABLEND_ON");
            highlightMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            highlightMaterial.renderQueue = 3000;
            
            Color highlightColor = validPlacementColor;
            highlightColor.a = 0.5f;
            highlightMaterial.color = highlightColor;
        }
        
        gridHighlightPrefab.GetComponent<Renderer>().material = highlightMaterial;
        gridHighlightPrefab.SetActive(false);
        DontDestroyOnLoad(gridHighlightPrefab);
    }
    
    // Public methods
    public void RemoveUnit(Vector2Int gridPos, GameObject groundObj)
    {
        string tileKey = GetTileKey(groundObj, gridPos);
        
        if (tileUnits.ContainsKey(tileKey))
        {
            GameObject unit = tileUnits[tileKey];
            SetTileOccupied(groundObj, gridPos, false);
            
            if (unit != null)
            {
                Destroy(unit);
            }
        }
    }
    
    public GameObject GetUnitAt(Vector2Int gridPos, GameObject groundObj)
    {
        string tileKey = GetTileKey(groundObj, gridPos);
        
        if (tileUnits.ContainsKey(tileKey))
        {
            return tileUnits[tileKey];
        }
        
        return null;
    }
}

public class UnitGridInfo : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public GameObject groundObject;
    [HideInInspector] public UnitPlacementManager placementManager;
    
    void OnDestroy()
    {
        if (placementManager != null)
        {
            placementManager.RemoveUnit(gridPosition, groundObject);
        }
    }
}