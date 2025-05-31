using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Quick script to fix Canvas positioning and create proper UI
/// </summary>
public class CanvasFixer : MonoBehaviour
{
    [Header("Fix Options")]
    public bool fixExistingCanvas = true;
    public bool createNewCanvas = false;
    public bool createUI = true;
    
    [ContextMenu("Fix Canvas and Create UI")]
    public void FixCanvasAndCreateUI()
    {
        if (fixExistingCanvas)
        {
            FixExistingCanvas();
        }
        
        if (createNewCanvas)
        {
            CreateNewCanvas();
        }
        
        if (createUI)
        {
            CreateSelectionUI();
        }
    }
    
    void FixExistingCanvas()
    {
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas canvas in allCanvases)
        {
            Debug.Log($"Fixing canvas: {canvas.name}");
            
            // Set proper render mode
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 0;
            
            // Reset transform
            canvas.transform.position = Vector3.zero;
            canvas.transform.rotation = Quaternion.identity;
            canvas.transform.localScale = Vector3.one;
            
            // Fix RectTransform
            RectTransform canvasRect = canvas.GetComponent<RectTransform>();
            if (canvasRect != null)
            {
                canvasRect.localPosition = Vector3.zero;
                canvasRect.localRotation = Quaternion.identity;
                canvasRect.localScale = Vector3.one;
                canvasRect.anchorMin = Vector2.zero;
                canvasRect.anchorMax = Vector2.one;
                canvasRect.anchoredPosition = Vector2.zero;
                canvasRect.sizeDelta = Vector2.zero;
            }
            
            // Add CanvasScaler if missing
            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler == null)
            {
                scaler = canvas.gameObject.AddComponent<CanvasScaler>();
            }
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            
            // Add GraphicRaycaster if missing
            if (canvas.GetComponent<GraphicRaycaster>() == null)
            {
                canvas.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
        
        Debug.Log($"Fixed {allCanvases.Length} canvas(es)");
    }
    
    void CreateNewCanvas()
    {
        GameObject canvasGO = new GameObject("UICanvas");
        Canvas canvas = canvasGO.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10; // Make sure it's on top
        
        CanvasScaler scaler = canvasGO.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasGO.AddComponent<GraphicRaycaster>();
        
        Debug.Log("Created new canvas: UICanvas");
    }
    
    void CreateSelectionUI()
    {
        // Find a working canvas
        Canvas canvas = null;
        Canvas[] allCanvases = FindObjectsByType<Canvas>(FindObjectsSortMode.None);
        
        foreach (Canvas c in allCanvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }
        
        if (canvas == null)
        {
            Debug.LogError("No suitable canvas found! Run 'Fix Canvas' first.");
            return;
        }
        
        // Check if UI already exists
        if (canvas.transform.Find("UnitInfoPanel") != null)
        {
            Debug.Log("UI already exists!");
            return;
        }
        
        CreateEventSystem();
        CreateUnitInfoPanel(canvas);
        
        Debug.Log("UI created successfully!");
    }
    
    void CreateEventSystem()
    {
        UnityEngine.EventSystems.EventSystem eventSystem = FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>();
        if (eventSystem == null)
        {
            GameObject eventSystemGO = new GameObject("EventSystem");
            eventSystemGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            Debug.Log("Created EventSystem");
        }
    }
    
    void CreateUnitInfoPanel(Canvas canvas)
    {
        // Create main panel
        GameObject panel = new GameObject("UnitInfoPanel");
        panel.transform.SetParent(canvas.transform, false);
        
        // Add Image component for background
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);
        
        // Setup RectTransform for top-right positioning
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(1, 1);      // Top-right anchor
        panelRect.anchorMax = new Vector2(1, 1);      // Top-right anchor
        panelRect.pivot = new Vector2(1, 1);          // Pivot at top-right
        panelRect.anchoredPosition = new Vector2(-20, -20); // 20 pixels from top-right
        panelRect.sizeDelta = new Vector2(300, 200);  // Panel size
        
