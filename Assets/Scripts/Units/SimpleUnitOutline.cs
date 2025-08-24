using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class SimpleUnitOutline : MonoBehaviour
{
    [Header("Outline Settings")]
    [SerializeField] private Color outlineColor = Color.green;
    [SerializeField] private float outlineWidth = 0.02f;
    [SerializeField] private bool animateOutline = true;
    [SerializeField] private float pulseSpeed = 2f;
    [SerializeField] private float minScale = 0.98f;
    [SerializeField] private float maxScale = 1.05f;
    
    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;
    
    // Track outlined units
    private Dictionary<GameObject, GameObject> outlineObjects = new Dictionary<GameObject, GameObject>();
    private Dictionary<GameObject, GameObject> targetOutlineObjects = new Dictionary<GameObject, GameObject>();
    private GameObject selectedUnit;
    private Coroutine pulseCoroutine;
    
    void Start()
    {
        if (enableDebugLogs)
            Debug.Log("SimpleUnitOutline initialized successfully.");
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
            AddOutline(selectedUnit);
            if (enableDebugLogs)
                Debug.Log($"Added outline to {selectedUnit.name}");
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
    
    private void AddOutline(GameObject unit)
    {
        if (unit == null || outlineObjects.ContainsKey(unit)) return;
        
        // Create outline object
        GameObject outlineObj = CreateOutlineObject(unit);
        if (outlineObj != null)
        {
            outlineObjects[unit] = outlineObj;
            
            // Start animation if enabled
            if (animateOutline)
            {
                StartPulseAnimation(outlineObj);
            }
        }
    }
    
    private void RemoveOutline(GameObject unit)
    {
        if (unit == null || !outlineObjects.ContainsKey(unit)) return;
        
        // Stop animation
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
            pulseCoroutine = null;
        }
        
        // Destroy outline object
        GameObject outlineObj = outlineObjects[unit];
        if (outlineObj != null)
        {
            Destroy(outlineObj);
        }
        
        outlineObjects.Remove(unit);
    }
    
    private GameObject CreateOutlineObject(GameObject unit)
    {
        // Get the main renderer from the unit
        Renderer unitRenderer = unit.GetComponent<Renderer>();
        if (unitRenderer == null)
        {
            // Try to find renderer in children
            unitRenderer = unit.GetComponentInChildren<Renderer>();
        }
        
        if (unitRenderer == null)
        {
            Debug.LogWarning($"No renderer found on unit {unit.name}");
            return null;
        }
        
        // Create outline object
        GameObject outlineObj = new GameObject($"{unit.name}_Outline");
        outlineObj.transform.SetParent(unit.transform);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one * (1f + outlineWidth);
        
        // Copy the mesh
        MeshFilter unitMeshFilter = unitRenderer.GetComponent<MeshFilter>();
        if (unitMeshFilter != null && unitMeshFilter.sharedMesh != null)
        {
            // Add mesh filter and renderer
            MeshFilter outlineMeshFilter = outlineObj.AddComponent<MeshFilter>();
            MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
            
            // Set mesh
            outlineMeshFilter.sharedMesh = unitMeshFilter.sharedMesh;
            
            // Create outline material
            Material outlineMaterial = CreateOutlineMaterial();
            outlineRenderer.material = outlineMaterial;
            
            // Set rendering order (render behind the original)
            outlineRenderer.sortingOrder = -1;
        }
        else
        {
            Debug.LogWarning($"No mesh found on unit {unit.name}");
            Destroy(outlineObj);
            return null;
        }
        
        return outlineObj;
    }
    
    private Material CreateOutlineMaterial()
    {
        // Create a simple unlit material for the outline
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = outlineColor;
        
        // Make it slightly transparent
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 2999; // Render before transparent objects
        
        Color transparentColor = outlineColor;
        transparentColor.a = 0.8f;
        mat.color = transparentColor;
        
        return mat;
    }
    
    private void StartPulseAnimation(GameObject outlineObj)
    {
        if (pulseCoroutine != null)
        {
            StopCoroutine(pulseCoroutine);
        }
        
        pulseCoroutine = StartCoroutine(PulseOutline(outlineObj));
    }
    
    private IEnumerator PulseOutline(GameObject outlineObj)
    {
        while (outlineObj != null)
        {
            float time = Time.time * pulseSpeed;
            float pulseValue = Mathf.Lerp(minScale, maxScale, (Mathf.Sin(time) + 1f) * 0.5f);
            
            // Update scale
            outlineObj.transform.localScale = Vector3.one * pulseValue;
            
            yield return null;
        }
    }
    
    public void SetOutlineColor(Color color)
    {
        outlineColor = color;
        
        // Update existing outlines
        foreach (var outlineObj in outlineObjects.Values)
        {
            if (outlineObj != null)
            {
                Renderer renderer = outlineObj.GetComponent<Renderer>();
                if (renderer != null && renderer.material != null)
                {
                    Color transparentColor = color;
                    transparentColor.a = 0.8f;
                    renderer.material.color = transparentColor;
                }
            }
        }
    }
    
    void OnDestroy()
    {
        // Clean up all outline objects
        foreach (var outlineObj in outlineObjects.Values)
        {
            if (outlineObj != null)
            {
                Destroy(outlineObj);
            }
        }
        outlineObjects.Clear();
        
        // Clean up all target outline objects
        foreach (var outlineObj in targetOutlineObjects.Values)
        {
            if (outlineObj != null)
            {
                Destroy(outlineObj);
            }
        }
        targetOutlineObjects.Clear();
    }
    
    // Public getters
    public bool IsUnitOutlined(GameObject unit)
    {
        return outlineObjects.ContainsKey(unit);
    }
    
    public GameObject GetSelectedUnit()
    {
        return selectedUnit;
    }
    
    // Target outline methods for attack system
    public void AddTargetOutline(GameObject unit, Color color)
    {
        if (unit == null || targetOutlineObjects.ContainsKey(unit)) return;
        
        // Create target outline object
        GameObject outlineObj = CreateTargetOutlineObject(unit, color);
        if (outlineObj != null)
        {
            targetOutlineObjects[unit] = outlineObj;
            
            if (enableDebugLogs)
                Debug.Log($"Added target outline to {unit.name}");
        }
    }
    
    public void RemoveTargetOutline(GameObject unit)
    {
        if (unit == null || !targetOutlineObjects.ContainsKey(unit)) return;
        
        // Destroy target outline object
        GameObject outlineObj = targetOutlineObjects[unit];
        if (outlineObj != null)
        {
            Destroy(outlineObj);
        }
        
        targetOutlineObjects.Remove(unit);
        
        if (enableDebugLogs)
            Debug.Log($"Removed target outline from {unit.name}");
    }
    
    public void ClearTargetOutlines()
    {
        foreach (var kvp in targetOutlineObjects)
        {
            if (kvp.Value != null)
            {
                Destroy(kvp.Value);
            }
        }
        targetOutlineObjects.Clear();
        
        if (enableDebugLogs)
            Debug.Log("Cleared all target outlines");
    }
    
    private GameObject CreateTargetOutlineObject(GameObject unit, Color color)
    {
        // Get the main renderer from the unit
        Renderer unitRenderer = unit.GetComponent<Renderer>();
        if (unitRenderer == null)
        {
            // Try to find renderer in children
            unitRenderer = unit.GetComponentInChildren<Renderer>();
        }
        
        if (unitRenderer == null)
        {
            Debug.LogWarning($"No renderer found on target unit {unit.name}");
            return null;
        }
        
        // Create target outline object
        GameObject outlineObj = new GameObject($"{unit.name}_TargetOutline");
        outlineObj.transform.SetParent(unit.transform);
        outlineObj.transform.localPosition = Vector3.zero;
        outlineObj.transform.localRotation = Quaternion.identity;
        outlineObj.transform.localScale = Vector3.one * (1f + outlineWidth);
        
        // Copy the mesh
        MeshFilter unitMeshFilter = unitRenderer.GetComponent<MeshFilter>();
        if (unitMeshFilter != null && unitMeshFilter.sharedMesh != null)
        {
            // Add mesh filter and renderer
            MeshFilter outlineMeshFilter = outlineObj.AddComponent<MeshFilter>();
            MeshRenderer outlineRenderer = outlineObj.AddComponent<MeshRenderer>();
            
            // Set mesh
            outlineMeshFilter.sharedMesh = unitMeshFilter.sharedMesh;
            
            // Create target outline material with specified color
            Material outlineMaterial = CreateTargetOutlineMaterial(color);
            outlineRenderer.material = outlineMaterial;
            
            // Set rendering order (render behind the original)
            outlineRenderer.sortingOrder = -1;
        }
        else
        {
            Debug.LogWarning($"No mesh found on target unit {unit.name}");
            Destroy(outlineObj);
            return null;
        }
        
        return outlineObj;
    }
    
    private Material CreateTargetOutlineMaterial(Color color)
    {
        // Create a simple unlit material for the target outline
        Material mat = new Material(Shader.Find("Unlit/Color"));
        mat.color = color;
        
        // Make it slightly transparent
        mat.SetFloat("_Mode", 3); // Transparent mode
        mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        mat.SetInt("_ZWrite", 0);
        mat.DisableKeyword("_ALPHATEST_ON");
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        mat.renderQueue = 2999; // Render before transparent objects
        
        Color transparentColor = color;
        transparentColor.a = 0.8f;
        mat.color = transparentColor;
        
        return mat;
    }
}