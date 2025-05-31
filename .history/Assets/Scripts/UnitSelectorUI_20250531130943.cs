using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitSelectorUI : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI selectedUnitText;
    public TextMeshProUGUI unitCountText;
    
    private SimpleUnitSelector unitSelector;
    
    void Start()
    {
        unitSelector = FindFirstObjectByType<SimpleUnitSelector>();
        
        if (unitSelector == null)
        {
            Debug.LogError("SimpleUnitSelector not found!");
            return;
        }
        
        UpdateUI();
    }
    
    void Update()
    {
        UpdateUI();
    }
    
    void UpdateUI()
    {
        if (unitSelector == null) return;
        
        // Update selected unit text
        if (selectedUnitText != null)
        {
            selectedUnitText.text = $"Selected: {unitSelector.GetCurrentUnitName()}";
        }
        
        // Update count text
        if (unitCountText != null)
        {
            unitCountText.text = $"Units: {unitSelector.GetUnitsPlaced()}/{unitSelector.GetMaxUnits()}";
        }
    }
}