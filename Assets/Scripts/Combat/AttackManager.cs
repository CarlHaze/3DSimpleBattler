using UnityEngine;
using System.Collections.Generic;

public class AttackManager : MonoBehaviour
{
    [Header("Attack Settings")]
    public Color attackRangeColor = Color.red;
    public Color targetableRangeColor = Color.yellow;
    public Material attackRangeOverlayMaterial;
    
    private Camera gameCamera;
    private GridOverlayManager gridManager;
    private UnitPlacementManager placementManager;
    private SimpleUnitOutline outlineController;
    
    // Current attack state
    private GameObject attackingUnit;
    private List<GameObject> attackRangeOverlays = new List<GameObject>();
    private List<GameObject> validTargets = new List<GameObject>();
    private bool inAttackMode = false;
    private GameObject currentGroundObject;
    private GameObject unitToReselectAfterAttack;
    
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
        ClearAttackRangeOverlays();
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
        
        // Show attack range overlays
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
                    // Check if there's a target at this position
                    GameObject target = GetTargetAtPosition(checkPos, groundObject);
                    bool hasTarget = target != null && IsValidTarget(target);
                    
                    if (hasTarget)
                    {
                        validTargets.Add(target);
                        CreateAttackRangeOverlay(checkPos, groundObject, targetableRangeColor);
                    }
                    else
                    {
                        CreateAttackRangeOverlay(checkPos, groundObject, attackRangeColor);
                    }
                }
            }
        }
    }
    
    void CreateAttackRangeOverlay(Vector2Int gridPos, GameObject groundObject, Color overlayColor)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Create a flat quad overlay for the grid tile
        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = $"AttackRangeOverlay_{gridPos.x}_{gridPos.y}";
        overlay.transform.position = worldPos + Vector3.up * 0.01f; // Slightly above ground
        overlay.transform.rotation = Quaternion.Euler(90, 0, 0); // Flat on ground
        overlay.transform.localScale = new Vector3(gridManager.gridSize * 0.9f, gridManager.gridSize * 0.9f, 1f);
        
        // Remove collider to prevent physics interactions
        Destroy(overlay.GetComponent<Collider>());
        
        // Set material and color with transparency
        Renderer renderer = overlay.GetComponent<Renderer>();
        Material overlayMaterial;
        
        if (attackRangeOverlayMaterial != null)
        {
            overlayMaterial = new Material(attackRangeOverlayMaterial);
        }
        else
        {
            overlayMaterial = new Material(Shader.Find("Standard"));
            // Set up transparency
            overlayMaterial.SetFloat("_Mode", 3); // Transparent mode
            overlayMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            overlayMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            overlayMaterial.SetInt("_ZWrite", 0);
            overlayMaterial.DisableKeyword("_ALPHATEST_ON");
            overlayMaterial.EnableKeyword("_ALPHABLEND_ON");
            overlayMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            overlayMaterial.renderQueue = 3000;
        }
        
        // Set semi-transparent color
        Color transparentColor = overlayColor;
        transparentColor.a = 0.4f; // Semi-transparent
        overlayMaterial.color = transparentColor;
        renderer.material = overlayMaterial;
        
        attackRangeOverlays.Add(overlay);
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
        
        // Check if attacker has enough AP for basic attack
        if (!attackerCharacter.Stats.CanSpendAP(1))
        {
            SimpleMessageLog.Log($"{attackerCharacter.CharacterName} doesn't have enough AP to attack!");
            ExitAttackMode();
            return;
        }
        
        // Consume 1 AP for basic attack
        attackerCharacter.Stats.SpendAP(1);
        
        // Get base attack power
        int attackPower = attackerCharacter.Stats.Attack;
        
        // Allow attacker's class logic to modify outgoing damage
        if (attackerCharacter.CharacterClass?.ClassLogic != null)
        {
            attackerCharacter.CharacterClass.ClassLogic.OnAttackCalculated(attackerCharacter, targetCharacter, ref attackPower);
        }
        
        // Apply damage and get the actual damage dealt
        int actualDamage = targetCharacter.Stats.TakeDamage(attackPower, attackerCharacter);
        
        // Notify attacker's class logic of damage dealt
        if (attackerCharacter.CharacterClass?.ClassLogic != null)
        {
            attackerCharacter.CharacterClass.ClassLogic.OnDealDamage(attackerCharacter, targetCharacter, actualDamage);
        }
        
        // Log the attack with actual damage dealt
        string attackerName = attackerCharacter.CharacterName;
        string targetName = targetCharacter.CharacterName;
        SimpleMessageLog.Log($"{attackerName} attacks {targetName} for {actualDamage} damage!");
        
        // Notify ActionMenuController that an attack was performed to prevent auto-selection
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null)
        {
            actionMenu.OnAttackPerformed();
        }
        
        // Handle defeated unit first
        if (!targetCharacter.Stats.IsAlive)
        {
            SimpleMessageLog.Log($"{targetName} is defeated!");
            
            // Notify class logic of unit defeat
            if (targetCharacter.CharacterClass?.ClassLogic != null)
            {
                targetCharacter.CharacterClass.ClassLogic.OnUnitDefeated(targetCharacter, attackerCharacter);
            }
            
            HandleUnitDefeated(target);
        }
        
        // Store the attacking unit before exiting attack mode
        GameObject unitToReselect = attackingUnit;
        
        // Exit attack mode immediately
        ExitAttackMode();
        
        // Keep unit selected and show action menu after attack for player units
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Combat && 
            turnManager.IsPlayerTurn() && unitToReselect == turnManager.GetCurrentUnit())
        {
            // Store the unit for re-selection and invoke after delay
            unitToReselectAfterAttack = unitToReselect;
            Invoke(nameof(ReselectUnitAfterAttack), 0.1f);
        }
    }
    
    void HandleUnitDefeated(GameObject defeatedUnit)
    {
        // Clean up any placement manager references
        UnitGridInfo unitInfo = defeatedUnit.GetComponent<UnitGridInfo>();
        if (unitInfo != null && unitInfo.placementManager != null)
        {
            unitInfo.placementManager.SetTileOccupied(unitInfo.groundObject, unitInfo.gridPosition, false);
        }
        
        // Clear selection if the defeated unit was selected in ActionMenuController
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null && actionMenu.GetSelectedUnit() == defeatedUnit)
        {
            actionMenu.DeselectUnit();
        }
        
        // Clear selection if the defeated unit was selected in MovementManager
        UnitMovementManager movementManager = FindFirstObjectByType<UnitMovementManager>();
        if (movementManager != null && movementManager.GetSelectedUnit() == defeatedUnit)
        {
            movementManager.ClearSelection();
        }
        
        // You can add death animations, loot drops, etc. here before destruction
        
        // Destroy the unit completely to free memory
        Destroy(defeatedUnit);
    }
    
    void ClearAttackRangeOverlays()
    {
        foreach (GameObject overlay in attackRangeOverlays)
        {
            if (overlay != null)
                Destroy(overlay);
        }
        attackRangeOverlays.Clear();
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
    
    void EndPlayerTurnDelayed()
    {
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null)
        {
            turnManager.EndTurn();
        }
    }
    
    void ReselectUnitAfterAttack()
    {
        // Re-select the unit to show the action menu
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null && unitToReselectAfterAttack != null)
        {
            actionMenu.SelectUnit(unitToReselectAfterAttack);
            unitToReselectAfterAttack = null; // Clear the reference after use
        }
    }
    
    void OnDestroy()
    {
        ClearAttackRangeOverlays();
    }
}