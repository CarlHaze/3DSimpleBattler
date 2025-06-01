using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class SimpleMessageLog : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI logText;
    public ScrollRect scrollRect; // Add this reference
    
    [Header("Settings")]
    public int maxLines = 10;
    public bool showTimestamps = false;
    public bool autoScrollToBottom = true;
    
    private Queue<string> messages = new Queue<string>();
    private static SimpleMessageLog instance;
    
    public static SimpleMessageLog Instance => instance;
    
    void Awake()
    {
        instance = this;
    }
    
    void Start()
    {
        if (logText == null)
        {
            Debug.LogError("SimpleMessageLog: No logText assigned! Please assign a TextMeshPro component.");
        }
        
        if (scrollRect == null)
        {
            // Try to find ScrollRect automatically
            scrollRect = GetComponentInParent<ScrollRect>();
            if (scrollRect == null)
            {
                scrollRect = FindFirstObjectByType<ScrollRect>();
            }
        }
    }
    
    public static void Log(string message)
    {
        if (Instance != null)
            Instance.AddMessage(message);
    }
    
    public void AddMessage(string message)
    {
        if (logText == null) return;
        
        string finalMessage = message;
        if (showTimestamps)
        {
            finalMessage = $"[{Time.time:F1}s] {message}";
        }
        
        messages.Enqueue(finalMessage);
        
        // Remove old messages if we exceed max lines
        while (messages.Count > maxLines)
        {
            messages.Dequeue();
        }
        
        // Update the text display
        UpdateDisplay();
        
        // Auto scroll to bottom
        if (autoScrollToBottom && scrollRect != null)
        {
            ScrollToBottom();
        }
    }
    
    void UpdateDisplay()
    {
        if (logText == null) return;
        
        logText.text = string.Join("\n", messages);
    }
    
    void ScrollToBottom()
    {
        // Force rebuild the layout first
        Canvas.ForceUpdateCanvases();
        
        // Set scroll position to bottom
        scrollRect.verticalNormalizedPosition = 0f;
    }
    
    public void ClearLog()
    {
        messages.Clear();
        UpdateDisplay();
    }
}