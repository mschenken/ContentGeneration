// This sample code demonstrates how to create geometry "on demand" based on camera motion.

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMotion : MonoBehaviour 
{
    public float translateFactor = 0.3f;
    public float rotateFactor = 5.0f;

    private Camera cameraComponent;

    void Start()
    {
        cameraComponent = GetComponent<Camera>();
        if (cameraComponent == null)
        {
            Debug.LogError("CameraMotion script must be attached to a Camera object.");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        float dx = Input.GetAxis("Horizontal");
        float dz = Input.GetAxis("Vertical");

        transform.Translate(0, 0, dz * translateFactor);
        transform.Rotate(0, dx * rotateFactor, 0);
    }
}