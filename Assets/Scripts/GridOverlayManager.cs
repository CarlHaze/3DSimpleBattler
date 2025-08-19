using System.Collections.Generic;
using UnityEngine;

public class GridOverlayManager : MonoBehaviour
{
    [Header("Grid Settings")]
    public float gridSize = 1f;
    public Material gridMaterial;
    public string groundLayerName = "Ground";
    
    [Header("Grid Appearance")]
    public Color gridColor = Color.white;
    public float lineWidth = 0.05f;
    public float gridHeight = 0.01f; // Small offset above ground to prevent z-fighting
    
    [Header("Auto Setup")]
    public bool generateOnStart = true;
    public bool updateInRealtime = false;
    
    [Header("Height Detection")]
    public float raycastHeight = 10f; // How high above to start raycasting
    public LayerMask groundLayerMask = -1; // What layers to consider as ground
    public bool debugRaycasts = false; // Show debug info for raycasts
    
    private List<GameObject> gridObjects = new List<GameObject>();
    private int groundLayer;
    
    void Start()
    {
        groundLayer = LayerMask.NameToLayer(groundLayerName);
        
        // IMPORTANT: Set up the layer mask to include the ground layer
        groundLayerMask = 1 << groundLayer;
        
        
        if (generateOnStart)
        {
            GenerateGridForAllGroundObjects();
        }
    }
    
    void Update()
    {
        if (updateInRealtime)
        {
            ClearExistingGrids();
            GenerateGridForAllGroundObjects();
        }
    }
    
