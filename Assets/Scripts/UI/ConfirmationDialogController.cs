using UnityEngine;
using UnityEngine.UIElements;
using System;

public class ConfirmationDialogController : MonoBehaviour
{
    private UIDocument uiDocument;
    private VisualElement overlay;
    private Label messageLabel;
    private Label unitsLabel;
    private Button confirmButton;
    private Button cancelButton;
    
    private Action onConfirm;
    private Action onCancel;
    private UnitPlacementManager placementManager;
    
    void Start()
    {
        uiDocument = GetComponent<UIDocument>();
        
        if (uiDocument == null)
        {
            Debug.LogError("UIDocument component not found on ConfirmationDialogController!");
            return;
        }
        
        // Set sorting order high to ensure it appears on top
        uiDocument.sortingOrder = 1000;
        
        // Get UI elements
        VisualElement root = uiDocument.rootVisualElement;
        overlay = root.Q<VisualElement>("Overlay");
        messageLabel = root.Q<Label>("MessageLabel");
        unitsLabel = root.Q<Label>("UnitsLabel");
        confirmButton = root.Q<Button>("ConfirmButton");
        cancelButton = root.Q<Button>("CancelButton");
        
        if (overlay == null)
        {
            Debug.LogError("Overlay element not found in confirmation dialog!");
            return;
        }
        
        if (confirmButton == null || cancelButton == null)
        {
            Debug.LogError($"Button(s) not found - Confirm: {confirmButton != null}, Cancel: {cancelButton != null}");
            return;
        }
        
        // Set up button handlers
        confirmButton.clicked += OnConfirmClicked;
        cancelButton.clicked += OnCancelClicked;
        
        Debug.Log("ConfirmationDialog button handlers registered successfully");
        
        // Set up overlay to block input
        overlay.pickingMode = PickingMode.Position;
        overlay.RegisterCallback<MouseDownEvent>(OnOverlayClicked);
        
        // Find placement manager to disable input when dialog is shown
        placementManager = FindFirstObjectByType<UnitPlacementManager>();
        
        // Hide dialog initially
        HideDialog();
        
        Debug.Log("ConfirmationDialogController initialized with sorting order: " + uiDocument.sortingOrder);
    }
    
    public void ShowDialog(string message, string unitsMessage, Action confirmCallback, Action cancelCallback = null)
    {
        onConfirm = confirmCallback;
        onCancel = cancelCallback;
        
        if (messageLabel != null)
        {
            messageLabel.text = message;
        }
        
        if (unitsLabel != null)
        {
            unitsLabel.text = unitsMessage;
        }
        
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.Flex;
        }
        
        // Temporarily disable placement manager input
        if (placementManager != null)
        {
            placementManager.enabled = false;
        }
    }
    
    public void HideDialog()
    {
        if (overlay != null)
        {
            overlay.style.display = DisplayStyle.None;
        }
        
        // Re-enable placement manager input
        if (placementManager != null)
        {
            placementManager.enabled = true;
        }
        
        onConfirm = null;
        onCancel = null;
    }
    
    private void OnConfirmClicked()
    {
        Debug.Log("Confirmation dialog: Confirm clicked");
        // Store callback before hiding dialog (which clears it)
        var callback = onConfirm;
        HideDialog();
        callback?.Invoke();
    }
    
    private void OnCancelClicked()
    {
        Debug.Log("Confirmation dialog: Cancel clicked");
        // Store callback before hiding dialog (which clears it)
        var callback = onCancel;
        HideDialog();
        callback?.Invoke();
    }
    
    private void OnOverlayClicked(MouseDownEvent evt)
    {
        // Block all input to elements behind the overlay
        evt.StopPropagation();
        Debug.Log("Confirmation dialog: Overlay clicked - blocking input");
    }
}