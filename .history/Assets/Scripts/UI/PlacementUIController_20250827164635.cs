using UnityEngine;
using UnityEngine.UIElements;

public class PlacementUIController : MonoBehaviour
{
    private UIDocument uiDocument;
    private SimpleUnitSelector unitSelector;
    private ModeManager modeManager;
    private AttackManager attackManager;
    private UnitMovementManager movementManager;
    
    // References to the labels
    private Label unitCountLabel;
    private Label selectedUnitLabel;
    private Label modeLabel;
    
    // Track if placement mode has been entered at least once
    private bool hasEnteredPlacementMode = false;
    
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

        
        // Find the unit selector and mode manager
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        modeManager = FindFirstObjectByType<ModeManager>();
        attackManager = FindFirstObjectByType<AttackManager>();
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        
        if (unitSelector == null)
        {
            Debug.LogError("SimpleUnitSelector not found!");
            return;
        }
        
        if (modeManager == null)
        {
            Debug.LogError("ModeManager not found!");
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
        if (unitSelector == null || modeManager == null) return;

        // Update mode label with dynamic state checking
        if (modeLabel != null)
        {
            string modeText = GetCurrentModeDisplayName();
            modeLabel.text = $"Mode: {modeText}";
        }
        
        // Update selected unit text (show after first placement mode entry)
        if (selectedUnitLabel != null)
        {
            if (modeManager.IsInPlacementMode() || hasEnteredPlacementMode)
            {
                string unitName = unitSelector.GetCurrentUnitName();
                if (modeManager.IsInPlacementMode() && !string.IsNullOrEmpty(unitName) && unitName != "None")
                {
                    selectedUnitLabel.text = $"Selected: {unitName}";
                }
                else
                {
                    selectedUnitLabel.text = "Selected: None";
                }
                selectedUnitLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                selectedUnitLabel.style.display = DisplayStyle.None;
            }
        }
        
        // Update count text (show after first placement mode entry)
        if (unitCountLabel != null)
        {
            if (modeManager.IsInPlacementMode() || hasEnteredPlacementMode)
            {
                unitCountLabel.text = $"Units: {unitSelector.GetUnitsPlaced()}/{unitSelector.GetMaxUnits()}";
                unitCountLabel.style.display = DisplayStyle.Flex;
            }
            else
            {
                unitCountLabel.style.display = DisplayStyle.None;
            }
        }
    }
    
    public void OnModeChanged(GameMode newMode)
    {
        if (newMode == GameMode.Placement)
        {
            hasEnteredPlacementMode = true;
        }
        UpdateUI();
    }
    
    private string GetCurrentModeDisplayName()
    {
        // Check for attack mode first (highest priority)
        if (attackManager != null && attackManager.IsInAttackMode())
        {
            return "Attacking";
        }
        
        // Check for movement mode next
        if (movementManager != null && movementManager.IsInMovementMode())
        {
            return "Moving";
        }
        
        // Fall back to regular mode manager
        if (modeManager != null)
        {
            return modeManager.GetModeDisplayName();
        }
        
        return "Unknown";
    }
}