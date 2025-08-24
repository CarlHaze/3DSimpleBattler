using UnityEngine;

[CreateAssetMenu(fileName = "Ranger Logic", menuName = "SimpleBattler/Class Logic/Ranger Logic")]
public class RangerLogic : ClassLogic
{
    [Header("Ranger Settings")]
    [SerializeField] private float baseCriticalChance = 0.15f; // 15% base crit
    [SerializeField] private float criticalMultiplier = 2.0f; // 2x damage on crit
    [SerializeField] private float longRangeCritBonus = 0.1f; // +10% crit at max range
    [SerializeField] private int maxRangeForBonus = 3; // Range 3+ gets bonus
    [SerializeField] private bool showCritEffects = true;
    
    private void Awake()
    {
        logicName = "Ranger Logic";
        logicDescription = "High critical hit chance, bonus at long range";
    }
    
    public override void OnAttackCalculated(Character attacker, Character target, ref int damage)
    {
        // Calculate critical hit chance
        float critChance = GetCriticalChance(attacker);
        
        // Check if this attack crits
        if (Random.value <= critChance)
        {
            int originalDamage = damage;
            damage = Mathf.RoundToInt(damage * GetCriticalMultiplier(attacker));
            
            if (showCritEffects)
            {
                SimpleMessageLog.Log($"{attacker.CharacterName} scores a CRITICAL HIT! (+{damage - originalDamage} damage)");
            }
        }
    }
    
    public override float GetCriticalChance(Character character)
    {
        float totalCritChance = baseCriticalChance;
        
        // Add long range bonus if applicable
        // Note: In a full implementation, you'd need to track the attack range
        // For now, we'll use the character's base attack range as a proxy
        if (character?.Stats != null && character.Stats.AttackRange >= maxRangeForBonus)
        {
            totalCritChance += longRangeCritBonus;
        }
        
        return totalCritChance;
    }
    
    public override float GetCriticalMultiplier(Character character)
    {
        return criticalMultiplier;
    }
    
    public override void OnMoveComplete(Character character, Vector3 fromPosition, Vector3 toPosition)
    {
        // Rangers are mobile - slight speed boost after moving (could implement as temporary modifier)
        if (showCritEffects)
        {
            float distance = Vector3.Distance(fromPosition, toPosition);
            if (distance > 2.0f) // Long move
            {
                SimpleMessageLog.Log($"{character.CharacterName} gains momentum from the long move!");
                // In full implementation: Apply temporary speed boost
            }
        }
    }
    
    public override void OnTurnStart(Character character)
    {
        // Rangers are alert - they could get a slight accuracy bonus each turn
        // This is where you might refresh temporary bonuses or abilities
    }
    
    public override bool CanUseSkill(Character character, SkillSO skill)
    {
        // Rangers might have restrictions on heavy armor skills, etc.
        // For now, allow all skills
        return true;
    }
    
    public override void OnDealDamage(Character attacker, Character target, int finalDamage)
    {
        // Rangers mark targets they hit for follow-up attacks
        // In a full system, you might apply a "marked" debuff to the target
        if (showCritEffects && finalDamage > attacker.Stats.Attack)
        {
            // This was likely a critical hit
            SimpleMessageLog.Log($"{attacker.CharacterName} marks {target.CharacterName} for follow-up!");
        }
    }
    
    public override string GetLogicSummary()
    {
        return $"Ranger Logic: {baseCriticalChance * 100}% crit chance " +
               $"({criticalMultiplier}x damage), +{longRangeCritBonus * 100}% at range {maxRangeForBonus}+";
    }
}