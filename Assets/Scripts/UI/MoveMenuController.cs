using UnityEngine;
using UnityEngine.UIElements;

public class MoveMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private SimpleUnitSelector unitSelector;
    private TurnManager turnManager;
    private ModeManager modeManager;
    
    // References to UI elements
    private Label unitLabel;
    
    void Start()
    {
        // Get the UIDocument component
        uiDocument = GetComponent<UIDocument>();
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on MoveMenuController!");
            return;
        }
        
        // Get the root element and find the unit label
        VisualElement root = uiDocument.rootVisualElement;
        unitLabel = root.Q<Label>("UnitLabel");
        
        if (unitLabel == null)
        {
            Debug.LogError("UnitLabel not found in MoveMenu UI!");
            return;
        }
        
        // Find required managers
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        turnManager = FindFirstObjectByType<TurnManager>();
        modeManager = FindFirstObjectByType<ModeManager>();
        
        if (unitSelector == null)
        {
            Debug.LogError("SimpleUnitSelector not found!");
            return;
        }
        
        if (turnManager == null)
        {
            Debug.LogError("TurnManager not found!");
            return;
        }
        
        if (modeManager == null)
        {
            Debug.LogError("ModeManager not found!");
            return;
        }
        
        UpdateUI();
        Debug.Log("MoveMenuController initialized");
    }
    
    void Update()
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (unitSelector == null || turnManager == null || modeManager == null || unitLabel == null) return;
        
        // Show the menu only after placement is complete and during combat
        bool placementComplete = unitSelector.IsInitialPlacementComplete();
        bool inCombat = turnManager.GetCurrentPhase() == BattlePhase.Combat;
        bool showMoveMenu = placementComplete && inCombat;
        
        // Get the root container to hide/show the entire UI
        VisualElement root = uiDocument.rootVisualElement;
        VisualElement container = root.Q<VisualElement>("Container");
        
        if (container != null)
        {
            container.style.display = showMoveMenu ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        // If UI is hidden, no need to update the label
        if (!showMoveMenu) return;
        
        // Show player unit count (same as PlacementUI was doing)
        int placedUnits = unitSelector.GetUnitsPlaced();
        int maxUnits = unitSelector.GetMaxUnits();
        
        // Update the unit count label
        unitLabel.text = $"Units: {placedUnits}/{maxUnits}";
    }
}