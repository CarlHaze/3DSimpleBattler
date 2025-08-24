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


    
    // Class bonuses (applied from character class)
    private int classHPBonus = 0;
    private int classAttackBonus = 0;
    private int classDefenseBonus = 0;
    private int classSpeedBonus = 0;

    public int MaxHP => baseMaxHP + classHPBonus;
    public int CurrentHP => currentHP;
    public int Attack => baseAttack + classAttackBonus;
    public int Defense => baseDefense + classDefenseBonus;
    public int Speed => baseSpeed + classSpeedBonus;
    public int AttackRange => baseAttackRange;
    
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

    public void Initialize()
    {
        if (currentHP == 0) // Only set if not already initialized
        {
            currentHP = MaxHP;
        }
    }
    
    public void ApplyClassBonuses(CharacterClass characterClass)
    {
        if (characterClass == null) return;
        
        int oldMaxHP = MaxHP;
        
        classHPBonus = characterClass.GetHealthBonus();
        classAttackBonus = characterClass.GetAttackBonus();
        classDefenseBonus = characterClass.GetDefenseBonus();
        classSpeedBonus = characterClass.GetSpeedBonus();
        
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
    }
    
    public void ClearClassBonuses()
    {
        int oldMaxHP = MaxHP;
        
        classHPBonus = 0;
        classAttackBonus = 0;
        classDefenseBonus = 0;
        classSpeedBonus = 0;
        
        // Cap current HP if it exceeds new max HP
        if (currentHP > MaxHP)
        {
            currentHP = MaxHP;
        }
    }

    public int TakeDamage(int amount)
    {
        int damage = Mathf.Max(amount - Defense, 1);
        currentHP = Mathf.Clamp(currentHP - damage, 0, MaxHP);
        return damage; // Return actual damage dealt
    }

    public void Heal(int amount)
    {
        currentHP = Mathf.Clamp(currentHP + amount, 0, MaxHP);
    }
}