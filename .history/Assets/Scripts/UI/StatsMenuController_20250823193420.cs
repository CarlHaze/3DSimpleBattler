using UnityEngine;
using UnityEngine.UIElements;

public class StatsMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private UnitMovementManager movementManager;
    
    // References to the labels and panel
    private VisualElement panel;
    private Label nameLabel;
    private Label hpLabel;
    private Label attackLabel;
    private Label defenseLabel;
    private Label speedLabel;
    
    void Start()
    {
        // Get the UIDocument component
        uiDocument = GetComponent<UIDocument>();
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found!");
            return;
        }
        
        // Get the root element
        VisualElement root = uiDocument.rootVisualElement;
        
        // Get the panel first
        panel = root.Q<VisualElement>("panel");
        
        // Find all the labels by their names from your UI
        nameLabel = root.Q<Label>("NameLabel");
        hpLabel = root.Q<Label>("HPLabel");
        attackLabel = root.Q<Label>("ATTLabel");
        defenseLabel = root.Q<Label>("DEFLabel");
        speedLabel = root.Q<Label>("SPDLabel");
        
        // Find the movement manager to get selected units
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        
        // Hide the panel initially by adding the hide class
        if (panel != null)
        {
            panel.AddToClassList("hide");
            Debug.Log("Panel hidden on start");
        }
        
        Debug.Log("StatsMenuController initialized");
    }
    
    void Update()
    {
        // Check if we have a selected unit
        if (movementManager != null)
        {
            GameObject selectedUnit = movementManager.GetSelectedUnit();
            
            if (selectedUnit != null)
            {
                Character character = selectedUnit.GetComponent<Character>();
                if (character != null)
                {
                    ShowPanel();
                    UpdateStats(character.Stats, character.CharacterName);
                }
            }
            else
            {
                HidePanel();
            }
        }
    }
    
    public void UpdateStats(CharacterStats stats, string unitName = "Unit")
    {
        if (stats == null) return;
        
        // Update each label with the corresponding stat and label
        if (nameLabel != null) nameLabel.text = unitName;
        if (hpLabel != null) hpLabel.text = $"Health: {stats.CurrentHP}/{stats.MaxHP}";
        if (attackLabel != null) attackLabel.text = $"Attack: {stats.Attack}";
        if (defenseLabel != null) defenseLabel.text = $"Defense: {stats.Defense}";
        if (speedLabel != null) speedLabel.text = $"Speed: {stats.Speed}";
    }
    
    void ClearStats()
    {
        if (nameLabel != null) nameLabel.text = "No Unit Selected";
        if (hpLabel != null) hpLabel.text = "Health: 0/0";
        if (attackLabel != null) attackLabel.text = "Attack: 0";
        if (defenseLabel != null) defenseLabel.text = "Defense: 0";
        if (speedLabel != null) speedLabel.text = "Speed: 0";
    }
    
    void ShowPanel()
    {
        if (panel != null)
        {
            panel.RemoveFromClassList("hide");
            Debug.Log("Panel shown");
        }
    }
    
    void HidePanel()
    {
        if (panel != null)
        {
            panel.AddToClassList("hide");
            Debug.Log("Panel hidden");
        }
    }
}