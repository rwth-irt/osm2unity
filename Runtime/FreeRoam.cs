using System;
using UnityEngine;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

/// Keys:
///	wasd / arrows	- movement
///	hold shift		- enable fast movement mode
///	right mouse  	- enable free look

public class FreeRoam : MonoBehaviour
{
    /// Max speed of movement.
    public float maxSpeed = 10f;

    /// Speed of camera movement when shift is held down,
    public float fastMovementSpeed = 50f;

    /// Sensitivity for free look.
    public float freeLookSensitivity = 3f;

    /// Rate at which speed increases when pressing W.
    public float accelerationRate = 20f;

    /// Rate at which speed decreases when braking (S key) or releasing W.
    public float decelerationRate = 20f;

    /// <summary>
    /// Set to true when free looking (on right mouse button).
    /// </summary>
    private bool looking = false;

    private float currentSpeed = 0f;

    private Terrain terrain;

    void Update()
    {
        var fastMode = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
        var maxSpeed = fastMode ? this.fastMovementSpeed : this.maxSpeed;

        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))
        {
            // transform.position = transform.position + (-transform.right * movementSpeed * Time.deltaTime);
            float newRotationY = transform.localEulerAngles.y - maxSpeed * Time.deltaTime * freeLookSensitivity;
            transform.localEulerAngles = new Vector3(0f, newRotationY, 0f);
            AlignWithTerrain();
        }

        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow))
        {
            // transform.position = transform.position + (transform.right * movementSpeed * Time.deltaTime);
            float newRotationY = transform.localEulerAngles.y + maxSpeed * Time.deltaTime * freeLookSensitivity;
            transform.localEulerAngles = new Vector3(0f, newRotationY, 0f);
            AlignWithTerrain();
        }

        bool accelerating = Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        bool braking = Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);

        if (accelerating)
        {
            // Increase the current speed upto (max movementSpeed)
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, accelerationRate * Time.deltaTime);
        }
        else if (braking)
        {
            // Decrease the current speed / reverse
            currentSpeed = Mathf.MoveTowards(currentSpeed, -maxSpeed, decelerationRate * Time.deltaTime);
        }
        else
        {
            // Come to a stop at the same brake rate when W is released
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decelerationRate * Time.deltaTime);
        }

        transform.position = transform.position + (transform.forward * currentSpeed * Time.deltaTime);
        AlignWithTerrain();

        if (looking)
        {
            float newRotationX = transform.localEulerAngles.y + Input.GetAxis("Mouse X") * freeLookSensitivity;
            float newRotationY = transform.localEulerAngles.x - Input.GetAxis("Mouse Y") * freeLookSensitivity;
            transform.localEulerAngles = new Vector3(newRotationY, newRotationX, 0f);
        }

        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartLooking();
        }
        else if (Input.GetKeyUp(KeyCode.Mouse1))
        {
            StopLooking();
        }

    }

    void OnDisable()
    {
        StopLooking();
    }

    /// <summary>
    /// Enable free looking.
    /// </summary>
    public void StartLooking()
    {
        looking = true;
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Disable free looking.
    /// </summary>
    public void StopLooking()
    {
        looking = false;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    void AlignWithTerrain()
    {
        if (terrain == null) terrain = Terrain.activeTerrain;

        if (terrain == null)
        {
            Debug.LogError("Terrain reference is null.");
        }

        Vector3 pos = transform.position;

        // Get terrain height at current position
        float terrainHeight = terrain.SampleHeight(pos) + terrain.transform.position.y;
        pos.y = terrainHeight;

        // Set bike position
        transform.position = pos;

        Vector3 terrainSize = terrain.terrainData.size;
        // Consider the offset of terrain from world origin
        float normX = (pos.x - terrain.transform.position.x) / terrainSize.x;
        float normZ = (pos.z - terrain.transform.position.z) / terrainSize.z;

        // Get terrain normal for slope alignment
        Vector3 normal = terrain.terrainData.GetInterpolatedNormal(normX, normZ);

        // Get the current yaw rotation
        float yaw = transform.eulerAngles.y;
        Vector3 yawRotation = new Vector3(0, yaw, 0);

        // Calculate the rotation of the x-z plane to align with terrain
        // Vector3 terrainTilt = Quaternion.FromToRotation(normal, yawRotation).eulerAngles;
        // Debug.Log(terrainTilt);

        // Project the normal vector in world space to the local space of bicycle
        Vector3 localNormal = Quaternion.Inverse(Quaternion.Euler(yawRotation)) * normal;
        // Debug.Log(localNormal);

        // Convert normal vector to degrees
        float pitch = -Mathf.Atan2(-localNormal.z, localNormal.y) * Mathf.Rad2Deg;
        float roll = -Mathf.Atan2(localNormal.x, localNormal.y) * Mathf.Rad2Deg;

        // In projector based simulation only rotate the pitch and roll of the camera and bicycle according to the terrain
        // transform.rotation = Quaternion.Euler(terrainTilt.x, yaw, terrainTilt.z);
        transform.rotation = Quaternion.Euler(pitch, yaw, roll);
    }
}