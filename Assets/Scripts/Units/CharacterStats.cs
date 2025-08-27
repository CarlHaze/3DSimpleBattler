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
    
    [Header("Action & Move Points")]
    [SerializeField] private int baseMaxAP = 2; // Action Points per turn (for skills/attacks)
    [SerializeField] private int currentAP;
    [SerializeField] private int baseMaxMP = 3; // Move Points per turn (for movement)
    [SerializeField] private int currentMP;
    
    // Class bonuses (applied from character class SO)
    private int classHPBonus = 0;
    private int classAttackBonus = 0;
    private int classDefenseBonus = 0;
    private int classSpeedBonus = 0;
    private int classAttackRangeBonus = 0;
    private int classAPBonus = 0;
    private int classMPBonus = 0;
    
    // Reference to the owning character for class logic callbacks
    private Character owningCharacter;

    public int MaxHP => baseMaxHP + classHPBonus;
    public int CurrentHP => currentHP;
    public int Attack => baseAttack + classAttackBonus;
    public int Defense => baseDefense + classDefenseBonus;
    public int Speed => baseSpeed + classSpeedBonus;
    public int AttackRange => baseAttackRange + classAttackRangeBonus;
    
    // Action Points and Move Points
    public int MaxAP => baseMaxAP + classAPBonus;
    public int CurrentAP => currentAP;
    public int MaxMP => baseMaxMP + classMPBonus;
    public int CurrentMP => currentMP;
    
    // Base stats getters (without class bonuses)
    public int BaseMaxHP => baseMaxHP;
    public int BaseAttack => baseAttack;
    public int BaseDefense => baseDefense;
    public int BaseSpeed => baseSpeed;
    public int BaseAttackRange => baseAttackRange;

    public bool IsAlive => currentHP > 0;
    
    // Debug display for inspector
    public string StatsDisplay => $"HP: {CurrentHP}/{MaxHP} | ATK: {Attack} | DEF: {Defense} | SPD: {Speed} | RNG: {AttackRange}";

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
        
        // Initialize AP and MP to max values
        RefreshActionPoints();
        RefreshMovePoints();
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
        
        // Always reset HP to match new MaxHP (important for designer workflow)
        currentHP = MaxHP;
        
        // Initialize AP and MP to max values
        RefreshActionPoints();
        RefreshMovePoints();
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
    
    // Action Points Management
    public void RefreshActionPoints()
    {
        currentAP = MaxAP;
    }
    
    public bool CanSpendAP(int cost)
    {
        return currentAP >= cost;
    }
    
    public bool SpendAP(int cost)
    {
        if (!CanSpendAP(cost)) return false;
        currentAP = Mathf.Max(0, currentAP - cost);
        return true;
    }
    
    public void RestoreAP(int amount)
    {
        currentAP = Mathf.Clamp(currentAP + amount, 0, MaxAP);
    }
    
    // Move Points Management
    public void RefreshMovePoints()
    {
        currentMP = MaxMP;
    }
    
    public bool CanSpendMP(int cost)
    {
        return currentMP >= cost;
    }
    
    public bool SpendMP(int cost)
    {
        if (!CanSpendMP(cost)) return false;
        currentMP = Mathf.Max(0, currentMP - cost);
        return true;
    }
    
    public void RestoreMP(int amount)
    {
        currentMP = Mathf.Clamp(currentMP + amount, 0, MaxMP);
    }
    
    // Utility methods for turn management
    public void RefreshTurnResources()
    {
        RefreshActionPoints();
        RefreshMovePoints();
    }
    
    public bool HasActionsRemaining()
    {
        return currentAP > 0 || currentMP > 0;
    }
}