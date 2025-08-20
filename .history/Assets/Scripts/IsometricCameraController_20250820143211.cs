using UnityEngine;

public class TacticsCameraController : MonoBehaviour
{
    [Header("Camera Position")]
    [SerializeField] private float height = 15f;
    [SerializeField] private float angle = 45f; // Isometric angle
    [SerializeField] private float tilt = 30f;   // Look-down angle
    
    [Header("Movement")]
    [SerializeField] private float panSpeed = 8f;
    [SerializeField] private float mousePanSpeed = 2f;
    [SerializeField] private float edgePanSpeed = 5f;
    [SerializeField] private float edgePanBorder = 50f;
    
    [Header("Zoom")]
    [SerializeField] private float zoomSpeed = 3f;
    [SerializeField] private float minZoom = 5f;
    [SerializeField] private float maxZoom = 20f;
    
    [Header("Rotation")]
    [SerializeField] private float rotationSpeed = 90f;
    [SerializeField] private KeyCode rotateLeftKey = KeyCode.Q;
    [SerializeField] private KeyCode rotateRightKey = KeyCode.E;
    [SerializeField] private bool snapRotation = true;
    
    [Header("Bounds")]
    [SerializeField] private bool useBounds = true;
    [SerializeField] private Vector2 mapSize = new Vector2(20f, 20f);
    [SerializeField] private Vector3 mapCenter = Vector3.zero;
    
    [Header("Input")]
    [SerializeField] private bool enableKeyboardPan = true;
    [SerializeField] private bool enableMouseDragPan = true;
    [SerializeField] private bool enableEdgePan = true;
    [SerializeField] private KeyCode dragKey = KeyCode.Mouse2;
    
    // Private variables
    private Camera cam;
    private Vector3 targetPosition;
    private float currentRotationY;
    private float targetRotationY;
    private bool isRotating = false;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    
    void Start()
    {
        InitializeCamera();
        SetupInitialPosition();
    }
    
