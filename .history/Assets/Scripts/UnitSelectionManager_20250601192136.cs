using UnityEngine;
using System.Collections.Generic;

public class UnitSelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public Material selectionRingMaterial;
    public Color selectionColor = Color.yellow;
    public float ringHeight = 0.1f;
    public float ringScale = 1.2f;
    
    [Header("Movement Settings")]
    public int movementRange = 3;
    public Material movementHighlightMaterial;
    public Color validMoveColor = Color.blue;
    public Color invalidMoveColor = Color.red;
    public KeyCode deselectKey = KeyCode.Escape;
    
    private GridOverlayManager gridManager;
    private UnitPlacementManager placementManager;
    private Camera gameCamera;
    
    // Selection tracking
    private GameObject selectedUnit;
    private GameObject selectionRing;
    private UnitGridInfo selectedUnitInfo;
    
    // Movement highlighting
    private List<GameObject> movementHighlights = new List<GameObject>();
    private bool showingMovementRange = false;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        gameCamera = Camera.main;
        
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        if (gridManager == null)
        {
            Debug.LogError("GridOverlayManager not found!");
        }
        
        if (placementManager == null)
        {
            Debug.LogError("UnitPlacementManager not found!");
        }
        
        CreateSelectionRing();
        CreateMovementHighlightMaterials();
    }
    
    void Update()
    {
        HandleInput();
    }
    
    void HandleInput()
    {
        if (Input.GetKeyDown(deselectKey))
        {
            DeselectUnit();
            return;
        }
        
        if (Input.GetMouseButtonDown(0))
        {
            // Don't interfere with placement mode
            if (placementManager != null && placementManager.enabled)
                return;
                
            HandleMouseClick();
        }
    }
    
    void HandleMouseClick()
    {
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        // Try to click on movement highlight first
        if (showingMovementRange && Physics.Raycast(ray, out hit))
        {
            MovementHighlight highlight = hit.collider.GetComponent<MovementHighlight>();
            if (highlight != null && highlight.isValid)
            {
                MoveSelectedUnit(highlight.gridPosition, highlight.groundObject);
                return;
            }
        }
        
        // Try to select a unit
        if (Physics.Raycast(ray, out hit))
        {
            UnitGridInfo unitInfo = hit.collider.GetComponent<UnitGridInfo>();
            if (unitInfo != null)
            {
                SelectUnit(hit.collider.gameObject);
                return;
            }
        }
        
        // Click on empty space - deselect
        DeselectUnit();
    }
    
    public void SelectUnit(GameObject unit)
    {
        if (selectedUnit == unit) return; // Already selected
        
        // Deselect previous unit
        if (selectedUnit != null)
        {
            DeselectUnit();
        }
        
        selectedUnit = unit;
        selectedUnitInfo = unit.GetComponent<UnitGridInfo>();
        
        if (selectedUnitInfo == null)
        {
            Debug.LogError("Selected unit doesn't have UnitGridInfo component!");
            selectedUnit = null;
            return;
        }
        
        ShowSelectionRing();
        ShowMovementRange();
        
        Debug.Log($"Selected unit at grid position {selectedUnitInfo.gridPosition}");
    }
    
    public void DeselectUnit()
    {
        selectedUnit = null;
        selectedUnitInfo = null;
        HideSelectionRing();
        HideMovementRange();
        
        Debug.Log("Unit deselected");
    }
    
    void ShowSelectionRing()
    {
        if (selectedUnit == null || selectionRing == null) return;
        
        Vector3 unitPos = selectedUnit.transform.position;
        unitPos.y += ringHeight;
        
        selectionRing.transform.position = unitPos;
        selectionRing.transform.localScale = Vector3.one * ringScale;
        selectionRing.SetActive(true);
    }
    
    void HideSelectionRing()
    {
        if (selectionRing != null)
        {
            selectionRing.SetActive(false);
        }
    }
    
    void ShowMovementRange()
    {
        if (selectedUnitInfo == null || gridManager == null) return;
        
        HideMovementRange(); // Clear existing highlights
        
        Vector2Int currentPos = selectedUnitInfo.gridPosition;
        GameObject groundObj = selectedUnitInfo.groundObject;
        
        Debug.Log($"Showing movement range from {currentPos} on {groundObj.name}");
        
        // Calculate all positions within movement range using Manhattan distance
        for (int x = -movementRange; x <= movementRange; x++)
        {
            for (int z = -movementRange; z <= movementRange; z++)
            {
                // Skip the current position
                if (x == 0 && z == 0) continue;
                
                // Check if position is within movement range using Manhattan distance
                int distance = Mathf.Abs(x) + Mathf.Abs(z);
                if (distance > movementRange) continue;
                
                Vector2Int targetPos = currentPos + new Vector2Int(x, z);
                
                // Check if position is valid on the SAME ground object
                if (IsValidMovePosition(targetPos, groundObj))
                {
                    CreateMovementHighlight(targetPos, groundObj, true);
                }
                else if (gridManager.IsValidGridPosition(targetPos, groundObj))
                {
                    // Position is on grid but invalid (occupied/obstructed)
                    CreateMovementHighlight(targetPos, groundObj, false);
                }
            }
        }
        
        showingMovementRange = true;
        Debug.Log($"Created {movementHighlights.Count} movement highlights");
    }
    
    bool IsValidMovePosition(Vector2Int gridPos, GameObject groundObj)
    {
        // Check if position is within grid bounds
        if (!gridManager.IsValidGridPosition(gridPos, groundObj))
            return false;
        
        // Check if position is occupied
        if (placementManager.IsTileOccupied(groundObj, gridPos))
            return false;
        
        // Check for obstructions (you might want to add this method to UnitPlacementManager)
        // For now, we'll assume it's valid if not occupied
        return true;
    }
    
    void CreateMovementHighlight(Vector2Int gridPos, GameObject groundObj, bool isValid)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
        worldPos.y += 0.05f; // Slight offset above ground
        
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        highlight.name = $"MovementHighlight_{gridPos.x}_{gridPos.y}";
        highlight.transform.position = worldPos;
        highlight.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        
        // Remove default collider and add our custom component
        Destroy(highlight.GetComponent<Collider>());
        BoxCollider newCollider = highlight.AddComponent<BoxCollider>();
        newCollider.size = new Vector3(1f, 0.1f, 1f);
        
        MovementHighlight highlightComp = highlight.AddComponent<MovementHighlight>();
        highlightComp.gridPosition = gridPos;
        highlightComp.groundObject = groundObj;
        highlightComp.isValid = isValid;
        
        // Set material and color
        Renderer renderer = highlight.GetComponent<Renderer>();
        Material mat = new Material(movementHighlightMaterial);
        mat.color = isValid ? validMoveColor : invalidMoveColor;
        renderer.material = mat;
        
        movementHighlights.Add(highlight);
        
        Debug.Log($"Created {(isValid ? "VALID" : "INVALID")} movement highlight at {worldPos} for grid {gridPos}");
    }
    
    void HideMovementRange()
    {
        foreach (GameObject highlight in movementHighlights)
        {
            if (highlight != null)
            {
                Destroy(highlight);
            }
        }
        movementHighlights.Clear();
        showingMovementRange = false;
    }
    
    void MoveSelectedUnit(Vector2Int newGridPos, GameObject groundObj)
    {
        if (selectedUnit == null || selectedUnitInfo == null) return;
        
        // Validate the move
        if (!IsValidMovePosition(newGridPos, groundObj))
        {
            Debug.Log("Invalid move position!");
            return;
        }
        
        // Clear old position
        placementManager.SetTileOccupied(selectedUnitInfo.groundObject, selectedUnitInfo.gridPosition, false);
        
        // Update unit position
        Vector3 newWorldPos = gridManager.GridToWorldPosition(newGridPos, groundObj);
        newWorldPos.y = selectedUnit.transform.position.y; // Maintain current height
        
        selectedUnit.transform.position = newWorldPos;
        
        // Update unit info
        selectedUnitInfo.gridPosition = newGridPos;
        selectedUnitInfo.groundObject = groundObj;
        
        // Mark new position as occupied
        placementManager.SetTileOccupied(groundObj, newGridPos, true, selectedUnit);
        
        // Update visual feedback
        ShowSelectionRing();
        ShowMovementRange();
        
        Debug.Log($"Unit moved to {newGridPos} on {groundObj.name}");
    }
    
    void CreateSelectionRing()
    {
        selectionRing = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        selectionRing.name = "SelectionRing";
        selectionRing.transform.localScale = new Vector3(1.2f, 0.02f, 1.2f);
        
        // Remove collider
        Destroy(selectionRing.GetComponent<Collider>());
        
        // Create material
        if (selectionRingMaterial == null)
        {
            selectionRingMaterial = new Material(Shader.Find("Standard"));
            selectionRingMaterial.SetFloat("_Mode", 3);
            selectionRingMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            selectionRingMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            selectionRingMaterial.SetInt("_ZWrite", 0);
            selectionRingMaterial.EnableKeyword("_ALPHABLEND_ON");
            selectionRingMaterial.renderQueue = 3000;
        }
        
        Material mat = new Material(selectionRingMaterial);
        Color ringColor = selectionColor;
        ringColor.a = 0.8f;
        mat.color = ringColor;
        
        selectionRing.GetComponent<Renderer>().material = mat;
        selectionRing.SetActive(false);
        DontDestroyOnLoad(selectionRing);
    }
    
    void CreateMovementHighlightMaterials()
    {
        if (movementHighlightMaterial == null)
        {
            movementHighlightMaterial = new Material(Shader.Find("Standard"));
            movementHighlightMaterial.SetFloat("_Mode", 3);
            movementHighlightMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            movementHighlightMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            movementHighlightMaterial.SetInt("_ZWrite", 0);
            movementHighlightMaterial.EnableKeyword("_ALPHABLEND_ON");
            movementHighlightMaterial.renderQueue = 3000;
            
            Color highlightColor = validMoveColor;
            highlightColor.a = 0.6f;
            movementHighlightMaterial.color = highlightColor;
        }
    }
    
    // Public getters
    public GameObject GetSelectedUnit() => selectedUnit;
    public bool HasSelectedUnit() => selectedUnit != null;
    public Vector2Int GetSelectedUnitPosition() => selectedUnitInfo != null ? selectedUnitInfo.gridPosition : Vector2Int.zero;
}

// Component for movement highlight objects
public class MovementHighlight : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public GameObject groundObject;
    [HideInInspector] public bool isValid;
}