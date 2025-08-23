using UnityEngine;
using TMPro;

public class UnitStatsDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI selectedUnitStatsText;
    
    private UnitMovementManager movementManager;
    
    void Start()
    {
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        
        if (movementManager == null)
        {
            Debug.LogError("UnitMovementManager not found!");
            return;
        }
        
        if (selectedUnitStatsText == null)
        {
            Debug.LogError("SelectedUnitStats TextMeshPro component not assigned!");
            return;
        }
        
        UpdateStatsDisplay();
    }
    
    void Update()
    {
        UpdateStatsDisplay();
    }
    
    void UpdateStatsDisplay()
    {
        if (movementManager == null || selectedUnitStatsText == null) return;
        
        GameObject selectedUnit = movementManager.GetSelectedUnit();
        Debug.Log($"Selected unit: {(selectedUnit != null ? selectedUnit.name : "null")}");
        
        if (selectedUnit == null)
        {
            selectedUnitStatsText.text = "No unit selected";
            return;
        }
        
        Character character = selectedUnit.GetComponent<Character>();
        if (character == null)
        {
            selectedUnitStatsText.text = $"Selected: {selectedUnit.name}\nNo stats available";
            return;
        }
        
        CharacterStats stats = character.Stats;
        string displayName = !string.IsNullOrEmpty(character.CharacterName) ? character.CharacterName : selectedUnit.name;
        
        selectedUnitStatsText.text = $"{displayName}\nHP: {stats.CurrentHP}/{stats.MaxHP}";
    }
}