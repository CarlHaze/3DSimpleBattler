using UnityEngine;
using TMPro;

public class UnitStatsDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI selectedUnitStatsText;
    
    [Header("Debug")]
    public bool forceVisibleSettings = true;
   
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
        Debug.Log("UnitMovementManager found");
       
        if (selectedUnitStatsText == null)
        {
            Debug.LogError("SelectedUnitStats TextMeshPro component not assigned!");
            return;
        }
        Debug.Log("TextMeshPro component assigned");
        
        // Force visible settings on start
        if (forceVisibleSettings)
        {
            SetupVisibleText();
        }
       
        UpdateStatsDisplay();
    }
    
    void SetupVisibleText()
    {
        if (selectedUnitStatsText == null) return;
        
        Debug.Log("Setting up visible text properties...");
        
        // Force visible color and size
        selectedUnitStatsText.color = Color.white;
        selectedUnitStatsText.fontSize = 18;
        selectedUnitStatsText.alpha = 1f;
        
        // Ensure proper alignment
        selectedUnitStatsText.alignment = TextAlignmentOptions.TopLeft;
        
        // Force material properties if needed
        if (selectedUnitStatsText.fontMaterial != null)
        {
            var material = selectedUnitStatsText.fontMaterial;
            if (material.HasProperty("_FaceColor"))
            {
                Color faceColor = Color.white;
                faceColor.a = 1f;
                material.SetColor("_FaceColor", faceColor);
            }
        }
        
        // Test with initial text
        selectedUnitStatsText.text = "Stats Display Ready";
        
        Debug.Log($"Text setup complete. Color: {selectedUnitStatsText.color}, Size: {selectedUnitStatsText.fontSize}");
    }
   
    void Update()
    {
        UpdateStatsDisplay();
    }
   
    void UpdateStatsDisplay()
    {
        if (movementManager == null || selectedUnitStatsText == null) return;
       
        GameObject selectedUnit = movementManager.GetSelectedUnit();
       
        // Only update if selection changed
        if (selectedUnit == lastSelectedUnit) return;
        lastSelectedUnit = selectedUnit;
        
        Debug.Log($"Selection changed to: {(selectedUnit != null ? selectedUnit.name : "null")}");
       
        if (selectedUnit == null)
        {
            SetText("No unit selected");
            return;
        }
       
        Character character = selectedUnit.GetComponent<Character>();
        if (character == null)
        {
            SetText($"Selected: {selectedUnit.name}\nNo Character component");
            Debug.LogError($"Character component missing on {selectedUnit.name}");
            return;
        }
       
        CharacterStats stats = character.Stats;
        string displayName = !string.IsNullOrEmpty(character.CharacterName) ? character.CharacterName : selectedUnit.name;
       
        string statsText = $"{displayName}\nHP: {stats.CurrentHP}/{stats.MaxHP}\nATK: {stats.Attack} | DEF: {stats.Defense}";
        SetText(statsText);
    }
    
    void SetText(string text)
    {
        selectedUnitStatsText.text = text;
        
        // Force refresh
        selectedUnitStatsText.SetAllDirty();
        
        Debug.Log($"Text set to: '{text}'");
        Debug.Log($"TMP active: {selectedUnitStatsText.gameObject.activeInHierarchy}");
        Debug.Log($"TMP color: {selectedUnitStatsText.color}");
        Debug.Log($"TMP fontSize: {selectedUnitStatsText.fontSize}");
    }
}