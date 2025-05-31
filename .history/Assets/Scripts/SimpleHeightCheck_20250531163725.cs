using UnityEngine;

public class SimpleHeightCheck : MonoBehaviour
{
    [Header("Height Rules")]
    public float maxDropDistance = 1.5f; // Maximum distance to look down for ground
    public LayerMask groundLayerMask = 1;
    
    [Header("Debug")]
    public bool enableDebugLogs = true;
    public bool showDebugRays = true;
    
    private GridOverlayManager gridManager;
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        
        if (enableDebugLogs)
        {
            Debug.Log($"SimpleHeightCheck initialized. MaxDropDistance: {maxDropDistance}, GroundLayerMask: {groundLayerMask}");
        }
    }
    
    public bool IsPositionReachable(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        if (enableDebugLogs)
        {
            Debug.Log($"=== HEIGHT CHECK === Grid: {gridPos}, WorldPos: {worldPos}, GroundObject: {groundObject.name}");
        }
        
        // Your simple and effective approach!
        bool result = CanPlaceHere(worldPos);
        
        if (enableDebugLogs)
        {
            Debug.Log($"HEIGHT CHECK RESULT: {result} for position {gridPos}");
        }
        
        return result;
    }
    
    bool CanPlaceHere(Vector3 position)
    {
        // Raycast downwards from the position to check if there's ground within reach
        RaycastHit hit;
        Vector3 rayStart = position + Vector3.up * 0.5f; // Start slightly above the position
        Vector3 rayDirection = Vector3.down;
        float rayDistance = maxDropDistance + 0.5f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Raycasting from {rayStart} down {rayDistance} units. LayerMask: {groundLayerMask}");
        }
        
        // Draw debug ray if enabled
        if (showDebugRays)
        {
            Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.red, 2f);
        }
        
        if (Physics.Raycast(rayStart, rayDirection, out hit, rayDistance, groundLayerMask))
        {
            // Check if the hit ground is within our acceptable drop distance
            float dropDistance = position.y - hit.point.y;
            
            if (enableDebugLogs)
            {
                Debug.Log($"HIT FOUND! Hit object: {hit.collider.name}, Hit point: {hit.point.y}, Position: {position.y}, Drop distance: {dropDistance}");
            }
            
            bool withinDropDistance = dropDistance <= maxDropDistance;
            
            if (enableDebugLogs)
            {
                Debug.Log($"Within drop distance ({maxDropDistance}): {withinDropDistance}");
            }
            
            return withinDropDistance;
        }
        else
        {
            if (enableDebugLogs)
            {
                Debug.Log($"NO HIT FOUND within {rayDistance} units");
            }
            return false;
        }
    }
    
    // Visual debugging in scene view
    void OnDrawGizmos()
    {
        if (!showDebugRays) return;
        
        // Draw gizmos showing the layer mask objects
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & groundLayerMask) != 0)
            {
                Gizmos.color = Color.green;
                Renderer renderer = obj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Gizmos.DrawWireCube(renderer.bounds.center, renderer.bounds.size);
                }
            }
        }
    }
}