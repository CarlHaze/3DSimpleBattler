using UnityEngine;

/// <summary>
/// Abstract base class for defining class-specific behaviors as ScriptableObjects.
/// Only implement methods you need - empty virtual methods provide no-op defaults.
/// </summary>
[CreateAssetMenu(fileName = "New Class Logic", menuName = "SimpleBattler/Class Logic/Base Logic")]
public abstract class ClassLogic : ScriptableObject
{
    [Header("Logic Information")]
    public string logicName = "Base Logic";
    public string logicDescription = "Base class logic with no special behaviors";
    
    // Combat Events
    public virtual void OnAttackCalculated(Character attacker, Character target, ref int damage)
    {
        // Override to modify outgoing damage
    }
    
    public virtual void OnTakeDamage(Character character, Character attacker, ref int damage)
    {
        // Override to modify incoming damage
    }
    
    public virtual void OnDealDamage(Character attacker, Character target, int finalDamage)
    {
        // Override for post-damage effects (healing, buffs, etc.)
    }
    
    public virtual void OnUnitDefeated(Character character, Character killer)
    {
        // Override for death effects
    }
    
    // Turn-based Events (if implementing turn system later)
    public virtual void OnTurnStart(Character character)
    {
        // Override for turn start effects (regeneration, status effects, etc.)
    }
    
    public virtual void OnTurnEnd(Character character)
    {
        // Override for turn end effects
    }
    
    // Movement Events
    public virtual void OnMoveStart(Character character, Vector3 fromPosition, Vector3 toPosition)
    {
        // Override for pre-movement effects
    }
    
    public virtual void OnMoveComplete(Character character, Vector3 fromPosition, Vector3 toPosition)
    {
        // Override for post-movement effects
    }
    
    // Skill Events
    public virtual void OnSkillUsed(Character character, SkillSO skill, GameObject target)
    {
        // Override for skill-related effects
    }
    
    // Stat Modification Events
    public virtual void OnStatsCalculated(Character character, CharacterStats stats)
    {
        // Override to apply dynamic stat modifications
        // Called after base stats and class bonuses are applied
    }
    
    // Status Events
    public virtual void OnHealthChanged(Character character, int oldHealth, int newHealth)
    {
        // Override for health change reactions (low health buffs, etc.)
    }
    
    // Utility Methods
    public virtual bool CanUseSkill(Character character, SkillSO skill)
    {
        // Override to add class-specific skill restrictions
        return true;
    }
    
    public virtual float GetCriticalChance(Character character)
    {
        // Override to provide class-specific crit chances
        return 0f; // Base classes have no crit
    }
    
    public virtual float GetCriticalMultiplier(Character character)
    {
        // Override to provide class-specific crit damage
        return 1.5f; // Standard 1.5x crit damage
    }
    
    // Designer-friendly display
    public virtual string GetLogicSummary()
    {
        return $"{logicName}: {logicDescription}";
    }
    
    void OnValidate()
    {
        if (string.IsNullOrEmpty(logicName))
            logicName = GetType().Name;
    }
}