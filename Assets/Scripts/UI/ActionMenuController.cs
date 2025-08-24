using UnityEngine;
using UnityEngine.UIElements;

public class ActionMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement panel;
    private Button moveButton;
    private Button attackButton;
    private Button cancelButton;
    
    private UnitMovementManager movementManager;
    private ModeManager modeManager;
    private SimpleUnitSelector unitSelector;
    private AttackManager attackManager;
    
    // Track selected unit
    private GameObject selectedUnit;
    private bool isMenuVisible = false;
    private float lastAttackTime = 0f;
    private const float ATTACK_COOLDOWN = 0.1f; // Prevent selection immediately after attack
    
    void Start()
    {
        // Get components
        uiDocument = GetComponent<UIDocument>();
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        modeManager = FindFirstObjectByType<ModeManager>();
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        attackManager = FindFirstObjectByType<AttackManager>();
        
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
        cancelButton = root.Q<Button>("CancelButton");
        
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
        
        if (cancelButton == null)
        {
            Debug.LogError("CancelButton not found!");
            return;
        }
        
        // Set up button callbacks
        moveButton.clicked += OnMoveButtonPressed;
        attackButton.clicked += OnAttackButtonPressed;
        cancelButton.clicked += OnCancelButtonPressed;
        
        // Hide menu initially
        HideActionMenu();
        
        Debug.Log("ActionMenuController initialized");
    }
    
    void Update()
    {
        // Check for unit selection when not in placement mode and not in movement mode
        if (modeManager != null && !modeManager.IsInPlacementMode())
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
            
            CheckForUnitSelection();
        }
    }
    
    void CheckForUnitSelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            // Don't allow unit selection immediately after an attack
            if (Time.time - lastAttackTime < ATTACK_COOLDOWN)
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
                    // Select player unit and show action menu
                    SelectUnit(clickedObject);
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
        if (selectedUnit == unit && isMenuVisible) return; // Already selected
        
        selectedUnit = unit;
        ShowActionMenu();
        
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
        
        moveButton.SetEnabled(!shouldDisable);
        attackButton.SetEnabled(!shouldDisable);
        
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
        
        Debug.Log("Attack button pressed");
        
        // Enter attack mode for the selected unit
        if (attackManager != null)
        {
            attackManager.StartAttackMode(selectedUnit);
        }
        
        // Hide the action menu while in attack mode
        HideActionMenu();
    }
    
    void OnCancelButtonPressed()
    {
        Debug.Log("Cancel button pressed");
        
        // Deselect unit and return to explore mode
        DeselectUnit();
        
        if (modeManager != null)
        {
            modeManager.SetMode(GameMode.Explore);
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
}