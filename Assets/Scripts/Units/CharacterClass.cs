using UnityEngine;

[System.Serializable]
public class CharacterClass
{
    [SerializeField] protected string className = "None";
    
    public string ClassName => className;
    
    public virtual int GetAttackBonus() { return 0; }
    public virtual int GetDefenseBonus() { return 0; }
    public virtual int GetHealthBonus() { return 0; }
    public virtual int GetSpeedBonus() { return 0; }
    
    public virtual string GetDescription()
    {
        return $"{className} - A basic character class";
    }
}