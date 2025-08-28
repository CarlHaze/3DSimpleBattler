using UnityEngine;
using UnityEngine.UIElements;

public class ActionMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement panel;
    private Button moveButton;
    private Button attackButton;
    private Button endButton;
    
    private UnitMovementManager movementManager;
    private ModeManager modeManager;
    private SimpleUnitSelector unitSelector;
    private AttackManager attackManager;
    private SkillManager skillManager;
    private SkillSelectionController skillSelectionController;
    private TurnManager turnManager;
    
    // Track selected unit
    private GameObject selectedUnit;
    private bool isMenuVisible = false;
    private float lastAttackTime = 0f;
    private float lastSkillTime = 0f;
    private const float ATTACK_COOLDOWN = 0.1f; // Prevent selection immediately after attack
    private const float SKILL_COOLDOWN = 0.1f; // Prevent selection immediately after skill use
    
    void Start()
    {
        // Get components
        uiDocument = GetComponent<UIDocument>();
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        modeManager = FindFirstObjectByType<ModeManager>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        attackManager = FindFirstObjectByType<AttackManager>();
        skillManager = FindFirstObjectByType<SkillManager>();
        skillSelectionController = FindFirstObjectByType<SkillSelectionController>();
        turnManager = FindFirstObjectByType<TurnManager>();
        
        // Subscribe to turn manager events
        if (turnManager != null)
        {
            turnManager.OnPhaseChange += OnBattlePhaseChanged;
        }
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on ActionMenuController!");
            return;
        }
        
        if (movementManager == null)
        {
            Debug.LogError("UnitMovementManager not found!");
            return;
        }
        
        if (modeManager == null)
        {
            Debug.LogError("ModeManager not found!");
            return;
        }
        
        // Get UI elements
        VisualElement root = uiDocument.rootVisualElement;
        panel = root.Q<VisualElement>("Panel");
        moveButton = root.Q<Button>("MoveButton");
        attackButton = root.Q<Button>("ATTButton");
        endButton = root.Q<Button>("EndButton");
        
        if (panel == null)
        {
            Debug.LogError("Panel not found!");
            return;
        }
        
        if (moveButton == null)
        {
            Debug.LogError("MoveButton not found!");
            return;
        }
        
        if (attackButton == null)
        {
            Debug.LogError("ATTButton not found!");
            return;
        }
        
        if (endButton == null)
        {
            Debug.LogError("EndButton not found!");
            return;
        }
        
        // Set up button callbacks
        moveButton.clicked += OnMoveButtonPressed;
        attackButton.clicked += OnAttackButtonPressed;
        endButton.clicked += OnEndTurnButtonPressed;
        
        // Hide menu initially
        HideActionMenu();
        
        Debug.Log("ActionMenuController initialized");
    }
    
    void Update()
    {
        // Check for unit selection when not in placement mode and not in movement mode
        if (modeManager != null && !modeManager.IsInPlacementMode() && 
            (turnManager == null || turnManager.GetCurrentPhase() != BattlePhase.Placement))
        {
            // Don't handle unit selection if movement manager is in movement mode or attack manager is active
            if (movementManager != null && (movementManager.IsMoving() || movementManager.IsInMovementMode()))
            {
                return; // Movement system is handling input
            }
            
            if (attackManager != null && attackManager.IsInAttackMode())
            {
                return; // Attack system is handling input
            }
            
            if (skillManager != null && skillManager.IsInSkillMode())
            {
                return; // Skill system is handling input
            }
            
            // Don't handle unit selection if skill menu is visible
            if (skillSelectionController != null && skillSelectionController.IsSkillMenuVisible())
            {
                return; // Skill selection system is handling input
            }
            
            CheckForUnitSelection();
        }
    }
    
    void CheckForUnitSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Don't allow unit selection immediately after an attack or skill use
            if (Time.time - lastAttackTime < ATTACK_COOLDOWN)
            {
                return;
            }
            
            if (Time.time - lastSkillTime < SKILL_COOLDOWN)
            {
                return;
            }
            
            // Check if we're clicking on a UI element first
            if (IsClickingOnUI())
            {
                return; // Don't process unit selection when clicking UI
            }
            
            Camera gameCamera = Camera.main;
            if (gameCamera == null) gameCamera = FindFirstObjectByType<Camera>();
            
            Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                if (clickedObject.CompareTag("Player"))
                {
                    // Only allow selecting player units during their turn in combat phase
                    if (CanSelectUnit(clickedObject))
                    {
                        SelectUnit(clickedObject);
                    }
                }
                else
                {
                    // Clicked somewhere else, deselect unit
                    DeselectUnit();
                }
            }
            else
            {
                // Clicked in empty space, deselect unit
                DeselectUnit();
            }
        }
    }
    
    bool IsClickingOnUI()
    {
        if (uiDocument == null || panel == null || !isMenuVisible) return false;
        
        // Only check for UI clicks when the action menu is actually visible
        // Get mouse position in UI coordinates
        Vector2 localMousePosition = RuntimePanelUtils.ScreenToPanel(
            uiDocument.rootVisualElement.panel,
            Input.mousePosition
        );
        
        // Check if mouse is over the action menu panel specifically
        VisualElement elementUnderMouse = uiDocument.rootVisualElement.panel.Pick(localMousePosition);
        
        // Return true only if we're clicking on the action menu panel or its children
        return elementUnderMouse != null && IsChildOfPanel(elementUnderMouse, panel);
    }
    
    bool IsChildOfPanel(VisualElement element, VisualElement targetPanel)
    {
        VisualElement current = element;
        while (current != null)
        {
            if (current == targetPanel)
                return true;
            current = current.parent;
        }
        return false;
    }
    
    public void SelectUnit(GameObject unit)
    {
        if (selectedUnit == unit && isMenuVisible) 
        {
            // Even if already selected, update button states in case resources changed
            UpdateMoveButtonState();
            return;
        }
        
        selectedUnit = unit;
        
        // Only show action menu during combat phase, not placement phase
        if (turnManager == null || turnManager.GetCurrentPhase() != BattlePhase.Placement)
        {
            ShowActionMenu();
        }
        
        // Notify movement manager about selection (but don't enter movement mode yet)
        if (movementManager != null)
        {
            movementManager.SetSelectedUnit(unit, false); // false = don't enter movement mode
        }
        
        Debug.Log($"Unit selected: {unit.name}");
    }
    
    public void DeselectUnit()
    {
        selectedUnit = null;
        HideActionMenu();
        
        // Clear selection in movement manager
        if (movementManager != null)
        {
            movementManager.ClearSelection();
        }
        
        Debug.Log("Unit deselected");
    }
    
    void ShowActionMenu()
    {
        if (panel != null)
        {
            panel.RemoveFromClassList("hide");
            isMenuVisible = true;
            UpdateMoveButtonState();
            Debug.Log("Action menu shown");
        }
    }
    
    void UpdateMoveButtonState()
    {
        if (moveButton == null) return;
        
        // Disable buttons if in placement mode or initial placement not complete
        bool shouldDisable = false;
        
        if (modeManager != null && modeManager.IsInPlacementMode())
        {
            shouldDisable = true;
        }
        else if (unitSelector != null && !unitSelector.IsInitialPlacementComplete())
        {
            shouldDisable = true;
        }
        
        // Check resource availability for selected unit
        bool canMove = !shouldDisable;
        bool canAttack = !shouldDisable;
        
        if (selectedUnit != null && !shouldDisable)
        {
            Character character = selectedUnit.GetComponent<Character>();
            if (character?.Stats != null)
            {
                // Move requires at least 1 MP
                canMove = character.Stats.CanSpendMP(1);
                
                // Attack requires at least 1 AP
                canAttack = character.Stats.CanSpendAP(1);
            }
        }
        
        moveButton.SetEnabled(canMove);
        attackButton.SetEnabled(canAttack);
        
        // End Turn button should only be enabled during combat phase for player turns
        bool enableEndTurn = !shouldDisable && turnManager != null && 
                            turnManager.GetCurrentPhase() == BattlePhase.Combat && 
                            turnManager.IsPlayerTurn();
        endButton.SetEnabled(enableEndTurn);
        
        if (shouldDisable)
        {
            Debug.Log("Action buttons disabled - placement mode or initial placement not complete");
        }
    }
    
    void HideActionMenu()
    {
        if (panel != null)
        {
            panel.AddToClassList("hide");
            isMenuVisible = false;
            Debug.Log("Action menu hidden");
        }
    }
    
    void OnMoveButtonPressed()
    {
        if (selectedUnit == null) return;
        
        // Check if unit has enough MP
        Character character = selectedUnit.GetComponent<Character>();
        if (character?.Stats != null)
        {
            if (!character.Stats.CanSpendMP(1))
            {
                SimpleMessageLog.Log($"No MP! {character.CharacterName} needs at least 1 Move Point to move.");
                return;
            }
        }
        
        Debug.Log("Move button pressed");
        
        // Enter movement mode for the selected unit
        if (movementManager != null)
        {
            movementManager.SetSelectedUnit(selectedUnit, true); // true = enter movement mode
        }
        
        // Hide the action menu while in movement mode
        HideActionMenu();
    }
    
    void OnAttackButtonPressed()
    {
        if (selectedUnit == null) return;
        
        // Check if unit has enough AP
        Character character = selectedUnit.GetComponent<Character>();
        if (character?.Stats != null)
        {
            if (!character.Stats.CanSpendAP(1))
            {
                SimpleMessageLog.Log($"No AP! {character.CharacterName} needs at least 1 Action Point to attack.");
                return;
            }
        }
        
        Debug.Log("Attack button pressed");
        
        // Show skill selection menu instead of directly entering attack mode
        if (skillSelectionController != null)
        {
            skillSelectionController.ShowSkillMenu(selectedUnit);
            HideActionMenu();
        }
        else
        {
            // Fallback to basic attack if skill system not available
            if (attackManager != null)
            {
                attackManager.StartAttackMode(selectedUnit);
            }
            HideActionMenu();
        }
    }
    
    void OnEndTurnButtonPressed()
    {
        Debug.Log("End Turn button pressed");
        
        // End the current player's turn
        if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Combat && turnManager.IsPlayerTurn())
        {
            // Show remaining resources before ending turn
            if (selectedUnit != null)
            {
                Character character = selectedUnit.GetComponent<Character>();
                if (character?.Stats != null)
                {
                    SimpleMessageLog.Log($"{character.CharacterName} ends turn (AP: {character.Stats.CurrentAP}, MP: {character.Stats.CurrentMP})");
                }
            }
            
            // Deselect unit and hide menu
            DeselectUnit();
            
            // End the turn
            turnManager.EndTurn();
        }
        else
        {
            Debug.LogWarning("Cannot end turn - not in combat phase or not player's turn");
        }
    }
    
    // Public methods for external control
    public bool IsUnitSelected()
    {
        return selectedUnit != null;
    }
    
    public GameObject GetSelectedUnit()
    {
        return selectedUnit;
    }
    
    public bool IsMenuVisible()
    {
        return isMenuVisible;
    }
    
    public void OnAttackPerformed()
    {
        lastAttackTime = Time.time;
    }
    
    public void OnSkillPerformed()
    {
        lastSkillTime = Time.time;
    }
    
    bool CanSelectUnit(GameObject unit)
    {
        // During placement phase, allow unit selection for stats viewing but don't show action menu
        if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Placement)
        {
            return unit.CompareTag("Player"); // Allow selecting placed player units for stats
        }
        
        // If no turn manager or not in combat, allow selection (fallback for non-turn-based scenarios)
        if (turnManager == null)
        {
            return true;
        }
        
        // During combat phase, only allow selecting the current turn's unit
        if (turnManager.GetCurrentPhase() == BattlePhase.Combat)
        {
            return turnManager.IsPlayerTurn() && turnManager.GetCurrentUnit() == unit;
        }
        
        return false;
    }
    
    // Method to end the current player's turn after completing an action
    void EndPlayerTurnAfterAction()
    {
        if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Combat && turnManager.IsPlayerTurn())
        {
            Debug.Log("Player completed action - ending turn");
            turnManager.EndTurn();
        }
    }
    
    void OnBattlePhaseChanged(BattlePhase newPhase)
    {
        // Hide action menu during placement phase
        if (newPhase == BattlePhase.Placement)
        {
            HideActionMenu();
            DeselectUnit();
        }
    }
    
    void OnDestroy()
    {
        // Unsubscribe from events
        if (turnManager != null)
        {
            turnManager.OnPhaseChange -= OnBattlePhaseChanged;
        }
    }
}