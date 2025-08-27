using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class SkillSelectionController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement skillPanel;
    private ListView skillListView;
    private Label skillDescription;
    private Label skillCost;
    private Button backButton;
    
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
        skillListView = root.Q<ListView>("SkillListView");
        skillDescription = root.Q<Label>("SkillDescription");
        skillCost = root.Q<Label>("SkillCost");
        backButton = root.Q<Button>("BackButton");
        
        if (backButton != null)
        {
            backButton.clicked += OnBackButtonClicked;
        }
        
        // Set up ListView
        if (skillListView != null)
        {
            skillListView.makeItem = () => 
            {
                var label = new Label();
                label.AddToClassList("text");
                label.style.paddingLeft = 10;
                label.style.paddingTop = 5;
                label.style.paddingBottom = 5;
                label.style.fontSize = 14;
                return label;
            };
            skillListView.bindItem = (element, index) =>
            {
                var label = element as Label;
                if (index < availableSkills.Count)
                {
                    SkillSO skill = availableSkills[index];
                    if (skill == null)
                    {
                        label.text = "Basic Attack";
                    }
                    else
                    {
                        label.text = skill.skillName;
                        if (skill.apCost > 0)
                        {
                            label.text += $" (AP: {skill.apCost})";
                        }
                    }
                }
            };
            skillListView.selectionChanged += OnSkillSelectionChanged;
        }
        
        // Hide menu initially using both display and class
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
        Debug.Log($"Character {character.CharacterName} has {(knownSkills != null ? knownSkills.Count : 0)} known skills");
        
        // Also check if character has a class with starting skills
        if (character.CharacterClass != null && character.CharacterClass.startingSkills != null)
        {
            Debug.Log($"Character class {character.CharacterClass.className} has {character.CharacterClass.startingSkills.Count} starting skills");
        }

        if (knownSkills != null && knownSkills.Count > 0)
        {
            foreach (SkillSO skill in knownSkills)
            {
                if (skill != null && skill.CanUseSkill(character))
                {
                    Debug.Log($"Adding skill: {skill.skillName} (AP Cost: {skill.apCost})");
                    availableSkills.Add(skill);
                }
                else if (skill == null)
                {
                    Debug.LogWarning("Found null skill in known skills list");
                }
                else
                {
                    Debug.Log($"Skill {skill.skillName} cannot be used by this character (AP check failed?)");
                }
            }
        }
        else
        {
            Debug.LogWarning($"Character {character.CharacterName} has no known skills! Check if skills are assigned in the Character component or CharacterClass ScriptableObject.");
        }
        
        // Add basic attack as default option
        availableSkills.Insert(0, null); // null represents basic attack
        Debug.Log($"Total available skills: {availableSkills.Count}");
        
        // Update ListView itemsSource
        if (skillListView != null)
        {
            Debug.Log($"Updating ListView with {availableSkills.Count} skills");
            skillListView.itemsSource = availableSkills;
            skillListView.Rebuild();
            skillListView.RefreshItems();
            
            // Verify the list is populated
            if (skillListView.itemsSource != null)
            {
                Debug.Log($"ListView itemsSource has {skillListView.itemsSource.Count} items");
            }
        }
        else
        {
            Debug.LogError("skillListView is null!");
        }
        
        // Clear description initially
        if (skillDescription != null)
        {
            skillDescription.text = "Select a skill to see its description";
        }
        if (skillCost != null)
        {
            skillCost.text = "";
        }
        
        if (skillPanel != null)
        {
            skillPanel.style.display = DisplayStyle.Flex;
            skillPanel.RemoveFromClassList("hide");
            Debug.Log("Skill panel shown");
            
            // Force focus on the list view for keyboard navigation
            if (skillListView != null && availableSkills.Count > 0)
            {
                skillListView.selectedIndex = 0;
                skillListView.Focus();
            }
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
            skillPanel.AddToClassList("hide");
        }
        selectedUnit = null;
        availableSkills.Clear();
    }
    
    private void OnSkillSelectionChanged(IEnumerable<object> selectedItems)
    {
        foreach (var item in selectedItems)
        {
            SkillSO skill = item as SkillSO;
            UpdateSkillDescription(skill);
        }
    }
    
    void Update()
    {
        // Only handle input when skill menu is visible
        if (!IsSkillMenuVisible()) return;
        
        // Handle keyboard/controller navigation
        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.E))
        {
            UseSelectedSkill();
        }
        else if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.Q))
        {
            OnBackButtonClicked();
        }
        
        // Handle double-click to use skill
        if (skillListView != null && skillListView.selectedIndex >= 0)
        {
            if (Input.GetMouseButtonDown(0))
            {
                // Check if we're double-clicking on the selected skill
                if (Time.time - lastClickTime < 0.3f && lastSelectedIndex == skillListView.selectedIndex)
                {
                    UseSelectedSkill();
                }
                lastClickTime = Time.time;
                lastSelectedIndex = skillListView.selectedIndex;
            }
        }
    }
    
    private float lastClickTime = 0f;
    private int lastSelectedIndex = -1;
    
    private void UpdateSkillDescription(SkillSO skill)
    {
        if (selectedUnit == null) return;
        
        Character character = selectedUnit.GetComponent<Character>();
        if (character == null) return;
        
        if (skill == null)
        {
            // Basic attack
            if (skillDescription != null)
            {
                skillDescription.text = "Basic Attack: A standard attack dealing base damage.";
            }
            if (skillCost != null)
            {
                skillCost.text = "AP Cost: 1";
            }
        }
        else
        {
            // Show skill description
            if (skillDescription != null)
            {
                skillDescription.text = skill.description;
            }
            if (skillCost != null)
            {
                skillCost.text = $"AP Cost: {skill.apCost}";
            }
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
    
    private void OnBackButtonClicked()
    {
        HideSkillMenu();
        
        // Re-show action menu if needed
        if (actionMenuController != null && selectedUnit != null)
        {
            actionMenuController.SelectUnit(selectedUnit);
        }
    }
    
    public void UseSelectedSkill()
    {
        if (skillListView != null && skillListView.selectedIndex >= 0)
        {
            SkillSO selectedSkill = availableSkills[skillListView.selectedIndex];
            OnSkillSelected(selectedSkill);
        }
    }
    
    public bool IsSkillMenuVisible()
    {
        return skillPanel != null && skillPanel.style.display == DisplayStyle.Flex && !skillPanel.ClassListContains("hide");
    }
}