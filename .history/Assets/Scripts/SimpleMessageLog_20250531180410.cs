using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class SimpleMessageLog : MonoBehaviour
{
    [Header("UI Reference")]
    public TextMeshProUGUI logText;
    
    [Header("Settings")]
    public int maxLines = 10;
    public bool showTimestamps = false;
    
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
    }
    
    void UpdateDisplay()
    {
        if (logText == null) return;
        
        logText.text = string.Join("\n", messages);
    }
    
    public void ClearLog()
    {
        messages.Clear();
        UpdateDisplay();
    }
}