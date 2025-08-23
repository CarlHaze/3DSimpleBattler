using UnityEngine;
using UnityEngine.UIElements;

public class StatsMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    
    // References to the labels
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
        
        // Find all the labels by their names from your UI
        nameLabel = root.Q<Label>("NameLabel");
        hpLabel = root.Q<Label>("HPLabel");
        attackLabel = root.Q<Label>("ATTLabel");
        defenseLabel = root.Q<Label>("DEFLabel");
        speedLabel = root.Q<Label>("SPDLabel");
    }
    
    public void UpdateStats(CharacterStats stats, string unitName = "Unit")
    {
        if (stats == null) return;
        
        // Update each label with the corresponding stat
        if (nameLabel != null) nameLabel.text = unitName;
        if (hpLabel != null) hpLabel.text = $"{stats.CurrentHP}/{stats.MaxHP}";
        if (attackLabel != null) attackLabel.text = stats.Attack.ToString();
        if (defenseLabel != null) defenseLabel.text = stats.Defense.ToString();
        if (speedLabel != null) speedLabel.text = stats.Speed.ToString();
    }
}