    [ContextMenu("Generate Grid")]
    public void GenerateGridForAllGroundObjects()
    {
        ClearExistingGrids();
        
        // Find all objects in the ground layer
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == groundLayer && obj.GetComponent<Renderer>() != null)
            {
                GenerateGridForObject(obj);
            }
        }
    }
    
    public void GenerateGridForObject(GameObject groundObject)
    {
        Renderer objectRenderer = groundObject.GetComponent<Renderer>();
        if (objectRenderer == null) return;
        
        Bounds bounds = objectRenderer.bounds;
        
        // Calculate grid dimensions
        Vector3 size = bounds.size;
        int gridCountX = Mathf.CeilToInt(size.x / gridSize);
        int gridCountZ = Mathf.CeilToInt(size.z / gridSize);
        
        // Create parent object for this ground's grid
        GameObject gridParent = new GameObject($"Grid_{groundObject.name}");
        gridParent.transform.SetParent(this.transform);
        gridObjects.Add(gridParent);
        
        // Generate grid lines with proper height detection
        GenerateGridLines(gridParent, bounds, gridCountX, gridCountZ, groundObject);
    }
    
    private void GenerateGridLines(GameObject parent, Bounds bounds, int gridCountX, int gridCountZ, GameObject groundObject)
    {
        // For complex terrain, create flat grid squares instead of jagged lines
        // This creates a clean grid overlay that's easier to read
        
        for (int x = 0; x < gridCountX; x++)
        {
            for (int z = 0; z < gridCountZ; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                
                // Check if this position has ground
                if (HasGroundAt(gridPos, groundObject))
                {
                    CreateGridSquare(parent, gridPos, groundObject);
                }
            }
        }
    }
    
    private void CreateGridSquare(GameObject parent, Vector2Int gridPos, GameObject groundObject)
    {
        // Get the ground position for this grid cell
        Vector3 centerPos = GetActualGroundPosition(gridPos, groundObject);
        centerPos.y += gridHeight;
        
        // Create a flat square outline for this grid cell
        float halfSize = gridSize * 0.5f;
        
        // Calculate the four corners of the square (flat at the ground height)
        Vector3[] corners = new Vector3[4]
        {
            new Vector3(centerPos.x - halfSize, centerPos.y, centerPos.z - halfSize), // Bottom-left
            new Vector3(centerPos.x + halfSize, centerPos.y, centerPos.z - halfSize), // Bottom-right
            new Vector3(centerPos.x + halfSize, centerPos.y, centerPos.z + halfSize), // Top-right
            new Vector3(centerPos.x - halfSize, centerPos.y, centerPos.z + halfSize)  // Top-left
        };
        
        // Create the four sides of the square
        CreateGridLine(parent, corners[0], corners[1], $"GridSquare_{gridPos.x}_{gridPos.y}_Bottom");
        CreateGridLine(parent, corners[1], corners[2], $"GridSquare_{gridPos.x}_{gridPos.y}_Right");
        CreateGridLine(parent, corners[2], corners[3], $"GridSquare_{gridPos.x}_{gridPos.y}_Top");
        CreateGridLine(parent, corners[3], corners[0], $"GridSquare_{gridPos.x}_{gridPos.y}_Left");
    }
    
    private void CreateGridLine(GameObject parent, Vector3 start, Vector3 end, string lineName)
    {
        GameObject lineObj = new GameObject(lineName);
        lineObj.transform.SetParent(parent.transform);
        
        LineRenderer lr = lineObj.AddComponent<LineRenderer>();
        lr.material = gridMaterial != null ? gridMaterial : CreateDefaultGridMaterial();
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.positionCount = 2;
        lr.useWorldSpace = true;
        lr.sortingOrder = 1;
        
        lr.SetPosition(0, start);
        lr.SetPosition(1, end);
    }
    
    private Material CreateDefaultGridMaterial()
    {
        Material mat = new Material(Shader.Find("Sprites/Default"));
        mat.color = gridColor;
        return mat;
    }
    
    [ContextMenu("Clear Grid")]
    public void ClearExistingGrids()
    {
        foreach (GameObject gridObj in gridObjects)
        {
            if (gridObj != null)
            {
                if (Application.isPlaying)
                    Destroy(gridObj);
                else
                    DestroyImmediate(gridObj);
            }
        }
        gridObjects.Clear();
    }
    
    [ContextMenu("Test Ground Detection")]
    public void TestGroundDetection()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (obj.layer == groundLayer && obj.GetComponent<Renderer>() != null)
            {
                
                // Test a few grid positions
                for (int x = 0; x < 3; x++)
                {
                    for (int z = 0; z < 3; z++)
                    {
                        Vector2Int testPos = new Vector2Int(x, z);
                        Vector3 groundPos = GetActualGroundPosition(testPos, obj);
                        bool hasGround = HasGroundAt(testPos, obj);
                        
                    }
                }
                break; // Only test first ground object
            }
        }
    }
    
    // Get grid position from world position
    public Vector2Int WorldToGridPosition(Vector3 worldPos, GameObject groundObject)
    {
        Renderer objectRenderer = groundObject.GetComponent<Renderer>();
        if (objectRenderer == null) return Vector2Int.zero;
        
        Bounds bounds = objectRenderer.bounds;
        Vector3 localPos = worldPos - bounds.min;
        
        int gridX = Mathf.FloorToInt(localPos.x / gridSize);
        int gridZ = Mathf.FloorToInt(localPos.z / gridSize);
        
        return new Vector2Int(gridX, gridZ);
    }
    
    // FIXED: Get world position from grid position with proper height detection
    public Vector3 GridToWorldPosition(Vector2Int gridPos, GameObject groundObject)
    {
        return GetActualGroundPosition(gridPos, groundObject);
    }
    
    // NEW: Get the actual ground position by raycasting
    private Vector3 GetActualGroundPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Renderer objectRenderer = groundObject.GetComponent<Renderer>();
        if (objectRenderer == null) return Vector3.zero;
        
        Bounds bounds = objectRenderer.bounds;
        
        // Calculate the X and Z position
        float worldX = bounds.min.x + (gridPos.x * gridSize) + (gridSize * 0.5f);
        float worldZ = bounds.min.z + (gridPos.y * gridSize) + (gridSize * 0.5f);
        
        // Start raycast from well above the object
        Vector3 rayStart = new Vector3(worldX, bounds.max.y + raycastHeight, worldZ);
        RaycastHit hit;
        
        if (debugRaycasts)
        {
        }
        
        // Try multiple raycast approaches to find the ground
        
        // Method 1: Raycast against the specific ground object's collider
        Collider groundCollider = groundObject.GetComponent<Collider>();
        if (groundCollider != null)
        {
            Ray ray = new Ray(rayStart, Vector3.down);
            if (groundCollider.Raycast(ray, out hit, raycastHeight + bounds.size.y))
            {
                if (debugRaycasts)
                {
                }
                return hit.point;
            }
        }
        
        // Method 2: Raycast against all objects in the ground layer
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastHeight + bounds.size.y, groundLayerMask))
        {
            if (debugRaycasts)
            {
            }
            return hit.point;
        }
        
        // Method 3: Raycast against everything and check if it's the ground object
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastHeight + bounds.size.y))
        {
            if (hit.collider.gameObject == groundObject)
            {
                if (debugRaycasts)
                {
                }
                return hit.point;
            }
        }
        
        if (debugRaycasts)
        {
        }
        
        // Fallback: Use the bounds max as before
        return new Vector3(worldX, bounds.max.y, worldZ);
    }
    
    // NEW: Get the actual ground height at a specific grid position
    public float GetGroundHeightAt(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 groundPos = GetActualGroundPosition(gridPos, groundObject);
        return groundPos.y;
    }
    
    // NEW: Check if there's actually walkable ground at this position
    public bool HasGroundAt(Vector2Int gridPos, GameObject groundObject)
    {
        Renderer objectRenderer = groundObject.GetComponent<Renderer>();
        if (objectRenderer == null) return false;
        
        Bounds bounds = objectRenderer.bounds;
        
        // Calculate the X and Z position
        float worldX = bounds.min.x + (gridPos.x * gridSize) + (gridSize * 0.5f);
        float worldZ = bounds.min.z + (gridPos.y * gridSize) + (gridSize * 0.5f);
        
        // Start raycast from well above the object
        Vector3 rayStart = new Vector3(worldX, bounds.max.y + raycastHeight, worldZ);
        RaycastHit hit;
        
        // Try the same methods as GetActualGroundPosition
        
        // Method 1: Check against the specific ground object's collider
        Collider groundCollider = groundObject.GetComponent<Collider>();
        if (groundCollider != null)
        {
            Ray ray = new Ray(rayStart, Vector3.down);
            if (groundCollider.Raycast(ray, out hit, raycastHeight + bounds.size.y))
            {
                return true;
            }
        }
        
        // Method 2: Check against ground layer mask
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastHeight + bounds.size.y, groundLayerMask))
        {
            return true;
        }
        
        // Method 3: Check against everything and verify it's the ground object
        if (Physics.Raycast(rayStart, Vector3.down, out hit, raycastHeight + bounds.size.y))
        {
            if (hit.collider.gameObject == groundObject)
            {
                return true;
            }
        }
        
        return false;
    }
    
    // Check if a grid position is valid for a ground object
    public bool IsValidGridPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Renderer objectRenderer = groundObject.GetComponent<Renderer>();
        if (objectRenderer == null) return false;
        
        Bounds bounds = objectRenderer.bounds;
        int maxGridX = Mathf.CeilToInt(bounds.size.x / gridSize);
        int maxGridZ = Mathf.CeilToInt(bounds.size.z / gridSize);
        
        // Check if within grid bounds
        bool inBounds = gridPos.x >= 0 && gridPos.x < maxGridX && gridPos.y >= 0 && gridPos.y < maxGridZ;
        
        // Also check if there's actually ground at this position
        return inBounds && HasGroundAt(gridPos, groundObject);
    }
}

