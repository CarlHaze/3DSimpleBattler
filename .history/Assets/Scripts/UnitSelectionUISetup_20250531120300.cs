using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// This script helps create the UI for the Unit Selection System
/// Attach this to an empty GameObject and run it in Play mode to auto-generate the UI
/// </summary>
public class UnitSelectionUISetup : MonoBehaviour
{
    [Header("Auto-Setup Settings")]
    public bool createUIOnStart = true;
    public Vector2 panelPosition = new Vector2(-300, 200);
    public Vector2 panelSize = new Vector2(280, 150);
    
    void Start()
    {
        if (createUIOnStart)
        {
            CreateSelectionUI();
        }
    }
    
    [ContextMenu("Create Selection UI")]
    public void CreateSelectionUI()
    {
        // Create Canvas if it doesn't exist
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("Canvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create EventSystem if it doesn't exist
        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
        
        // Create Unit Info Panel
        GameObject panel = new GameObject("UnitInfoPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        // Panel background
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 0.8f);
        
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.anchoredPosition = panelPosition;
        panelRect.sizeDelta = panelSize;
        
        // Title
        GameObject title = new GameObject("Title");
        title.transform.SetParent(panel.transform, false);
        TextMeshProUGUI titleText = title.AddComponent<TextMeshProUGUI>();
        titleText.text = "Selected Unit";
        titleText.fontSize = 16;
        titleText.fontStyle = FontStyles.Bold;
        titleText.color = Color.white;
        titleText.alignment = TextAlignmentOptions.Center;
        
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0, 0.8f);
        titleRect.anchorMax = new Vector2(1, 1);
        titleRect.offsetMin = new Vector2(10, 0);
        titleRect.offsetMax = new Vector2(-10, -5);
        
        // Unit Name
        GameObject unitName = new GameObject("UnitName");
        unitName.transform.SetParent(panel.transform, false);
        TextMeshProUGUI unitNameText = unitName.AddComponent<TextMeshProUGUI>();
        unitNameText.text = "Unit Name";
        unitNameText.fontSize = 14;
        unitNameText.color = Color.yellow;
        
        RectTransform nameRect = unitName.GetComponent<RectTransform>();
        nameRect.anchorMin = new Vector2(0, 0.6f);
        nameRect.anchorMax = new Vector2(1, 0.8f);
        nameRect.offsetMin = new Vector2(10, 0);
        nameRect.offsetMax = new Vector2(-10, 0);
        
        // Unit Position
        GameObject unitPosition = new GameObject("UnitPosition");
        unitPosition.transform.SetParent(panel.transform, false);
        TextMeshProUGUI unitPositionText = unitPosition.AddComponent<TextMeshProUGUI>();
        unitPositionText.text = "Position: (0, 0)";
        unitPositionText.fontSize = 12;
        unitPositionText.color = Color.white;
        
        RectTransform posRect = unitPosition.GetComponent<RectTransform>();
        posRect.anchorMin = new Vector2(0, 0.4f);
        posRect.anchorMax = new Vector2(1, 0.6f);
        posRect.offsetMin = new Vector2(10, 0);
        posRect.offsetMax = new Vector2(-10, 0);
        
        // Unit Stats
        GameObject unitStats = new GameObject("UnitStats");
        unitStats.transform.SetParent(panel.transform, false);
        TextMeshProUGUI unitStatsText = unitStats.AddComponent<TextMeshProUGUI>();
        unitStatsText.text = "HP: 100/100\nMove: 3\nAttack: 25";
        unitStatsText.fontSize = 11;
        unitStatsText.color = Color.white;
        
        RectTransform statsRect = unitStats.GetComponent<RectTransform>();
        statsRect.anchorMin = new Vector2(0, 0.15f);
        statsRect.anchorMax = new Vector2(1, 0.4f);
        statsRect.offsetMin = new Vector2(10, 0);
        statsRect.offsetMax = new Vector2(-10, 0);
        
        // Deselect Button
        GameObject buttonGO = new GameObject("DeselectButton");
        buttonGO.transform.SetParent(panel.transform, false);
        
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        
        Button deselectButton = buttonGO.AddComponent<Button>();
        
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0, 0);
        buttonRect.anchorMax = new Vector2(1, 0.15f);
        buttonRect.offsetMin = new Vector2(10, 5);
        buttonRect.offsetMax = new Vector2(-10, -5);
        
