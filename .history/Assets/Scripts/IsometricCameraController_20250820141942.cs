using UnityEngine;

public class IsometricCameraController : MonoBehaviour
{
    [Header("Camera Movement")]
    public float panSpeed = 5f;
    public float mousePanSpeed = 2f;
    public float edgePanSpeed = 3f;
    public float edgePanBorder = 10f;
    
    [Header("Camera Zoom")]
    public float zoomSpeed = 5f;
    public float minZoom = 3f;
    public float maxZoom = 15f;
    
    [Header("Camera Rotation")]
    public float rotationSpeed = 90f; // degrees per second
    public KeyCode rotateLeftKey = KeyCode.Q;
    public KeyCode rotateRightKey = KeyCode.E;
    
    [Header("Camera Bounds")]
    public Vector2 mapBounds = new Vector2(20f, 20f);
    public bool constrainToBounds = true;
    
    [Header("Input Settings")]
    public bool enableKeyboardPan = true;
    public bool enableMouseDragPan = true;
    public bool enableEdgePan = true;
    public bool enableZoom = true;
    public bool enableRotation = true;
    
    [Header("Mouse Settings")]
    public KeyCode dragPanKey = KeyCode.Mouse2; // Middle mouse button
    
    private Camera cam;
    private Vector3 lastMousePosition;
    private bool isDragging = false;
    private Vector3 pivotPoint;
    private float currentZoom;
    
    void Start()
    {
        cam = GetComponent<Camera>();
        if (cam == null)
        {
            Debug.LogError("IsometricCameraController requires a Camera component!");
            enabled = false;
            return;
        }
        
        // Set up isometric view
        SetupIsometricView();
        
        // Initialize zoom
        currentZoom = cam.orthographicSize;
        
        // Find map center as pivot point
        FindMapCenter();
    }
    
    void SetupIsometricView()
    {
        // Set camera to orthographic for true isometric view
        cam.orthographic = true;
        
        // Set typical isometric angle (30 degrees from horizontal)
        transform.rotation = Quaternion.Euler(30f, 45f, 0f);
    }
    
    void FindMapCenter()
    {
        // Find all ground objects to determine map bounds
        GameObject[] groundObjects = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        Bounds combinedBounds = new Bounds();
        bool boundsInitialized = false;
        
        foreach (GameObject obj in groundObjects)
        {
            if (obj.layer == LayerMask.NameToLayer("Ground") && obj.GetComponent<Renderer>() != null)
            {
                Renderer renderer = obj.GetComponent<Renderer>();
                if (!boundsInitialized)
                {
                    combinedBounds = renderer.bounds;
                    boundsInitialized = true;
                }
                else
                {
                    combinedBounds.Encapsulate(renderer.bounds);
                }
            }
        }
        
        if (boundsInitialized)
        {
            pivotPoint = combinedBounds.center;
            mapBounds = new Vector2(combinedBounds.size.x, combinedBounds.size.z);
        }
        else
        {
            pivotPoint = Vector3.zero;
        }
        
        Debug.Log($"Map center found at: {pivotPoint}, bounds: {mapBounds}");
    }
    
    void Update()
    {
        HandleKeyboardInput();
        HandleMouseInput();
        HandleEdgePanning();
        HandleZoom();
        HandleRotation();
        
        // Apply camera bounds
        if (constrainToBounds)
        {
            ApplyBounds();
        }
    }
    
