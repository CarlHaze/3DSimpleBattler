using UnityEngine;
using UnityEngine.UIElements;

public class HealthBarController : MonoBehaviour
{
    private UIDocument uiDocument;
    private ProgressBar healthProgressBar;
    private Character character;
    
    // Cache previous values to avoid unnecessary updates
    private int lastCurrentHP = -1;
    private int lastMaxHP = -1;
    
    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        character = GetComponentInParent<Character>();
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on HealthBarController!");
            return;
        }
        
        if (character == null)
        {
            Debug.LogError("Character component not found in parent!");
            return;
        }
        
        SetupUI();
        UpdateHealthBar();
    }
    
    private void SetupUI()
    {
        var root = uiDocument.rootVisualElement;
        healthProgressBar = root.Q<ProgressBar>("HealthProgressBar");
        
        if (healthProgressBar == null)
        {
            Debug.LogError("HealthProgressBar not found in UI!");
        }
    }
    
    public void UpdateHealthBar()
    {
        if (healthProgressBar == null || character == null || character.Stats == null)
            return;
            
        var stats = character.Stats;
        int currentHP = stats.CurrentHP;
        int maxHP = stats.MaxHP;
        
        // Only update if values have changed
        if (currentHP == lastCurrentHP && maxHP == lastMaxHP)
            return;
            
        lastCurrentHP = currentHP;
        lastMaxHP = maxHP;
        
        // Update the progress bar value (0-100 scale)
        float healthPercentage = maxHP > 0 ? (float)currentHP / maxHP * 100f : 0f;
        healthProgressBar.value = healthPercentage;
        
        // Update the title to show current/max health
        healthProgressBar.title = $"{currentHP}/{maxHP}";
    }
    
    void Update()
    {
        // Update health bar only when necessary (cached values prevent unnecessary UI updates)
        UpdateHealthBar();
    }
}