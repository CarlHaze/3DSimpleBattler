using UnityEngine;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    [Header("Attack Settings")]
    public Color attackRangeColor = Color.red;
    public Color validTargetColor = Color.orange;
    public Material attackHighlightMaterial;
    
    private Camera gameCamera;
    private GridOverlayManager gridManager;
    private UnitPlacementManager placementManager;
    private SimpleUnitOutline outlineController;
    
    // Current attack state
    private GameObject attackingUnit;
    private List<GameObject> attackRangeHighlights = new List<GameObject>();
    private List<GameObject> validTargets = new List<GameObject>();
    private bool inAttackMode = false;
    private GameObject currentGroundObject;
    
    void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        outlineController = FindFirstObjectByType<SimpleUnitOutline>();
        
        if (gridManager == null)
            Debug.LogError("GridOverlayManager not found!");
        if (placementManager == null)
            Debug.LogError("UnitPlacementManager not found!");
    }
    
    void Update()
    {
        if (inAttackMode && !IsAttacking())
        {
            HandleAttackInput();
        }
    }
    
    void HandleAttackInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                // Check if we clicked on a valid target
                if (TryAttackTarget(clickedObject))
                {
                    return; // Attack handled
                }
            }
        }
        
        // Exit attack mode with right click or escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            ExitAttackMode();
        }
    }
    
    public void StartAttackMode(GameObject unit)
    {
        attackingUnit = unit;
        inAttackMode = true;
        
        if (unit != null)
        {
            UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
            if (unitInfo != null)
            {
                currentGroundObject = unitInfo.groundObject;
                ShowAttackRange(unitInfo.gridPosition, unitInfo.groundObject);
                SimpleMessageLog.Log($"Attack mode activated for {unit.name}");
            }
        }
    }
    
    public void ExitAttackMode()
    {
        inAttackMode = false;
        ClearAttackHighlights();
        validTargets.Clear();
        attackingUnit = null;
        currentGroundObject = null;
        SimpleMessageLog.Log("Exited attack mode");
    }
    
    void ShowAttackRange(Vector2Int unitPosition, GameObject groundObject)
    {
        validTargets.Clear();
        
        // Get attack range from character stats
        Character character = attackingUnit.GetComponent<Character>();
        if (character == null || character.Stats == null) return;
        
        int attackRange = character.Stats.AttackRange;
        
        // Show attack range highlights
        for (int x = -attackRange; x <= attackRange; x++)
        {
            for (int z = -attackRange; z <= attackRange; z++)
            {
                // Skip the unit's current position
                if (x == 0 && z == 0) continue;
                
                // Use Manhattan distance for attack range
                int distance = Mathf.Abs(x) + Mathf.Abs(z);
                if (distance > attackRange) continue;
                
                Vector2Int checkPos = unitPosition + new Vector2Int(x, z);
                
                // Check if position is valid
                if (gridManager.IsValidGridPosition(checkPos, groundObject))
                {
                    CreateAttackRangeHighlight(checkPos, groundObject);
                    
                    // Check if there's a target at this position
                    GameObject target = GetTargetAtPosition(checkPos, groundObject);
                    if (target != null && IsValidTarget(target))
                    {
                        validTargets.Add(target);
                        HighlightValidTarget(target);
                    }
                }
            }
        }
    }
    
    void CreateAttackRangeHighlight(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        highlight.name = $"AttackRangeHighlight_{gridPos.x}_{gridPos.y}";
        highlight.transform.position = worldPos;
        highlight.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
        
        // Remove collider to prevent physics interactions
        Destroy(highlight.GetComponent<Collider>());
        
        // Set material and color
        Renderer renderer = highlight.GetComponent<Renderer>();
        if (attackHighlightMaterial != null)
        {
            Material mat = new Material(attackHighlightMaterial);
            mat.color = attackRangeColor;
            renderer.material = mat;
        }
        else
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = attackRangeColor;
            renderer.material = mat;
        }
        
        attackRangeHighlights.Add(highlight);
    }
    
    void HighlightValidTarget(GameObject target)
    {
        // Add orange outline to valid targets
        if (outlineController != null)
        {
            outlineController.AddTargetOutline(target, validTargetColor);
        }
    }
    
    GameObject GetTargetAtPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float checkRadius = gridManager.gridSize * 0.4f;
        
        Collider[] colliders = Physics.OverlapSphere(worldPos, checkRadius);
        
        foreach (Collider col in colliders)
        {
            if (IsValidTarget(col.gameObject))
            {
                return col.gameObject;
            }
        }
        
        return null;
    }
    
    bool IsValidTarget(GameObject target)
    {
        // Can attack enemies
        if (target.CompareTag("Enemy"))
            return true;
            
        // Can attack other player units (for friendly fire scenarios)
        if (target.CompareTag("Player") && target != attackingUnit)
            return true;
            
        return false;
    }
    
    bool TryAttackTarget(GameObject clickedObject)
    {
        if (attackingUnit == null) return false;
        
        // Check if clicked on a valid target
        if (validTargets.Contains(clickedObject))
        {
            PerformAttack(clickedObject);
            return true;
        }
        
        return false;
    }
    
    void PerformAttack(GameObject target)
    {
        Character attackerCharacter = attackingUnit.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();
        
        if (attackerCharacter == null || targetCharacter == null)
        {
            Debug.LogError("Attack failed: Missing Character component");
            return;
        }
        
        // Calculate damage: attacker's attack vs target's defense
        int attackPower = attackerCharacter.Stats.Attack;
        int targetDefense = targetCharacter.Stats.Defense;
        int damage = Mathf.Max(attackPower - targetDefense, 1); // Minimum 1 damage
        
        // Apply damage
        targetCharacter.Stats.TakeDamage(damage);
        
        // Log the attack
        string attackerName = attackerCharacter.CharacterName;
        string targetName = targetCharacter.CharacterName;
        SimpleMessageLog.Log($"{attackerName} attacks {targetName} for {damage} damage!");
        
        if (!targetCharacter.Stats.IsAlive)
        {
            SimpleMessageLog.Log($"{targetName} is defeated!");
            HandleUnitDefeated(target);
        }
        
        // Exit attack mode after successful attack
        ExitAttackMode();
    }
    
    void HandleUnitDefeated(GameObject defeatedUnit)
    {
        // You can add death animations, loot drops, etc. here
        // For now, just disable the unit
        defeatedUnit.SetActive(false);
    }
    
    void ClearAttackHighlights()
    {
        foreach (GameObject highlight in attackRangeHighlights)
        {
            if (highlight != null)
                Destroy(highlight);
        }
        attackRangeHighlights.Clear();
        
        // Clear target outlines
        if (outlineController != null)
        {
            outlineController.ClearTargetOutlines();
        }
    }
    
    // Public getters
    public bool IsInAttackMode()
    {
        return inAttackMode;
    }
    
    public bool IsAttacking()
    {
        // For now, attacks are instant. In the future, you might have attack animations
        return false;
    }
    
    public GameObject GetAttackingUnit()
    {
        return attackingUnit;
    }
    
    void OnDestroy()
    {
        ClearAttackHighlights();
    }
}