    void HandleKeyboardInput()
    {
        if (!enableKeyboardPan) return;
        
        Vector3 moveDirection = Vector3.zero;
        
        // WASD and Arrow Keys
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow))
            moveDirection += Vector3.forward;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow))
            moveDirection += Vector3.back;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
            moveDirection += Vector3.left;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
            moveDirection += Vector3.right;
        
        // Transform direction relative to camera rotation
        moveDirection = transform.TransformDirection(moveDirection);
        moveDirection.y = 0; // Keep movement horizontal
        
        transform.position += moveDirection.normalized * panSpeed * Time.deltaTime;
    }
    
    void HandleMouseInput()
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
            
            // Convert screen space movement to world space
            Vector3 worldDelta = cam.ScreenToWorldPoint(new Vector3(mouseDelta.x, mouseDelta.y, cam.nearClipPlane));
            Vector3 cameraWorldDelta = cam.ScreenToWorldPoint(Vector3.zero);
            
            Vector3 move = (cameraWorldDelta - worldDelta) * mousePanSpeed;
            move.y = 0; // Keep movement horizontal
            
            transform.position += move;
            lastMousePosition = Input.mousePosition;
        }
    }
    
    void HandleEdgePanning()
    {
        if (!enableEdgePan) return;
        
        Vector3 mousePos = Input.mousePosition;
        Vector3 moveDirection = Vector3.zero;
        
        // Check screen edges
        if (mousePos.x < edgePanBorder)
            moveDirection += Vector3.left;
        if (mousePos.x > Screen.width - edgePanBorder)
            moveDirection += Vector3.right;
        if (mousePos.y < edgePanBorder)
            moveDirection += Vector3.back;
        if (mousePos.y > Screen.height - edgePanBorder)
            moveDirection += Vector3.forward;
        
        // Transform direction relative to camera rotation
        moveDirection = transform.TransformDirection(moveDirection);
        moveDirection.y = 0; // Keep movement horizontal
        
        transform.position += moveDirection.normalized * edgePanSpeed * Time.deltaTime;
    }
    
    void HandleZoom()
    {
        if (!enableZoom) return;
        
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        
        if (Mathf.Abs(scroll) > 0.01f)
        {
            currentZoom -= scroll * zoomSpeed;
            currentZoom = Mathf.Clamp(currentZoom, minZoom, maxZoom);
            cam.orthographicSize = currentZoom;
        }
    }
    
    void HandleRotation()
    {
        if (!enableRotation) return;
        
        float rotationInput = 0f;
        
        if (Input.GetKey(rotateLeftKey))
            rotationInput = -1f;
        else if (Input.GetKey(rotateRightKey))
            rotationInput = 1f;
        
        if (Mathf.Abs(rotationInput) > 0.01f)
        {
            // Rotate around the pivot point (map center)
            RotateAroundPivot(rotationInput * rotationSpeed * Time.deltaTime);
        }
    }
    
    void RotateAroundPivot(float angleY)
    {
        // Store current distance from pivot
        Vector3 directionToPivot = pivotPoint - transform.position;
        float distance = directionToPivot.magnitude;
        
        // Rotate the camera around the pivot point
        transform.RotateAround(pivotPoint, Vector3.up, angleY);
        
        // Maintain the same distance and relative height
        Vector3 newDirection = (transform.position - pivotPoint).normalized;
        transform.position = pivotPoint + newDirection * distance;
        
        // Look towards the pivot point while maintaining isometric angle
        Vector3 lookDirection = (pivotPoint - transform.position).normalized;
        float currentXRotation = transform.eulerAngles.x;
        transform.LookAt(pivotPoint);
        
        // Restore the isometric X rotation
        Vector3 eulerAngles = transform.eulerAngles;
        eulerAngles.x = currentXRotation;
        transform.eulerAngles = eulerAngles;
    }
    
    void ApplyBounds()
    {
        Vector3 pos = transform.position;
        
        // Calculate camera bounds based on orthographic size and screen aspect
        float verticalSize = cam.orthographicSize;
        float horizontalSize = verticalSize * cam.aspect;
        
        // Apply bounds
        pos.x = Mathf.Clamp(pos.x, 
            pivotPoint.x - mapBounds.x/2 + horizontalSize, 
            pivotPoint.x + mapBounds.x/2 - horizontalSize);
        pos.z = Mathf.Clamp(pos.z, 
            pivotPoint.z - mapBounds.y/2 + verticalSize, 
            pivotPoint.z + mapBounds.y/2 - verticalSize);
        
        transform.position = pos;
    }
    
    // Public methods for external control
    public void SetPivotPoint(Vector3 newPivot)
    {
        pivotPoint = newPivot;
    }
    
    public void FocusOnPosition(Vector3 targetPosition, float duration = 1f)
    {
        StartCoroutine(SmoothMoveToPosition(targetPosition, duration));
    }
    
    public void SetZoom(float zoom)
    {
        currentZoom = Mathf.Clamp(zoom, minZoom, maxZoom);
        cam.orthographicSize = currentZoom;
    }
    
    System.Collections.IEnumerator SmoothMoveToPosition(Vector3 targetPosition, float duration)
    {
        Vector3 startPosition = transform.position;
        Vector3 endPosition = new Vector3(targetPosition.x, transform.position.y, targetPosition.z);
        
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            t = Mathf.SmoothStep(0f, 1f, t); // Smooth easing
            
            transform.position = Vector3.Lerp(startPosition, endPosition, t);
            yield return null;
        }
        
        transform.position = endPosition;
    }
    
    void OnDrawGizmosSelected()
    {
        // Draw map bounds
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(pivotPoint, new Vector3(mapBounds.x, 1f, mapBounds.y));
        
        // Draw pivot point
        Gizmos.color = Color.red;
        Gizmos.DrawSphere(pivotPoint, 0.5f);
    }
}