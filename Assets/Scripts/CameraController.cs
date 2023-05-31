using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    public Transform target; // The target to follow (your player's Transform)
    public Vector3 offset; // The offset to maintain the distance from the target
    public float smoothSpeed = 5f; // The speed at which the camera will follow the target
    
    private void LateUpdate()
    {
        // Calculate the desired position
        Vector3 desiredPosition = target.position + offset;

        // Smoothly interpolate between the current position and the desired position
        Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed * Time.deltaTime);

        // Update the camera's position
        transform.position = smoothedPosition;
    }

}
