using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SkillSelectionController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement skillPanel;
    private VisualElement skillContainer;
    private Button skillCancelButton;
    private ScrollView skillScrollView;
    
    private ActionMenuController actionMenuController;
    private AttackManager attackManager;
    private SkillManager skillManager;
    private GameObject selectedUnit;
    private List<SkillSO> availableSkills = new List<SkillSO>();
    
    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        actionMenuController = FindFirstObjectByType<ActionMenuController>();
        attackManager = FindFirstObjectByType<AttackManager>();
        skillManager = FindFirstObjectByType<SkillManager>();
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on SkillSelectionController!");
            return;
        }
        
        VisualElement root = uiDocument.rootVisualElement;
        skillPanel = root.Q<VisualElement>("SkillPanel");
        skillContainer = root.Q<VisualElement>("SkillContainer");
        skillCancelButton = root.Q<Button>("SkillCancelButton");
        skillScrollView = root.Q<ScrollView>("SkillScrollView");
        
        if (skillCancelButton != null)
        {
            skillCancelButton.clicked += OnSkillCancelClicked;
        }
        
        HideSkillMenu();
    }
    
    public void ShowSkillMenu(GameObject unit)
    {
        selectedUnit = unit;
        availableSkills.Clear();
        
        if (unit == null)
        {
            Debug.LogError("No unit provided for skill selection!");
            return;
        }
        
        Debug.Log($"ShowSkillMenu: Opening skill menu for {unit.name}");
        
        Character character = unit.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogError("Unit does not have Character component!");
            return;
        }
        
        // Get all available skills for this character
        List<SkillSO> knownSkills = character.KnownSkills;
        Debug.Log($"Character has {(knownSkills != null ? knownSkills.Count : 0)} known skills");
        
        if (knownSkills != null)
        {
            foreach (SkillSO skill in knownSkills)
            {
                if (skill != null && skill.CanUseSkill(character))
                {
                    Debug.Log($"Adding skill: {skill.skillName}");
                    availableSkills.Add(skill);
                }
                else if (skill == null)
                {
                    Debug.LogWarning("Found null skill in known skills list");
                }
                else
                {
                    Debug.Log($"Skill {skill.skillName} cannot be used by this character");
                }
            }
        }
        
        // Add basic attack as default option
        availableSkills.Insert(0, null); // null represents basic attack
        Debug.Log($"Total available skills: {availableSkills.Count}");
        
        PopulateSkillButtons();
        
        if (skillPanel != null)
        {
            skillPanel.style.display = DisplayStyle.Flex;
            Debug.Log("Skill panel shown");
        }
        else
        {
            Debug.LogError("Skill panel is null!");
        }
    }
    
    public void HideSkillMenu()
    {
        if (skillPanel != null)
        {
            skillPanel.style.display = DisplayStyle.None;
        }
        selectedUnit = null;
        availableSkills.Clear();
    }
    
    private void PopulateSkillButtons()
    {
        if (skillContainer == null) return;
        
        // Clear existing buttons
        skillContainer.Clear();
        
        foreach (SkillSO skill in availableSkills)
        {
            Button skillButton = new Button();
            
            if (skill == null)
            {
                // Basic attack
                skillButton.text = "Basic Attack";
                skillButton.clicked += () => OnSkillSelected(null);
            }
            else
            {
                skillButton.text = $"{skill.skillName}";
                if (skill.manaCost > 0)
                {
                    skillButton.text += $" (Cost: {skill.manaCost})";
                }
                skillButton.clicked += () => OnSkillSelected(skill);
            }
            
            skillButton.AddToClassList("button");
            skillButton.style.marginBottom = 2;
            skillButton.style.marginLeft = 5;
            skillButton.style.marginRight = 5;
            
            skillContainer.Add(skillButton);
        }
    }
    
    private void OnSkillSelected(SkillSO selectedSkill)
    {
        if (selectedUnit == null) 
        {
            Debug.LogError("OnSkillSelected: No unit selected!");
            return;
        }
        
        Debug.Log($"OnSkillSelected: {(selectedSkill != null ? selectedSkill.skillName : "Basic Attack")} selected for {selectedUnit.name}");
        
        // Store the unit before hiding the menu so we don't lose the reference
        GameObject unitToUse = selectedUnit;
        
        HideSkillMenu();
        
        if (selectedSkill == null)
        {
            // Basic attack - use existing attack system
            if (attackManager != null)
            {
                Debug.Log($"Starting basic attack mode for {unitToUse.name}");
                attackManager.StartAttackMode(unitToUse);
            }
            else
            {
                Debug.LogError("AttackManager not found!");
            }
        }
        else
        {
            // Use selected skill
            Debug.Log($"Using skill: {selectedSkill.skillName} for {unitToUse.name}");
            UseSkill(selectedSkill, unitToUse);
        }
    }
    
    private void UseSkill(SkillSO skill, GameObject unit)
    {
        if (skill == null || unit == null) return;
        
        // For now, treat skills like attacks but with skill-specific logic
        // You can expand this to handle different skill types
        Character character = unit.GetComponent<Character>();
        if (character == null) return;
        
        SimpleMessageLog.Log($"{character.CharacterName} prepares to use {skill.skillName}!");
        
        // Start skill targeting mode (similar to attack mode but with skill parameters)
        StartSkillTargetingMode(skill, unit);
    }
    
    private void StartSkillTargetingMode(SkillSO skill, GameObject unit)
    {
        // Use the new SkillManager for skill-specific targeting
        if (skillManager != null)
        {
            Debug.Log($"Starting skill targeting mode for {skill.skillName} with unit {unit.name}");
            skillManager.StartSkillMode(unit, skill);
        }
        else
        {
            Debug.LogError("SkillManager not found! Falling back to AttackManager");
            // Fallback to attack manager for basic attacks
            if (attackManager != null)
            {
                attackManager.StartAttackMode(unit);
            }
        }
    }
    
    private void OnSkillCancelClicked()
    {
        HideSkillMenu();
        
        // Re-show action menu if needed
        if (actionMenuController != null && selectedUnit != null)
        {
            actionMenuController.SelectUnit(selectedUnit);
        }
    }
    
    public bool IsSkillMenuVisible()
    {
        return skillPanel != null && skillPanel.style.display == DisplayStyle.Flex;
    }
}