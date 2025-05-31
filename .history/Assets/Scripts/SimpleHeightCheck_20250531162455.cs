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
        
        // Check all 8 adjacent positions (including diagonals)
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
        
        // Check if at least one adjacent position is climbable
        foreach (Vector2Int adjPos in adjacentPositions)
        {
            if (IsAdjacentPositionClimbable(adjPos, groundObject, targetHeight))
            {
                return true; // Found at least one climbable adjacent position
            }
        }
        
        // If no adjacent positions are climbable, check if it's at near-ground level
        return targetHeight <= maxClimbHeight;
    }
    
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