using UnityEngine;

public class SimpleHeightCheck : MonoBehaviour
{
    [Header("Height Rules")]
    public float maxHeightFromGround = 1.5f; // Maximum height above ground level (Y=0)
    public LayerMask groundLayerMask = 1;
    
    private GridOverlayManager gridManager;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
    }
    
    public bool IsPositionReachable(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float groundHeight = GetGroundHeightAtPosition(worldPos);
        
        // Simple rule: if ground height is more than maxHeightFromGround above Y=0, it's unreachable
        return groundHeight <= maxHeightFromGround;
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