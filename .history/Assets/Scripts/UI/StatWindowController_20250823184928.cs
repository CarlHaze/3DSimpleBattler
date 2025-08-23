using UnityEngine;
using UnityEngine.UIElements;

public class StatWindowController : MonoBehaviour
{
    [Header("UI Assets")]
    [SerializeField] private VisualTreeAsset statWindowUXML;
    [SerializeField] private StyleSheet statWindowUSS;
    
    [Header("Settings")]
    [SerializeField] private bool showOnStart = false;
    [SerializeField] private KeyCode toggleKey = KeyCode.I; // 'I' for Info/Inventory
    
    // UI Element References
    private UIDocument uiDocument;
    private VisualElement statWindow;
    private VisualElement rootElement;
    
    // Header Elements
    private Label windowTitle;
    private Button closeButton;
    
    // Unit Info Elements
    private Label unitNameLabel;
    private Label unitClassLabel;
    
    // Health Elements
    private ProgressBar healthBar;
    private Label healthText;
    
    // Stat Elements
    private Label attackValue;
    private Label defenseValue;
    private Label speedValue;
    private Label magicValue;
    
    // Action Buttons
    private Button moveButton;
    private Button attackButton;
    
    // Current Character Reference
    private Character currentCharacter;
    private UnitMovementManager movementManager;
    
    // State
    private bool isVisible = false;
    
    void Start()
    {
        InitializeUI();
        
        // Find movement manager for integration
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        
        if (showOnStart)
        {
            ShowWindow();
        }
        else
        {
            HideWindow();
        }
        
        Debug.Log("StatWindowController initialized. Press 'I' to toggle stats window.");
    }
    
    void Update()
    {
        // Toggle window with hotkey
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWindow();
        }
        
        // Auto-update selected unit stats
        if (isVisible && movementManager != null)
        {
            GameObject selectedUnit = movementManager.GetSelectedUnit();
            if (selectedUnit != null)
            {
                Character character = selectedUnit.GetComponent<Character>();
                if (character != null && character != currentCharacter)
                {
                    SetCharacter(character);
                }
            }
            else if (currentCharacter != null)
            {
                // Clear window when no unit selected
                ClearWindow();
            }
        }
        
