using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Camera Setup")]
    [SerializeField] private Vector3 offset = new Vector3(0, 10, -8);
    [SerializeField] private Vector3 lookAtOffset = Vector3.zero;
    
    [Header("Camera Movement")]
    [SerializeField] private float panSpeed = 5f;
    [SerializeField] private float mousePanSpeed = 2f;
    [SerializeField] private float edgePanSpeed = 3f;
    [SerializeField] private float edgePanBorder = 10f;
    
    [Header("Camera Zoom")]
    [SerializeField] private float zoomSpeed = 2f;
    [SerializeField] private float minZoom = 3f;
    [SerializeField] private float maxZoom = 15f;
    
    [Header("Camera Rotation")]
    [SerializeField] private float rotationSpeed = 45f; // Reduced for smoother rotation
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
    [SerializeField] private bool snapRotation = true; // Snap to 90-degree increments
    
    [Header("Camera Bounds")]
    [SerializeField] private Vector2 mapBounds = new Vector2(20f, 20f);
    [SerializeField] private bool constrainToBounds = true;
    [SerializeField] private bool autoDetectBounds = true;
    
    [Header("Input Settings")]
    [SerializeField] private bool enableKeyboardPan = true;
    [SerializeField] private bool enableMouseDragPan = true;
    [SerializeField] private bool enableEdgePan = true;
    [SerializeField] private bool enableZoom = true;
    [SerializeField] private bool enableRotation = true;
    [SerializeField] private KeyCode dragPanKey = KeyCode.Mouse2;
    
    // Private variables
    private Camera cam;
    private Vector3 targetPosition;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private Vector3 mapCenter;
    private float currentRotationY = 45f; // Start at 45 degrees for isometric view
    private float targetRotationY = 45f;
    private bool isRotating = false;
    
    void Start()
    {
        InitializeCamera();
        FindMapBounds();
        SetupInitialPosition();
    }
    
    void InitializeCamera()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("IsometricCameraController requires a Camera component!");
            enabled = false;
            return;
        }
        
        // Ensure orthographic projection for isometric view
        cam.orthographic = true;
        
        // Set initial rotation for isometric view
        transform.rotation = Quaternion.Euler(30f, currentRotationY, 0f);
    }
    
    void FindMapBounds()
    {
        if (!autoDetectBounds) return;
        
        // Find all ground objects
        GroundGridObject[] groundObjects = FindObjectsByType<GroundGridObject>(FindObjectsSortMode.None);
        
        if (groundObjects.Length == 0)
        {
            // Fallback: look for objects on Ground layer
            GameObject[] allObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
            Bounds combinedBounds = new Bounds();
            bool boundsFound = false;
            
            foreach (GameObject obj in allObjects)
            {
                if (obj.layer == LayerMask.NameToLayer("Ground"))
                {
                    Renderer renderer = obj.GetComponent<Renderer>();
                    if (renderer != null)
                    {
                        if (!boundsFound)
                        {
                            combinedBounds = renderer.bounds;
                            boundsFound = true;
                        }
                        else
                        {
                            combinedBounds.Encapsulate(renderer.bounds);
                        }
                    }
                }
            }
            
            if (boundsFound)
            {
                mapCenter = combinedBounds.center;
                mapBounds = new Vector2(combinedBounds.size.x * 1.2f, combinedBounds.size.z * 1.2f);
            }
        }
        else
        {
            // Use ground objects to determine bounds
            Bounds combinedBounds = new Bounds();
            bool boundsFound = false;
            
            foreach (GroundGridObject groundObj in groundObjects)
            {
                Renderer renderer = groundObj.GetComponent<Renderer>();
                if (renderer != null)
                {
                    if (!boundsFound)
                    {
                        combinedBounds = renderer.bounds;
                        boundsFound = true;
                    }
                    else
                    {
                        combinedBounds.Encapsulate(renderer.bounds);
                    }
                }
            }
            
            if (boundsFound)
            {
                mapCenter = combinedBounds.center;
                mapBounds = new Vector2(combinedBounds.size.x * 1.2f, combinedBounds.size.z * 1.2f);
            }
        }
        
        Debug.Log($"Map center: {mapCenter}, Map bounds: {mapBounds}");
    }
    
    void SetupInitialPosition()
    {
        // Position camera above and behind the map center
        Vector3 initialPos = mapCenter + offset;
        transform.position = initialPos;
        targetPosition = initialPos;
        
        // Look at the map center
        Vector3 lookAtTarget = mapCenter + lookAtOffset;
        transform.LookAt(lookAtTarget);
        
        // Adjust to maintain isometric angle
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.x = 30f; // Fixed isometric angle
        eulerAngles.z = 0f;  // No roll
        transform.eulerAngles = eulerAngles;
    }
    
    void Update()
    {
        HandleInput();
        UpdateCameraPosition();
        UpdateCameraRotation();
        
        if (constrainToBounds)
        {
            ApplyBounds();
        }
    }
    
    void HandleInput()
    {
        HandleKeyboardMovement();
        HandleMouseDrag();
        HandleEdgePanning();
        HandleZoom();
        HandleRotation();
    }
    
    void HandleKeyboardMovement()
    {
        if (!enableKeyboardPan) return;
        
        Vector3 moveDirection = Vector3.zero;
        
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDirection += Vector3.back;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDirection += Vector3.right;
        
        if (moveDirection != Vector3.zero)
        {
            // Transform movement relative to camera rotation (Y-axis only)
            Vector3 forward = new Vector3(0, 0, 1);
            Vector3 right = new Vector3(1, 0, 0);
            
            // Rotate directions based on current camera Y rotation
            forward = Quaternion.AngleAxis(currentRotationY, Vector3.up) * forward;
            right = Quaternion.AngleAxis(currentRotationY, Vector3.up) * right;
            
            Vector3 movement = (forward * moveDirection.z + right * moveDirection.x).normalized;
            targetPosition += movement * panSpeed * Time.deltaTime;
        }
    }
    
    void HandleMouseDrag()
    {
        if (!enableMouseDragPan) return;
        
        if (Input.GetKeyDown(dragPanKey))
        {
            isDragging = true;
            lastMousePosition = Input.mousePosition;
        }
        
        if (Input.GetKeyUp(dragPanKey))
        {
            isDragging = false;
        }
        
        if (isDragging)
        {
            Vector3 mouseDelta = Input.mousePosition - lastMousePosition;
            
            // Convert screen movement to world movement
            float sensitivity = mousePanSpeed * cam.orthographicSize / 5f;
            Vector3 worldDelta = new Vector3(-mouseDelta.x, 0, -mouseDelta.y) * sensitivity * 0.01f;
            
            // Rotate movement based on camera rotation
            worldDelta = Quaternion.AngleAxis(currentRotationY, Vector3.up) * worldDelta;
            
            targetPosition += worldDelta;
            lastMousePosition = Input.mousePosition;
        }
    }
    
    void HandleEdgePanning()
    {
        if (!enableEdgePan) return;
        
        Vector3 mousePos = Input.mousePosition;
        Vector3 moveDirection = Vector3.zero;
        
        if (mousePos.x < edgePanBorder)
            moveDirection += Vector3.left;
        if (mousePos.x > Screen.width - edgePanBorder)
            moveDirection += Vector3.right;
        if (mousePos.y < edgePanBorder)
            moveDirection += Vector3.back;
        if (mousePos.y > Screen.height - edgePanBorder)
            moveDirection += Vector3.forward;
        
        if (moveDirection != Vector3.zero)
        {
            // Transform movement relative to camera rotation
            Vector3 forward = new Vector3(0, 0, 1);
            Vector3 right = new Vector3(1, 0, 0);
            
            forward = Quaternion.AngleAxis(currentRotationY, Vector3.up) * forward;
            right = Quaternion.AngleAxis(currentRotationY, Vector3.up) * right;
            
            Vector3 movement = (forward * moveDirection.z + right * moveDirection.x).normalized;
            targetPosition += movement * edgePanSpeed * Time.deltaTime;
        }
    }
    
    void HandleZoom()
    {
        if (!enableZoom) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = cam.orthographicSize - scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
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
                // Snap to 90-degree increments
                float rotationAmount = rotateLeft ? -90f : 90f;
                targetRotationY = currentRotationY + rotationAmount;
                
                // Normalize to 0-360 range
                while (targetRotationY < 0) targetRotationY += 360f;
                while (targetRotationY >= 360) targetRotationY -= 360f;
                
                isRotating = true;
                StartCoroutine(SmoothRotation());
            }
            else
            {
                // Continuous rotation
                float rotationAmount = (rotateLeft ? -1f : 1f) * rotationSpeed * Time.deltaTime;
                currentRotationY += rotationAmount;
                targetRotationY = currentRotationY;
            }
        }
    }
    
    System.Collections.IEnumerator SmoothRotation()
    {
        float startRotation = currentRotationY;
        float duration = 0.5f; // Rotation duration
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t);
            
            currentRotationY = Mathf.LerpAngle(startRotation, targetRotationY, t);
            yield return null;
        }
        
        currentRotationY = targetRotationY;
        isRotating = false;
    }
    
    void UpdateCameraPosition()
    {
        // Smoothly move camera to target position
        float smoothSpeed = 8f;
        transform.position = Vector3.Lerp(transform.position, targetPosition, smoothSpeed * Time.deltaTime);
    }
    
    void UpdateCameraRotation()
    {
        // Apply rotation and maintain isometric view
        Quaternion targetRotation = Quaternion.Euler(30f, currentRotationY, 0f);
        transform.rotation = targetRotation;
    }
    
    void ApplyBounds()
    {
        // Keep camera within map bounds
        float halfWidth = mapBounds.x * 0.5f;
        float halfHeight = mapBounds.y * 0.5f;
        
        // Account for camera zoom when calculating bounds
        float zoomPadding = cam.orthographicSize * 0.5f;
        
        targetPosition.x = Mathf.Clamp(targetPosition.x, 
            mapCenter.x - halfWidth + zoomPadding, 
            mapCenter.x + halfWidth - zoomPadding);
        targetPosition.z = Mathf.Clamp(targetPosition.z, 
            mapCenter.z - halfHeight + zoomPadding, 
            mapCenter.z + halfHeight - zoomPadding);
    }
    
    // Public methods for external control
    public void FocusOnPosition(Vector3 worldPosition, float duration = 1f)
    {
        StopAllCoroutines();
        StartCoroutine(SmoothFocusTo(worldPosition, duration));
    }
    
    System.Collections.IEnumerator SmoothFocusTo(Vector3 targetWorldPos, float duration)
    {
        Vector3 startPos = targetPosition;
        Vector3 endPos = new Vector3(targetWorldPos.x, targetPosition.y, targetWorldPos.z);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            targetPosition = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }
        
        targetPosition = endPos;
    }
    
    public void SetMapBounds(Vector2 newBounds)
    {
        mapBounds = newBounds;
    }
    
    public void SetMapCenter(Vector3 newCenter)
    {
        mapCenter = newCenter;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw map bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(mapCenter, new Vector3(mapBounds.x, 1f, mapBounds.y));
        
        // Draw map center
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mapCenter, 0.5f);
        
        // Draw camera target position
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(targetPosition, 0.3f);
    }
}