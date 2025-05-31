using UnityEngine;

public class SimpleHeightCheck : MonoBehaviour
{
    [Header("Height Rules")]
    public float maxDropDistance = 1.5f; // Maximum distance to look down for ground
    public LayerMask groundLayerMask = 1;
    
    private GridOverlayManager gridManager;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
    }
    
    public bool IsPositionReachable(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Your simple and effective approach!
        return CanPlaceHere(worldPos);
    }
    
    bool CanPlaceHere(Vector3 position)
    {
        // Raycast downwards from the position to check if there's ground within reach
        RaycastHit hit;
        Vector3 rayStart = position + Vector3.up * 0.5f; // Start slightly above the position
        
        if (Physics.Raycast(rayStart, Vector3.down, out hit, maxDropDistance + 0.5f, groundLayerMask))
        {
            // Check if the hit ground is within our acceptable drop distance
            float dropDistance = position.y - hit.point.y;
            return dropDistance <= maxDropDistance;
        }
        
        // If no ground found within range, not placeable
        return false;
    }
}