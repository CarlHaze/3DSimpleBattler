using UnityEngine;

[CreateAssetMenu(fileName = "Warrior Logic", menuName = "SimpleBattler/Class Logic/Warrior Logic")]
public class WarriorLogic : ClassLogic
{
    [Header("Warrior Settings")]
    [SerializeField] private float rageThreshold = 0.5f; // Activate rage at 50% health
    [SerializeField] private float rageDamageMultiplier = 1.5f;
    [SerializeField] private int rageDefenseBonus = 3;
    [SerializeField] private bool showRageEffects = true;
    
    private void Awake()
    {
        logicName = "Warrior Logic";
        logicDescription = "Gains rage mode at low health, increasing damage and defense";
    }
    
    public override void OnAttackCalculated(Character attacker, Character target, ref int damage)
    {
        // Apply rage damage bonus if warrior is in rage mode
        if (IsInRageMode(attacker))
        {
            int originalDamage = damage;
            damage = Mathf.RoundToInt(damage * rageDamageMultiplier);
            
            if (showRageEffects)
            {
                SimpleMessageLog.Log($"{attacker.CharacterName} attacks with RAGE! (+{damage - originalDamage} damage)");
            }
        }
    }
    
    public override void OnStatsCalculated(Character character, CharacterStats stats)
    {
        // Apply rage defense bonus dynamically
        if (IsInRageMode(character))
        {
            // We can't directly modify stats here, but we can apply temporary bonuses
            // This would need integration with a buff/debuff system
            if (showRageEffects)
            {
                // For now, just log it. In a full implementation, you'd apply a temporary modifier
                Debug.Log($"{character.CharacterName} is in RAGE MODE! (+{rageDefenseBonus} defense)");
            }
        }
    }
    
    public override void OnHealthChanged(Character character, int oldHealth, int newHealth)
    {
        bool wasInRage = IsInRageModeForHealth(oldHealth, character.Stats.MaxHP);
        bool nowInRage = IsInRageModeForHealth(newHealth, character.Stats.MaxHP);
        
        // Entered rage mode
        if (!wasInRage && nowInRage)
        {
            if (showRageEffects)
            {
                SimpleMessageLog.Log($"{character.CharacterName} enters RAGE MODE!");
            }
        }
        // Exited rage mode (healed above threshold)
        else if (wasInRage && !nowInRage)
        {
            if (showRageEffects)
            {
                SimpleMessageLog.Log($"{character.CharacterName} exits rage mode");
            }
        }
    }
    
    public override void OnTakeDamage(Character character, Character attacker, ref int damage)
    {
        // Warriors are tough - reduce damage slightly when in rage
        if (IsInRageMode(character))
        {
            int reduction = rageDefenseBonus;
            damage = Mathf.Max(1, damage - reduction); // Always take at least 1 damage
            
            if (showRageEffects && reduction > 0)
            {
                SimpleMessageLog.Log($"{character.CharacterName}'s rage reduces damage by {reduction}!");
            }
        }
    }
    
    public override void OnDealDamage(Character attacker, Character target, int finalDamage)
    {
        // Warrior gains slight health when dealing damage in rage mode (lifesteal effect)
        if (IsInRageMode(attacker))
        {
            int lifesteal = Mathf.Max(1, finalDamage / 4); // 25% lifesteal
            attacker.Stats.Heal(lifesteal);
            
            if (showRageEffects)
            {
                SimpleMessageLog.Log($"{attacker.CharacterName} gains {lifesteal} health from rage!");
            }
        }
    }
    
    // Utility methods
    private bool IsInRageMode(Character character)
    {
        if (character?.Stats == null) return false;
        return IsInRageModeForHealth(character.Stats.CurrentHP, character.Stats.MaxHP);
    }
    
    private bool IsInRageModeForHealth(int currentHP, int maxHP)
    {
        if (maxHP <= 0) return false;
        float healthPercent = (float)currentHP / maxHP;
        return healthPercent <= rageThreshold;
    }
    
    public override string GetLogicSummary()
    {
        return $"Warrior Logic: Rage mode at {rageThreshold * 100}% health " +
               $"({rageDamageMultiplier}x damage, +{rageDefenseBonus} defense, lifesteal)";
    }
}