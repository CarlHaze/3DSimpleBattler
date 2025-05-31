using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitSelectionManager : MonoBehaviour
{
    [Header("Selection Settings")]
    public LayerMask unitLayerMask = -1;
    public KeyCode deselectKey = KeyCode.Escape;
    
    [Header("Visual Selection Indicator")]
    public GameObject selectionIndicatorPrefab;
    public Material selectionMaterial;
    public Color selectedColor = Color.yellow;
    public float indicatorHeight = 0.1f;
    public float indicatorScale = 1.2f;
    public bool animateIndicator = true;
    public float animationSpeed = 2f;
    
    [Header("UI Panel")]
    public GameObject unitInfoPanel;
    public TextMeshProUGUI unitNameText;
    public TextMeshProUGUI unitPositionText;
    public TextMeshProUGUI unitStatsText;
    public Button deselectButton;
    
    [Header("Audio")]
    public AudioClip selectionSound;
    public AudioClip deselectSound;
    
    // Private variables
    private Camera gameCamera;
    private GameObject currentSelectedUnit;
    private GameObject selectionIndicator;
    private AudioSource audioSource;
    private UnitPlacementManager placementManager;
    
    // Events for other systems to listen to
    public System.Action<GameObject> OnUnitSelected;
    public System.Action OnUnitDeselected;
    
    void Start()
    {
        // Get required components
        gameCamera = Camera.main;
        if (gameCamera == null)
            gameCamera = FindFirstObjectByType<Camera>();
            
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        audioSource = GetComponent<AudioSource>();
        
        // Create audio source if it doesn't exist
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();
            
        // Create default selection indicator if not provided
        if (selectionIndicatorPrefab == null)
            CreateDefaultSelectionIndicator();
            
        // Setup UI
        SetupUI();
        
        // Hide unit info panel initially
        if (unitInfoPanel != null)
            unitInfoPanel.SetActive(false);
    }
    
    void Update()
    {
        HandleInput();
        AnimateSelectionIndicator();
    }
    
    void HandleInput()
    {
        // Handle deselection key
        if (Input.GetKeyDown(deselectKey))
        {
            DeselectUnit();
            return;
        }
        
        // Handle mouse clicks for selection
        if (Input.GetMouseButtonDown(0))
        {
            // Don't interfere with placement mode
            UnitPlacementManager placement = FindFirstObjectByType<UnitPlacementManager>();
            if (placement != null && placement.enabled)
            {
                // Check if placement manager is in placement mode via reflection or public property
                // For now, we'll check if the mouse is over a unit vs ground
                Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                
                // First check for units
                if (Physics.Raycast(ray, out hit, Mathf.Infinity, unitLayerMask))
                {
                    GameObject clickedObject = hit.collider.gameObject;
                    UnitGridInfo unitInfo = clickedObject.GetComponent<UnitGridInfo>();
                    
                    if (unitInfo != null)
                    {
                        SelectUnit(clickedObject);
                        return;
                    }
                }
                
                // If we didn't hit a unit, check if we should deselect
                if (currentSelectedUnit != null)
                {
                    // Only deselect if we're not clicking on UI
                    if (!IsPointerOverUI())
                    {
                        DeselectUnit();
                    }
                }
            }
            else
            {
                // Normal selection mode when not placing units
                TrySelectUnit();
            }
        }
    }
    
    void TrySelectUnit()
    {
        Ray ray = gameCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit, Mathf.Infinity, unitLayerMask))
        {
            GameObject clickedObject = hit.collider.gameObject;
            UnitGridInfo unitInfo = clickedObject.GetComponent<UnitGridInfo>();
            
            if (unitInfo != null)
            {
                if (currentSelectedUnit == clickedObject)
                {
                    // Clicking on already selected unit - could add double-click logic here
                    return;
                }
                else
                {
                    SelectUnit(clickedObject);
                }
            }
        }
        else
        {
            // Clicked on empty space
            if (!IsPointerOverUI())
            {
                DeselectUnit();
            }
        }
    }
    
    public void SelectUnit(GameObject unit)
    {
        if (unit == null) return;
        
        UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
        if (unitInfo == null)
        {
            Debug.LogWarning($"Tried to select {unit.name} but it doesn't have UnitGridInfo component!");
            return;
        }
        
        // Deselect previous unit
        if (currentSelectedUnit != null)
        {
            DeselectUnit();
        }
        
        // Select new unit
        currentSelectedUnit = unit;
        ShowSelectionIndicator(unit);
        UpdateUI(unit);
        PlaySelectionSound();
        
        // Notify other systems
        OnUnitSelected?.Invoke(unit);
        
        Debug.Log($"Selected unit: {unit.name}");
    }
    
    public void DeselectUnit()
    {
        if (currentSelectedUnit == null) return;
        
        Debug.Log($"Deselected unit: {currentSelectedUnit.name}");
        
        currentSelectedUnit = null;
        HideSelectionIndicator();
        HideUI();
        PlayDeselectSound();
        
        // Notify other systems
        OnUnitDeselected?.Invoke();
    }
    
    void ShowSelectionIndicator(GameObject unit)
    {
        if (selectionIndicator == null)
        {
            selectionIndicator = Instantiate(selectionIndicatorPrefab);
        }
        
        selectionIndicator.SetActive(true);
        
        // Position the indicator
        Vector3 position = unit.transform.position;
        position.y += indicatorHeight;
        selectionIndicator.transform.position = position;
        
        // Scale the indicator based on unit size
        Bounds unitBounds = GetUnitBounds(unit);
        float scale = Mathf.Max(unitBounds.size.x, unitBounds.size.z) * indicatorScale;
        selectionIndicator.transform.localScale = Vector3.one * scale;
        
        // Set color
        Renderer indicatorRenderer = selectionIndicator.GetComponent<Renderer>();
        if (indicatorRenderer != null && selectionMaterial != null)
        {
            Material mat = new Material(selectionMaterial);
            mat.color = selectedColor;
            indicatorRenderer.material = mat;
        }
    }
    
    void HideSelectionIndicator()
    {
        if (selectionIndicator != null)
        {
            selectionIndicator.SetActive(false);
        }
    }
    
    void AnimateSelectionIndicator()
    {
        if (!animateIndicator || selectionIndicator == null || !selectionIndicator.activeInHierarchy)
            return;
            
        // Simple pulsing animation
        float pulse = Mathf.Sin(Time.time * animationSpeed) * 0.1f + 1f;
        Vector3 baseScale = Vector3.one;
        
        if (currentSelectedUnit != null)
        {
            Bounds unitBounds = GetUnitBounds(currentSelectedUnit);
            float scale = Mathf.Max(unitBounds.size.x, unitBounds.size.z) * indicatorScale;
            baseScale = Vector3.one * scale;
        }
        
        selectionIndicator.transform.localScale = baseScale * pulse;
    }
    
    void UpdateUI(GameObject unit)
    {
        if (unitInfoPanel == null) return;
        
        unitInfoPanel.SetActive(true);
        
        UnitGridInfo unitInfo = unit.GetComponent<UnitGridInfo>();
        
        // Update unit name
        if (unitNameText != null)
        {
            unitNameText.text = unit.name.Replace("(Clone)", "");
        }
        
        // Update position info
        if (unitPositionText != null && unitInfo != null)
        {
            unitPositionText.text = $"Grid Position: ({unitInfo.gridPosition.x}, {unitInfo.gridPosition.y})";
        }
        
        // Update stats (placeholder for future stats system)
        if (unitStatsText != null)
        {
            unitStatsText.text = "HP: 100/100\nMove: 3\nAttack: 25";
        }
    }
    
    void HideUI()
    {
        if (unitInfoPanel != null)
        {
            unitInfoPanel.SetActive(false);
        }
    }
    
    void SetupUI()
    {
        if (deselectButton != null)
        {
            deselectButton.onClick.AddListener(DeselectUnit);
        }
    }
    
    void PlaySelectionSound()
    {
        if (audioSource != null && selectionSound != null)
        {
            audioSource.PlayOneShot(selectionSound);
        }
    }
    
    void PlayDeselectSound()
    {
        if (audioSource != null && deselectSound != null)
        {
            audioSource.PlayOneShot(deselectSound);
        }
    }
    
    Bounds GetUnitBounds(GameObject unit)
    {
        Renderer renderer = unit.GetComponent<Renderer>();
        if (renderer != null)
        {
            return renderer.bounds;
        }
        
        Collider collider = unit.GetComponent<Collider>();
        if (collider != null)
        {
            return collider.bounds;
        }
        
        // Default bounds
        return new Bounds(unit.transform.position, Vector3.one);
    }
    
    void CreateDefaultSelectionIndicator()
    {
        // Create a simple ring indicator
        selectionIndicatorPrefab = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        selectionIndicatorPrefab.name = "SelectionIndicator";
        
        // Make it a thin ring
        selectionIndicatorPrefab.transform.localScale = new Vector3(1f, 0.02f, 1f);
        
        // Remove collider
        Destroy(selectionIndicatorPrefab.GetComponent<Collider>());
        
        // Create material
        if (selectionMaterial == null)
        {
            selectionMaterial = new Material(Shader.Find("Standard"));
            selectionMaterial.SetFloat("_Mode", 3); // Transparent mode
            selectionMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            selectionMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            selectionMaterial.SetInt("_ZWrite", 0);
            selectionMaterial.DisableKeyword("_ALPHATEST_ON");
            selectionMaterial.EnableKeyword("_ALPHABLEND_ON");
            selectionMaterial.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            selectionMaterial.renderQueue = 3000;
            selectionMaterial.SetFloat("_Metallic", 0.5f);
            selectionMaterial.SetFloat("_Glossiness", 0.8f);
            
            Color color = selectedColor;
            color.a = 0.8f;
            selectionMaterial.color = color;
        }
        
        selectionIndicatorPrefab.GetComponent<Renderer>().material = selectionMaterial;
        selectionIndicatorPrefab.SetActive(false);
        DontDestroyOnLoad(selectionIndicatorPrefab);
    }
    
    bool IsPointerOverUI()
    {
        return UnityEngine.EventSystems.EventSystem.current != null && 
               UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject();
    }
    
    // Public getters
    public GameObject GetSelectedUnit()
    {
        return currentSelectedUnit;
    }
    
    public bool HasSelectedUnit()
    {
        return currentSelectedUnit != null;
    }
    
    public UnitGridInfo GetSelectedUnitInfo()
    {
        if (currentSelectedUnit != null)
        {
            return currentSelectedUnit.GetComponent<UnitGridInfo>();
        }
        return null;
    }
    
    // Debug helpers
    void OnDrawGizmos()
    {
        if (currentSelectedUnit != null)
        {
            Gizmos.color = selectedColor;
            Gizmos.DrawWireSphere(currentSelectedUnit.transform.position, 0.5f);
        }
    }
}