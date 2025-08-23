using UnityEngine;
using TMPro;

public class UnitStatsDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI selectedUnitStatsText;
   
    private UnitMovementManager movementManager;
    private GameObject lastSelectedUnit;
   
    void Start()
    {
        Debug.Log("UnitStatsDisplay Start() called");
       
        movementManager = FindFirstObjectByType<UnitMovementManager>();
       
        if (movementManager == null)
        {
            Debug.LogError("UnitMovementManager not found!");
            return;
        }
       
        if (selectedUnitStatsText == null)
        {
            Debug.LogError("TextMeshPro component not assigned!");
            return;
        }
        
        // Simple test
        selectedUnitStatsText.text = "Ready - Select a unit";
        Debug.Log("Initial text set");
    }
   
    void Update()
    {
        UpdateStatsDisplay();
    }
   
    void UpdateStatsDisplay()
    {
        if (movementManager == null || selectedUnitStatsText == null) return;
       
        GameObject selectedUnit = movementManager.GetSelectedUnit();
       
        // Only update when selection changes
        if (selectedUnit == lastSelectedUnit) return;
        lastSelectedUnit = selectedUnit;
        
        Debug.Log($"Selection changed to: {(selectedUnit != null ? selectedUnit.name : "null")}");
       
        if (selectedUnit == null)
        {
            selectedUnitStatsText.text = "No unit selected";
            Debug.Log("Set text to: No unit selected");
            return;
        }
       
        Character character = selectedUnit.GetComponent<Character>();
        if (character == null)
        {
            selectedUnitStatsText.text = $"Selected: {selectedUnit.name}\nNo Character component";
            Debug.Log($"No Character component on {selectedUnit.name}");
            return;
        }
       
        CharacterStats stats = character.Stats;
        string displayName = !string.IsNullOrEmpty(character.CharacterName) ? character.CharacterName : selectedUnit.name;
       
        string newText = $"{displayName}\nHP: {stats.CurrentHP}/{stats.MaxHP}\nATK: {stats.Attack} | DEF: {stats.Defense}";
        selectedUnitStatsText.text = newText;
        
        Debug.Log($"Set text to: {newText}");
    }
}