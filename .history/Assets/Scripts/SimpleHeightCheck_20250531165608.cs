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
        bool result = CanPlaceHere(worldPos, groundObject);
        
        if (enableDebugLogs)
        {
            Debug.Log($"HEIGHT CHECK RESULT: {result} for position {gridPos}");
        }
        
        return result;
    }
    
    bool CanPlaceHere(Vector3 position, GameObject excludeGroundObject)
    {
        // Raycast downwards from the position to check if there's ground within reach
        // BUT exclude the ground object we're placing on
        RaycastHit[] hits;
        Vector3 rayStart = position + Vector3.up * 0.5f; // Start slightly above the position
        Vector3 rayDirection = Vector3.down;
        float rayDistance = maxDropDistance + 0.5f;
        
        if (enableDebugLogs)
        {
            Debug.Log($"Raycasting from {rayStart} down {rayDistance} units. LayerMask: {groundLayerMask}");
            Debug.Log($"Excluding ground object: {excludeGroundObject.name}");
        }
        
        // Draw debug ray if enabled
        if (showDebugRays)
        {
            Debug.DrawRay(rayStart, rayDirection * rayDistance, Color.red, 2f);
        }
        
        // Use RaycastAll to get all hits, then filter out the excluded object
        hits = Physics.RaycastAll(rayStart, rayDirection, rayDistance, groundLayerMask);
        
        if (enableDebugLogs)
        {
            Debug.Log($"Found {hits.Length} raycast hits");
        }
        
        RaycastHit closestValidHit = new RaycastHit();
        bool foundValidHit = false;
        float closestDistance = float.MaxValue;
        
        foreach (RaycastHit hit in hits)
        {
            // Skip the ground object we're placing on
            if (hit.collider.gameObject == excludeGroundObject)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"Skipping hit on excluded object: {hit.collider.name}");
                }
                continue;
            }
            
            // Find the closest valid hit
            if (hit.distance < closestDistance)
            {
                closestDistance = hit.distance;
                closestValidHit = hit;
                foundValidHit = true;
            }
        }
        
        if (foundValidHit)
        {
            // Check if the hit ground is within our acceptable drop distance
            float dropDistance = position.y - closestValidHit.point.y;
            
            if (enableDebugLogs)
            {
                Debug.Log($"VALID HIT FOUND! Hit object: {closestValidHit.collider.name}, Hit point: {closestValidHit.point.y}, Position: {position.y}, Drop distance: {dropDistance}");
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
                Debug.Log($"NO VALID HIT FOUND within {rayDistance} units (excluding {excludeGroundObject.name}) - BLOCKING PLACEMENT");
            }
            return false; // This should block placement!
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