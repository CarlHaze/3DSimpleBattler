using UnityEngine;

[System.Serializable]
public class CharacterStats
{
    [Header("Base Stats")]
    [SerializeField] private int baseMaxHP = 100;
    [SerializeField] private int currentHP;
    [SerializeField] private int baseAttack = 10;
    [SerializeField] private int baseDefense = 5;
    [SerializeField] private int baseSpeed = 5;
    [SerializeField] private int baseAttackRange = 1;
    
    // Class bonuses (applied from character class SO)
    private int classHPBonus = 0;
    private int classAttackBonus = 0;
    private int classDefenseBonus = 0;
    private int classSpeedBonus = 0;
    private int classAttackRangeBonus = 0;
    
    // Reference to the owning character for class logic callbacks
    private Character owningCharacter;

    public int MaxHP => baseMaxHP + classHPBonus;
    public int CurrentHP => currentHP;
    public int Attack => baseAttack + classAttackBonus;
    public int Defense => baseDefense + classDefenseBonus;
    public int Speed => baseSpeed + classSpeedBonus;
    public int AttackRange => baseAttackRange + classAttackRangeBonus;
    
    // Base stats getters (without class bonuses)
    public int BaseMaxHP => baseMaxHP;
    public int BaseAttack => baseAttack;
    public int BaseDefense => baseDefense;
    public int BaseSpeed => baseSpeed;
    public int BaseAttackRange => baseAttackRange;

    public bool IsAlive => currentHP > 0;

    public CharacterStats()
    {
        Initialize();
    }

    public void Initialize(Character character = null)
    {
        owningCharacter = character;
        if (currentHP == 0) // Only set if not already initialized
        {
            currentHP = MaxHP;
        }
    }
    
    // Initialize from CharacterClassSO
    public void InitializeFromClassSO(CharacterClassSO classSO, Character character = null)
    {
        owningCharacter = character;
        
        // Set base stats from SO
        baseMaxHP = classSO.baseMaxHP;
        baseAttack = classSO.baseAttack;
        baseDefense = classSO.baseDefense;
        baseSpeed = classSO.baseSpeed;
        baseAttackRange = classSO.baseAttackRange;
        
        // Apply class bonuses
        ApplyClassBonuses(classSO);
        
        // Initialize HP
        if (currentHP == 0)
        {
            currentHP = MaxHP;
        }
    }
    
    public void ApplyClassBonuses(CharacterClassSO classSO)
    {
        if (classSO == null) return;
        
        int oldMaxHP = MaxHP;
        
        classHPBonus = classSO.healthBonus;
        classAttackBonus = classSO.attackBonus;
        classDefenseBonus = classSO.defenseBonus;
        classSpeedBonus = classSO.speedBonus;
        classAttackRangeBonus = classSO.attackRangeBonus;
        
        // If HP increased due to class, increase current HP proportionally
        if (MaxHP > oldMaxHP)
        {
            float hpRatio = (float)currentHP / oldMaxHP;
            currentHP = Mathf.RoundToInt(MaxHP * hpRatio);
        }
        // If HP decreased, cap current HP to new max
        else if (currentHP > MaxHP)
        {
            currentHP = MaxHP;
        }
        
        // Notify class logic of stat changes
        if (owningCharacter != null && classSO.ClassLogic != null)
        {
            classSO.ClassLogic.OnStatsCalculated(owningCharacter, this);
        }
    }
    
    public void ClearClassBonuses()
    {
        int oldMaxHP = MaxHP;
        
        classHPBonus = 0;
        classAttackBonus = 0;
        classDefenseBonus = 0;
        classSpeedBonus = 0;
        classAttackRangeBonus = 0;
        
        // Cap current HP if it exceeds new max HP
        if (currentHP > MaxHP)
        {
            currentHP = MaxHP;
        }
    }

    public int TakeDamage(int amount, Character attacker = null)
    {
        int oldHP = currentHP;
        int damage = Mathf.Max(amount - Defense, 1);
        
        // Allow class logic to modify incoming damage
        if (owningCharacter?.CharacterClass?.ClassLogic != null)
        {
            owningCharacter.CharacterClass.ClassLogic.OnTakeDamage(owningCharacter, attacker, ref damage);
        }
        
        currentHP = Mathf.Clamp(currentHP - damage, 0, MaxHP);
        
        // Notify class logic of health change
        if (owningCharacter?.CharacterClass?.ClassLogic != null && currentHP != oldHP)
        {
            owningCharacter.CharacterClass.ClassLogic.OnHealthChanged(owningCharacter, oldHP, currentHP);
        }
        
        return damage; // Return actual damage dealt
    }

    public void Heal(int amount)
    {
        int oldHP = currentHP;
        currentHP = Mathf.Clamp(currentHP + amount, 0, MaxHP);
        
        // Notify class logic of health change
        if (owningCharacter?.CharacterClass?.ClassLogic != null && currentHP != oldHP)
        {
            owningCharacter.CharacterClass.ClassLogic.OnHealthChanged(owningCharacter, oldHP, currentHP);
        }
    }
}