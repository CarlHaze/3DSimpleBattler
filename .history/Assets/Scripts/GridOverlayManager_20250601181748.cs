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
    
    private List<GameObject> gridObjects = new List<GameObject>();
    private int groundLayerMask;
    
    void Start()
    {
        groundLayerMask = LayerMask.NameToLayer(groundLayerName);
        
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
            if (obj.layer == groundLayerMask && obj.GetComponent<Renderer>() != null)
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
        
        // Generate grid lines
        GenerateGridLines(gridParent, bounds, gridCountX, gridCountZ);
    }
    
    private void GenerateGridLines(GameObject parent, Bounds bounds, int gridCountX, int gridCountZ)
    {
        Vector3 startPos = bounds.min;
        
        // Calculate the top surface position
        float topY = bounds.max.y + gridHeight;
        
        // Create vertical lines (along Z-axis)
        for (int x = 0; x <= gridCountX; x++)
        {
            Vector3 lineStart = new Vector3(startPos.x + (x * gridSize), topY, startPos.z);
            Vector3 lineEnd = new Vector3(startPos.x + (x * gridSize), topY, startPos.z + (gridCountZ * gridSize));
            
            CreateGridLine(parent, lineStart, lineEnd, $"GridLine_V_{x}");
        }
        
        // Create horizontal lines (along X-axis)
        for (int z = 0; z <= gridCountZ; z++)
        {
            Vector3 lineStart = new Vector3(startPos.x, topY, startPos.z + (z * gridSize));
            Vector3 lineEnd = new Vector3(startPos.x + (gridCountX * gridSize), topY, startPos.z + (z * gridSize));
            
            CreateGridLine(parent, lineStart, lineEnd, $"GridLine_H_{z}");
        }
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
    
    // Get world position from grid position
    public Vector3 GridToWorldPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Renderer objectRenderer = groundObject.GetComponent<Renderer>();
        if (objectRenderer == null) return Vector3.zero;
        
        Bounds bounds = objectRenderer.bounds;
        
        float worldX = bounds.min.x + (gridPos.x * gridSize) + (gridSize * 0.5f);
        float worldY = bounds.max.y;
        float worldZ = bounds.min.z + (gridPos.y * gridSize) + (gridSize * 0.5f);
        
        return new Vector3(worldX, worldY, worldZ);
    }
    
    // Check if a grid position is valid for a ground object
    public bool IsValidGridPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Renderer objectRenderer = groundObject.GetComponent<Renderer>();
        if (objectRenderer == null) return false;
        
        Bounds bounds = objectRenderer.bounds;
        int maxGridX = Mathf.CeilToInt(bounds.size.x / gridSize);
        int maxGridZ = Mathf.CeilToInt(bounds.size.z / gridSize);
        
        return gridPos.x >= 0 && gridPos.x < maxGridX && gridPos.y >= 0 && gridPos.y < maxGridZ;
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