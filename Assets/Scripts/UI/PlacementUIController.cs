using UnityEngine;
using UnityEngine.UIElements;

public class PlacementUIController : MonoBehaviour
{
    private UIDocument uiDocument;
    private SimpleUnitSelector unitSelector;
    private ModeManager modeManager;
    private AttackManager attackManager;
    private UnitMovementManager movementManager;
    private TurnManager turnManager;
    private ConfirmationDialogController confirmationDialog;
    
    // References to the labels and buttons
    private Label unitCountLabel;
    private Label selectedUnitLabel;
    private Label modeLabel;
    private Button startBattleBtn;
    
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
        
        // Find the labels and buttons by their names
        unitCountLabel = root.Q<Label>("UnitCountLabel");
        selectedUnitLabel = root.Q<Label>("SelectedUnitLabel");
        modeLabel = root.Q<Label>("ModeLabel");
        startBattleBtn = root.Q<Button>("StartBattleBtn");

        
        // Find the unit selector and mode manager
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        modeManager = FindFirstObjectByType<ModeManager>();
        attackManager = FindFirstObjectByType<AttackManager>();
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        turnManager = FindFirstObjectByType<TurnManager>();
        confirmationDialog = FindFirstObjectByType<ConfirmationDialogController>();
        
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
        
        // Set up StartBattleBtn click handler
        if (startBattleBtn != null)
        {
            startBattleBtn.clicked += OnStartBattleClicked;
        }
        else
        {
            Debug.LogWarning("StartBattleBtn not found in UI!");
        }
        
        if (confirmationDialog == null)
        {
            Debug.LogWarning("ConfirmationDialogController not found - confirmation dialogs will be disabled!");
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
        
        // Update StartBattleBtn visibility and state
        if (startBattleBtn != null)
        {
            if (modeManager.IsInPlacementMode() && modeManager.isBattleMap)
            {
                startBattleBtn.style.display = DisplayStyle.Flex;
                // Enable button only if we have at least 1 unit placed
                bool hasUnits = unitSelector != null && unitSelector.GetUnitsPlaced() > 0;
                startBattleBtn.SetEnabled(hasUnits);
                startBattleBtn.text = hasUnits ? "Start Battle!" : "Place Units First";
            }
            else
            {
                startBattleBtn.style.display = DisplayStyle.None;
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
    
    private void OnStartBattleClicked()
    {
        if (modeManager == null || unitSelector == null)
        {
            Debug.LogError("ModeManager or UnitSelector not found!");
            return;
        }
        
        if (unitSelector.GetUnitsPlaced() == 0)
        {
            Debug.Log("Cannot start battle - no units placed");
            return;
        }
        
        int placedUnits = unitSelector.GetUnitsPlaced();
        int maxUnits = unitSelector.GetMaxUnits();
        
        // If all units are placed, start battle immediately
        if (placedUnits >= maxUnits)
        {
            StartBattle();
        }
        // If not all units are placed, show confirmation dialog
        else
        {
            if (confirmationDialog != null)
            {
                string message = "Are you sure you want to start the battle?";
                string unitsMessage = $"You have only placed {placedUnits} out of {maxUnits} units.";
                
                confirmationDialog.ShowDialog(
                    message,
                    unitsMessage,
                    () => {
                        Debug.Log("PlacementUIController: Confirm callback triggered!");
                        StartBattle();
                    }, // onConfirm
                    () => {
                        Debug.Log("PlacementUIController: Cancel callback triggered!");
                    } // onCancel
                );
            }
            else
            {
                // Fallback if no confirmation dialog - start battle directly
                Debug.LogWarning("No confirmation dialog available - starting battle directly");
                StartBattle();
            }
        }
    }
    
    private void StartBattle()
    {
        Debug.Log("PlacementUIController: StartBattle method called!");
        
        if (modeManager == null || unitSelector == null)
        {
            Debug.LogError("Cannot start battle - missing components!");
            return;
        }
        
        int placedUnits = unitSelector.GetUnitsPlaced();
        Debug.Log($"Starting battle with {placedUnits} units placed");
        
        // Force complete the initial placement phase (needed for early start)
        unitSelector.ForceCompleteInitialPlacement();
        
        // Switch to explore mode first
        Debug.Log("Switching to Explore mode...");
        modeManager.SetMode(GameMode.Explore);
        
        // Initialize the combat system
        if (turnManager != null)
        {
            Debug.Log("Calling TurnManager.StartCombatPhase()...");
            turnManager.StartCombatPhase();
        }
        else
        {
            Debug.LogWarning("TurnManager not found - combat system may not initialize properly!");
        }
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