    void InitializeCamera()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("TacticsCameraController requires a Camera component!");
            enabled = false;
            return;
        }
        
        // Force orthographic for tactics games
        cam.orthographic = true;
        
        // Auto-detect map center if not set
        if (mapCenter == Vector3.zero)
        {
            AutoDetectMapCenter();
        }
    }
    
    void AutoDetectMapCenter()
    {
        // Find ground objects to center on
        GameObject[] groundObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Bounds combinedBounds = new Bounds();
        bool boundsFound = false;
        
        foreach (GameObject obj in groundObjects)
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
            mapSize = new Vector2(combinedBounds.size.x * 1.5f, combinedBounds.size.z * 1.5f);
            Debug.Log($"Auto-detected map center: {mapCenter}, size: {mapSize}");
        }
    }
    
    void SetupInitialPosition()
    {
        // Set initial rotation angles
        currentRotationY = angle;
        targetRotationY = angle;
        
        // Position camera above map center
        UpdateCameraPosition();
        UpdateCameraRotation();
        
        // Set target position to current position (no auto-movement)
        targetPosition = transform.position;
    }
    
    void Update()
    {
        HandleInput();
        UpdateCamera();
        ApplyBounds();
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
        
        Vector3 movement = Vector3.zero;
        
        // Get input
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
            // Rotate movement based on camera rotation
            movement = Quaternion.AngleAxis(currentRotationY, Vector3.up) * movement;
            targetPosition += movement.normalized * panSpeed * Time.deltaTime;
        }
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
            
            // Convert to world movement
            float sensitivity = mousePanSpeed * cam.orthographicSize / 10f;
            Vector3 worldMovement = new Vector3(-mouseDelta.x, 0, -mouseDelta.y) * sensitivity * 0.01f;
            
            // Rotate movement based on camera rotation
            worldMovement = Quaternion.AngleAxis(currentRotationY, Vector3.up) * worldMovement;
            
            targetPosition += worldMovement;
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
            // Rotate movement based on camera rotation
            movement = Quaternion.AngleAxis(currentRotationY, Vector3.up) * movement;
            targetPosition += movement.normalized * edgePanSpeed * Time.deltaTime;
        }
    }
    
    void HandleZoom()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            float newSize = cam.orthographicSize - scroll * zoomSpeed;
            cam.orthographicSize = Mathf.Clamp(newSize, minZoom, maxZoom);
        }
    }
    
    void HandleRotation()
    {
        if (isRotating) return;
        
        bool rotateLeft = Input.GetKeyDown(rotateLeftKey);
        bool rotateRight = Input.GetKeyDown(rotateRightKey);
        
        if (rotateLeft || rotateRight)
        {
            if (snapRotation)
            {
                float rotationAmount = rotateLeft ? -90f : 90f;
                targetRotationY = currentRotationY + rotationAmount;
                
                // Normalize angle
                while (targetRotationY < 0) targetRotationY += 360f;
                while (targetRotationY >= 360) targetRotationY -= 360f;
                
                StartCoroutine(SmoothRotation());
            }
            else
            {
                float rotationAmount = (rotateLeft ? -1f : 1f) * rotationSpeed * Time.deltaTime;
                currentRotationY += rotationAmount;
                targetRotationY = currentRotationY;
            }
        }
    }
    
    System.Collections.IEnumerator SmoothRotation()
    {
        isRotating = true;
        float startRotation = currentRotationY;
        float duration = 0.5f;
        float elapsed = 0f;
        
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration);
            
            currentRotationY = Mathf.LerpAngle(startRotation, targetRotationY, t);
            yield return null;
        }
        
        currentRotationY = targetRotationY;
        isRotating = false;
    }
    
    void UpdateCamera()
    {
        // Smoothly move to target position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, targetPosition, 10f * Time.deltaTime);
        transform.position = smoothedPosition;
        
        // Update rotation
        UpdateCameraRotation();
    }
    
    void UpdateCameraPosition()
    {
        // Calculate position based on height and angles
        Vector3 offset = new Vector3(0, height, 0);
        
        // Apply rotation to get isometric offset
        offset = Quaternion.AngleAxis(currentRotationY, Vector3.up) * 
                 Quaternion.AngleAxis(-tilt, Vector3.right) * offset;
        
        Vector3 desiredPosition = mapCenter + offset;
        transform.position = desiredPosition;
    }
    
    void UpdateCameraRotation()
    {
        // Calculate look direction
        Vector3 lookDirection = (mapCenter - transform.position).normalized;
        
        // Apply rotation
        Quaternion targetRotation = Quaternion.LookRotation(lookDirection);
        
        // Override with our specific isometric angles
        Vector3 eulerAngles = targetRotation.eulerAngles;
        eulerAngles.x = tilt;
        eulerAngles.y = currentRotationY;
        eulerAngles.z = 0f;
        
        transform.rotation = Quaternion.Euler(eulerAngles);
    }
    
    void ApplyBounds()
    {
        if (!useBounds) return;
        
        float halfWidth = mapSize.x * 0.5f;
        float halfHeight = mapSize.y * 0.5f;
        
        // Add padding based on zoom level
        float padding = cam.orthographicSize * 0.5f;
        
        targetPosition.x = Mathf.Clamp(targetPosition.x, 
            mapCenter.x - halfWidth + padding, 
            mapCenter.x + halfWidth - padding);
        targetPosition.z = Mathf.Clamp(targetPosition.z, 
            mapCenter.z - halfHeight + padding, 
            mapCenter.z + halfHeight - padding);
    }
    
    // Public methods
    public void FocusOnPosition(Vector3 worldPosition)
    {
        targetPosition = new Vector3(worldPosition.x, targetPosition.y, worldPosition.z);
    }
    
    public void SetMapBounds(Vector3 center, Vector2 size)
    {
        mapCenter = center;
        mapSize = size;
    }
    
    public void ResetToMapCenter()
    {
        targetPosition = new Vector3(mapCenter.x, targetPosition.y, mapCenter.z);
    }
    
    // Gizmos for debugging
    void OnDrawGizmosSelected()
    {
        // Draw map bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(mapCenter, new Vector3(mapSize.x, 1f, mapSize.y));
        
        // Draw map center
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(mapCenter, 0.5f);
        
        // Draw camera target
        Gizmos.color = Color.blue;
        Gizmos.DrawSphere(targetPosition, 0.3f);
    }
}