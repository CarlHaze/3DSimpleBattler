using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class TurnManager : MonoBehaviour
{
    [Header("Turn Settings")]
    public float playerSpeedBias = 0.1f; // Small bias towards player units when speeds are equal
    
    [Header("Battle Phases")]
    public BattlePhase currentPhase = BattlePhase.Placement;
    
    // Turn order and management
    private List<GameObject> allUnits = new List<GameObject>();
    private List<GameObject> turnOrder = new List<GameObject>();
    private int currentTurnIndex = 0;
    private GameObject currentUnit;
    
    // References to other managers
    private ModeManager modeManager;
    private SimpleUnitSelector unitSelector;
    private ActionMenuController actionMenuController;
    
    // Events for other systems to subscribe to
    public System.Action<GameObject> OnTurnStart;
    public System.Action<GameObject> OnTurnEnd;
    public System.Action<BattlePhase> OnPhaseChange;
    
    void Start()
    {
        // Get references to other managers
        modeManager = FindFirstObjectByType<ModeManager>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        actionMenuController = FindFirstObjectByType<ActionMenuController>();
        
        // Start in placement phase
        SetBattlePhase(BattlePhase.Placement);
        
        Debug.Log("TurnManager initialized - starting in Placement phase");
    }
    
    void Update()
    {
        // Check if we should transition from placement to combat
        if (currentPhase == BattlePhase.Placement)
        {
            CheckPlacementComplete();
        }
    }
    
    void CheckPlacementComplete()
    {
        // Check if placement is complete (max units placed and not in placement mode)
        if (unitSelector != null && unitSelector.IsInitialPlacementComplete() && 
            modeManager != null && !modeManager.IsInPlacementMode())
        {
            StartCombatPhase();
        }
    }
    
    public void StartCombatPhase()
    {
        Debug.Log("Starting Combat Phase");
        SetBattlePhase(BattlePhase.Combat);
        
        // Find all units on the battlefield
        RefreshUnitList();
        
        // Calculate turn order based on speed
        CalculateTurnOrder();
        
        // Start the first turn
        if (turnOrder.Count > 0)
        {
            StartTurn(0);
        }
        else
        {
            Debug.LogWarning("No units found for combat!");
        }
    }
    
    void RefreshUnitList()
    {
        allUnits.Clear();
        
        // Find all player units
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("Player");
        allUnits.AddRange(playerUnits);
        
        // Find all enemy units
        GameObject[] enemyUnits = GameObject.FindGameObjectsWithTag("Enemy");
        allUnits.AddRange(enemyUnits);
        
        // Filter out units that don't have Character component or are dead
        allUnits = allUnits.Where(unit => {
            Character character = unit.GetComponent<Character>();
            return character != null && character.Stats != null && character.Stats.IsAlive;
        }).ToList();
        
        Debug.Log($"Found {allUnits.Count} units for combat ({playerUnits.Length} player, {enemyUnits.Length} enemy)");
    }
    
    void CalculateTurnOrder()
    {
        // Sort units by speed (highest first), with player bias for ties
        turnOrder = allUnits.OrderByDescending(unit => {
            Character character = unit.GetComponent<Character>();
            float speed = character.Stats.Speed;
            
            // Add small bias for player units
            if (unit.CompareTag("Player"))
            {
                speed += playerSpeedBias;
            }
            
            return speed;
        }).ToList();
        
        Debug.Log("Turn order calculated:");
        for (int i = 0; i < turnOrder.Count; i++)
        {
            Character character = turnOrder[i].GetComponent<Character>();
            Debug.Log($"{i + 1}. {character.CharacterName} (Speed: {character.Stats.Speed})");
        }
    }
    
    void StartTurn(int turnIndex)
    {
        currentTurnIndex = turnIndex;
        currentUnit = turnOrder[currentTurnIndex];
        
        Character character = currentUnit.GetComponent<Character>();
        Debug.Log($"Starting turn for: {character.CharacterName}");
        
        // Refresh AP and MP for the current unit's turn
        if (character?.Stats != null)
        {
            character.Stats.RefreshTurnResources();
            Debug.Log($"{character.CharacterName} - AP: {character.Stats.CurrentAP}/{character.Stats.MaxAP}, MP: {character.Stats.CurrentMP}/{character.Stats.MaxMP}");
        }
        
        // Notify other systems
        OnTurnStart?.Invoke(currentUnit);
        
        // Handle different unit types
        if (currentUnit.CompareTag("Player"))
        {
            StartPlayerTurn();
        }
        else if (currentUnit.CompareTag("Enemy"))
        {
            StartEnemyTurn();
        }
    }
    
    void StartPlayerTurn()
    {
        Debug.Log("Player turn started");
        
        // Set to explore mode so player can select and act
        if (modeManager != null)
        {
            modeManager.SetMode(GameMode.Explore);
        }
        
        // Auto-select the current player unit for convenience
        if (actionMenuController != null)
        {
            actionMenuController.SelectUnit(currentUnit);
        }
        
        // Player turn continues until they complete an action or manually end turn
    }
    
    void StartEnemyTurn()
    {
        Debug.Log("Enemy turn started - implementing basic AI");
        
        // For now, implement very simple AI
        // TODO: Implement proper AI system later
        PerformSimpleEnemyAction();
    }
    
    void PerformSimpleEnemyAction()
    {
        // Very basic AI: find nearest player unit and attack if in range, otherwise pass turn
        Character enemyCharacter = currentUnit.GetComponent<Character>();
        if (enemyCharacter == null)
        {
            EndTurn();
            return;
        }
        
        // Find nearest player unit
        GameObject nearestPlayer = FindNearestPlayerUnit();
        if (nearestPlayer == null)
        {
            Debug.Log("No player units found - enemy passes turn");
            EndTurn();
            return;
        }
        
        // Check if in attack range (simplified - not using grid distance for now)
        float distance = Vector3.Distance(currentUnit.transform.position, nearestPlayer.transform.position);
        float attackRange = enemyCharacter.Stats.AttackRange * 2f; // Rough conversion
        
        if (distance <= attackRange)
        {
            // Perform attack
            PerformEnemyAttack(nearestPlayer);
        }
        else
        {
            Debug.Log($"{enemyCharacter.CharacterName} is too far to attack - passing turn");
            EndTurn();
        }
    }
    
    GameObject FindNearestPlayerUnit()
    {
        GameObject[] playerUnits = GameObject.FindGameObjectsWithTag("Player");
        GameObject nearest = null;
        float nearestDistance = float.MaxValue;
        
        foreach (GameObject player in playerUnits)
        {
            Character playerCharacter = player.GetComponent<Character>();
            if (playerCharacter?.Stats?.IsAlive == true)
            {
                float distance = Vector3.Distance(currentUnit.transform.position, player.transform.position);
                if (distance < nearestDistance)
                {
                    nearestDistance = distance;
                    nearest = player;
                }
            }
        }
        
        return nearest;
    }
    
    void PerformEnemyAttack(GameObject target)
    {
        Character attacker = currentUnit.GetComponent<Character>();
        Character targetCharacter = target.GetComponent<Character>();
        
        if (attacker?.Stats == null || targetCharacter?.Stats == null)
        {
            EndTurn();
            return;
        }
        
        // Simple damage calculation
        int damage = attacker.Stats.Attack;
        int actualDamage = targetCharacter.Stats.TakeDamage(damage, attacker);
        
        Debug.Log($"{attacker.CharacterName} attacks {targetCharacter.CharacterName} for {actualDamage} damage!");
        SimpleMessageLog.Log($"{attacker.CharacterName} attacks {targetCharacter.CharacterName} for {actualDamage} damage!");
        
        // Check if target was defeated
        if (!targetCharacter.Stats.IsAlive)
        {
            SimpleMessageLog.Log($"{targetCharacter.CharacterName} is defeated!");
            HandleUnitDefeated(target);
        }
        
        // End enemy turn
        EndTurn();
    }
    
    void HandleUnitDefeated(GameObject defeatedUnit)
    {
        // Remove from turn order
        turnOrder.Remove(defeatedUnit);
        
        // Adjust current turn index if necessary
        if (currentTurnIndex >= turnOrder.Count)
        {
            currentTurnIndex = 0;
        }
        
        // Check for battle end conditions
        CheckBattleEndConditions();
    }
    
    void CheckBattleEndConditions()
    {
        bool hasPlayerUnits = turnOrder.Any(unit => unit.CompareTag("Player"));
        bool hasEnemyUnits = turnOrder.Any(unit => unit.CompareTag("Enemy"));
        
        if (!hasPlayerUnits)
        {
            EndBattle(BattleResult.Defeat);
        }
        else if (!hasEnemyUnits)
        {
            EndBattle(BattleResult.Victory);
        }
    }
    
    void EndBattle(BattleResult result)
    {
        Debug.Log($"Battle ended: {result}");
        SimpleMessageLog.Log($"Battle ended: {result}");
        SetBattlePhase(BattlePhase.BattleEnd);
    }
    
    public void EndTurn()
    {
        if (currentPhase != BattlePhase.Combat) return;
        
        Character character = currentUnit?.GetComponent<Character>();
        Debug.Log($"Ending turn for: {character?.CharacterName ?? "Unknown"}");
        
        // Notify other systems
        OnTurnEnd?.Invoke(currentUnit);
        
        // Clear any active selections or modes
        if (actionMenuController != null)
        {
            actionMenuController.DeselectUnit();
        }
        
        // Move to next turn
        NextTurn();
    }
    
    void NextTurn()
    {
        // Refresh unit list in case units were defeated
        RefreshUnitList();
        
        if (turnOrder.Count == 0)
        {
            Debug.LogWarning("No units left for combat!");
            return;
        }
        
        // Move to next unit in turn order
        currentTurnIndex = (currentTurnIndex + 1) % turnOrder.Count;
        StartTurn(currentTurnIndex);
    }
    
    void SetBattlePhase(BattlePhase newPhase)
    {
        if (currentPhase != newPhase)
        {
            BattlePhase oldPhase = currentPhase;
            currentPhase = newPhase;
            
            Debug.Log($"Battle phase changed: {oldPhase} -> {newPhase}");
            OnPhaseChange?.Invoke(newPhase);
        }
    }
    
    // Public getters for other systems
    public BattlePhase GetCurrentPhase() => currentPhase;
    public GameObject GetCurrentUnit() => currentUnit;
    public bool IsPlayerTurn() => currentUnit != null && currentUnit.CompareTag("Player");
    public bool IsEnemyTurn() => currentUnit != null && currentUnit.CompareTag("Enemy");
    public List<GameObject> GetTurnOrder() => new List<GameObject>(turnOrder);
    public int GetCurrentTurnIndex() => currentTurnIndex;
    
    // Manual turn control for testing
    [ContextMenu("End Current Turn")]
    public void ManualEndTurn()
    {
        EndTurn();
    }
    
    [ContextMenu("Start Combat")]
    public void ManualStartCombat()
    {
        StartCombatPhase();
    }
}

[System.Serializable]
public enum BattlePhase
{
    Placement,      // Placing units on battlefield
    Combat,         // Turn-based combat
    BattleEnd       // Battle finished
}

[System.Serializable]
public enum BattleResult
{
    Victory,
    Defeat,
    Draw
}