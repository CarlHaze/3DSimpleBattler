using UnityEngine;

public class HeightValidator : MonoBehaviour
{
    [Header("Height Rules")]
    public float maxClimbHeight = 1.5f; // Maximum height units can climb (like steps)
    public float searchRadius = 2f; // How far to look for adjacent reachable positions
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
        
        // Check if there's at least one adjacent position that's reachable
        Vector2Int[] adjacentPositions = {
            new Vector2Int(gridPos.x + 1, gridPos.y),
            new Vector2Int(gridPos.x - 1, gridPos.y),
            new Vector2Int(gridPos.x, gridPos.y + 1),
            new Vector2Int(gridPos.x, gridPos.y - 1),
            // Also check diagonal positions
            new Vector2Int(gridPos.x + 1, gridPos.y + 1),
            new Vector2Int(gridPos.x + 1, gridPos.y - 1),
            new Vector2Int(gridPos.x - 1, gridPos.y + 1),
            new Vector2Int(gridPos.x - 1, gridPos.y - 1)
        };
        
        foreach (Vector2Int adjPos in adjacentPositions)
        {
            if (IsValidAdjacentPosition(adjPos, groundObject, targetHeight))
            {
                return true; // Found at least one reachable adjacent position
            }
        }
        
        // If no adjacent positions are reachable, check if it's at ground level
        return IsAtGroundLevel(targetWorldPos);
    }
    
    bool IsValidAdjacentPosition(Vector2Int adjPos, GameObject groundObject, float targetHeight)
    {
        // Check if adjacent position is valid grid position
        if (!gridManager.IsValidGridPosition(adjPos, groundObject))
        {
            // Check other ground objects for this position
            return CheckPositionOnOtherGrounds(adjPos, targetHeight);
        }
        
        Vector3 adjWorldPos = gridManager.GridToWorldPosition(adjPos, groundObject);
        float adjHeight = GetGroundHeightAtPosition(adjWorldPos);
        
        // Check if height difference is climbable
        float heightDifference = Mathf.Abs(targetHeight - adjHeight);
        return heightDifference <= maxClimbHeight;
    }
    
    bool CheckPositionOnOtherGrounds(Vector2Int gridPos, float targetHeight)
    {
        // Find all ground objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                if (gridManager.IsValidGridPosition(gridPos, obj))
                {
                    Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, obj);
                    float height = GetGroundHeightAtPosition(worldPos);
                    float heightDifference = Mathf.Abs(targetHeight - height);
                    
                    if (heightDifference <= maxClimbHeight)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }
    
    bool IsAtGroundLevel(Vector3 worldPos)
    {
        // Raycast down to find the base ground level
        RaycastHit hit;
        if (Physics.Raycast(worldPos + Vector3.up * 10f, Vector3.down, out hit, 20f, groundLayerMask))
        {
            float groundLevel = hit.point.y;
            float currentHeight = GetGroundHeightAtPosition(worldPos);
            float heightAboveGround = currentHeight - groundLevel;
            
            // If it's close to ground level, consider it reachable
            return heightAboveGround <= maxClimbHeight;
        }
        
        return false;
    }
    
    float GetGroundHeightAtPosition(Vector3 worldPos)
    {
        // Raycast down from above to find the ground height at this position
        RaycastHit hit;
        Vector3 rayStart = worldPos + Vector3.up * 10f;
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, 20f, groundLayerMask))
        {
            return hit.point.y;
        }
        
        // If no ground found, return the Y position of the world pos
        return worldPos.y;
    }
    
    // Debug visualization
    void OnDrawGizmos()
    {
        if (gridManager == null) return;
        
        // Draw reachability checks for all positions (when selected)
        if (Application.isPlaying && gameObject == UnityEditor.Selection.activeGameObject)
        {
            DrawReachabilityGizmos();
        }
    }
    
    void DrawReachabilityGizmos()
    {
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                DrawGroundReachability(obj);
            }
        }
    }
    
    void DrawGroundReachability(GameObject groundObj)
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
                Vector2Int gridPos = new Vector2Int(x, z);
                Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
                
                if (IsPositionReachable(gridPos, groundObj))
                {
                    Gizmos.color = Color.green;
                }
                else
                {
                    Gizmos.color = Color.red;
                }
                
                Gizmos.DrawWireCube(worldPos + Vector3.up * 0.5f, Vector3.one * 0.3f);
            }
        }
    }
}