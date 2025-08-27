using System.Collections.Generic;
using UnityEngine;

public class UnitPlacementManager : MonoBehaviour
{
    [Header("Placement Settings")]
    public GameObject playerPrefab;
    public KeyCode activationKey = KeyCode.P;
    public LayerMask groundLayerMask = 1; // Ground layer for walking/collision detection
    public LayerMask gridLayerMask = 1; // Grid layer for grid objects
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
    private ModeManager modeManager;
    private SimpleUnitSelector unitSelector;
    private SimpleHeightCheck heightCheck;
    private TerrainHeightDetector terrainHeightDetector;
    private GameObject highlightObject;
    private Vector2Int currentGridPos = Vector2Int.one * -1;
    private GameObject currentGroundObject;
    
    // Simple tile occupation tracking
    private Dictionary<string, bool> tileOccupation = new Dictionary<string, bool>();
    private Dictionary<string, GameObject> tileUnits = new Dictionary<string, GameObject>();
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        gameCamera = Camera.main;
        modeManager = FindFirstObjectByType<ModeManager>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        heightCheck = FindFirstObjectByType<SimpleHeightCheck>();
        terrainHeightDetector = FindFirstObjectByType<TerrainHeightDetector>();
        
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        if (gridManager == null)
        {
            Debug.LogError("GridOverlayManager not found!");
        }
        
        if (unitSelector == null)
        {
            Debug.LogError("SimpleUnitSelector not found!");
        }
        
        if (heightCheck == null)
        {
            Debug.LogError("SimpleHeightCheck not found!");
        }
        
        if (terrainHeightDetector == null)
        {
            Debug.LogWarning("TerrainHeightDetector not found - units may not follow terrain height properly!");
        }
        
        if (gridHighlightPrefab == null)
        {
            CreateDefaultHighlight();
        }
        
