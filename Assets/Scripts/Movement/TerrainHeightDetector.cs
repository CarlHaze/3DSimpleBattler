using UnityEngine;
using System.Collections.Generic;

public class TerrainHeightDetector : MonoBehaviour
{
    [Header("Height Detection Settings")]
    [SerializeField] private float raycastStartHeight = 20f;
    [SerializeField] private float maxRaycastDistance = 50f;
    [SerializeField] private LayerMask groundLayerMask = -1;
    [SerializeField] private float heightTolerance = 0.1f; // Used for future height comparison features
    [SerializeField] private bool debugRaycasts = false;
    [SerializeField] private bool showDebugSpheres = false;
    
    [Header("Movement Validation")]
    [SerializeField] private float maxStepHeight = 1.5f; // Maximum height units can step up
    [SerializeField] private bool allowClimbing = true; // Can units climb onto higher surfaces
    [SerializeField] private float climbCheckRadius = 0.3f; // Radius for checking climb surfaces (future feature)
    
    private GridOverlayManager gridManager;
    private Dictionary<string, float> heightCache = new Dictionary<string, float>();
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        if (gridManager == null)
        {
            Debug.LogError("TerrainHeightDetector requires GridOverlayManager in scene!");
        }
        
        // Set up layer mask if not configured
        if (groundLayerMask == -1)
        {
            groundLayerMask = 1 << LayerMask.NameToLayer("Ground");
        }
    }
    
    /// <summary>
    /// Get the exact ground height at a world position
    /// </summary>
    public float GetGroundHeightAtWorldPosition(Vector3 worldPosition)
    {
        Vector3 rayStart = new Vector3(worldPosition.x, worldPosition.y + raycastStartHeight, worldPosition.z);
        RaycastHit hit;
        
        if (debugRaycasts)
        {
            Debug.DrawRay(rayStart, Vector3.down * maxRaycastDistance, Color.red, 0.1f);
        }
        
        // Raycast down to find ground
        if (Physics.Raycast(rayStart, Vector3.down, out hit, maxRaycastDistance, groundLayerMask))
        {
            if (showDebugSpheres)
            {
                // Draw a debug sphere using Gizmos instead (will be visible in scene view)
                Debug.DrawRay(hit.point, Vector3.up * 0.2f, Color.green, 1f);
                Debug.DrawRay(hit.point, Vector3.down * 0.2f, Color.green, 1f);
                Debug.DrawRay(hit.point, Vector3.left * 0.2f, Color.green, 1f);
                Debug.DrawRay(hit.point, Vector3.right * 0.2f, Color.green, 1f);
                Debug.DrawRay(hit.point, Vector3.forward * 0.2f, Color.green, 1f);
                Debug.DrawRay(hit.point, Vector3.back * 0.2f, Color.green, 1f);
            }
            return hit.point.y;
        }
        
        if (debugRaycasts)
        {
            Debug.LogWarning($"No ground found at position {worldPosition}");
        }
        
        return worldPosition.y; // Fallback to original height
    }
    
    /// <summary>
    /// Get the ground height at a specific grid position
    /// </summary>
    public float GetGroundHeightAtGridPosition(Vector2Int gridPos, GameObject groundObject)
    {
        if (gridManager == null) return 0f;
        
        // Create cache key
        string cacheKey = $"{groundObject.GetInstanceID()}_{gridPos.x}_{gridPos.y}";
        
        // Check cache first
        if (heightCache.ContainsKey(cacheKey))
        {
            return heightCache[cacheKey];
        }
        
        // Convert grid position to world position (flat)
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Get actual height using raycast
        float actualHeight = GetGroundHeightAtWorldPosition(worldPos);
        
        // Cache the result
        heightCache[cacheKey] = actualHeight;
        
        return actualHeight;
    }
    
    /// <summary>
    /// Get the complete ground position (with correct height) for a grid position
    /// </summary>
    public Vector3 GetGroundPositionAtGridPosition(Vector2Int gridPos, GameObject groundObject)
    {
        if (gridManager == null) return Vector3.zero;
        
        // Get the flat world position first
        Vector3 flatWorldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Get the actual ground height
        float actualHeight = GetGroundHeightAtGridPosition(gridPos, groundObject);
        
        // Return position with correct height
        return new Vector3(flatWorldPos.x, actualHeight, flatWorldPos.z);
    }
    
    /// <summary>
    /// Check if a position is walkable (has ground beneath it)
    /// </summary>
    public bool IsPositionWalkable(Vector3 worldPosition)
    {
        Vector3 rayStart = new Vector3(worldPosition.x, worldPosition.y + raycastStartHeight, worldPosition.z);
        RaycastHit hit;
        
        // Check if there's ground below this position
        if (Physics.Raycast(rayStart, Vector3.down, out hit, maxRaycastDistance, groundLayerMask))
        {
            // Check if the hit point is not too far below the expected position
            float heightDifference = Mathf.Abs(hit.point.y - worldPosition.y);
            return heightDifference <= maxStepHeight;
        }
        
        return false;
    }
    
    /// <summary>
    /// Check if a unit can move from one position to another (height validation)
    /// </summary>
    public bool CanMoveToPosition(Vector3 fromPosition, Vector3 toPosition)
    {
        // Get actual ground heights at both positions
        float fromHeight = GetGroundHeightAtWorldPosition(fromPosition);
        float toHeight = GetGroundHeightAtWorldPosition(toPosition);
        
        float heightDifference = toHeight - fromHeight;
        
        // Use height tolerance for small height differences (stepping on small obstacles)
        if (Mathf.Abs(heightDifference) <= heightTolerance)
        {
            return true; // Small height differences are always allowed
        }
        
        // Check if the height difference is within acceptable limits
        if (heightDifference > maxStepHeight)
        {
            // Too high to step up
            if (!allowClimbing)
            {
                return false;
            }
            
            // Check if there's a climbable surface
            return IsClimbable(fromPosition, toPosition, heightDifference);
        }
        
        // Check if falling too far (optional - you might want units to be able to jump down)
        if (heightDifference < -maxStepHeight * 2f) // Allow falling 2x step height
        {
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// Check if a surface is climbable
    /// </summary>
    private bool IsClimbable(Vector3 fromPosition, Vector3 toPosition, float heightDifference)
    {
        // For now, simple check - can climb up to maxStepHeight * 2
        if (heightDifference <= maxStepHeight * 2f)
        {
            // Use climbCheckRadius to ensure there's solid ground around the target position
            Collider[] nearbyColliders = Physics.OverlapSphere(toPosition, climbCheckRadius, groundLayerMask);
            if (nearbyColliders.Length > 0)
            {
                return true; // Found solid ground within climb radius
            }
        }
        
        // Could add more complex checks here:
        // - Check for ramps/slopes
        // - Check for intermediate stepping stones
        // - Check surface angle
        
        return false;
    }
    
    /// <summary>
    /// Check if a grid position is accessible from another grid position
    /// </summary>
    public bool CanMoveFromGridToGrid(Vector2Int fromGridPos, Vector2Int toGridPos, GameObject groundObject)
    {
        Vector3 fromWorldPos = GetGroundPositionAtGridPosition(fromGridPos, groundObject);
        Vector3 toWorldPos = GetGroundPositionAtGridPosition(toGridPos, groundObject);
        
        return CanMoveToPosition(fromWorldPos, toWorldPos);
    }
    
    /// <summary>
    /// Get the height difference between two grid positions
    /// </summary>
    public float GetHeightDifference(Vector2Int fromGridPos, Vector2Int toGridPos, GameObject groundObject)
    {
        float fromHeight = GetGroundHeightAtGridPosition(fromGridPos, groundObject);
        float toHeight = GetGroundHeightAtGridPosition(toGridPos, groundObject);
        
        return toHeight - fromHeight;
    }
    
    /// <summary>
    /// Clear the height cache (call when terrain changes)
    /// </summary>
    public void ClearHeightCache()
    {
        heightCache.Clear();
    }
    
    /// <summary>
    /// Get path with height validation - returns positions with correct heights
    /// </summary>
    public List<Vector3> GetHeightAwarePath(Vector2Int startGrid, Vector2Int endGrid, GameObject groundObject)
    {
        List<Vector3> path = new List<Vector3>();
        
        // Simple direct path for now - you can enhance this with A* pathfinding later
        Vector2Int currentGrid = startGrid;
        Vector2Int direction = new Vector2Int(
            endGrid.x > startGrid.x ? 1 : (endGrid.x < startGrid.x ? -1 : 0),
            endGrid.y > startGrid.y ? 1 : (endGrid.y < startGrid.y ? -1 : 0)
        );
        
        // Add start position
        path.Add(GetGroundPositionAtGridPosition(currentGrid, groundObject));
        
        // Move step by step, checking height at each position
        while (currentGrid != endGrid)
        {
            Vector2Int nextGrid = currentGrid;
            
            // Move towards target
            if (currentGrid.x != endGrid.x)
            {
                nextGrid.x += direction.x;
            }
            else if (currentGrid.y != endGrid.y)
            {
                nextGrid.y += direction.y;
            }
            
            // Check if this step is valid
            if (CanMoveFromGridToGrid(currentGrid, nextGrid, groundObject))
            {
                currentGrid = nextGrid;
                path.Add(GetGroundPositionAtGridPosition(currentGrid, groundObject));
            }
            else
            {
                // Cannot reach target due to height constraints
                if (debugRaycasts)
                {
                    Debug.LogWarning($"Cannot move from {currentGrid} to {nextGrid} due to height constraints");
                }
                break;
            }
        }
        
        return path;
    }
    
    // Debug visualization
    void OnDrawGizmosSelected()
    {
        if (!showDebugSpheres) return;
        
        // Draw debug spheres for cached height positions
        Gizmos.color = Color.blue;
        foreach (var kvp in heightCache)
        {
            string[] parts = kvp.Key.Split('_');
            if (parts.Length >= 3)
            {
                // This is a simplified visualization - in a real scenario you'd need the ground object reference
                Vector3 pos = new Vector3(float.Parse(parts[1]), kvp.Value, float.Parse(parts[2]));
                Gizmos.DrawWireSphere(pos, 0.1f);
            }
        }
    }
    
    // Public getters for configuration
    public float MaxStepHeight => maxStepHeight;
    public bool AllowClimbing => allowClimbing;
    public LayerMask GroundLayerMask => groundLayerMask;
}