using UnityEngine;
using TMPro;

public class UnitStatsDisplay : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI selectedUnitStatsText;
   
    private UnitMovementManager movementManager;
    private GameObject lastSelectedUnit;
    private GameObject lastSelectedEnemyUnit;
    private SimpleUnitOutline outlineController;
   
    void Start()
    {
        Debug.Log("UnitStatsDisplay Start() called");
       
        movementManager = FindFirstObjectByType<UnitMovementManager>();
        outlineController = FindFirstObjectByType<SimpleUnitOutline>();
       
        if (movementManager == null)
        {
            Debug.LogError("UnitMovementManager not found!");
            return;
        }
        
        if (outlineController == null)
        {
            Debug.LogWarning("SimpleUnitOutline not found - enemy highlighting will not work!");
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
        CheckForEnemySelection();
    }
   
    void UpdateStatsDisplay()
    {
        if (movementManager == null || selectedUnitStatsText == null) return;
       
        GameObject selectedUnit = movementManager.GetSelectedUnit();
       
        // Only update when selection changes
        if (selectedUnit == lastSelectedUnit) return;
        lastSelectedUnit = selectedUnit;
        
        // Clear enemy selection when player unit is selected
        if (selectedUnit != null && lastSelectedEnemyUnit != null)
        {
            ClearEnemySelection();
        }
        
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
        
        string classInfo = "";
        if (character.CharacterClass != null)
        {
            classInfo = $"\nClass: {character.CharacterClass.ClassName}";
        }
       
        string newText = $"{displayName}{classInfo}\nHP: {stats.CurrentHP}/{stats.MaxHP}\nATK: {stats.Attack} | DEF: {stats.Defense}";
        selectedUnitStatsText.text = newText;
        
        Debug.Log($"Set text to: {newText}");
    }
    
    void CheckForEnemySelection()
    {
        if (selectedUnitStatsText == null) return;
        
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
                    // Only process enemy click if no player unit is selected or if movement isn't active
                    if (movementManager.GetSelectedUnit() == null || !movementManager.IsMoving())
                    {
                        DisplayEnemyStats(clickedObject);
                    }
                }
                else if (!clickedObject.CompareTag("Player"))
                {
                    ClearEnemySelection();
                }
            }
        }
    }
    
    void DisplayEnemyStats(GameObject enemyUnit)
    {
        if (enemyUnit == lastSelectedEnemyUnit) return;
        
        // Clear previous enemy selection
        ClearEnemySelection();
        
        lastSelectedEnemyUnit = enemyUnit;
        
        // Add red highlight to enemy
        if (outlineController != null)
        {
            outlineController.SetSelectedUnit(enemyUnit);
            outlineController.SetOutlineColor(Color.red);
        }
        
        Character character = enemyUnit.GetComponent<Character>();
        if (character == null)
        {
            selectedUnitStatsText.text = $"Enemy: {enemyUnit.name}\nNo Character component";
            Debug.Log($"No Character component on enemy {enemyUnit.name}");
            return;
        }
        
        CharacterStats stats = character.Stats;
        string displayName = !string.IsNullOrEmpty(character.CharacterName) ? character.CharacterName : enemyUnit.name;
        
        string classInfo = "";
        if (character.CharacterClass != null)
        {
            classInfo = $"\nClass: {character.CharacterClass.ClassName}";
        }
        
        string newText = $"Enemy: {displayName}{classInfo}\nHP: {stats.CurrentHP}/{stats.MaxHP}\nATK: {stats.Attack} | DEF: {stats.Defense}";
        selectedUnitStatsText.text = newText;
        
        Debug.Log($"Set enemy text to: {newText}");
    }
    
    void ClearEnemySelection()
    {
        if (lastSelectedEnemyUnit != null)
        {
            // Remove enemy highlight
            if (outlineController != null)
            {
                outlineController.ClearSelectedUnit();
                outlineController.SetOutlineColor(Color.green); // Reset to default player color
            }
            
            lastSelectedEnemyUnit = null;
        }
    }
}