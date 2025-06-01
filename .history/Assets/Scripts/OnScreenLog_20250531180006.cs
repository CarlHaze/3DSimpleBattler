using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class OnScreenLog : MonoBehaviour
{
    [Header("Log Settings")]
    public int maxLogEntries = 10;
    public float messageDuration = 3f;
    public bool autoCreateUI = true;
    
    [Header("UI References")]
    public Transform logContainer;
    public GameObject logEntryPrefab;
    
    [Header("Colors")]
    public Color errorColor = Color.red;
    public Color warningColor = Color.yellow;
    public Color infoColor = Color.white;
    public Color successColor = Color.green;
    
    private List<LogEntry> logEntries = new List<LogEntry>();
    private static OnScreenLog instance;
    
    // Static access for easy logging from anywhere
    public static OnScreenLog Instance => instance;
    
    void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
    
    void Start()
    {
        if (autoCreateUI && logContainer == null)
        {
            CreateLogUI();
        }
    }
    
    void CreateLogUI()
    {
        // Find or create Canvas
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasGO = new GameObject("LogCanvas");
            canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // Make sure it's on top
            canvasGO.AddComponent<CanvasScaler>();
            canvasGO.AddComponent<GraphicRaycaster>();
        }
        
        // Create log container
        GameObject containerGO = new GameObject("LogContainer");
        containerGO.transform.SetParent(canvas.transform, false);
        
        // Add VerticalLayoutGroup for automatic spacing
        VerticalLayoutGroup layoutGroup = containerGO.AddComponent<VerticalLayoutGroup>();
        layoutGroup.childAlignment = TextAnchor.LowerLeft;
        layoutGroup.spacing = 5f;
        layoutGroup.childControlHeight = false;
        layoutGroup.childControlWidth = false;
        layoutGroup.childForceExpandHeight = false;
        layoutGroup.childForceExpandWidth = false;
        
        // Position container on the left side
        RectTransform containerRect = containerGO.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0, 0);
        containerRect.anchorMax = new Vector2(0, 1);
        containerRect.pivot = new Vector2(0, 0);
        containerRect.anchoredPosition = new Vector2(20, 20);
        containerRect.sizeDelta = new Vector2(400, -40);
        
        logContainer = containerGO.transform;
        
        // Create simple log entry prefab
        CreateLogEntryPrefab();
        
        Debug.Log("OnScreenLog UI created successfully!");
    }
    
    void CreateLogEntryPrefab()
    {
        GameObject entryGO = new GameObject("LogEntry");
        
        // Add background image
        Image background = entryGO.AddComponent<Image>();
        background.color = new Color(0, 0, 0, 0.7f);
        
        // Setup RectTransform
        RectTransform entryRect = entryGO.GetComponent<RectTransform>();
        entryRect.sizeDelta = new Vector2(400, 30);
        
        // Add text component
        GameObject textGO = new GameObject("Text");
        textGO.transform.SetParent(entryGO.transform, false);
        
        TextMeshProUGUI text = textGO.AddComponent<TextMeshProUGUI>();
        text.text = "Log Entry";
        text.fontSize = 14;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Left;
        
        RectTransform textRect = textGO.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 2);
        textRect.offsetMax = new Vector2(-10, -2);
        
        // Add LogEntryComponent
        LogEntryComponent entryComponent = entryGO.AddComponent<LogEntryComponent>();
        entryComponent.textComponent = text;
        entryComponent.backgroundImage = background;
        
        logEntryPrefab = entryGO;
        logEntryPrefab.SetActive(false);
    }
    
    // Static logging methods for easy access
    public static void LogError(string message)
    {
        if (Instance != null)
            Instance.AddLogEntry(message, LogType.Error);
    }
    
    public static void LogWarning(string message)
    {
        if (Instance != null)
            Instance.AddLogEntry(message, LogType.Warning);
    }
    
    public static void LogInfo(string message)
    {
        if (Instance != null)
            Instance.AddLogEntry(message, LogType.Info);
    }
    
    public static void LogSuccess(string message)
    {
        if (Instance != null)
            Instance.AddLogEntry(message, LogType.Success);
    }
    
    public void AddLogEntry(string message, LogType logType)
    {
        if (logContainer == null || logEntryPrefab == null) return;
        
        // Create new log entry
        GameObject newEntry = Instantiate(logEntryPrefab, logContainer);
        newEntry.SetActive(true);
        
        LogEntryComponent entryComponent = newEntry.GetComponent<LogEntryComponent>();
        entryComponent.textComponent.text = message;
        
        // Set color based on log type
        Color textColor = logType switch
        {
            LogType.Error => errorColor,
            LogType.Warning => warningColor,
            LogType.Success => successColor,
            _ => infoColor
        };
        entryComponent.textComponent.color = textColor;
        
        // Create log entry data
        LogEntry logEntry = new LogEntry
        {
            gameObject = newEntry,
            timeCreated = Time.time,
            duration = messageDuration
        };
        
        logEntries.Add(logEntry);
        
        // Remove oldest entries if we exceed max count
        while (logEntries.Count > maxLogEntries)
        {
            RemoveLogEntry(0);
        }
        
        // Start fade out coroutine
        StartCoroutine(FadeOutEntry(logEntry));
    }
    
    System.Collections.IEnumerator FadeOutEntry(LogEntry entry)
    {
        yield return new WaitForSeconds(entry.duration - 1f); // Start fading 1 second before removal
        
        if (entry.gameObject != null)
        {
            LogEntryComponent component = entry.gameObject.GetComponent<LogEntryComponent>();
            float fadeTime = 1f;
            float startTime = Time.time;
            
            Color originalTextColor = component.textComponent.color;
            Color originalBgColor = component.backgroundImage.color;
            
            while (Time.time - startTime < fadeTime)
            {
                float alpha = 1f - ((Time.time - startTime) / fadeTime);
                
                Color textColor = originalTextColor;
                textColor.a = alpha;
                component.textComponent.color = textColor;
                
                Color bgColor = originalBgColor;
                bgColor.a = alpha * 0.7f; // Background was 0.7 alpha originally
                component.backgroundImage.color = bgColor;
                
                yield return null;
            }
            
            // Remove the entry
            if (logEntries.Contains(entry))
            {
                logEntries.Remove(entry);
            }
            
            if (entry.gameObject != null)
            {
                Destroy(entry.gameObject);
            }
        }
    }
    
    void RemoveLogEntry(int index)
    {
        if (index >= 0 && index < logEntries.Count)
        {
            LogEntry entry = logEntries[index];
            logEntries.RemoveAt(index);
            
            if (entry.gameObject != null)
            {
                Destroy(entry.gameObject);
            }
        }
    }
    
    void Update()
    {
        // Clean up any null entries
        for (int i = logEntries.Count - 1; i >= 0; i--)
        {
            if (logEntries[i].gameObject == null)
            {
                logEntries.RemoveAt(i);
            }
        }
    }
}

public enum LogType
{
    Info,
    Warning,
    Error,
    Success
}

[System.Serializable]
public class LogEntry
{
    public GameObject gameObject;
    public float timeCreated;
    public float duration;
}

public class LogEntryComponent : MonoBehaviour
{
    public TextMeshProUGUI textComponent;
    public Image backgroundImage;
}