        // Button Text
        GameObject buttonTextGO = new GameObject("Text");
        buttonTextGO.transform.SetParent(buttonGO.transform, false);
        TextMeshProUGUI buttonText = buttonTextGO.AddComponent<TextMeshProUGUI>();
        buttonText.text = "Deselect";
        buttonText.fontSize = 12;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        
        RectTransform buttonTextRect = buttonTextGO.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.offsetMin = Vector2.zero;
        buttonTextRect.offsetMax = Vector2.zero;
        
        // Setup UnitSelectionManager if it exists
        UnitSelectionManager selectionManager = FindFirstObjectByType<UnitSelectionManager>();
        if (selectionManager != null)
        {
            selectionManager.unitInfoPanel = panel;
            selectionManager.unitNameText = unitNameText;
            selectionManager.unitPositionText = unitPositionText;
            selectionManager.unitStatsText = unitStatsText;
            selectionManager.deselectButton = deselectButton;
            
            Debug.Log("Connected UI to UnitSelectionManager!");
        }
        else
        {
            Debug.LogWarning("UnitSelectionManager not found. Create one and assign the UI references manually.");
        }
        
        // Hide panel initially
        panel.SetActive(false);
        
        Debug.Log("Unit Selection UI created successfully!");
    }
}

// Enhanced UnitGridInfo to work better with selection system
[System.Serializable]
public class UnitStats
{
    public int maxHP = 100;
    public int currentHP = 100;
    public int moveRange = 3;
    public int attackDamage = 25;
    public int attackRange = 1;
    
    public UnitStats()
    {
        currentHP = maxHP;
    }
}

// Add this component to units to make them more compatible with the selection system
public class SelectableUnit : MonoBehaviour
{
    [Header("Unit Information")]
    public string unitName = "Unit";
    public UnitStats stats = new UnitStats();
    
    [Header("Visual Settings")]
    public bool highlightOnHover = true;
    public Color hoverColor = Color.cyan;
    
    private Material originalMaterial;
    private Material hoverMaterial;
    private Renderer unitRenderer;
    private bool isHovering = false;
    
    void Start()
    {
        unitRenderer = GetComponent<Renderer>();
        if (unitRenderer != null)
        {
            originalMaterial = unitRenderer.material;
            
            if (highlightOnHover)
            {
                hoverMaterial = new Material(originalMaterial);
                hoverMaterial.color = hoverColor;
                hoverMaterial.SetFloat("_Metallic", 0.3f);
                hoverMaterial.SetFloat("_Glossiness", 0.6f);
            }
        }
        
        // Set default name if empty
        if (string.IsNullOrEmpty(unitName))
        {
            unitName = gameObject.name.Replace("(Clone)", "");
        }
    }
    
    void OnMouseEnter()
    {
        if (highlightOnHover && !isHovering && hoverMaterial != null)
        {
            isHovering = true;
            unitRenderer.material = hoverMaterial;
        }
    }
    
    void OnMouseExit()
    {
        if (isHovering && originalMaterial != null)
        {
            isHovering = false;
            unitRenderer.material = originalMaterial;
        }
    }
    
    public void TakeDamage(int damage)
    {
        stats.currentHP = Mathf.Max(0, stats.currentHP - damage);
        if (stats.currentHP <= 0)
        {
            Die();
        }
    }
    
    public void Heal(int amount)
    {
        stats.currentHP = Mathf.Min(stats.maxHP, stats.currentHP + amount);
    }
    
    public bool IsAlive()
    {
        return stats.currentHP > 0;
    }
    
    void Die()
    {
        // Handle unit death
        Debug.Log($"{unitName} has died!");
        
        // Remove from placement manager
        UnitGridInfo gridInfo = GetComponent<UnitGridInfo>();
        if (gridInfo != null && gridInfo.placementManager != null)
        {
            gridInfo.placementManager.RemoveUnit(gridInfo.gridPosition, gridInfo.groundObject);
        }
        
        // Deselect if this unit is selected
        UnitSelectionManager selectionManager = FindFirstObjectByType<UnitSelectionManager>();
        if (selectionManager != null && selectionManager.GetSelectedUnit() == gameObject)
        {
            selectionManager.DeselectUnit();
        }
        
        Destroy(gameObject);
    }
    
    void OnDestroy()
    {
        // Clean up materials
        if (hoverMaterial != null)
        {
            Destroy(hoverMaterial);
        }
    }
}