// Optional: Grid cell data structure for game logic
[System.Serializable]
public class GridCell
{
    public Vector2Int gridPosition;
    public Vector3 worldPosition;
    public bool isOccupied;
    public GameObject occupyingObject;
    public GameObject groundObject;
    
    public GridCell(Vector2Int gridPos, Vector3 worldPos, GameObject ground)
    {
        gridPosition = gridPos;
        worldPosition = worldPos;
        groundObject = ground;
        isOccupied = false;
        occupyingObject = null;
    }
}

// Component to attach to individual ground objects
[RequireComponent(typeof(Renderer))]
public class GroundGridObject : MonoBehaviour
{
    [Header("Individual Grid Settings")]
    public bool autoGenerateGrid = true;
    public bool customGridSize = false;
    public float customSize = 1f;
    
    private GridOverlayManager gridManager;
    
    void Start()
    {
        // Find the grid manager in the scene
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        
        if (autoGenerateGrid && gridManager != null)
        {
            // Small delay to ensure the object is fully initialized
            Invoke(nameof(GenerateMyGrid), 0.1f);
        }
    }
    
    private void GenerateMyGrid()
    {
        if (gridManager != null)
        {
            // Temporarily override grid size if custom size is set
            float originalSize = gridManager.gridSize;
            if (customGridSize)
            {
                gridManager.gridSize = customSize;
            }
            
            gridManager.GenerateGridForObject(this.gameObject);
            
            // Restore original grid size
            if (customGridSize)
            {
                gridManager.gridSize = originalSize;
            }
        }
    }
    
    // For editor use - draws grid preview in scene view
    void OnDrawGizmosSelected()
    {
        Renderer objectRenderer = GetComponent<Renderer>();
        if (objectRenderer != null)
        {
            Bounds bounds = objectRenderer.bounds;
            float size = customGridSize ? customSize : 1f;
            
            Gizmos.color = Color.yellow;
            
            // Draw grid preview in scene view
            int gridCountX = Mathf.CeilToInt(bounds.size.x / size);
            int gridCountZ = Mathf.CeilToInt(bounds.size.z / size);
            
            Vector3 startPos = bounds.min;
            float topY = bounds.max.y + 0.01f;
            
            // Draw vertical lines
            for (int x = 0; x <= gridCountX; x++)
            {
                Vector3 start = new Vector3(startPos.x + (x * size), topY, startPos.z);
                Vector3 end = new Vector3(startPos.x + (x * size), topY, startPos.z + (gridCountZ * size));
                Gizmos.DrawLine(start, end);
            }
            
            // Draw horizontal lines
            for (int z = 0; z <= gridCountZ; z++)
            {
                Vector3 start = new Vector3(startPos.x, topY, startPos.z + (z * size));
                Vector3 end = new Vector3(startPos.x + (gridCountX * size), topY, startPos.z + (z * size));
                Gizmos.DrawLine(start, end);
            }
        }
    }
}