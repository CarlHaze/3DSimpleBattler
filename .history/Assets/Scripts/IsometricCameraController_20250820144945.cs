using UnityEngine;

public class SimpleTacticsCameraController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float panSpeed = 10f;
    [SerializeField] private float mousePanSpeed = 2f;
    [SerializeField] private float edgePanSpeed = 8f;
    [SerializeField] private float edgePanBorder = 50f;
    
    [Header("Zoom (Height)")]
    [SerializeField] private float zoomSpeed = 5f;
    [SerializeField] private float minHeight = 5f;
    [SerializeField] private float maxHeight = 30f;
    
    [Header("Camera Tilt & Rotation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private float tiltSpeed = 45f;
    [SerializeField] private float minTilt = 0f;   // Looking straight down
    [SerializeField] private float maxTilt = 60f;  // Maximum angle
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
    [SerializeField] private KeyCode tiltUpKey = KeyCode.R;
    [SerializeField] private KeyCode tiltDownKey = KeyCode.F;
    [SerializeField] private KeyCode resetCameraKey = KeyCode.T;
    
    [Header("Rotation Settings")]
    [SerializeField] private bool snapRotation = true;
    [SerializeField] private float snapAngle = 45f; // 45 or 90 degrees
    
    [Header("Vertical Panning")]
    [SerializeField] private bool enableVerticalPan = true;
    [SerializeField] private float verticalPanSpeed = 8f;
    [SerializeField] private KeyCode panUpKey = KeyCode.Space;
    [SerializeField] private KeyCode panDownKey = KeyCode.LeftControl;
    
    [Header("Input Settings")]
    [SerializeField] private bool enableKeyboardPan = true;
    [SerializeField] private bool enableMouseDragPan = true;
    [SerializeField] private bool enableEdgePan = true;
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private bool enableTilt = true;
    [SerializeField] private KeyCode dragKey = KeyCode.Mouse2;
    
    [Header("Bounds (Optional)")]
    [SerializeField] private bool useBounds = false;
    [SerializeField] private Vector2 mapBounds = new Vector2(20f, 20f);
    [SerializeField] private Vector3 mapCenter = Vector3.zero;
    
    // Private variables
    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private float currentYRotation = 0f;
    private float currentXRotation = 90f; // Start looking straight down
    private float targetYRotation = 0f;
    private bool isRotating = false;
    
    void Start()
    {
        InitializeCamera();
    }
    
    void InitializeCamera()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("SimpleTacticsCameraController requires a Camera component!");
            enabled = false;
            return;
        }
        
        // Keep it in perspective mode (don't force orthographic)
        cam.orthographic = false;
        
        // Set initial rotation to look straight down
        transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
        
        Debug.Log("Camera initialized in perspective mode, looking straight down");
    }
    
    void Update()
    {
        HandleMovement();
        HandleVerticalPanning();
        HandleZoom();
        HandleRotation();
        HandleTilt();
        HandleReset();
        
        if (useBounds)
        {
            ApplyBounds();
        }
        
        // Update rotation smoothly if snapping
        if (snapRotation && isRotating)
        {
            UpdateSmoothRotation();
        }
    }
    
    void HandleMovement()
    {
        Vector3 movement = Vector3.zero;
        
        // Keyboard movement
        if (enableKeyboardPan)
        {
            if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
                movement.z += 1f;
            if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
                movement.z -= 1f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
                movement.x -= 1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
                movement.x += 1f;
            
            if (movement != Vector3.zero)
            {
                // Transform movement relative to camera's Y rotation
                movement = Quaternion.AngleAxis(currentYRotation, Vector3.up) * movement;
                transform.position += movement.normalized * panSpeed * Time.deltaTime;
            }
        }
        
        // Mouse drag
        HandleMouseDrag();
        
        // Edge panning
        HandleEdgePanning();
    }
    
    void HandleMouseDrag()
    {
        if (!enableMouseDragPan) return;
        
        if (Input.GetKeyDown(dragKey))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        
        if (Input.GetKeyUp(dragKey))
        {
            isDragging = false;
        }
        
        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // Convert screen movement to world movement
            float sensitivity = mousePanSpeed * transform.position.y * 0.001f;
            Vector3 worldMovement = new Vector3(-mouseDelta.x, 0, -mouseDelta.y) * sensitivity;
            
            // Transform relative to camera rotation
            worldMovement = Quaternion.AngleAxis(currentYRotation, Vector3.up) * worldMovement;
            
            transform.position += worldMovement;
            lastMousePosition = Input.mousePosition;
        }
    }
    
    void HandleEdgePanning()
    {
        if (!enableEdgePan) return;
        
        Vector3 mousePos = Input.mousePosition;
        Vector3 movement = Vector3.zero;
        
        // Check screen edges
        if (mousePos.x < edgePanBorder)
            movement.x -= 1f;
        if (mousePos.x > Screen.width - edgePanBorder)
            movement.x += 1f;
        if (mousePos.y < edgePanBorder)
            movement.z -= 1f;
        if (mousePos.y > Screen.height - edgePanBorder)
            movement.z += 1f;
        
        if (movement != Vector3.zero)
        {
            // Transform movement relative to camera rotation
            movement = Quaternion.AngleAxis(currentYRotation, Vector3.up) * movement;
            transform.position += movement.normalized * edgePanSpeed * Time.deltaTime;
        }
    }
    
    void HandleZoom()
    {
        if (!enableZoom) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            // Move camera up/down for zoom (since we're looking down)
            Vector3 pos = transform.position;
            pos.y -= scroll * zoomSpeed;
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
            transform.position = pos;
        }
    }
    
    void HandleRotation()
    {
        if (!enableRotation || isRotating) return;
        
        bool rotateLeft = Input.GetKeyDown(rotateLeftKey);
        bool rotateRight = Input.GetKeyDown(rotateRightKey);
        
        if (rotateLeft || rotateRight)
        {
            if (snapRotation)
            {
                // Snap rotation to set increments
                float rotationAmount = rotateLeft ? -snapAngle : snapAngle;
                targetYRotation = currentYRotation + rotationAmount;
                
                // Normalize to 0-360 range
                while (targetYRotation < 0f) targetYRotation += 360f;
                while (targetYRotation >= 360f) targetYRotation -= 360f;
                
                isRotating = true;
                
                Debug.Log($"Rotating to {targetYRotation} degrees");
            }
            else
            {
                // Continuous rotation
                float rotationInput = rotateLeft ? -1f : 1f;
                currentYRotation += rotationInput * rotationSpeed * Time.deltaTime;
                
                // Keep rotation in 0-360 range
                if (currentYRotation < 0f) currentYRotation += 360f;
                if (currentYRotation >= 360f) currentYRotation -= 360f;
                
                // Apply rotation immediately
                transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
            }
        }
    }
    
    void HandleTilt()
    {
        if (!enableTilt) return;
        
        float tiltInput = 0f;
        
        if (Input.GetKey(tiltUpKey))
            tiltInput = -1f; // Tilt up (look more forward)
        else if (Input.GetKey(tiltDownKey))
            tiltInput = 1f;  // Tilt down (look more down)
        
        if (Mathf.Abs(tiltInput) > 0.01f)
        {
            currentXRotation += tiltInput * tiltSpeed * Time.deltaTime;
            currentXRotation = Mathf.Clamp(currentXRotation, 90f - maxTilt, 90f - minTilt);
            
            // Apply rotation
            transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
        }
    }
    
    void ApplyBounds()
    {
        Vector3 pos = transform.position;
        
        pos.x = Mathf.Clamp(pos.x, 
            mapCenter.x - mapBounds.x * 0.5f, 
            mapCenter.x + mapBounds.x * 0.5f);
        pos.z = Mathf.Clamp(pos.z, 
            mapCenter.z - mapBounds.y * 0.5f, 
            mapCenter.z + mapBounds.y * 0.5f);
        
        transform.position = pos;
    }
    
    void HandleVerticalPanning()
    {
        if (!enableVerticalPan) return;
        
        float verticalInput = 0f;
        
        if (Input.GetKey(panUpKey))
            verticalInput = 1f;
        else if (Input.GetKey(panDownKey))
            verticalInput = -1f;
        
        if (Mathf.Abs(verticalInput) > 0.01f)
        {
            Vector3 pos = transform.position;
            pos.y += verticalInput * verticalPanSpeed * Time.deltaTime;
            pos.y = Mathf.Clamp(pos.y, minHeight, maxHeight);
            transform.position = pos;
        }
    }
    
    void HandleReset()
    {
        if (Input.GetKeyDown(resetCameraKey))
        {
            ResetToTopDown();
            Debug.Log("Camera reset to top-down view");
        }
    }
    
    void UpdateSmoothRotation()
    {
        float rotationSpeed = 180f; // degrees per second for smooth rotation
        
        currentYRotation = Mathf.MoveTowardsAngle(currentYRotation, targetYRotation, rotationSpeed * Time.deltaTime);
        
        // Apply the rotation
        transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
        
        // Check if we've reached the target
        if (Mathf.Approximately(currentYRotation, targetYRotation))
        {
            currentYRotation = targetYRotation;
            isRotating = false;
        }
    }
    
    // Public methods
    public void FocusOnPosition(Vector3 worldPosition, float duration = 1f)
    {
        StartCoroutine(SmoothMoveTo(worldPosition, duration));
    }
    
    System.Collections.IEnumerator SmoothMoveTo(Vector3 targetPosition, float duration)
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        transform.position = endPos;
    }
    
    public void ResetToTopDown()
    {
        currentXRotation = 90f;
        currentYRotation = 0f;
        targetYRotation = 0f;
        isRotating = false;
        transform.rotation = Quaternion.Euler(currentXRotation, currentYRotation, 0f);
    }
    
    // Debug info
    void OnDrawGizmosSelected()
    {
        if (useBounds)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(mapCenter, new Vector3(mapBounds.x, 1f, mapBounds.y));
        }
    }
}