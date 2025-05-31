using UnityEngine;

public class SimpleHeightCheck : MonoBehaviour
{
    [Header("Height Rules")]
    public float maxClimbHeight = 1.5f; // Maximum height difference we can climb
    public LayerMask groundLayerMask = 1;
    
    private GridOverlayManager gridManager;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
    }
    
    public bool IsPositionReachable(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 targetWorldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float targetHeight = GetGroundHeightAtPosition(targetWorldPos);
        
        // If it's at ground level (low height), it's always reachable
        if (targetHeight <= maxClimbHeight)
        {
            return true;
        }
        
        // For higher positions, check if there's a valid path from a lower position
        return HasReachableAdjacentPosition(gridPos, groundObject, targetHeight, new System.Collections.Generic.HashSet<string>());
    }
    
    bool HasReachableAdjacentPosition(Vector2Int gridPos, GameObject groundObject, float targetHeight, System.Collections.Generic.HashSet<string> visited)
    {
        // Create a unique key for this position to avoid infinite loops
        string posKey = $"{groundObject.GetInstanceID()}_{gridPos.x}_{gridPos.y}";
        if (visited.Contains(posKey))
        {
            return false;
        }
        visited.Add(posKey);
        
        // Check all 8 adjacent positions
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
            if (IsAdjacentPositionValidAndReachable(adjPos, groundObject, targetHeight, visited))
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool IsAdjacentPositionValidAndReachable(Vector2Int adjPos, GameObject groundObject, float targetHeight, System.Collections.Generic.HashSet<string> visited)
    {
        // Check on the same ground object first
        if (gridManager.IsValidGridPosition(adjPos, groundObject))
        {
            Vector3 adjWorldPos = gridManager.GridToWorldPosition(adjPos, groundObject);
            float adjHeight = GetGroundHeightAtPosition(adjWorldPos);
            
            // Calculate height difference (positive = need to climb up)
            float heightDiff = targetHeight - adjHeight;
            
            // Can climb up to maxClimbHeight, or drop down any reasonable amount
            if (heightDiff <= maxClimbHeight && heightDiff >= -5f)
            {
                // If this adjacent position is at ground level, we found a path!
                if (adjHeight <= maxClimbHeight)
                {
                    return true;
                }
                
                // Otherwise, check if this adjacent position is itself reachable
                // (but with a depth limit to prevent infinite recursion)
                if (visited.Count < 10) // Limit search depth
                {
                    return HasReachableAdjacentPosition(adjPos, groundObject, adjHeight, visited);
                }
            }
        }
        
        // Check other ground objects for this position
        return CheckOtherGroundObjectsRecursive(adjPos, targetHeight, visited);
    }
    
    bool CheckOtherGroundObjectsRecursive(Vector2Int gridPos, float targetHeight, System.Collections.Generic.HashSet<string> visited)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                if (gridManager.IsValidGridPosition(gridPos, obj))
                {
                    Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, obj);
                    float height = GetGroundHeightAtPosition(worldPos);
                    
                    // Calculate height difference (positive = need to climb up)
                    float heightDiff = targetHeight - height;
                    
                    // Can climb up to maxClimbHeight, or drop down any reasonable amount
                    if (heightDiff <= maxClimbHeight && heightDiff >= -5f)
                    {
                        // If this position is at ground level, we found a path!
                        if (height <= maxClimbHeight)
                        {
                            return true;
                        }
                        
                        // Otherwise, check if this position is itself reachable
                        if (visited.Count < 10) // Limit search depth
                        {
                            return HasReachableAdjacentPosition(gridPos, obj, height, visited);
                        }
                    }
                }
            }
        }
        
        return false;
    }
    
    // Keep the old methods for the recursive system
    bool IsAdjacentPositionClimbable(Vector2Int adjPos, GameObject groundObject, float targetHeight)
    {
        // First check on the same ground object
        if (gridManager.IsValidGridPosition(adjPos, groundObject))
        {
            Vector3 adjWorldPos = gridManager.GridToWorldPosition(adjPos, groundObject);
            float adjHeight = GetGroundHeightAtPosition(adjWorldPos);
            
            // Calculate height difference (positive = need to climb up)
            float heightDiff = targetHeight - adjHeight;
            
            // Can climb up to maxClimbHeight, or drop down any reasonable amount
            if (heightDiff <= maxClimbHeight && heightDiff >= -5f)
            {
                return true;
            }
        }
        
        // Check other ground objects for this position
        return CheckOtherGroundObjects(adjPos, targetHeight);
    }
    
    bool CheckOtherGroundObjects(Vector2Int gridPos, float targetHeight)
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                if (gridManager.IsValidGridPosition(gridPos, obj))
                {
                    Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, obj);
                    float height = GetGroundHeightAtPosition(worldPos);
                    
                    // Calculate height difference (positive = need to climb up)
                    float heightDiff = targetHeight - height;
                    
                    // Can climb up to maxClimbHeight, or drop down any reasonable amount
                    if (heightDiff <= maxClimbHeight && heightDiff >= -5f)
                    {
                        return true;
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