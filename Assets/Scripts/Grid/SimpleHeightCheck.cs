using UnityEngine;

public class SimpleHeightCheck : MonoBehaviour
{
    [Header("Height Rules")]
    public float maxHeightToCheck = 2f; // Only check positions above this height
    public float maxClimbHeight = 1.5f; // Maximum height difference we can climb
    public float maxAbsoluteHeight = 5f; // Maximum absolute height allowed for placement
    public LayerMask groundLayerMask = 1; // Ground layer for walking surfaces
    public LayerMask gridLayerMask = 1; // Grid layer for grid objects
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    
    private GridOverlayManager gridManager;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        
    }
    
    public bool IsPositionReachable(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float currentHeight = GetGroundHeightAtPosition(worldPos);
        
        // First check: is the position above the absolute maximum height?
        if (currentHeight > maxAbsoluteHeight)
        {
            if (enableDebugLogs)
                Debug.Log($"Position {gridPos} rejected: height {currentHeight:F2} exceeds maximum allowed height {maxAbsoluteHeight:F2}");
            return false;
        }
        
        // If position is not very high, it's probably reachable
        if (currentHeight <= maxHeightToCheck)
        {
            return true;
        }
        
        // For high positions, check if there's a climbable path from adjacent positions
        bool hasClimbablePath = HasClimbableAdjacentPosition(gridPos, groundObject, currentHeight);
        
        if (enableDebugLogs && !hasClimbablePath)
            Debug.Log($"Position {gridPos} rejected: no climbable path found (height: {currentHeight:F2})");
        
        return hasClimbablePath;
    }
    
    bool HasClimbableAdjacentPosition(Vector2Int gridPos, GameObject groundObject, float currentHeight)
    {
        // Check all 8 adjacent grid positions
        Vector2Int[] adjacentPositions = {
            new Vector2Int(gridPos.x + 1, gridPos.y),     // Right
            new Vector2Int(gridPos.x - 1, gridPos.y),     // Left
            new Vector2Int(gridPos.x, gridPos.y + 1),     // Up
            new Vector2Int(gridPos.x, gridPos.y - 1),     // Down
            new Vector2Int(gridPos.x + 1, gridPos.y + 1), // Top-right
            new Vector2Int(gridPos.x + 1, gridPos.y - 1), // Bottom-right
            new Vector2Int(gridPos.x - 1, gridPos.y + 1), // Top-left
            new Vector2Int(gridPos.x - 1, gridPos.y - 1)  // Bottom-left
        };
        
        foreach (Vector2Int adjPos in adjacentPositions)
        {
            if (IsAdjacentPositionClimbable(adjPos, groundObject, currentHeight))
            {
                return true;
            }
        }
        
        // Also check adjacent positions on other ground objects
        return CheckAdjacentOnOtherGrounds(gridPos, currentHeight);
    }
    
    bool IsAdjacentPositionClimbable(Vector2Int adjPos, GameObject groundObject, float currentHeight)
    {
        // Check if this adjacent position exists on the same ground object
        if (gridManager.IsValidGridPosition(adjPos, groundObject))
        {
            Vector3 adjWorldPos = gridManager.GridToWorldPosition(adjPos, groundObject);
            float adjHeight = GetGroundHeightAtPosition(adjWorldPos);
            
            // Calculate height difference (how much we need to climb UP to reach current position)
            float heightDiff = currentHeight - adjHeight;
            
            
            // If we can climb from adjacent to current (heightDiff <= maxClimbHeight)
            // AND the adjacent position is low enough to be reachable
            if (heightDiff <= maxClimbHeight && adjHeight <= maxHeightToCheck)
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool CheckAdjacentOnOtherGrounds(Vector2Int gridPos, float currentHeight)
    {
        // Get all ground objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        // Check the same grid position on other ground objects
        Vector2Int[] adjacentPositions = {
            new Vector2Int(gridPos.x + 1, gridPos.y),
            new Vector2Int(gridPos.x - 1, gridPos.y),
            new Vector2Int(gridPos.x, gridPos.y + 1),
            new Vector2Int(gridPos.x, gridPos.y - 1),
            new Vector2Int(gridPos.x + 1, gridPos.y + 1),
            new Vector2Int(gridPos.x + 1, gridPos.y - 1),
            new Vector2Int(gridPos.x - 1, gridPos.y + 1),
            new Vector2Int(gridPos.x - 1, gridPos.y - 1)
        };
        
        foreach (GameObject obj in allObjects)
        {
            // Check both ground and grid layers for height validation
            if ((((1 << obj.layer) & groundLayerMask) != 0 || ((1 << obj.layer) & gridLayerMask) != 0) && obj.GetComponent<Renderer>() != null)
            {
                foreach (Vector2Int adjPos in adjacentPositions)
                {
                    if (gridManager.IsValidGridPosition(adjPos, obj))
                    {
                        Vector3 adjWorldPos = gridManager.GridToWorldPosition(adjPos, obj);
                        float adjHeight = GetGroundHeightAtPosition(adjWorldPos);
                        
                        // Calculate height difference
                        float heightDiff = currentHeight - adjHeight;
                        
                        // If we can climb from adjacent to current AND adjacent is reachable
                        if (heightDiff <= maxClimbHeight && adjHeight <= maxHeightToCheck)
                        {
                            return true;
                        }
                    }
                }
            }
        }
        
        return false;
    }
    
    float GetGroundHeightAtPosition(Vector3 worldPos)
    {
        // Simple raycast down to find ground height
        RaycastHit hit;
        Vector3 rayStart = worldPos + Vector3.up * 5f; // Start 5 units above
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 10f, groundLayerMask))
        {
            return hit.point.y;
        }
        
        // If no ground found, return the world position Y
        return worldPos.y;
    }
}