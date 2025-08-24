using UnityEngine;

public class HealthBarFaceCamera : MonoBehaviour
{
    private Camera _mainCamera;

    private void Start()
    {
        _mainCamera = Camera.main;
        
        if (_mainCamera == null)
        {
            Debug.LogWarning("Main camera not found! Health bar may not face camera correctly.");
        }
    }

    private void LateUpdate()
    {
        if (_mainCamera == null) return;

        // Make the health bar face the camera
        Vector3 directionToCamera = _mainCamera.transform.position - transform.position;
        directionToCamera.y = 0;  // Keep the health bar upright

        // Apply rotation to face the camera
        if (directionToCamera == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
        transform.rotation = targetRotation;
    }
}