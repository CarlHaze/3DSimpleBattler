using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SpawnPosition
{
    public Vector2Int gridPos;
    public GameObject groundObject;
}

public class EnemySpawner : MonoBehaviour
{
    [Header("Enemy Settings")]
    public GameObject enemyPrefab;
    public int enemiesToSpawn = 3;
    public LayerMask groundLayerMask = 1;
    public LayerMask obstructionLayerMask = -1;
    public float enemyHeightOffset = 0f;
    
    [Header("Spawn Control")]
    public bool spawnOnStart = true;
    public KeyCode spawnKey = KeyCode.E;
    public int minDistanceFromUnits = 2;
    
    private GridOverlayManager gridManager;
    private UnitPlacementManager placementManager;
    private List<GameObject> spawnedEnemies = new List<GameObject>();
    
    void Start()
    {
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        
        if (gridManager == null)
        {
            Debug.LogError("GridOverlayManager not found!");
            return;
        }
        
        if (spawnOnStart)
        {
            // Delay spawn to ensure everything is initialized
            Invoke(nameof(SpawnEnemies), 0.1f);
        }
    }
    
    void Update()
    {
        if (Input.GetKeyDown(spawnKey))
        {
            SpawnEnemies();
        }
    }
    
    [ContextMenu("Spawn Enemies")]
    public void SpawnEnemies()
    {
        if (enemyPrefab == null)
        {
            Debug.LogError("No enemy prefab assigned!");
            return;
        }
        
        ClearExistingEnemies();
        
        // Get all ground objects
        List<GameObject> groundObjects = GetAllGroundObjects();
        
        if (groundObjects.Count == 0)
        {
            Debug.LogError("No ground objects found!");
            return;
        }
        
        // Collect ALL possible valid positions first
        List<SpawnPosition> allValidPositions = new List<SpawnPosition>();
        
        foreach (GameObject groundObj in groundObjects)
        {
            List<SpawnPosition> positions = GetAllValidPositionsOnGround(groundObj);
            allValidPositions.AddRange(positions);
        }
        
        Debug.Log($"Found {allValidPositions.Count} total valid positions across {groundObjects.Count} ground objects");
        
        // Shuffle the positions for random selection
        for (int i = 0; i < allValidPositions.Count; i++)
        {
            SpawnPosition temp = allValidPositions[i];
            int randomIndex = Random.Range(i, allValidPositions.Count);
            allValidPositions[i] = allValidPositions[randomIndex];
            allValidPositions[randomIndex] = temp;
        }
        
        // Try to spawn enemies with distance checking
        int enemiesSpawned = 0;
        
        foreach (SpawnPosition spawnPos in allValidPositions)
        {
            if (enemiesSpawned >= enemiesToSpawn) break;
            
            // Check distance from already spawned enemies
            if (IsMinimumDistanceFromSpawnedEnemies(spawnPos.gridPos, spawnPos.groundObject))
            {
                if (TrySpawnEnemyAt(spawnPos.gridPos, spawnPos.groundObject))
                {
                    enemiesSpawned++;
                    Debug.Log($"Enemy {enemiesSpawned} spawned at {spawnPos.gridPos} on {spawnPos.groundObject.name}");
                }
            }
        }
        
        Debug.Log($"Spawned {enemiesSpawned}/{enemiesToSpawn} enemies from {allValidPositions.Count} valid positions");
    }
    
    List<SpawnPosition> GetAllValidPositionsOnGround(GameObject groundObject)
    {
        List<SpawnPosition> validPositions = new List<SpawnPosition>();
        
        Renderer renderer = groundObject.GetComponent<Renderer>();
        if (renderer == null) return validPositions;
        
        Bounds bounds = renderer.bounds;
        Vector3 size = bounds.size;
        int gridCountX = Mathf.CeilToInt(size.x / gridManager.gridSize);
        int gridCountZ = Mathf.CeilToInt(size.z / gridManager.gridSize);
        
        // Check every position on this ground object
        for (int x = 0; x < gridCountX; x++)
        {
            for (int z = 0; z < gridCountZ; z++)
            {
                Vector2Int gridPos = new Vector2Int(x, z);
                
                if (IsBasicValidSpawnPosition(gridPos, groundObject))
                {
                    validPositions.Add(new SpawnPosition { gridPos = gridPos, groundObject = groundObject });
                }
            }
        }
        
        return validPositions;
    }
    
    bool IsBasicValidSpawnPosition(Vector2Int gridPos, GameObject groundObject)
    {
        // Check if position is within grid bounds
        if (!gridManager.IsValidGridPosition(gridPos, groundObject))
        {
            return false;
        }
        
        // Check if tile is occupied by player units
        if (placementManager != null && placementManager.IsTileOccupied(groundObject, gridPos))
        {
            return false;
        }
        
        // Check minimum distance from player units only (not other enemies yet)
        if (!IsMinimumDistanceFromPlayerUnits(gridPos, groundObject))
        {
            return false;
        }
        
        // Check for physical obstructions
        if (IsPositionObstructed(gridPos, groundObject))
        {
            return false;
        }
        
        return true;
    }
    