        // Update health bar if character is set
        if (currentCharacter != null && isVisible)
        {
            UpdateHealthDisplay();
        }
    }
    
    void InitializeUI()
    {
        // Get or create UIDocument
        uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            uiDocument = gameObject.AddComponent<UIDocument>();
        }
        
        // Load UXML and USS
        if (statWindowUXML == null)
        {
            Debug.LogError("StatWindow UXML asset not assigned!");
            return;
        }
        
        uiDocument.visualTreeAsset = statWindowUXML;
        
        if (statWindowUSS != null)
        {
            uiDocument.styleSheets.Add(statWindowUSS);
        }
        
        // Get root element
        rootElement = uiDocument.rootVisualElement;
        statWindow = rootElement.Q<VisualElement>("stat-window");
        
        if (statWindow == null)
        {
            Debug.LogError("Could not find stat-window element in UXML!");
            return;
        }
        
        // Cache UI element references
        CacheUIReferences();
        
        // Setup button events
        SetupButtonEvents();
        
        Debug.Log("UI initialized successfully");
    }
    
    void CacheUIReferences()
    {
        // Header elements
        windowTitle = statWindow.Q<Label>("window-title");
        closeButton = statWindow.Q<Button>("close-button");
        
        // Unit info elements
        unitNameLabel = statWindow.Q<Label>("unit-name");
        unitClassLabel = statWindow.Q<Label>("unit-class");
        
        // Health elements
        healthBar = statWindow.Q<ProgressBar>("health-bar");
        healthText = statWindow.Q<Label>("health-text");
        
        // Stat value elements
        attackValue = statWindow.Q<Label>("attack-value");
        defenseValue = statWindow.Q<Label>("defense-value");
        speedValue = statWindow.Q<Label>("speed-value");
        magicValue = statWindow.Q<Label>("magic-value");
        
        // Action buttons
        moveButton = statWindow.Q<Button>("move-button");
        attackButton = statWindow.Q<Button>("attack-button");
    }
    
    void SetupButtonEvents()
    {
        // Close button
        if (closeButton != null)
        {
            closeButton.clicked += HideWindow;
        }
        
        // Action buttons (placeholder functionality for now)
        if (moveButton != null)
        {
            moveButton.clicked += OnMoveButtonClicked;
        }
        
        if (attackButton != null)
        {
            attackButton.clicked += OnAttackButtonClicked;
        }
    }
    
    public void ShowWindow()
    {
        if (statWindow == null) return;
        
        isVisible = true;
        statWindow.AddToClassList("visible");
        
        // Show current selected unit if any
        if (movementManager != null)
        {
            GameObject selectedUnit = movementManager.GetSelectedUnit();
            if (selectedUnit != null)
            {
                Character character = selectedUnit.GetComponent<Character>();
                if (character != null)
                {
                    SetCharacter(character);
                }
            }
        }
    }
    
    public void HideWindow()
    {
        if (statWindow == null) return;
        
        isVisible = false;
        statWindow.RemoveFromClassList("visible");
        currentCharacter = null;
    }
    
    public void ToggleWindow()
    {
        if (isVisible)
        {
            HideWindow();
        }
        else
        {
            ShowWindow();
        }
    }
    
    public void SetCharacter(Character character)
    {
        currentCharacter = character;
        
        if (character == null)
        {
            ClearWindow();
            return;
        }
        
        // Update unit info
        if (unitNameLabel != null)
        {
            string characterName = !string.IsNullOrEmpty(character.CharacterName) 
                ? character.CharacterName 
                : character.gameObject.name;
            unitNameLabel.text = characterName;
        }
        
        if (unitClassLabel != null)
        {
            // This will be expanded when you add CharacterClass system
            unitClassLabel.text = "Fighter"; // Placeholder
        }
        
        // Update stats
        UpdateStatsDisplay();
        UpdateHealthDisplay();
        
        // Show window if not already visible
        if (!isVisible)
        {
            ShowWindow();
        }
    }
    
    void UpdateStatsDisplay()
    {
        if (currentCharacter == null || currentCharacter.Stats == null) return;
        
        CharacterStats stats = currentCharacter.Stats;
        
        if (attackValue != null) attackValue.text = stats.Attack.ToString();
        if (defenseValue != null) defenseValue.text = stats.Defense.ToString();
        if (speedValue != null) speedValue.text = stats.Speed.ToString();
        if (magicValue != null) magicValue.text = "0"; // Will use stats.Magic when you add it
    }
    
    void UpdateHealthDisplay()
    {
        if (currentCharacter == null || currentCharacter.Stats == null) return;
        
        CharacterStats stats = currentCharacter.Stats;
        
        // Update health bar
        if (healthBar != null)
        {
            healthBar.highValue = stats.MaxHP;
            healthBar.value = stats.CurrentHP;
            
            // Update health bar color based on health percentage
            float healthPercentage = (float)stats.CurrentHP / stats.MaxHP;
            
            healthBar.RemoveFromClassList("low-health");
            healthBar.RemoveFromClassList("critical-health");
            
            if (healthPercentage <= 0.25f)
            {
                healthBar.AddToClassList("critical-health");
            }
            else if (healthPercentage <= 0.5f)
            {
                healthBar.AddToClassList("low-health");
            }
        }
        
        // Update health text
        if (healthText != null)
        {
            healthText.text = $"{stats.CurrentHP}/{stats.MaxHP}";
        }
    }
    
    void ClearWindow()
    {
        currentCharacter = null;
        
        if (unitNameLabel != null) unitNameLabel.text = "No Unit Selected";
        if (unitClassLabel != null) unitClassLabel.text = "";
        
        if (attackValue != null) attackValue.text = "-";
        if (defenseValue != null) defenseValue.text = "-";
        if (speedValue != null) speedValue.text = "-";
        if (magicValue != null) magicValue.text = "-";
        
        if (healthBar != null)
        {
            healthBar.value = 0;
            healthBar.highValue = 100;
        }
        
        if (healthText != null) healthText.text = "0/0";
    }
    
    // Action Button Handlers (placeholder for now)
    void OnMoveButtonClicked()
    {
        if (currentCharacter == null) return;
        
        Debug.Log($"Move button clicked for {currentCharacter.CharacterName}");
        // Future: Integrate with movement system
        // movementManager?.SelectUnit(currentCharacter.gameObject);
    }
    
    void OnAttackButtonClicked()
    {
        if (currentCharacter == null) return;
        
        Debug.Log($"Attack button clicked for {currentCharacter.CharacterName}");
        // Future: Integrate with combat system
    }
    
    // Public methods for external systems
    public bool IsWindowVisible => isVisible;
    public Character CurrentCharacter => currentCharacter;
    
    public void RefreshDisplay()
    {
        if (currentCharacter != null)
        {
            UpdateStatsDisplay();
            UpdateHealthDisplay();
        }
    }
}