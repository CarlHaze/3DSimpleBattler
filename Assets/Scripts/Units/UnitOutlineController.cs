using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class UnitOutlineController : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Shader outlineShader;
    [SerializeField] private Color selectedOutlineColor = Color.green;
    [SerializeField] private Color activeUnitOutlineColor = Color.yellow;
    [SerializeField] private float outlineWidth = 0.008f;
    [SerializeField] private bool animateOutline = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minPulseWidth = 0.005f;
    [SerializeField] private float maxPulseWidth = 0.012f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Store original materials for restoration
    private Dictionary<Renderer, Material[]> originalMaterials = new Dictionary<Renderer, Material[]>();
    private Dictionary<Renderer, Material[]> outlineMaterials = new Dictionary<Renderer, Material[]>();
    
    // Track outlined units
    private HashSet<GameObject> outlinedUnits = new HashSet<GameObject>();
    private GameObject selectedUnit;
    private GameObject activeUnit; // For future turn system
    
    // Animation
    private Coroutine pulseCoroutine;
    
    void Start()
    {
        // Load outline shader if not assigned
        if (outlineShader == null)
        {
            outlineShader = Shader.Find("Custom/UnitOutline");
            if (outlineShader == null)
            {
                Debug.LogError("UnitOutline shader not found! Make sure the shader is compiled.");
                enabled = false;
                return;
            }
        }
        
        if (enableDebugLogs)
            Debug.Log("UnitOutlineController initialized successfully.");
    }
    
    public void SetSelectedUnit(GameObject unit)
    {
        // Clear previous selection
        if (selectedUnit != null)
        {
            RemoveOutline(selectedUnit);
        }
        
        selectedUnit = unit;
        
        if (selectedUnit != null)
        {
            AddOutline(selectedUnit, selectedOutlineColor, OutlineType.Selected);
            if (enableDebugLogs)
                Debug.Log($"Added selection outline to {selectedUnit.name}");
        }
    }
    
    public void SetActiveUnit(GameObject unit)
    {
        // Clear previous active unit
        if (activeUnit != null && activeUnit != selectedUnit)
        {
            RemoveOutline(activeUnit);
        }
        
        activeUnit = unit;
        
        if (activeUnit != null && activeUnit != selectedUnit)
        {
            AddOutline(activeUnit, activeUnitOutlineColor, OutlineType.Active);
            if (enableDebugLogs)
                Debug.Log($"Added active outline to {activeUnit.name}");
        }
    }
    
    public void ClearSelectedUnit()
    {
        if (selectedUnit != null)
        {
            RemoveOutline(selectedUnit);
            selectedUnit = null;
            
            if (enableDebugLogs)
                Debug.Log("Cleared selected unit outline");
        }
    }
    
    public void ClearActiveUnit()
    {
        if (activeUnit != null && activeUnit != selectedUnit)
        {
            RemoveOutline(activeUnit);
        }
        activeUnit = null;
    }
    
    public void ClearAllOutlines()
    {
        List<GameObject> unitsToRemove = new List<GameObject>(outlinedUnits);
        foreach (GameObject unit in unitsToRemove)
        {
            RemoveOutline(unit);
        }
        
        selectedUnit = null;
        activeUnit = null;
        
        if (enableDebugLogs)
            Debug.Log("Cleared all unit outlines");
    }
    
    private void AddOutline(GameObject unit, Color outlineColor, OutlineType type)
    {
        if (unit == null) return;
        
        // Get all renderers in the unit (including children)
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            // Skip if already has outline materials
            if (outlineMaterials.ContainsKey(renderer)) continue;
            
            // Store original materials
            if (!originalMaterials.ContainsKey(renderer))
            {
                originalMaterials[renderer] = renderer.materials;
            }
            
            // Create outline materials
            Material[] originalMats = originalMaterials[renderer];
            Material[] newMats = new Material[originalMats.Length];
            
            for (int i = 0; i < originalMats.Length; i++)
            {
                // Create new material with outline shader
                Material outlineMat = new Material(outlineShader);
                
                // Copy main texture if it exists
                if (originalMats[i].HasProperty("_MainTex"))
                {
                    outlineMat.SetTexture("_MainTex", originalMats[i].GetTexture("_MainTex"));
                }
                
                // Copy main color if it exists
                if (originalMats[i].HasProperty("_Color"))
                {
                    outlineMat.SetColor("_Color", originalMats[i].GetColor("_Color"));
                }
                else
                {
                    outlineMat.SetColor("_Color", Color.white);
                }
                
                // Set outline properties
                outlineMat.SetColor("_OutlineColor", outlineColor);
                outlineMat.SetFloat("_OutlineWidth", outlineWidth);
                
                newMats[i] = outlineMat;
            }
            
            // Store outline materials and apply them
            outlineMaterials[renderer] = newMats;
            renderer.materials = newMats;
        }
        
        outlinedUnits.Add(unit);
        
        // Start pulse animation if enabled and this is the selected unit
        if (animateOutline && type == OutlineType.Selected)
        {
            StartPulseAnimation(unit);
        }
    }
    
    private void RemoveOutline(GameObject unit)
    {
        if (unit == null) return;
        
        // Stop pulse animation
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Get all renderers in the unit
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        
        foreach (Renderer renderer in renderers)
        {
            // Restore original materials
            if (originalMaterials.ContainsKey(renderer))
            {
                renderer.materials = originalMaterials[renderer];
            }
            
            // Clean up outline materials
            if (outlineMaterials.ContainsKey(renderer))
            {
                foreach (Material mat in outlineMaterials[renderer])
                {
                    if (mat != null)
                        DestroyImmediate(mat);
                }
                outlineMaterials.Remove(renderer);
            }
        }
        
        outlinedUnits.Remove(unit);
    }
    
    private void StartPulseAnimation(GameObject unit)
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        pulseCoroutine = StartCoroutine(PulseOutline(unit));
    }
    
    private IEnumerator PulseOutline(GameObject unit)
    {
        while (unit != null && outlinedUnits.Contains(unit))
        {
            float time = Time.time * pulseSpeed;
            float pulseValue = Mathf.Lerp(minPulseWidth, maxPulseWidth, (Mathf.Sin(time) + 1f) * 0.5f);
            
            // Update outline width for all renderers
            Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                if (outlineMaterials.ContainsKey(renderer))
                {
                    foreach (Material mat in outlineMaterials[renderer])
                    {
                        if (mat != null)
                        {
                            mat.SetFloat("_OutlineWidth", pulseValue);
                        }
                    }
                }
            }
            
            yield return null;
        }
    }
    
    void OnDestroy()
    {
        // Clean up all materials when the component is destroyed
        ClearAllOutlines();
        
        // Clean up remaining material references
        foreach (var materialArray in outlineMaterials.Values)
        {
            foreach (Material mat in materialArray)
            {
                if (mat != null)
                    DestroyImmediate(mat);
            }
        }
        
        originalMaterials.Clear();
        outlineMaterials.Clear();
    }
    
    // Public getters
    public bool IsUnitOutlined(GameObject unit)
    {
        return outlinedUnits.Contains(unit);
    }
    
    public GameObject GetSelectedUnit()
    {
        return selectedUnit;
    }
    
    public GameObject GetActiveUnit()
    {
        return activeUnit;
    }
    
    // Utility method to set outline color dynamically
    public void SetOutlineColor(GameObject unit, Color color)
    {
        if (unit == null || !outlinedUnits.Contains(unit)) return;
        
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (outlineMaterials.ContainsKey(renderer))
            {
                foreach (Material mat in outlineMaterials[renderer])
                {
                    if (mat != null)
                    {
                        mat.SetColor("_OutlineColor", color);
                    }
                }
            }
        }
    }
    
    // Utility method to set outline width dynamically
    public void SetOutlineWidth(GameObject unit, float width)
    {
        if (unit == null || !outlinedUnits.Contains(unit)) return;
        
        Renderer[] renderers = unit.GetComponentsInChildren<Renderer>();
        foreach (Renderer renderer in renderers)
        {
            if (outlineMaterials.ContainsKey(renderer))
            {
                foreach (Material mat in outlineMaterials[renderer])
                {
                    if (mat != null)
                    {
                        mat.SetFloat("_OutlineWidth", width);
                    }
                }
            }
        }
    }
}

public enum OutlineType
{
    Selected,
    Active,
    Custom
}