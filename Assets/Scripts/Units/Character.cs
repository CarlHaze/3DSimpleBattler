using UnityEngine;
using System.Collections.Generic;

public class Character : MonoBehaviour
{
    [Header("Basic Information")]
    [SerializeField] private string characterName;
    
    [Header("Character Class")]
    [SerializeField] private CharacterClassSO characterClass;
    
    [Header("Stats (Auto-populated from Class)")]
    [SerializeField] private CharacterStats stats = new CharacterStats();
    
    [Header("Debug Info (Read Only)")]
    [SerializeField, ReadOnly] private string calculatedStats = "";
    
    [Header("Skills")]
    [SerializeField] private List<SkillSO> knownSkills = new List<SkillSO>();

    public string CharacterName => characterName;
    public CharacterStats Stats => stats;
    public CharacterClassSO CharacterClass => characterClass;
    public List<SkillSO> KnownSkills => knownSkills;

    private void Awake()
    {
        InitializeCharacter();
    }
    
    private void InitializeCharacter()
    {
        if (stats == null)
        {
            stats = new CharacterStats();
        }
        
        // Initialize stats with this character reference for class logic callbacks
        if (characterClass != null)
        {
            stats.InitializeFromClassSO(characterClass, this);
            InitializeSkills();
        }
        else
        {
            stats.Initialize(this);
        }
        
        // Set default name if not provided
        if (string.IsNullOrEmpty(characterName))
        {
            characterName = characterClass != null ? characterClass.className : "Unnamed Character";
        }
    }
    
    private void InitializeSkills()
    {
        if (characterClass == null) return;
        
        // Add starting skills from class
        foreach (var skill in characterClass.startingSkills)
        {
            if (skill != null && !knownSkills.Contains(skill))
            {
                knownSkills.Add(skill);
            }
        }
    }
    
    public void SetCharacterClass(CharacterClassSO newClass)
    {
        // Clear old class bonuses
        if (characterClass != null)
        {
            stats.ClearClassBonuses();
        }
        
        // Apply new class
        characterClass = newClass;
        if (characterClass != null)
        {
            stats.InitializeFromClassSO(characterClass, this);
            InitializeSkills();
        }
    }
    
    public string GetClassInfo()
    {
        if (characterClass != null)
        {
            return $"{characterClass.className}: {characterClass.description}";
        }
        return "No class assigned";
    }
    
    // Skill management
    public bool CanUseSkill(SkillSO skill)
    {
        if (!knownSkills.Contains(skill)) return false;
        if (!skill.CanUseSkill(this)) return false;
        if (characterClass?.ClassLogic != null)
        {
            return characterClass.ClassLogic.CanUseSkill(this, skill);
        }
        return true;
    }
    
    public void LearnSkill(SkillSO skill)
    {
        if (skill != null && !knownSkills.Contains(skill))
        {
            knownSkills.Add(skill);
        }
    }
    
    public void ForgetSkill(SkillSO skill)
    {
        if (knownSkills.Contains(skill))
        {
            knownSkills.Remove(skill);
        }
    }
    
    // Validation for designer use
    void OnValidate()
    {
        if (Application.isPlaying) return;
        
        // Auto-populate stats from class in editor
        if (characterClass != null && stats != null)
        {
            // Reset stats to avoid accumulating bonuses
            stats = new CharacterStats();
            stats.InitializeFromClassSO(characterClass, this);
            
            // Update debug display
            calculatedStats = stats.StatsDisplay;
            
            // Set default name if not provided
            if (string.IsNullOrEmpty(characterName))
            {
                characterName = characterClass.className;
            }
        }
        else
        {
            calculatedStats = "No class assigned";
        }
    }
}