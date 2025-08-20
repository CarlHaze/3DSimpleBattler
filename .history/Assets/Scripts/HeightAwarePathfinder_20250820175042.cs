using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HeightAwarePathfinder : MonoBehaviour
{
    [Header("Pathfinding Settings")]
    [SerializeField] private bool enableDiagonalMovement = true;
    [SerializeField] private float diagonalCost = 1.414f; // sqrt(2)
    [SerializeField] private float straightCost = 1f;
    [SerializeField] private int maxSearchNodes = 1000; // Prevent infinite loops
    [SerializeField] private bool debugPathfinding = false;
    
    [Header("Height Constraints")]
    [SerializeField] private float maxClimbHeight = 1.5f;
    [SerializeField] private float maxFallHeight = 3f;
    [SerializeField] private float heightPenalty = 0.5f; // REDUCED penalty for height changes
    [SerializeField] private bool preferDirectRoutes = true; // NEW: Prefer going over obstacles
    
    private TerrainHeightDetector heightDetector;
    private GridOverlayManager gridManager;
    private UnitPlacementManager placementManager;
    
    void Start()
    {
        heightDetector = FindFirstObjectByType<TerrainHeightDetector>();
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        
        if (heightDetector == null)
        {
            Debug.LogError("HeightAwarePathfinder requires TerrainHeightDetector!");
        }
        if (gridManager == null)
        {
            Debug.LogError("HeightAwarePathfinder requires GridOverlayManager!");
        }
    }
    
    /// <summary>
    /// Find a path from start to end that respects height constraints and obstacles
    /// </summary>
    public List<Vector2Int> FindPath(Vector2Int start, Vector2Int end, GameObject groundObject, int maxMovementRange)
    {
        if (heightDetector == null || gridManager == null)
        {
            Debug.LogWarning("Required components not found for pathfinding");
            return new List<Vector2Int> { end }; // Fallback to direct movement
        }
        
        // Always try direct movement first, even through elevated terrain
        int manhattanDistance = Mathf.Abs(end.x - start.x) + Mathf.Abs(end.y - start.y);
        if (manhattanDistance <= 2) // For short distances, prefer direct movement
        {
            if (CanMoveDirectly(start, end, groundObject))
            {
                if (debugPathfinding)
                {
                    Debug.Log($"Direct movement from {start} to {end}");
                }
                return new List<Vector2Int> { end };
            }
        }
        
        // Use A* pathfinding for longer distances or blocked direct paths
        return FindPathAStar(start, end, groundObject, maxMovementRange);
    }
    
    /// <summary>
    /// Check if we can move directly from start to end (including over obstacles)
    /// </summary>
    private bool CanMoveDirectly(Vector2Int from, Vector2Int to, GameObject groundObject)
    {
        // Check if target position is valid
        if (!gridManager.IsValidGridPosition(to, groundObject))
            return false;
        
        // Check if target is occupied by enemies (can't move onto enemies)
        if (IsEnemyAtPosition(to, groundObject))
            return false;
        
        // Check if target is occupied by another player unit (can't stack units)
        if (placementManager != null && placementManager.IsTileOccupied(groundObject, to))
        {
            GameObject unitAtTarget = placementManager.GetUnitAt(to, groundObject);
            if (unitAtTarget != null && unitAtTarget.CompareTag("Player"))
            {
                return false; // Can't move onto another player unit
            }
        }
        
        // NEW: More permissive height checking - allow climbing over walls
        if (heightDetector != null)
        {
            float fromHeight = heightDetector.GetGroundHeightAtGridPosition(from, groundObject);
            float toHeight = heightDetector.GetGroundHeightAtGridPosition(to, groundObject);
            float heightDifference = toHeight - fromHeight;
            
            // Allow movement if height difference is within climbable range
            if (heightDifference <= maxClimbHeight && heightDifference >= -maxFallHeight)
            {
                if (debugPathfinding)
                {
                    Debug.Log($"Direct climb from {from} to {to}: height diff = {heightDifference}");
                }
                return true;
            }
            
            if (debugPathfinding)
            {
                Debug.Log($"Height difference too large: {heightDifference} (max climb: {maxClimbHeight}, max fall: {maxFallHeight})");
            }
            return false;
        }
        
        return true;
    }
    
    /// <summary>
    /// A* pathfinding algorithm adapted for height-aware grid movement
    /// </summary>
    private List<Vector2Int> FindPathAStar(Vector2Int start, Vector2Int goal, GameObject groundObject, int maxMovementRange)
    {
        var openSet = new List<PathNode>();
        var closedSet = new HashSet<Vector2Int>();
        var allNodes = new Dictionary<Vector2Int, PathNode>();
        
        // Create start node
        PathNode startNode = new PathNode(start, 0, GetHeuristic(start, goal));
        openSet.Add(startNode);
        allNodes[start] = startNode;
        
        int searchCount = 0;
        
        while (openSet.Count > 0 && searchCount < maxSearchNodes)
        {
            searchCount++;
            
            // Get node with lowest f cost
            PathNode currentNode = openSet.OrderBy(n => n.FCost).ThenBy(n => n.HCost).First();
            openSet.Remove(currentNode);
            closedSet.Add(currentNode.Position);
            
            // Check if we reached the goal
            if (currentNode.Position == goal)
            {
                return ReconstructPath(currentNode);
            }
            
            // Check all neighbors
            foreach (Vector2Int neighbor in GetNeighbors(currentNode.Position))
            {
                // Skip if already evaluated
                if (closedSet.Contains(neighbor)) continue;
                
                // Skip if not valid grid position
                if (!gridManager.IsValidGridPosition(neighbor, groundObject)) continue;
                
                // Skip if too far from start (movement range limit)
                int distanceFromStart = Mathf.Abs(neighbor.x - start.x) + Mathf.Abs(neighbor.y - start.y);
                if (distanceFromStart > maxMovementRange) continue;
                
                // Calculate movement cost to this neighbor
                float moveCost = GetMovementCost(currentNode.Position, neighbor, groundObject);
                if (moveCost < 0) continue; // Invalid move (blocked by height or obstacles)
                
                float tentativeGCost = currentNode.GCost + moveCost;
                
                // Check if this path to neighbor is better
                PathNode neighborNode;
                if (!allNodes.TryGetValue(neighbor, out neighborNode))
                {
                    // New node
                    neighborNode = new PathNode(neighbor, tentativeGCost, GetHeuristic(neighbor, goal));
                    neighborNode.Parent = currentNode;
                    allNodes[neighbor] = neighborNode;
                    openSet.Add(neighborNode);
                }
                else if (tentativeGCost < neighborNode.GCost)
                {
                    // Better path to existing node
                    neighborNode.GCost = tentativeGCost;
                    neighborNode.Parent = currentNode;
                    
                    // Re-add to open set if it was closed
                    if (!openSet.Contains(neighborNode) && !closedSet.Contains(neighbor))
                    {
                        openSet.Add(neighborNode);
                    }
                }
            }
        }
        
        if (debugPathfinding)
        {
            Debug.LogWarning($"No path found from {start} to {goal} after {searchCount} iterations");
        }
        
        // No path found - return empty list
        return new List<Vector2Int>();
    }
    
    /// <summary>
    /// Get valid neighboring positions for pathfinding
    /// </summary>
    private List<Vector2Int> GetNeighbors(Vector2Int position)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        
        // Cardinal directions (up, down, left, right)
        neighbors.Add(new Vector2Int(position.x, position.y + 1)); // Up
        neighbors.Add(new Vector2Int(position.x, position.y - 1)); // Down
        neighbors.Add(new Vector2Int(position.x - 1, position.y)); // Left
        neighbors.Add(new Vector2Int(position.x + 1, position.y)); // Right
        
        // Diagonal directions (if enabled)
        if (enableDiagonalMovement)
        {
            neighbors.Add(new Vector2Int(position.x - 1, position.y + 1)); // Up-Left
            neighbors.Add(new Vector2Int(position.x + 1, position.y + 1)); // Up-Right
            neighbors.Add(new Vector2Int(position.x - 1, position.y - 1)); // Down-Left
            neighbors.Add(new Vector2Int(position.x + 1, position.y - 1)); // Down-Right
        }
        
        return neighbors;
    }
    
    /// <summary>
    /// Calculate movement cost between two adjacent positions
    /// </summary>
    private float GetMovementCost(Vector2Int from, Vector2Int to, GameObject groundObject)
    {
        // Check if the move is valid (not blocked by obstacles or height)
        if (!IsValidMove(from, to, groundObject))
        {
            return -1f; // Invalid move
        }
        
        // Base cost (diagonal vs straight)
        bool isDiagonal = Mathf.Abs(to.x - from.x) == 1 && Mathf.Abs(to.y - from.y) == 1;
        float baseCost = isDiagonal ? diagonalCost : straightCost;
        
        // REDUCED height penalty to encourage climbing over obstacles
        if (heightDetector != null)
        {
            float fromHeight = heightDetector.GetGroundHeightAtGridPosition(from, groundObject);
            float toHeight = heightDetector.GetGroundHeightAtGridPosition(to, groundObject);
            float heightDifference = Mathf.Abs(toHeight - fromHeight);
            
            // Only apply small penalty for height changes
            if (heightDifference > 0.1f)
            {
                baseCost *= (1f + heightDifference * heightPenalty);
            }
            
            // NEW: Bonus for moving towards goal over obstacles (prefer direct routes)
            if (preferDirectRoutes && toHeight > fromHeight)
            {
                baseCost *= 0.9f; // Small bonus for climbing (encourages going over walls)
            }
        }
        
        return baseCost;
    }
    
    /// <summary>
    /// Check if a move between two adjacent positions is valid
    /// </summary>
    private bool IsValidMove(Vector2Int from, Vector2Int to, GameObject groundObject)
    {
        // Check if occupied by enemies (cannot move through enemies)
        if (IsEnemyAtPosition(to, groundObject))
        {
            return false;
        }
        
        // Check if occupied by player units (cannot stack player units)
        if (placementManager != null && placementManager.IsTileOccupied(groundObject, to))
        {
            GameObject unitAtPosition = placementManager.GetUnitAt(to, groundObject);
            if (unitAtPosition != null && unitAtPosition.CompareTag("Player"))
            {
                return false; // Cannot move onto another player unit
            }
            // NOTE: We allow moving over/through non-player obstacles (walls, props, etc.)
        }
        
        // Height constraint checking - MUCH more permissive
        if (heightDetector != null)
        {
            float fromHeight = heightDetector.GetGroundHeightAtGridPosition(from, groundObject);
            float toHeight = heightDetector.GetGroundHeightAtGridPosition(to, groundObject);
            float heightDifference = toHeight - fromHeight;
            
            // Simply check if within climb/fall limits
            if (heightDifference > maxClimbHeight || heightDifference < -maxFallHeight)
            {
                if (debugPathfinding)
                {
                    Debug.Log($"Height constraint failed: {heightDifference} (limits: {maxClimbHeight}/{-maxFallHeight})");
                }
                return false;
            }
        }
        
        // For diagonal movement, be more permissive about corner cutting
        if (enableDiagonalMovement && Mathf.Abs(to.x - from.x) == 1 && Mathf.Abs(to.y - from.y) == 1)
        {
            // Allow diagonal movement unless both corners are blocked by extreme height differences
            Vector2Int corner1 = new Vector2Int(from.x, to.y);
            Vector2Int corner2 = new Vector2Int(to.x, from.y);
            
            if (heightDetector != null)
            {
                // Only block diagonal if BOTH corners have extreme height differences
                bool corner1Blocked = !CanMoveBasic(from, corner1, groundObject);
                bool corner2Blocked = !CanMoveBasic(from, corner2, groundObject);
                
                if (corner1Blocked && corner2Blocked)
                {
                    return false;
                }
            }
        }
        
        return true;
    }
    
    /// <summary>
    /// Basic movement check without pathfinding complexity
    /// </summary>
    private bool CanMoveBasic(Vector2Int from, Vector2Int to, GameObject groundObject)
    {
        if (heightDetector == null) return true;
        
        float fromHeight = heightDetector.GetGroundHeightAtGridPosition(from, groundObject);
        float toHeight = heightDetector.GetGroundHeightAtGridPosition(to, groundObject);
        float heightDifference = toHeight - fromHeight;
        
        return heightDifference <= maxClimbHeight && heightDifference >= -maxFallHeight;
    }
    
    /// <summary>
    /// Check if position is occupied by enemies
    /// </summary>
    private bool IsEnemyAtPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float checkRadius = gridManager.gridSize * 0.4f;
        
        Collider[] colliders = Physics.OverlapSphere(worldPos, checkRadius);
        
        foreach (Collider col in colliders)
        {
            if (col.CompareTag("Enemy"))
            {
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// Calculate heuristic cost (Manhattan distance for grid-based movement)
    /// </summary>
    private float GetHeuristic(Vector2Int from, Vector2Int to)
    {
        int dx = Mathf.Abs(to.x - from.x);
        int dy = Mathf.Abs(to.y - from.y);
        
        if (enableDiagonalMovement)
        {
            // For diagonal movement, use diagonal distance
            int diagonal = Mathf.Min(dx, dy);
            int straight = Mathf.Max(dx, dy) - diagonal;
            return diagonal * diagonalCost + straight * straightCost;
        }
        else
        {
            // Manhattan distance for cardinal movement only
            return (dx + dy) * straightCost;
        }
    }
    
    /// <summary>
    /// Reconstruct the path from goal to start
    /// </summary>
    private List<Vector2Int> ReconstructPath(PathNode goalNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        PathNode currentNode = goalNode;
        
        while (currentNode != null)
        {
            path.Add(currentNode.Position);
            currentNode = currentNode.Parent;
        }
        
        path.Reverse();
        path.RemoveAt(0); // Remove start position
        
        if (debugPathfinding && path.Count > 0)
        {
            Debug.Log($"Found path with {path.Count} steps: {string.Join(" -> ", path)}");
        }
        
        return path;
    }
    
    /// <summary>
    /// Public method to check if a position is reachable within movement range
    /// </summary>
    public bool IsPositionReachable(Vector2Int start, Vector2Int target, GameObject groundObject, int maxMovementRange)
    {
        List<Vector2Int> path = FindPath(start, target, groundObject, maxMovementRange);
        return path.Count > 0 && path.Count <= maxMovementRange;
    }
    
    /// <summary>
    /// Get all positions reachable within movement range
    /// </summary>
    public List<Vector2Int> GetReachablePositions(Vector2Int start, GameObject groundObject, int maxMovementRange)
    {
        List<Vector2Int> reachable = new List<Vector2Int>();
        
        // Use flood fill to find all reachable positions
        var visited = new HashSet<Vector2Int>();
        var queue = new Queue<(Vector2Int pos, int distance)>();
        
        queue.Enqueue((start, 0));
        visited.Add(start);
        
        while (queue.Count > 0)
        {
            var (currentPos, distance) = queue.Dequeue();
            
            if (distance < maxMovementRange)
            {
                foreach (Vector2Int neighbor in GetNeighbors(currentPos))
                {
                    if (visited.Contains(neighbor)) continue;
                    if (!gridManager.IsValidGridPosition(neighbor, groundObject)) continue;
                    
                    if (IsValidMove(currentPos, neighbor, groundObject))
                    {
                        visited.Add(neighbor);
                        queue.Enqueue((neighbor, distance + 1));
                        reachable.Add(neighbor);
                    }
                }
            }
        }
        
        return reachable;
    }
}

/// <summary>
/// Node class for A* pathfinding
/// </summary>
public class PathNode
{
    public Vector2Int Position;
    public float GCost; // Distance from start
    public float HCost; // Heuristic distance to goal
    public float FCost => GCost + HCost; // Total cost
    public PathNode Parent;
    
    public PathNode(Vector2Int position, float gCost, float hCost)
    {
        Position = position;
        GCost = gCost;
        HCost = hCost;
        Parent = null;
    }
}