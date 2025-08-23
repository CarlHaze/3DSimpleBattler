using UnityEngine;
using UnityEngine.UIElements;

public class PlacementUIController : MonoBehaviour
{
    private UIDocument uiDocument;
    private SimpleUnitSelector unitSelector;
    
    // References to the labels
    private Label unitCountLabel;
    private Label selectedUnitLabel;

    private Label modeLabel;
    
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
        
        // Find the labels by their names
        unitCountLabel = root.Q<Label>("UnitCountLabel");
        selectedUnitLabel = root.Q<Label>("SelectedUnitLabel");
        modeLabel = root.Q<Label>("ModeLabel");

        
        // Find the unit selector
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        
        if (unitSelector == null)
        {
            Debug.LogError("SimpleUnitSelector not found!");
            return;
        }
        
        UpdateUI();
        Debug.Log("PlacementUIController initialized");
    }
    
    void Update()
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (unitSelector == null) return;

        if (modeLabel != null)
        {
            modeLabel.text = $"Mode:";
        }
        
        // Update selected unit text
            if (selectedUnitLabel != null)
            {
                selectedUnitLabel.text = $"Selected: {unitSelector.GetCurrentUnitName()}";
            }
        
        // Update count text
        if (unitCountLabel != null)
        {
            unitCountLabel.text = $"Units: {unitSelector.GetUnitsPlaced()}/{unitSelector.GetMaxUnits()}";
        }
    }
}