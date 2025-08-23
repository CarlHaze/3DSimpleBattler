using UnityEngine;
using TMPro;

public class UnitStatsDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI selectedUnitStatsText;
   
    private UnitMovementManager movementManager;
   
    void Start()
    {
        Debug.Log("UnitStatsDisplay: Start() called");
        
        // Try to find the UnitMovementManager
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        
        if (movementManager == null)
        {
            Debug.LogError("UnitMovementManager not found in scene!");
            
            // Let's also try the old FindObjectOfType method as backup
            movementManager = FindObjectOfType<UnitMovementManager>();
            if (movementManager != null)
            {
                Debug.Log("Found UnitMovementManager using FindObjectOfType");
            }
            else
            {
                Debug.LogError("UnitMovementManager still not found with FindObjectOfType");
                return;
            }
        }
        else
        {
            Debug.Log($"Found UnitMovementManager: {movementManager.name}");
        }
       
        if (selectedUnitStatsText == null)
        {
            Debug.LogError("SelectedUnitStats TextMeshPro component not assigned!");
            
            // Try to find it automatically if not assigned
            selectedUnitStatsText = GetComponent<TextMeshProUGUI>();
            if (selectedUnitStatsText == null)
            {
                selectedUnitStatsText = GetComponentInChildren<TextMeshProUGUI>();
            }
            
            if (selectedUnitStatsText != null)
            {
                Debug.Log($"Auto-found TextMeshPro component: {selectedUnitStatsText.name}");
            }
            else
            {
                Debug.LogError("Could not find TextMeshPro component anywhere!");
                return;
            }
        }
        else
        {
            Debug.Log($"TextMeshPro component assigned: {selectedUnitStatsText.name}");
        }
       
        // Test the text component immediately
        selectedUnitStatsText.text = "UnitStatsDisplay initialized";
        Debug.Log("Set initial test text");
        
        UpdateStatsDisplay();
    }
   
    void Update()
    {
        UpdateStatsDisplay();
    }
   
    void UpdateStatsDisplay()
    {
        if (movementManager == null)
        {
            Debug.LogWarning("MovementManager is null in UpdateStatsDisplay");
            return;
        }
        
        if (selectedUnitStatsText == null)
        {
            Debug.LogWarning("selectedUnitStatsText is null in UpdateStatsDisplay");
            return;
        }
       
        GameObject selectedUnit = movementManager.GetSelectedUnit();
        
        // Only log when selection changes to avoid spam
        static GameObject lastSelectedUnit = null;
        if (selectedUnit != lastSelectedUnit)
        {
            Debug.Log($"Selection changed. Selected unit: {(selectedUnit != null ? selectedUnit.name : "null")}");
            lastSelectedUnit = selectedUnit;
        }
       
        if (selectedUnit == null)
        {
            selectedUnitStatsText.text = "No unit selected";
            return;
        }
       
        Character character = selectedUnit.GetComponent<Character>();
        if (character == null)
        {
            Debug.LogWarning($"Selected unit '{selectedUnit.name}' has no Character component!");
            selectedUnitStatsText.text = $"Selected: {selectedUnit.name}\nNo Character component found";
            return;
        }
       
        CharacterStats stats = character.Stats;
        if (stats == null)
        {
            Debug.LogWarning($"Character '{character.CharacterName}' has null stats!");
            selectedUnitStatsText.text = $"Selected: {character.CharacterName}\nStats not initialized";
            return;
        }
        
        string displayName = !string.IsNullOrEmpty(character.CharacterName) ? character.CharacterName : selectedUnit.name;
       
        string statsText = $"{displayName}\nHP: {stats.CurrentHP}/{stats.MaxHP}\nATK: {stats.Attack} | DEF: {stats.Defense}\nSPD: {stats.Speed}";
        selectedUnitStatsText.text = statsText;
        
        // Only log stats when selection changes to avoid spam
        if (selectedUnit != lastSelectedUnit)
        {
            Debug.Log($"Updated stats display: {statsText.Replace('\n', ' | ')}");
        }
    }
}