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
        
        if (gridManager == null)
            Debug.LogError("GridOverlayManager not found!");
        if (placementManager == null)
            Debug.LogError("UnitPlacementManager not found!");
    }
    
    void Update()
    {
        if (!isMoving)
        {
            HandleInput();
        }
    }
    
    void HandleInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            // First check if we clicked on a movement highlight
            if (selectedUnit != null && Physics.Raycast(ray, out hit))
            {
                if (TryMoveToPosition(hit.point))
                    return; // Movement handled, don't process unit selection
            }
            
            // If no movement, try to select a unit
            if (Physics.Raycast(ray, out hit))
            {
                HandleUnitSelection(hit.collider.gameObject);
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
        
        // Get unit's grid info
        UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
        if (unitInfo != null)
        {
            currentGroundObject = unitInfo.groundObject;
            ShowMovementRange(unitInfo.gridPosition, unitInfo.groundObject);
        }
        
        Debug.Log($"Selected unit: {unit.name} at position {unitInfo?.gridPosition}");
    }
    
    void DeselectUnit()
    {
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
        
        // Check if position is occupied
        if (placementManager.IsTileOccupied(groundObject, gridPos))
            return false;
        
        // Add any additional movement restrictions here
        // (e.g., terrain types, obstacles, etc.)
        
        return true;
    }
    
    void CreateMovementHighlight(Vector2Int gridPos, GameObject groundObject, bool isValid)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        highlight.name = $"MoveHighlight_{gridPos.x}_{gridPos.y}";
        highlight.transform.position = worldPos;
        highlight.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        
        // Remove collider so it doesn't interfere with raycasting
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
    
    bool TryMoveToPosition(Vector3 worldClickPos)
    {
        if (selectedUnit == null || currentGroundObject == null)
            return false;
        
        // Convert world position to grid position
        Vector2Int targetGridPos = gridManager.WorldToGridPosition(worldClickPos, currentGroundObject);
        
        // Check if this is a valid move position
        if (!validMovePositions.Contains(targetGridPos))
            return false;
        
        // Perform the move
        MoveUnit(targetGridPos);
        return true;
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
        
        Debug.Log($"Unit moved to {targetGridPos}");
        
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