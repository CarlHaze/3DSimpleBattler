using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Character Class", menuName = "SimpleBattler/Character Class")]
public class CharacterClassSO : ScriptableObject
{
    [Header("Basic Information")]
    public string className = "Warrior";
    public string description = "A fierce melee fighter";
    public Sprite classIcon;
    
    [Header("Base Stats")]
    public int baseMaxHP = 100;
    public int baseAttack = 10;
    public int baseDefense = 5;
    public int baseSpeed = 5;
    public int baseAttackRange = 1;
    
    [Header("Stat Bonuses")]
    public int healthBonus = 0;
    public int attackBonus = 0;
    public int defenseBonus = 0;
    public int speedBonus = 0;
    public int attackRangeBonus = 0;
    
    [Header("Class Logic")]
    [SerializeField] private ClassLogic classLogic;
    
    [Header("Skills")]
    public List<SkillSO> availableSkills = new List<SkillSO>();
    public List<SkillSO> startingSkills = new List<SkillSO>();
    
    [Header("Visual")]
    public Color classColor = Color.white;
    public GameObject classPrefab; // Optional custom prefab for this class
    
    // Public accessors
    public ClassLogic ClassLogic => classLogic;
    
    // Stat calculation methods
    public int GetTotalMaxHP(int baseHP) => baseHP + healthBonus;
    public int GetTotalAttack(int baseAttack) => baseAttack + attackBonus;
    public int GetTotalDefense(int baseDefense) => baseDefense + defenseBonus;
    public int GetTotalSpeed(int baseSpeed) => baseSpeed + speedBonus;
    public int GetTotalAttackRange(int baseRange) => baseRange + attackRangeBonus;
    
    // Designer-friendly stat display
    public string GetStatsDisplay()
    {
        return $"HP: {baseMaxHP} (+{healthBonus}), ATK: {baseAttack} (+{attackBonus}), " +
               $"DEF: {baseDefense} (+{defenseBonus}), SPD: {baseSpeed} (+{speedBonus}), " +
               $"RNG: {baseAttackRange} (+{attackRangeBonus})";
    }
    
    // Validation for designer usage
    void OnValidate()
    {
        if (string.IsNullOrEmpty(className))
            className = name;
            
        // Ensure stats are not negative
        baseMaxHP = Mathf.Max(1, baseMaxHP);
        baseAttack = Mathf.Max(0, baseAttack);
        baseDefense = Mathf.Max(0, baseDefense);
        baseSpeed = Mathf.Max(0, baseSpeed);
        baseAttackRange = Mathf.Max(1, baseAttackRange);
    }
}