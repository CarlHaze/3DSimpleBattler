using UnityEngine;
 
public class FaceCamera : MonoBehaviour
{
    private Camera _mainCamera;
    private Vector3 _offsetFromParent;
 
    private void Start()
    {
        _mainCamera = Camera.main;
 
        // Calculate the initial offset above the cube's position
        _offsetFromParent = transform.localPosition;
    }
 
    private void LateUpdate()
    {
        // Maintain the position offset above the parent
        transform.position = transform.parent.position + _offsetFromParent;
 
        // Make the canvas face the camera
        Vector3 directionToCamera = _mainCamera.transform.position - transform.position;
        directionToCamera.y = 0;  // Keep the text upright
 
        // Apply rotation to face the camera
        if (directionToCamera == Vector3.zero) return;
        Quaternion targetRotation = Quaternion.LookRotation(-directionToCamera, Vector3.up);
        transform.rotation = targetRotation;
    }
}
