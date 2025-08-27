using UnityEngine;
using System.Collections.Generic;

public class SkillManager : MonoBehaviour
{
    [Header("Skill Settings")]
    public Color skillRangeColor = Color.blue;
    public Color skillTargetableRangeColor = Color.cyan;
    public Material skillRangeOverlayMaterial;
    
    private Camera gameCamera;
    private GridOverlayManager gridManager;
    private UnitPlacementManager placementManager;
    private UnitMovementManager movementManager;
    private SimpleUnitOutline outlineController;
    
    // Current skill state
    private GameObject skillUser;
    private SkillSO currentSkill;
    private List<GameObject> skillRangeOverlays = new List<GameObject>();
    private List<GameObject> validTargets = new List<GameObject>();
    private bool inSkillMode = false;
    private GameObject currentGroundObject;
    
    void Start()
    {
        gameCamera = Camera.main;
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        gridManager = FindFirstObjectByType<GridOverlayManager>();
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        outlineController = FindFirstObjectByType<SimpleUnitOutline>();
        
        if (gridManager == null)
            Debug.LogError("GridOverlayManager not found!");
        if (placementManager == null)
            Debug.LogError("UnitPlacementManager not found!");
    }
    
    void Update()
    {
        if (inSkillMode && !IsExecutingSkill())
        {
            HandleSkillInput();
        }
    }
    
