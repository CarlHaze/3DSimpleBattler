using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "New Skill", menuName = "SimpleBattler/Skill")]
public class SkillSO : ScriptableObject
{
    [Header("Basic Information")]
    public string skillName = "Basic Attack";
    public string description = "A simple attack";
    public Sprite skillIcon;
    
    [Header("Skill Properties")]
    public SkillType skillType = SkillType.Active;
    public SkillTarget targetType = SkillTarget.Enemy;
    public int manaCost = 0;
    public int cooldown = 0;
    public int range = 1;
    
    [Header("Effects")]
    public int baseDamage = 0;
    public int healing = 0;
    public float damageMultiplier = 1.0f;
    public List<StatusEffectSO> statusEffects = new List<StatusEffectSO>();
    
    [Header("Animation & VFX")]
    public string animationTrigger = "";
    public GameObject vfxPrefab;
    public AudioClip soundEffect;
    
    [Header("Requirements")]
    public int minLevel = 1;
    public List<SkillSO> prerequisiteSkills = new List<SkillSO>();
    public List<string> requiredClasses = new List<string>();
    
    // Validation
    void OnValidate()
    {
        if (string.IsNullOrEmpty(skillName))
            skillName = name;
            
        manaCost = Mathf.Max(0, manaCost);
        cooldown = Mathf.Max(0, cooldown);
        range = Mathf.Max(1, range);
        minLevel = Mathf.Max(1, minLevel);
    }
    
    // Utility methods
    public bool CanUseSkill(Character character)
    {
        if (character?.Stats == null) return false;
        
        // Check mana cost (if implementing mana system)
        // Check cooldown (if implementing cooldown system)
        // Check class requirements
        if (requiredClasses.Count > 0)
        {
            bool hasRequiredClass = false;
            if (character.CharacterClass != null)
            {
                hasRequiredClass = requiredClasses.Contains(character.CharacterClass.className);
            }
            if (!hasRequiredClass) return false;
        }
        
        return true;
    }
    
    public string GetSkillSummary()
    {
        string summary = $"{skillName}: {description}";
        if (baseDamage > 0) summary += $" ({baseDamage} damage)";
        if (healing > 0) summary += $" ({healing} healing)";
        if (manaCost > 0) summary += $" (Cost: {manaCost})";
        if (cooldown > 0) summary += $" (CD: {cooldown})";
        return summary;
    }
}

[System.Serializable]
public enum SkillType
{
    Active,     // Must be actively used
    Passive,    // Always active
    Toggle      // Can be turned on/off
}

[System.Serializable]
public enum SkillTarget
{
    Self,
    Ally,
    Enemy,
    AnyUnit,
    Ground,
    Area
}

// Placeholder for status effects - could be expanded into full ScriptableObject system
[System.Serializable]
public class StatusEffectSO : ScriptableObject
{
    public string effectName = "Buff";
    public int duration = 1;
    public EffectType effectType = EffectType.Buff;
    
    [System.Serializable]
    public enum EffectType
    {
        Buff,
        Debuff,
        DoT,    // Damage over time
        HoT     // Healing over time
    }
}