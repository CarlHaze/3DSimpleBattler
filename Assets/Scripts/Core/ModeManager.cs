using UnityEngine;

public enum GameMode
{
    Explore,
    Placement,
    Battle
}

public class ModeManager : MonoBehaviour
{
    public static ModeManager Instance { get; private set; }
    
    [Header("Mode Settings")]
    public GameMode currentMode = GameMode.Explore;
    public KeyCode placementToggleKey = KeyCode.P;
    
    [Header("Battle Conditions")]
    public bool isBattleMap = false; // Set this to true for battle maps in the inspector
    public bool inBattle = false;
    public int maxUnitsToPlace = 4; // Maximum units that can be placed on this map
    
    private UnitPlacementManager placementManager;
    private PlacementUIController placementUI;
    private TurnManager turnManager;
    
    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        placementUI = FindFirstObjectByType<PlacementUIController>();
        turnManager = FindFirstObjectByType<TurnManager>();
        
        // Set initial mode based on isBattleMap setting
        if (isBattleMap)
        {
            SetMode(GameMode.Placement); // Start in placement mode for battle maps
            Debug.Log("Battle map - starting in Placement mode");
        }
        else
        {
            SetMode(GameMode.Explore);
            Debug.Log("Non-battle map - starting in Explore mode");
        }
    }
    
    void Update()
    {
        // Handle mode switching
        if (Input.GetKeyDown(placementToggleKey))
        {
            TogglePlacementMode();
        }
        
        // Only allow auto-switching for non-battle maps
        // Battle maps are controlled by the TurnManager
        if (!isBattleMap)
        {
            // Auto-switch to battle mode if inBattle condition is true
            if (inBattle && currentMode != GameMode.Battle)
            {
                SetMode(GameMode.Battle);
            }
            else if (!inBattle && currentMode == GameMode.Battle)
            {
                SetMode(GameMode.Explore);
            }
        }
    }
    
    public void TogglePlacementMode()
    {
        // For battle maps, allow toggling between placement and explore during placement phase
        if (isBattleMap)
        {
            // Check if we're in active combat phase
            if (turnManager != null && turnManager.GetCurrentPhase() == BattlePhase.Combat)
            {
                Debug.Log("Cannot enter placement mode during active combat");
                return;
            }
        }
        else if (inBattle)
        {
            Debug.Log("Cannot enter placement mode during battle");
            return;
        }
        
        if (currentMode == GameMode.Placement)
        {
            SetMode(GameMode.Explore);
        }
        else if (currentMode == GameMode.Explore)
        {
            SetMode(GameMode.Placement);
        }
    }
    
    public void SetMode(GameMode newMode)
    {
        if (currentMode == newMode) return;
        
        GameMode previousMode = currentMode;
        currentMode = newMode;
        
        Debug.Log($"Mode changed from {previousMode} to {newMode}");
        
        // Handle mode transitions
        switch (newMode)
        {
            case GameMode.Explore:
                OnEnterExploreMode();
                break;
            case GameMode.Placement:
                OnEnterPlacementMode();
                break;
            case GameMode.Battle:
                OnEnterBattleMode();
                break;
        }
        
        // Update UI
        if (placementUI != null)
        {
            placementUI.OnModeChanged(newMode);
        }
    }
    
    void OnEnterExploreMode()
    {
        // Exit placement mode if we were in it
        if (placementManager != null)
        {
            placementManager.enabled = true; // Keep enabled for camera/general functionality
            placementManager.OnExitPlacementMode();
        }
    }
    
    void OnEnterPlacementMode()
    {
        // Enable placement mode functionality
        if (placementManager != null)
        {
            placementManager.enabled = true;
            placementManager.OnEnterPlacementMode();
        }
    }
    
    void OnEnterBattleMode()
    {
        // Disable placement functionality during battle
        if (placementManager != null)
        {
            placementManager.enabled = false;
        }
    }
    
    public string GetModeDisplayName()
    {
        switch (currentMode)
        {
            case GameMode.Explore:
                return "Explore";
            case GameMode.Placement:
                return "Placement";
            case GameMode.Battle:
                return "Battle";
            default:
                return "Unknown";
        }
    }
    
    public bool IsInPlacementMode()
    {
        return currentMode == GameMode.Placement;
    }
    
    public bool IsInBattleMode()
    {
        return currentMode == GameMode.Battle;
    }
    
    public bool IsInExploreMode()
    {
        return currentMode == GameMode.Explore;
    }
    
    // Public methods to control battle state
    public void SetBattleMode(bool battleActive)
    {
        inBattle = battleActive;
        
        if (inBattle)
        {
            SetMode(GameMode.Battle);
        }
        else if (currentMode == GameMode.Battle)
        {
            SetMode(GameMode.Explore);
        }
    }
    
    public int GetMaxUnitsToPlace()
    {
        return maxUnitsToPlace;
    }
    
}