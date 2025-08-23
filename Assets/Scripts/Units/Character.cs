using UnityEngine;

public class Character : MonoBehaviour
{
    [SerializeField] private string characterName;
    [SerializeField] private CharacterStats stats = new CharacterStats();
    
    [Header("Character Class")]
    [SerializeField] private bool hasWarriorClass = false;

    private CharacterClass characterClass;

    public string CharacterName => characterName;
    public CharacterStats Stats => stats;
    public CharacterClass CharacterClass => characterClass;

    private void Awake()
    {
        if (stats == null)
        {
            stats = new CharacterStats();
        }
        
        ApplyCharacterClass();
        stats.Initialize();
    }
    
    private void ApplyCharacterClass()
    {
        // Clear any existing class first
        characterClass = null;
        
        // Simple class assignment based on boolean (can be expanded later)
        if (hasWarriorClass)
        {
            characterClass = new WarriorClass();
        }
        
        // Apply class bonuses if we have stats
        if (stats != null && characterClass != null)
        {
            stats.ApplyClassBonuses(characterClass);
        }
    }
    
    public void SetCharacterClass(CharacterClass newClass)
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
            stats.ApplyClassBonuses(characterClass);
        }
    }
    
    public string GetClassInfo()
    {
        if (characterClass != null)
        {
            return characterClass.GetDescription();
        }
        return "No class assigned";
    }
}