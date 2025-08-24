using UnityEngine;
using System.Collections.Generic;

public class SimpleUnitSelector : MonoBehaviour
{
    [Header("Unit Selection")]
    public List<GameObject> availableUnits = new List<GameObject>();
    public int maxUnitsToPlace = 4;
    public KeyCode cycleUnitsKey = KeyCode.Tab;
    
    [Header("Current Selection")]
    public int currentSelectedIndex = 0;
    
    private UnitPlacementManager placementManager;
    private int unitsPlaced = 0;
    private bool initialPlacementComplete = false;
    
    void Start()
    {
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        
        if (placementManager == null)
        {
            Debug.LogError("UnitPlacementManager not found!");
            return;
        }
        
        // Set the initial unit if we have any
        if (availableUnits.Count > 0)
        {
            UpdateSelectedUnit();
        }
        
        Debug.Log($"Unit Selector initialized. {availableUnits.Count} units available, max {maxUnitsToPlace} can be placed.");
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
        return unitsPlaced < maxUnitsToPlace;
    }
    
    public void OnUnitPlaced()
    {
        unitsPlaced++;
        Debug.Log($"Units placed: {unitsPlaced}/{maxUnitsToPlace}");
        
        // Mark initial placement as complete when we reach max units for the first time
        if (unitsPlaced >= maxUnitsToPlace && !initialPlacementComplete)
        {
            initialPlacementComplete = true;
            Debug.Log("Initial placement phase complete - actions now available!");
        }
    }
    
    public void OnUnitRemoved()
    {
        unitsPlaced = Mathf.Max(0, unitsPlaced - 1);
        Debug.Log($"Units placed: {unitsPlaced}/{maxUnitsToPlace}");
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
        return maxUnitsToPlace;
    }
    
    public bool IsInitialPlacementComplete()
    {
        return initialPlacementComplete;
    }
}