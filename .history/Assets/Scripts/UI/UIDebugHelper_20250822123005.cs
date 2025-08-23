using UnityEngine;
using TMPro;

public class UIDebugHelper : MonoBehaviour
{
    [Header("Debug Controls")]
    public bool forceVisibleText = false;
    public bool debugTextProperties = false;
    
    void Update()
    {
        if (forceVisibleText)
        {
            ForceTextVisible();
            forceVisibleText = false;
        }
        
        if (debugTextProperties)
        {
            DebugTextProperties();
            debugTextProperties = false;
        }
    }
    
    void ForceTextVisible()
    {
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null) return;
        
        Debug.Log("=== FORCING TEXT VISIBLE ===");
        
        // Force visible settings
        tmp.color = Color.white;
        tmp.fontSize = 24;
        tmp.text = "FORCED VISIBLE TEXT - HP: 100/100";
        tmp.alpha = 1f;
        tmp.fontMaterial.SetFloat("_FaceColor", 1f);
        
        // Force rebuild
        tmp.SetAllDirty();
        tmp.Rebuild(UnityEngine.UI.CanvasUpdate.PostLayout);
        
        Debug.Log("Text forced to be visible");
    }
    
    void DebugTextProperties()
    {
        TextMeshProUGUI tmp = GetComponent<TextMeshProUGUI>();
        if (tmp == null)
        {
            Debug.LogError("No TextMeshProUGUI component found!");
            return;
        }
        
        Debug.Log("=== TMP DEBUG INFO ===");
        Debug.Log($"Text: '{tmp.text}'");
        Debug.Log($"Font Size: {tmp.fontSize}");
        Debug.Log($"Color: {tmp.color}");
        Debug.Log($"Alpha: {tmp.alpha}");
        Debug.Log($"Enabled: {tmp.enabled}");
        Debug.Log($"GameObject Active: {tmp.gameObject.activeInHierarchy}");
        Debug.Log($"Canvas Group Alpha: {GetCanvasGroupAlpha()}");
        Debug.Log($"Font Asset: {(tmp.font != null ? tmp.font.name : "NULL")}");
        Debug.Log($"Material: {(tmp.fontMaterial != null ? tmp.fontMaterial.name : "NULL")}");
        Debug.Log($"Rect Transform: {tmp.rectTransform.rect}");
        Debug.Log($"Anchored Position: {tmp.rectTransform.anchoredPosition}");
        Debug.Log($"Canvas: {GetComponentInParent<Canvas>()?.name}");
        Debug.Log($"Canvas Render Mode: {GetComponentInParent<Canvas>()?.renderMode}");
        Debug.Log($"Canvas Sort Order: {GetComponentInParent<Canvas>()?.sortingOrder}");
    }
    
    float GetCanvasGroupAlpha()
    {
        CanvasGroup cg = GetComponentInParent<CanvasGroup>();
        return cg != null ? cg.alpha : 1f;
    }
}