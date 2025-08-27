using UnityEngine;
using UnityEngine.UIElements;

public class StatsMenuController : MonoBehaviour
{
    private UIDocument uiDocument;
    private UnitMovementManager movementManager;
    private ActionMenuController actionMenuController;
    private TurnManager turnManager;
    
    // References to the labels and panel
    private VisualElement panel;
    private Label nameLabel;
    private Label hpLabel;
    private Label attackLabel;
    private Label defenseLabel;
    private Label speedLabel;
    private Label apLabel;
    private Label mpLabel;
    
    // Enemy selection tracking
    private GameObject lastSelectedEnemyUnit;
    
    // Panel state tracking to prevent spam
    private bool isPanelVisible = false;
    
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
        
        // Get the panel first (note: capital P in "Panel")
        panel = root.Q<VisualElement>("Panel");
        
        // Find all the labels by their names from your UI
        nameLabel = root.Q<Label>("NameLabel");
        hpLabel = root.Q<Label>("HPLabel");
        attackLabel = root.Q<Label>("ATTLabel");
        defenseLabel = root.Q<Label>("DEFLabel");
        speedLabel = root.Q<Label>("SPDLabel");
        apLabel = root.Q<Label>("APLabel");
        mpLabel = root.Q<Label>("MPLabel");
        
        // Find the movement manager to get selected units
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        actionMenuController = FindFirstObjectByType<ActionMenuController>();
        turnManager = FindFirstObjectByType<TurnManager>();
        
        // Hide the panel initially by adding the hide class
        if (panel != null)
        {
            panel.AddToClassList("hide");
            isPanelVisible = false;
            Debug.Log("Panel hidden on start");
        }
        
        Debug.Log("StatsMenuController initialized");
    }
    
    void Update()
    {
        // Get selected unit from appropriate source based on current phase
        GameObject selectedUnit = GetCurrentlySelectedUnit();
        
        if (selectedUnit != null)
        {
            // Clear enemy selection when player unit is selected
            if (lastSelectedEnemyUnit != null)
            {
                lastSelectedEnemyUnit = null;
            }
            
            Character character = selectedUnit.GetComponent<Character>();
            if (character != null)
            {
                ShowPanel();
                UpdateStats(character.Stats, character.CharacterName);
            }
        }
        else
        {
            // Check for enemy clicks when no player unit is selected
            CheckForEnemySelection();
            
            // Only hide panel if no enemy is selected either
            if (lastSelectedEnemyUnit == null)
            {
                HidePanel();
            }
        }
    }
    
    GameObject GetCurrentlySelectedUnit()
    {
        // During placement phase or if no turn manager, check ActionMenuController
        if (turnManager == null || turnManager.GetCurrentPhase() == BattlePhase.Placement)
        {
            if (actionMenuController != null)
            {
                GameObject actionSelectedUnit = actionMenuController.GetSelectedUnit();
                if (actionSelectedUnit != null)
                {
                    return actionSelectedUnit;
                }
            }
        }
        
        // During combat phase or fallback, check UnitMovementManager
        if (movementManager != null)
        {
            return movementManager.GetSelectedUnit();
        }
        
        return null;
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
        if (apLabel != null) apLabel.text = $"AP: {stats.CurrentAP}/{stats.MaxAP}";
        if (mpLabel != null) mpLabel.text = $"MP: {stats.CurrentMP}/{stats.MaxMP}";
    }
    
    void ClearStats()
    {
        if (nameLabel != null) nameLabel.text = "No Unit Selected";
        if (hpLabel != null) hpLabel.text = "Health: 0/0";
        if (attackLabel != null) attackLabel.text = "Attack: 0";
        if (defenseLabel != null) defenseLabel.text = "Defense: 0";
        if (speedLabel != null) speedLabel.text = "Speed: 0";
        if (apLabel != null) apLabel.text = "AP: 0/0";
        if (mpLabel != null) mpLabel.text = "MP: 0/0";
    }
    
    void ShowPanel()
    {
        if (panel != null && !isPanelVisible)
        {
            panel.RemoveFromClassList("hide");
            isPanelVisible = true;
            Debug.Log("Panel shown");
        }
    }
    
    void HidePanel()
    {
        if (panel != null && isPanelVisible)
        {
            panel.AddToClassList("hide");
            isPanelVisible = false;
            Debug.Log("Panel hidden");
        }
    }
    
    void CheckForEnemySelection()
    {
        if (Input.GetMouseButtonDown(0))
        {
            Camera gameCamera = Camera.main;
            if (gameCamera == null) gameCamera = FindFirstObjectByType<Camera>();
            
            Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            
            if (Physics.Raycast(ray, out hit))
            {
                GameObject clickedObject = hit.collider.gameObject;
                
                if (clickedObject.CompareTag("Enemy"))
                {
                    // Only process enemy click if movement isn't active
                    if (!movementManager.IsMoving())
                    {
                        DisplayEnemyStats(clickedObject);
                    }
                }
                else if (!clickedObject.CompareTag("Player"))
                {
                    lastSelectedEnemyUnit = null;
                }
            }
        }
    }
    
    void DisplayEnemyStats(GameObject enemyUnit)
    {
        if (enemyUnit == lastSelectedEnemyUnit) return;
        
        lastSelectedEnemyUnit = enemyUnit;
        
        Character character = enemyUnit.GetComponent<Character>();
        if (character != null)
        {
            ShowPanel();
            string displayName = !string.IsNullOrEmpty(character.CharacterName) ? character.CharacterName : enemyUnit.name;
            UpdateStats(character.Stats, displayName);
        }
    }
}