        // Create title
        CreateUIText(panel, "Title", "Selected Unit", 18, FontStyles.Bold, Color.white, 
                    new Vector2(0, 0.8f), new Vector2(1, 1), new Vector4(10, 10, 10, 5));
        
        // Create unit name
        GameObject unitNameGO = CreateUIText(panel, "UnitName", "Unit Name", 16, FontStyles.Normal, Color.yellow, 
                                           new Vector2(0, 0.6f), new Vector2(1, 0.8f), new Vector4(10, 0, 10, 0));
        
        // Create position text
        GameObject unitPosGO = CreateUIText(panel, "UnitPosition", "Position: (0, 0)", 14, FontStyles.Normal, Color.white, 
                                          new Vector2(0, 0.4f), new Vector2(1, 0.6f), new Vector4(10, 0, 10, 0));
        
        // Create stats text
        GameObject unitStatsGO = CreateUIText(panel, "UnitStats", "HP: 100/100\nMove: 3\nAttack: 25", 12, FontStyles.Normal, Color.white, 
                                            new Vector2(0, 0.15f), new Vector2(1, 0.4f), new Vector4(10, 0, 10, 0));
        
        // Create deselect button
        GameObject buttonGO = CreateButton(panel, "DeselectButton", "Deselect", 
                                         new Vector2(0, 0), new Vector2(1, 0.15f), new Vector4(10, 5, 10, 5));
        
        // Try to connect to UnitSelectionManager
        UnitSelectionManager selectionManager = FindFirstObjectByType<UnitSelectionManager>();
        if (selectionManager != null)
        {
            selectionManager.unitInfoPanel = panel;
            selectionManager.unitNameText = unitNameGO.GetComponent<TextMeshProUGUI>();
            selectionManager.unitPositionText = unitPosGO.GetComponent<TextMeshProUGUI>();
            selectionManager.unitStatsText = unitStatsGO.GetComponent<TextMeshProUGUI>();
            selectionManager.deselectButton = buttonGO.GetComponent<Button>();
            
            Debug.Log("Connected UI to UnitSelectionManager!");
        }
        else
        {
            Debug.LogWarning("UnitSelectionManager not found. Create one first!");
        }
        
        // Hide panel initially
        panel.SetActive(false);
    }
    
    GameObject CreateUIText(GameObject parent, string name, string text, float fontSize, FontStyles fontStyle, Color color,
                           Vector2 anchorMin, Vector2 anchorMax, Vector4 margins)
    {
        GameObject textGO = new GameObject(name);
        textGO.transform.SetParent(parent.transform, false);
        
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.fontStyle = fontStyle;
        textComponent.color = color;
        textComponent.alignment = TextAlignmentOptions.Left;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = anchorMin;
        textRect.anchorMax = anchorMax;
        textRect.offsetMin = new Vector2(margins.x, margins.y);
        textRect.offsetMax = new Vector2(-margins.z, -margins.w);
        
        return textGO;
    }
    
    GameObject CreateButton(GameObject parent, string name, string buttonText, 
                           Vector2 anchorMin, Vector2 anchorMax, Vector4 margins)
    {
        GameObject buttonGO = new GameObject(name);
        buttonGO.transform.SetParent(parent.transform, false);
        
        // Button background
        Image buttonImage = buttonGO.AddComponent<Image>();
        buttonImage.color = new Color(0.8f, 0.2f, 0.2f, 0.8f);
        
        Button button = buttonGO.AddComponent<Button>();
        
        // Button rect
        RectTransform buttonRect = buttonGO.GetComponent<RectTransform>();
        buttonRect.anchorMin = anchorMin;
        buttonRect.anchorMax = anchorMax;
        buttonRect.offsetMin = new Vector2(margins.x, margins.y);
        buttonRect.offsetMax = new Vector2(-margins.z, -margins.w);
        
        // Button text
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(buttonGO.transform, false);
        
        TextMeshProUGUI textComponent = textGO.AddComponent<TextMeshProUGUI>();
        textComponent.text = buttonText;
        textComponent.fontSize = 14;
        textComponent.color = Color.white;
        textComponent.alignment = TextAlignmentOptions.Center;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        return buttonGO;
    }
}