    bool IsMinimumDistanceFromPlayerUnits(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Check distance from all existing player units only
        if (placementManager != null)
        {
            GameObject[] allUnits = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            foreach (GameObject unit in allUnits)
            {
                UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
                if (unitInfo != null)
                {
                    Vector3 unitWorldPos = gridManager.GridToWorldPosition(unitInfo.gridPosition, unitInfo.groundObject);
                    float distance = Vector3.Distance(worldPos, unitWorldPos);
                    float minDistance = minDistanceFromUnits * gridManager.gridSize;
                    
                    if (distance < minDistance)
                    {
                        return false;
                    }
                }
            }
        }
        
        return true;
    }
    
    bool IsMinimumDistanceFromSpawnedEnemies(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Check distance from already spawned enemies
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                EnemyGridInfo enemyInfo = enemy.GetComponent<EnemyGridInfo>();
                if (enemyInfo != null)
                {
                    Vector3 enemyWorldPos = gridManager.GridToWorldPosition(enemyInfo.gridPosition, enemyInfo.groundObject);
                    float distance = Vector3.Distance(worldPos, enemyWorldPos);
                    float minDistance = minDistanceFromUnits * gridManager.gridSize;
                    
                    if (distance < minDistance)
                    {
                        return false;
                    }
                }
            }
        }
        
        return true;
    }
    
    List<GameObject> GetAllGroundObjects()
    {
        List<GameObject> groundObjects = new List<GameObject>();
        GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        
        foreach (GameObject obj in allObjects)
        {
            if (((1 << obj.layer) & groundLayerMask) != 0 && obj.GetComponent<Renderer>() != null)
            {
                groundObjects.Add(obj);
            }
        }
        
        return groundObjects;
    }
    
    bool IsPositionObstructed(Vector2Int gridPos, GameObject groundObj)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObj);
        float heightOffset = CalculateEnemyHeightOffset();
        
        Vector3 startPos = worldPos;
        Vector3 endPos = worldPos + Vector3.up * (heightOffset * 2f);
        float capsuleRadius = 0.4f;
        
        // Check for obstructions
        RaycastHit[] hits = Physics.CapsuleCastAll(
            startPos, 
            endPos, 
            capsuleRadius, 
            Vector3.up, 
            0.1f, 
            obstructionLayerMask
        );
        
        foreach (RaycastHit hit in hits)
        {
            if (hit.collider.gameObject != groundObj)
            {
                return true;
            }
        }
        
        // Check for overlapping objects
        Collider[] overlapping = Physics.OverlapSphere(worldPos + Vector3.up * heightOffset, capsuleRadius, obstructionLayerMask);
        
        foreach (Collider col in overlapping)
        {
            if (col.gameObject != groundObj)
            {
                return true;
            }
        }
        
        return false;
    }
    
    bool TrySpawnEnemyAt(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float heightOffset = CalculateEnemyHeightOffset();
        worldPos.y += heightOffset;
        
        GameObject enemy = Instantiate(enemyPrefab, worldPos, Quaternion.identity);
        spawnedEnemies.Add(enemy);
        
        // Add grid info to enemy (optional, for consistency)
        EnemyGridInfo enemyInfo = enemy.AddComponent<EnemyGridInfo>();
        enemyInfo.gridPosition = gridPos;
        enemyInfo.groundObject = groundObject;
        enemyInfo.spawner = this;
        
        Debug.Log($"Enemy spawned at grid position {gridPos}");
        return true;
    }
    
    float CalculateEnemyHeightOffset()
    {
        if (enemyHeightOffset != 0f)
        {
            return enemyHeightOffset;
        }
        
        if (enemyPrefab != null)
        {
            Renderer prefabRenderer = enemyPrefab.GetComponent<Renderer>();
            if (prefabRenderer != null)
            {
                return prefabRenderer.bounds.size.y * 0.5f;
            }
            
            Collider prefabCollider = enemyPrefab.GetComponent<Collider>();
            if (prefabCollider != null)
            {
                return prefabCollider.bounds.size.y * 0.5f;
            }
        }
        
        return 1f;
    }
    
    [ContextMenu("Clear Enemies")]
    public void ClearExistingEnemies()
    {
        foreach (GameObject enemy in spawnedEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy);
            }
        }
        spawnedEnemies.Clear();
        Debug.Log("All enemies cleared");
    }
    
    public void RemoveEnemy(GameObject enemy)
    {
        if (spawnedEnemies.Contains(enemy))
        {
            spawnedEnemies.Remove(enemy);
        }
    }
}

// Simple component to track enemy grid info
public class EnemyGridInfo : MonoBehaviour
{
    [HideInInspector] public Vector2Int gridPosition;
    [HideInInspector] public GameObject groundObject;
    [HideInInspector] public EnemySpawner spawner;
    
    void OnDestroy()
    {
        if (spawner != null)
        {
            spawner.RemoveEnemy(gameObject);
        }
    }
}