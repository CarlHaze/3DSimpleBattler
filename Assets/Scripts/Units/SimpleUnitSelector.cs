using UnityEngine;
using System.Collections.Generic;

public class SimpleUnitSelector : MonoBehaviour
{
    [Header("Unit Selection")]
    public List<GameObject> availableUnits = new List<GameObject>();
    public KeyCode cycleUnitsKey = KeyCode.Tab;
    
    [Header("Current Selection")]
    public int currentSelectedIndex = 0;
    
    private UnitPlacementManager placementManager;
    private ModeManager modeManager;
    private int unitsPlaced = 0;
    private bool initialPlacementComplete = false;
    
    void Start()
    {
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        modeManager = FindFirstObjectByType<ModeManager>();
        
        if (placementManager == null)
        {
            Debug.LogError("UnitPlacementManager not found!");
            return;
        }
        
        if (modeManager == null)
        {
            Debug.LogError("ModeManager not found!");
            return;
        }
        
        // Set the initial unit if we have any
        if (availableUnits.Count > 0)
        {
            UpdateSelectedUnit();
        }
        
        Debug.Log($"Unit Selector initialized. {availableUnits.Count} units available, max {GetMaxUnits()} can be placed.");
    }
    
    void Update()
    {
        if (Input.GetKeyDown(cycleUnitsKey))
        {
            CycleToNextUnit();
        }
    }
    
    public void CycleToNextUnit()
    {
        if (availableUnits.Count == 0) return;
        
        currentSelectedIndex = (currentSelectedIndex + 1) % availableUnits.Count;
        UpdateSelectedUnit();
        
        Debug.Log($"Selected unit: {GetCurrentUnitName()} ({currentSelectedIndex + 1}/{availableUnits.Count})");
    }
    
    void UpdateSelectedUnit()
    {
        if (placementManager == null || availableUnits.Count == 0) return;
        
        // Update the placement manager's prefab
        placementManager.playerPrefab = availableUnits[currentSelectedIndex];
    }
    
    public bool CanPlaceMoreUnits()
    {
        return unitsPlaced < GetMaxUnits();
    }
    
    public void OnUnitPlaced()
    {
        unitsPlaced++;
        Debug.Log($"Units placed: {unitsPlaced}/{GetMaxUnits()}");
        
        // Mark initial placement as complete when we reach max units for the first time
        if (unitsPlaced >= GetMaxUnits() && !initialPlacementComplete)
        {
            initialPlacementComplete = true;
            Debug.Log("Initial placement phase complete - actions now available!");
        }
    }
    
    public void OnUnitRemoved()
    {
        unitsPlaced = Mathf.Max(0, unitsPlaced - 1);
        Debug.Log($"Units placed: {unitsPlaced}/{GetMaxUnits()}");
    }
    
    public string GetCurrentUnitName()
    {
        if (availableUnits.Count == 0) return "None";
        return availableUnits[currentSelectedIndex].name;
    }
    
    public int GetUnitsPlaced()
    {
        return unitsPlaced;
    }
    
    public int GetMaxUnits()
    {
        return modeManager != null ? modeManager.maxUnitsToPlace : 4; // Fallback to 4 if no ModeManager
    }
    
    public bool IsInitialPlacementComplete()
    {
        return initialPlacementComplete;
    }
    
    public void ForceCompleteInitialPlacement()
    {
        initialPlacementComplete = true;
        Debug.Log("Initial placement phase manually completed");
    }
}