        InitializeTileSystem();
    }
    
    void InitializeTileSystem()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            // Check both ground and grid layers for tile initialization
            if ((((1 << obj.layer) & groundLayerMask) != 0 || ((1 << obj.layer) & gridLayerMask) != 0) && obj.GetComponent<Renderer>() != null)
            {
                InitializeGroundTiles(obj);
            }
        }
        
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
    
    public bool IsTileOccupied(GameObject groundObj, Vector2Int gridPos)
    {
        string tileKey = GetTileKey(groundObj, gridPos);
        
        if (tileOccupation.ContainsKey(tileKey))
        {
            return tileOccupation[tileKey];
        }
        
        return true; // If tile not found, treat as occupied for safety
    }
    
    public void SetTileOccupied(GameObject groundObj, Vector2Int gridPos, bool occupied, GameObject unit = null)
    {
        string tileKey = GetTileKey(groundObj, gridPos);
        tileOccupation[tileKey] = occupied;
        
        if (occupied && unit != null)
        {
            tileUnits[tileKey] = unit;
        }
        else
        {
            if (tileUnits.ContainsKey(tileKey))
                tileUnits.Remove(tileKey);
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
                return true;
            }
        }
        
        Collider[] overlapping = Physics.OverlapSphere(worldPos + Vector3.up * heightOffset, capsuleRadius, obstructionLayerMask);
        
        foreach (Collider col in overlapping)
        {
            if (col.gameObject != groundObj)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void Update()
    {
        if (modeManager != null)
        {
            if (modeManager.IsInPlacementMode())
            {
                HandlePlacementInput();
                HandlePlacementPreview();
            }
            else
            {
                // Make sure highlight is hidden when not in placement mode
                HideHighlight();
            }
        }
    }
    
    void HandlePlacementInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            AttemptUnitPlacement();
        }
        
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            if (modeManager != null)
            {
                modeManager.SetMode(GameMode.Explore);
            }
        }
    }
    
    public void OnEnterPlacementMode()
    {
        // Check if trying to enter placement mode when max units are already placed
        if (unitSelector != null && !unitSelector.CanPlaceMoreUnits())
        {
            SimpleMessageLog.Log($"Maximum units already placed ({unitSelector.GetUnitsPlaced()}/{unitSelector.GetMaxUnits()})");
            if (modeManager != null)
            {
                modeManager.SetMode(GameMode.Explore);
            }
            return;
        }
        
        SimpleMessageLog.Log("Placement mode activated - Click to place unit");
    }
    
    public void OnExitPlacementMode()
    {
        HideHighlight();
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
        bool isReachable = heightCheck == null || heightCheck.IsPositionReachable(gridPos, groundObj);
        bool isValid = isValidPosition && !isOccupied && !isObstructed && isReachable;
        
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
        Debug.Log($"AttemptUnitPlacement: currentGroundObject={currentGroundObject}, playerPrefab={playerPrefab}");
        
        if (currentGroundObject == null) 
        {
            Debug.Log("Cannot place unit - no ground object");
            return;
        }
        
        if (playerPrefab == null)
        {
            Debug.Log("Cannot place unit - no prefab selected");
            return;
        }
        
        // Check unit limit first
        if (unitSelector != null && !unitSelector.CanPlaceMoreUnits())
        {
            Debug.Log("Cannot place unit - unit limit reached");
            return;
        }
        
        Vector2Int gridPos = currentGridPos;
        GameObject groundObj = currentGroundObject;
        
        bool isValidPosition = gridManager.IsValidGridPosition(gridPos, groundObj);
        bool isOccupied = IsTileOccupied(groundObj, gridPos);
        bool isObstructed = IsPositionObstructed(gridPos, groundObj);
        bool isReachable = heightCheck == null || heightCheck.IsPositionReachable(gridPos, groundObj);
        
        Debug.Log($"Placement validation - Valid:{isValidPosition}, Occupied:{isOccupied}, Obstructed:{isObstructed}, Reachable:{isReachable}");
        
        if (isValidPosition && !isOccupied && !isObstructed && isReachable)
        {
            SetTileOccupied(groundObj, gridPos, true);
            CreateUnit(gridPos, groundObj);
            
            // Only exit placement mode automatically if not on a battle map
            // On battle maps, stay in placement mode until 4 units are placed or StartBattleBtn is clicked
            if (exitModeAfterPlacement && modeManager != null)
            {
                if (!modeManager.isBattleMap)
                {
                    modeManager.SetMode(GameMode.Explore);
                }
                else
                {
                    // On battle maps, only exit if we've reached max units
                    if (unitSelector != null && !unitSelector.CanPlaceMoreUnits())
                    {
                        modeManager.SetMode(GameMode.Explore);
                        SimpleMessageLog.Log("Maximum units placed - switching to Explore mode. Click Start Battle to begin!");
                    }
                }
            }
        }
        else
        {
            Debug.Log("Unit placement failed - validation checks failed");
        }
    
    }
    
    void CreateUnit(Vector2Int gridPos, GameObject groundObj)
    {
        Vector3 worldPos;
        
        // Always use grid manager for basic position, then adjust height if needed
        worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
        
        // Optionally use terrain height detector to get more accurate height
        if (terrainHeightDetector != null)
        {
            float accurateHeight = terrainHeightDetector.GetGroundHeightAtGridPosition(gridPos, groundObj);
            worldPos.y = accurateHeight;
        }
        
        float heightOffset = CalculateUnitHeightOffset();
        worldPos.y += heightOffset;
        
        // Debug logging to help identify placement issues
        Debug.Log($"Placing unit at grid {gridPos} -> world {worldPos} on {groundObj.name}");
        
        GameObject newUnit = Instantiate(playerPrefab, worldPos, Quaternion.identity);
        SetTileOccupied(groundObj, gridPos, true, newUnit);
        
        UnitGridInfo unitInfo = newUnit.AddComponent<UnitGridInfo>();
        unitInfo.gridPosition = gridPos;
        unitInfo.groundObject = groundObj;
        unitInfo.placementManager = this;
        
        // Notify unit selector that a unit was placed
        if (unitSelector != null)
        {
            unitSelector.OnUnitPlaced();
        }
        
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
        return ((1 << obj.layer) & groundLayerMask) != 0 || ((1 << obj.layer) & gridLayerMask) != 0;
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
            
            // Notify unit selector that a unit was removed
            if (unitSelector != null)
            {
                unitSelector.OnUnitRemoved();
            }
            
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
            // Only decrement unit count if this is a player unit
            if (gameObject.CompareTag("Player"))
            {
                // Get the unit selector to notify of removal
                SimpleUnitSelector unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
                if (unitSelector != null)
                {
                    unitSelector.OnUnitRemoved();
                    Debug.Log($"UnitGridInfo.OnDestroy: Player unit {gameObject.name} removed - unit count decremented");
                }
            }
            else
            {
                Debug.Log($"UnitGridInfo.OnDestroy: Enemy unit {gameObject.name} removed - unit count NOT decremented");
            }
            
            // Clear the tile occupation (but don't destroy the unit since it's already being destroyed)
            placementManager.SetTileOccupied(groundObject, gridPosition, false);
        }
    }
}