    void HandleSkillInput()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                // Check if we clicked on a valid target or position
                if (TryUseSkillOn(clickedObject, hit.point))
                {
                    return; // Skill handled
                }
            }
        }
        
        // Exit skill mode with right click or escape
        if (Input.GetMouseButtonDown(1) || Input.GetKeyDown(KeyCode.Escape))
        {
            ExitSkillMode();
        }
    }
    
    public void StartSkillMode(GameObject unit, SkillSO skill)
    {
        skillUser = unit;
        currentSkill = skill;
        inSkillMode = true;
        
        if (unit != null && skill != null)
        {
            UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
            if (unitInfo != null)
            {
                currentGroundObject = unitInfo.groundObject;
                ShowSkillRange(unitInfo.gridPosition, unitInfo.groundObject, skill);
                SimpleMessageLog.Log($"Skill mode activated: {skill.skillName} for {unit.name}");
            }
        }
    }
    
    public void ExitSkillMode()
    {
        inSkillMode = false;
        ClearSkillRangeOverlays();
        validTargets.Clear();
        skillUser = null;
        currentSkill = null;
        currentGroundObject = null;
        SimpleMessageLog.Log("Exited skill mode");
    }
    
    void ShowSkillRange(Vector2Int unitPosition, GameObject groundObject, SkillSO skill)
    {
        validTargets.Clear();
        
        // Use skill's specific range instead of character's attack range
        int skillRange = skill.range;
        
        Debug.Log($"Showing skill range for {skill.skillName}: range = {skillRange}");
        
        // Show skill range overlays
        for (int x = -skillRange; x <= skillRange; x++)
        {
            for (int z = -skillRange; z <= skillRange; z++)
            {
                // Skip the unit's current position
                if (x == 0 && z == 0) continue;
                
                // Use Manhattan distance for skill range
                int distance = Mathf.Abs(x) + Mathf.Abs(z);
                if (distance > skillRange) continue;
                
                Vector2Int checkPos = unitPosition + new Vector2Int(x, z);
                
                // Check if position is valid
                if (gridManager.IsValidGridPosition(checkPos, groundObject))
                {
                    // Check what's at this position based on skill target type
                    bool isValidTarget = IsValidSkillTarget(checkPos, groundObject, skill);
                    
                    if (isValidTarget)
                    {
                        GameObject target = GetTargetAtPosition(checkPos, groundObject);
                        if (target != null)
                        {
                            validTargets.Add(target);
                        }
                        CreateSkillRangeOverlay(checkPos, groundObject, skillTargetableRangeColor);
                    }
                    else
                    {
                        // Show as possible target area even if no valid target
                        CreateSkillRangeOverlay(checkPos, groundObject, skillRangeColor);
                    }
                }
            }
        }
    }
    
    bool IsValidSkillTarget(Vector2Int gridPos, GameObject groundObject, SkillSO skill)
    {
        GameObject target = GetTargetAtPosition(gridPos, groundObject);
        
        switch (skill.targetType)
        {
            case SkillTarget.Enemy:
                return target != null && IsEnemy(target);
            case SkillTarget.Ally:
                return target != null && IsAlly(target);
            case SkillTarget.AnyUnit:
                return target != null && (IsEnemy(target) || IsAlly(target));
            case SkillTarget.Self:
                return target == skillUser;
            case SkillTarget.Ground:
                // For ground-targeted skills, any empty or occupied position is valid
                return true;
            case SkillTarget.Area:
                // For area skills, any position is valid
                return true;
            default:
                return false;
        }
    }
    
    GameObject GetTargetAtPosition(Vector2Int gridPos, GameObject groundObject)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        float checkRadius = gridManager.gridSize * 0.4f;
        
        Collider[] colliders = Physics.OverlapSphere(worldPos, checkRadius);
        
        foreach (Collider col in colliders)
        {
            if (col.gameObject.CompareTag("Player") || col.gameObject.CompareTag("Enemy"))
            {
                return col.gameObject;
            }
        }
        
        return null;
    }
    
    bool IsEnemy(GameObject target)
    {
        return target.CompareTag("Enemy");
    }
    
    bool IsAlly(GameObject target)
    {
        return target.CompareTag("Player") && target != skillUser;
    }
    
    void CreateSkillRangeOverlay(Vector2Int gridPos, GameObject groundObject, Color overlayColor)
    {
        Vector3 worldPos = gridManager.GridToWorldPosition(gridPos, groundObject);
        
        // Create a flat quad overlay for the grid tile
        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = $"SkillRangeOverlay_{gridPos.x}_{gridPos.y}";
        overlay.transform.position = worldPos + Vector3.up * 0.01f; // Slightly above ground
        overlay.transform.rotation = Quaternion.Euler(90, 0, 0); // Flat on ground
        overlay.transform.localScale = new Vector3(gridManager.gridSize * 0.9f, gridManager.gridSize * 0.9f, 1f);
        
        // Remove collider to prevent physics interactions
        Destroy(overlay.GetComponent<Collider>());
        
        // Set material and color with transparency
        Renderer renderer = overlay.GetComponent<Renderer>();
        Material overlayMaterial;
        
        if (skillRangeOverlayMaterial != null)
        {
            overlayMaterial = new Material(skillRangeOverlayMaterial);
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
        
        skillRangeOverlays.Add(overlay);
    }
    
    bool TryUseSkillOn(GameObject clickedObject, Vector3 worldPosition)
    {
        if (skillUser == null || currentSkill == null) return false;
        
        // Get the grid position of the clicked location
        Vector2Int targetGridPos = gridManager.WorldToGridPosition(worldPosition, currentGroundObject);
        
        // Check if clicked on a valid target based on skill type
        if (currentSkill.targetType == SkillTarget.Ground || currentSkill.targetType == SkillTarget.Area)
        {
            // For ground/area skills, execute at the clicked position
            ExecuteSkillAt(targetGridPos, clickedObject);
            return true;
        }
        else
        {
            // For unit-targeted skills, check if clicked on a valid target
            if (validTargets.Contains(clickedObject))
            {
                ExecuteSkillOn(clickedObject);
                return true;
            }
        }
        
        return false;
    }
    
    void ExecuteSkillOn(GameObject target)
    {
        if (currentSkill == null || skillUser == null || target == null) return;
        
        Debug.Log($"Executing {currentSkill.skillName} on {target.name}");
        
        // Notify ActionMenuController that a skill was performed to prevent auto-selection
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null)
        {
            actionMenu.OnSkillPerformed();
        }
        
        // Handle special skill effects
        switch (currentSkill.skillName.ToLower())
        {
            case "charge":
                ExecuteChargeSkill(target);
                break;
            default:
                ExecuteGenericSkill(target);
                break;
        }
        
        // Exit skill mode immediately
        ExitSkillMode();
        
        // Keep unit selected and show action menu after skill use for player units
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Combat && 
            turnManager.IsPlayerTurn() && skillUser == turnManager.GetCurrentUnit())
        {
            // Re-select the skill user to show the action menu after a small delay
            Invoke(nameof(ReselectUnitAfterSkill), 0.1f);
        }
    }
    
    void ExecuteSkillAt(Vector2Int gridPosition, GameObject targetObject)
    {
        if (currentSkill == null || skillUser == null) return;
        
        Debug.Log($"Executing {currentSkill.skillName} at grid position {gridPosition}");
        
        // Notify ActionMenuController that a skill was performed to prevent auto-selection
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null)
        {
            actionMenu.OnSkillPerformed();
        }
        
        // Handle ground-targeted skills
        switch (currentSkill.skillName.ToLower())
        {
            case "charge":
                // If there's a unit at the target position, charge to them
                if (targetObject != null && (targetObject.CompareTag("Player") || targetObject.CompareTag("Enemy")))
                {
                    ExecuteChargeSkill(targetObject);
                }
                break;
            default:
                ExecuteGenericSkillAt(gridPosition);
                break;
        }
        
        // Exit skill mode immediately
        ExitSkillMode();
        
        // Keep unit selected and show action menu after skill use for player units
        TurnManager turnManager = FindFirstObjectByType<TurnManager>();
        if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Combat && 
            turnManager.IsPlayerTurn() && skillUser == turnManager.GetCurrentUnit())
        {
            // Re-select the skill user to show the action menu after a small delay
            Invoke(nameof(ReselectUnitAfterSkill), 0.1f);
        }
    }
    
    void ExecuteChargeSkill(GameObject target)
    {
        Debug.Log("ExecuteChargeSkill: Starting charge execution");
        
        // Check AP cost and spend it
        Character userCharacter = skillUser.GetComponent<Character>();
        if (userCharacter?.Stats != null)
        {
            if (!userCharacter.Stats.CanSpendAP(currentSkill.apCost))
            {
                SimpleMessageLog.Log($"{userCharacter.CharacterName} doesn't have enough AP to use {currentSkill.skillName}!");
                return;
            }
            
            userCharacter.Stats.SpendAP(currentSkill.apCost);
            Debug.Log($"{userCharacter.CharacterName} spent {currentSkill.apCost} AP for charge - remaining: {userCharacter.Stats.CurrentAP}");
        }
        
        // Get positions
        UnitGridInfo userInfo = skillUser.GetComponent<UnitGridInfo>();
        UnitGridInfo targetInfo = target.GetComponent<UnitGridInfo>();
        
        if (userInfo == null)
        {
            Debug.LogWarning("ExecuteChargeSkill: skillUser has no UnitGridInfo component! Trying to add one...");
            userInfo = EnsureUnitGridInfo(skillUser);
            if (userInfo == null)
            {
                Debug.LogError("ExecuteChargeSkill: Could not create UnitGridInfo for skillUser!");
                // Fall back to position-based charge
                ExecutePositionBasedCharge(target);
                return;
            }
        }
        
        if (targetInfo == null)
        {
            Debug.LogWarning("ExecuteChargeSkill: target has no UnitGridInfo component! Trying to add one...");
            targetInfo = EnsureUnitGridInfo(target);
            if (targetInfo == null)
            {
                Debug.LogError("ExecuteChargeSkill: Could not create UnitGridInfo for target!");
                // Fall back to position-based charge
                ExecutePositionBasedCharge(target);
                return;
            }
        }
        
        Vector2Int userPos = userInfo.gridPosition;
        Vector2Int targetPos = targetInfo.gridPosition;
        
        Debug.Log($"ExecuteChargeSkill: User at {userPos}, Target at {targetPos}");
        
        // Calculate the position in front of the target (between user and target)
        Vector2Int direction = userPos - targetPos;
        direction = new Vector2Int(Mathf.Clamp(direction.x, -1, 1), Mathf.Clamp(direction.y, -1, 1));
        Vector2Int chargeToPos = targetPos + direction;
        
        Debug.Log($"ExecuteChargeSkill: Calculated charge position: {chargeToPos}");
        
        // Make sure the charge position is valid and empty
        if (!gridManager.IsValidGridPosition(chargeToPos, currentGroundObject) || 
            GetTargetAtPosition(chargeToPos, currentGroundObject) != null)
        {
            Debug.Log("ExecuteChargeSkill: Primary charge position blocked, trying alternatives");
            
            // If the ideal position is blocked, try adjacent positions
            Vector2Int[] alternatePositions = {
                targetPos + new Vector2Int(1, 0),
                targetPos + new Vector2Int(-1, 0),
                targetPos + new Vector2Int(0, 1),
                targetPos + new Vector2Int(0, -1)
            };
            
            bool foundAlternate = false;
            foreach (Vector2Int altPos in alternatePositions)
            {
                if (gridManager.IsValidGridPosition(altPos, currentGroundObject) && 
                    GetTargetAtPosition(altPos, currentGroundObject) == null)
                {
                    chargeToPos = altPos;
                    foundAlternate = true;
                    Debug.Log($"ExecuteChargeSkill: Found alternate position: {altPos}");
                    break;
                }
            }
            
            if (!foundAlternate)
            {
                Debug.LogWarning("ExecuteChargeSkill: No valid charge position found!");
                // Still execute damage even if we can't move
            }
        }
        
        // Move the unit to the charge position
        Debug.Log($"ExecuteChargeSkill: Moving unit to {chargeToPos}");
        MoveUnitToPosition(skillUser, chargeToPos);
        
        // Apply damage after a short delay to allow movement
        Debug.Log("ExecuteChargeSkill: Starting delayed attack coroutine");
        StartCoroutine(DelayedChargeAttack(skillUser, target, currentSkill, 0.1f));
        
        string userNameString = userCharacter != null ? userCharacter.CharacterName : skillUser.name;
        Character targetCharacter = target.GetComponent<Character>();
        string targetNameString = targetCharacter != null ? targetCharacter.CharacterName : target.name;
        
        SimpleMessageLog.Log($"{userNameString} charges at {targetNameString}!");
    }
    
    System.Collections.IEnumerator DelayedChargeAttack(GameObject attacker, GameObject target, SkillSO skill, float delay)
    {
        Debug.Log($"DelayedChargeAttack: Waiting {delay} seconds before applying damage");
        yield return new WaitForSeconds(delay);
        
        Debug.Log("DelayedChargeAttack: Applying charge damage");
        
        if (attacker == null)
        {
            Debug.LogError("DelayedChargeAttack: attacker is null!");
            yield break;
        }
        
        if (target == null)
        {
            Debug.LogError("DelayedChargeAttack: target is null!");
            yield break;
        }
        
        Character attackerCharacter = attacker.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();
        
        if (attackerCharacter == null)
        {
            Debug.LogError("DelayedChargeAttack: attacker has no Character component!");
            yield break;
        }
        
        if (targetCharacter == null)
        {
            Debug.LogError("DelayedChargeAttack: target has no Character component!");
            yield break;
        }
        
        if (skill == null)
        {
            Debug.LogError("DelayedChargeAttack: skill is null!");
            yield break;
        }
        
        // Calculate charge damage (base damage + skill multiplier)
        int chargeDamage = Mathf.RoundToInt(skill.baseDamage + (attackerCharacter.Stats.Attack * skill.damageMultiplier));
        
        Debug.Log($"DelayedChargeAttack: Calculated damage = {chargeDamage} (baseDamage: {skill.baseDamage} + attack: {attackerCharacter.Stats.Attack} * multiplier: {skill.damageMultiplier})");
        
        // Apply damage
        int actualDamage = targetCharacter.Stats.TakeDamage(chargeDamage, attackerCharacter);
        
        Debug.Log($"DelayedChargeAttack: Applied {actualDamage} damage to {targetCharacter.CharacterName}");
        
        SimpleMessageLog.Log($"{attackerCharacter.CharacterName} deals {actualDamage} charge damage to {targetCharacter.CharacterName}!");
        
        // Handle defeat
        if (!targetCharacter.Stats.IsAlive)
        {
            SimpleMessageLog.Log($"{targetCharacter.CharacterName} is defeated!");
            HandleUnitDefeated(target);
        }
    }
    
    void MoveUnitToPosition(GameObject unit, Vector2Int targetGridPos)
    {
        Debug.Log($"MoveUnitToPosition: Moving {unit.name} to {targetGridPos}");
        
        UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
        if (unitInfo == null)
        {
            Debug.LogError($"MoveUnitToPosition: {unit.name} has no UnitGridInfo component!");
            return;
        }
        
        Vector2Int oldPos = unitInfo.gridPosition;
        Debug.Log($"MoveUnitToPosition: Old position: {oldPos}");
        
        // Clear old position
        if (unitInfo.placementManager != null)
        {
            unitInfo.placementManager.SetTileOccupied(unitInfo.groundObject, unitInfo.gridPosition, false);
            Debug.Log($"MoveUnitToPosition: Cleared old position {oldPos}");
        }
        else
        {
            Debug.LogWarning("MoveUnitToPosition: No placement manager found!");
        }
        
        // Update grid position
        unitInfo.gridPosition = targetGridPos;
        Debug.Log($"MoveUnitToPosition: Updated grid position to {targetGridPos}");
        
        // Move to world position
        if (gridManager != null)
        {
            Vector3 worldPos = gridManager.GridToWorldPosition(targetGridPos, unitInfo.groundObject);
            Vector3 newPos = worldPos + Vector3.up * 0.5f; // Adjust height as needed
            unit.transform.position = newPos;
            Debug.Log($"MoveUnitToPosition: Moved to world position {newPos}");
        }
        else
        {
            Debug.LogError("MoveUnitToPosition: GridManager is null!");
        }
        
        // Set new position as occupied
        if (unitInfo.placementManager != null)
        {
            unitInfo.placementManager.SetTileOccupied(unitInfo.groundObject, targetGridPos, true);
            Debug.Log($"MoveUnitToPosition: Set new position {targetGridPos} as occupied");
        }
    }
    
    void ExecuteGenericSkill(GameObject target)
    {
        Character attackerCharacter = skillUser.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();
        
        if (attackerCharacter == null || targetCharacter == null) return;
        
        // Check if user has enough AP (should already be checked but double-check for safety)
        if (!attackerCharacter.Stats.CanSpendAP(currentSkill.apCost))
        {
            SimpleMessageLog.Log($"{attackerCharacter.CharacterName} doesn't have enough AP to use {currentSkill.skillName}!");
            return;
        }
        
        // Spend AP
        attackerCharacter.Stats.SpendAP(currentSkill.apCost);
        Debug.Log($"{attackerCharacter.CharacterName} spent {currentSkill.apCost} AP - remaining: {attackerCharacter.Stats.CurrentAP}");
        
        // Calculate skill damage
        int skillDamage = Mathf.RoundToInt(currentSkill.baseDamage + (attackerCharacter.Stats.Attack * currentSkill.damageMultiplier));
        
        // Apply damage or healing
        if (skillDamage > 0)
        {
            int actualDamage = targetCharacter.Stats.TakeDamage(skillDamage, attackerCharacter);
            SimpleMessageLog.Log($"{attackerCharacter.CharacterName} uses {currentSkill.skillName} on {targetCharacter.CharacterName} for {actualDamage} damage!");
        }
        else if (currentSkill.healing > 0)
        {
            // Handle healing skills
            targetCharacter.Stats.Heal(currentSkill.healing);
            SimpleMessageLog.Log($"{attackerCharacter.CharacterName} heals {targetCharacter.CharacterName} for {currentSkill.healing} HP!");
        }
        
        // Handle defeat
        if (!targetCharacter.Stats.IsAlive)
        {
            SimpleMessageLog.Log($"{targetCharacter.CharacterName} is defeated!");
            HandleUnitDefeated(target);
        }
    }
    
    void ExecuteGenericSkillAt(Vector2Int gridPosition)
    {
        Character character = skillUser.GetComponent<Character>();
        if (character == null) return;
        
        SimpleMessageLog.Log($"{character.CharacterName} uses {currentSkill.skillName} at position {gridPosition}!");
        // Add area effect logic here if needed
    }
    
    void HandleUnitDefeated(GameObject defeatedUnit)
    {
        // Clean up any placement manager references
        UnitGridInfo unitInfo = defeatedUnit.GetComponent<UnitGridInfo>();
        if (unitInfo != null && unitInfo.placementManager != null)
        {
            unitInfo.placementManager.SetTileOccupied(unitInfo.groundObject, unitInfo.gridPosition, false);
        }
        
        // Clear selections
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null && actionMenu.GetSelectedUnit() == defeatedUnit)
        {
            actionMenu.DeselectUnit();
        }
        
        UnitMovementManager movementManager = FindFirstObjectByType<UnitMovementManager>();
        if (movementManager != null && movementManager.GetSelectedUnit() == defeatedUnit)
        {
            movementManager.ClearSelection();
        }
        
        Destroy(defeatedUnit);
    }
    
    void ClearSkillRangeOverlays()
    {
        foreach (GameObject overlay in skillRangeOverlays)
        {
            if (overlay != null)
                Destroy(overlay);
        }
        skillRangeOverlays.Clear();
    }
    
    // Public getters
    public bool IsInSkillMode()
    {
        return inSkillMode;
    }
    
    public bool IsExecutingSkill()
    {
        // For now, skills are instant. In the future, you might have skill animations
        return false;
    }
    
    void ReselectUnitAfterSkill()
    {
        // Re-select the skill user to show the action menu
        ActionMenuController actionMenu = FindFirstObjectByType<ActionMenuController>();
        if (actionMenu != null && skillUser != null)
        {
            actionMenu.SelectUnit(skillUser);
        }
    }
    
    public GameObject GetSkillUser()
    {
        return skillUser;
    }
    
    public SkillSO GetCurrentSkill()
    {
        return currentSkill;
    }
    
    // Helper method to ensure a unit has UnitGridInfo component
    UnitGridInfo EnsureUnitGridInfo(GameObject unit)
    {
        UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
        if (unitInfo != null) return unitInfo;
        
        // Try to add the component
        unitInfo = unit.AddComponent<UnitGridInfo>();
        if (unitInfo == null) return null;
        
        // Try to set up basic grid info
        if (gridManager != null && placementManager != null)
        {
            // Convert world position to grid position
            Vector3 worldPos = unit.transform.position;
            unitInfo.groundObject = currentGroundObject; // Use current ground object
            unitInfo.gridPosition = gridManager.WorldToGridPosition(worldPos, currentGroundObject);
            unitInfo.placementManager = placementManager;
            
            Debug.Log($"EnsureUnitGridInfo: Added UnitGridInfo to {unit.name} at grid position {unitInfo.gridPosition}");
        }
        else
        {
            Debug.LogWarning("EnsureUnitGridInfo: GridManager or PlacementManager not available, setting default values");
            unitInfo.gridPosition = Vector2Int.zero;
        }
        
        return unitInfo;
    }
    
    // Fallback charge that works without perfect grid setup
    void ExecutePositionBasedCharge(GameObject target)
    {
        Debug.Log("ExecutePositionBasedCharge: Using position-based charge as fallback");
        
        Vector3 userPos = skillUser.transform.position;
        Vector3 targetPos = target.transform.position;
        
        // Calculate direction from user to target
        Vector3 direction = (targetPos - userPos).normalized;
        
        // Move user closer to target (about 1 unit away)
        Vector3 chargePosition = targetPos - direction * 1.5f;
        
        // Move the unit
        skillUser.transform.position = chargePosition;
        
        // Apply damage immediately since we're not using the grid system
        ApplyChargeDamage(target);
        
        Character userCharacter = skillUser.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();
        string userNameString = userCharacter != null ? userCharacter.CharacterName : skillUser.name;
        string targetNameString = targetCharacter != null ? targetCharacter.CharacterName : target.name;
        
        SimpleMessageLog.Log($"{userNameString} charges at {targetNameString}!");
    }
    
    // Helper to apply charge damage directly
    void ApplyChargeDamage(GameObject target)
    {
        Character attackerCharacter = skillUser.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();
        
        if (attackerCharacter == null || targetCharacter == null || currentSkill == null) return;
        
        // Calculate charge damage (base damage + skill multiplier)
        int chargeDamage = Mathf.RoundToInt(currentSkill.baseDamage + (attackerCharacter.Stats.Attack * currentSkill.damageMultiplier));
        
        Debug.Log($"ApplyChargeDamage: Calculated damage = {chargeDamage} (baseDamage: {currentSkill.baseDamage} + attack: {attackerCharacter.Stats.Attack} * multiplier: {currentSkill.damageMultiplier})");
        
        // Apply damage
        int actualDamage = targetCharacter.Stats.TakeDamage(chargeDamage, attackerCharacter);
        
        Debug.Log($"ApplyChargeDamage: Applied {actualDamage} damage to {targetCharacter.CharacterName}");
        
        SimpleMessageLog.Log($"{attackerCharacter.CharacterName} deals {actualDamage} charge damage to {targetCharacter.CharacterName}!");
        
        // Handle defeat
        if (!targetCharacter.Stats.IsAlive)
        {
            SimpleMessageLog.Log($"{targetCharacter.CharacterName} is defeated!");
            HandleUnitDefeated(target);
        }
    }
    
    void OnDestroy()
    {
        ClearSkillRangeOverlays();
    }
}