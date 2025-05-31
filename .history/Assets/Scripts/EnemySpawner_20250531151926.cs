using UnityEngine;
using System.Collections.Generic;

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
            SpawnEnemies();
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
        
        int enemiesSpawned = 0;
        int maxAttempts = enemiesToSpawn * 10; // Prevent infinite loops
        int attempts = 0;
        
        while (enemiesSpawned < enemiesToSpawn && attempts < maxAttempts)
        {
            attempts++;
            
            // Pick a random ground object
            GameObject randomGround = groundObjects[Random.Range(0, groundObjects.Count)];
            
            // Get a random valid position on that ground
            Vector2Int randomGridPos = GetRandomValidPosition(randomGround);
            
            if (randomGridPos.x >= 0 && randomGridPos.y >= 0) // Valid position found
            {
                if (TrySpawnEnemyAt(randomGridPos, randomGround))
                {
                    enemiesSpawned++;
                }
            }
        }
        
        Debug.Log($"Spawned {enemiesSpawned}/{enemiesToSpawn} enemies after {attempts} attempts");
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
    
    Vector2Int GetRandomValidPosition(GameObject groundObject)
    {
        Renderer renderer = groundObject.GetComponent<Renderer>();
        if (renderer == null) return new Vector2Int(-1, -1);
        
        Bounds bounds = renderer.bounds;
        Vector3 size = bounds.size;
        int gridCountX = Mathf.CeilToInt(size.x / gridManager.gridSize);
        int gridCountZ = Mathf.CeilToInt(size.z / gridManager.gridSize);
        
        // Try random positions
        for (int i = 0; i < 20; i++) // Max 20 attempts per ground object
        {
            int randomX = Random.Range(0, gridCountX);
            int randomZ = Random.Range(0, gridCountZ);
            Vector2Int gridPos = new Vector2Int(randomX, randomZ);
            
            if (IsValidSpawnPosition(gridPos, groundObject))
            {
                return gridPos;
            }
        }
        
        return new Vector2Int(-1, -1); // No valid position found
    }
    
    bool IsValidSpawnPosition(Vector2Int gridPos, GameObject groundObject)
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
        
        // Check for physical obstructions
        if (IsPositionObstructed(gridPos, groundObject))
        {
            return false;
        }
        
        return true;
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