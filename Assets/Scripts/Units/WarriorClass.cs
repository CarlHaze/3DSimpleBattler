using UnityEngine;

[System.Serializable]
public class WarriorClass : CharacterClass
{
    public WarriorClass()
    {
        className = "Warrior";
    }
    
    public override int GetAttackBonus()
    {
        return 5; // +5 attack bonus
    }
    
    public override int GetDefenseBonus()
    {
        return 3; // +3 defense bonus
    }
    
    public override int GetHealthBonus()
    {
        return 20; // +20 HP bonus
    }
    
    public override int GetSpeedBonus()
    {
        return -1; // -1 speed (warriors are slower but stronger)
    }
    
    public override string GetDescription()
    {
        return "Warrior - Strong melee fighter with high attack and defense. +5 ATK, +3 DEF, +20 HP, -1 SPD";
    }
}