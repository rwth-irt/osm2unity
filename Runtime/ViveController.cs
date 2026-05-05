using System.Collections;
using UnityEngine.InputSystem;
using UnityEngine.XR.OpenXR.Input;

/*
 * Copyright (c) 2026 Institute of Automatic Control - RWTH Aachen University
 * [Licensed under the BSD-3-Clause License]
 *
 * See LICENSE file for full license text.
 */

namespace UnityEngine.XR.OpenXR.Samples.ControllerSample
{
    public class ViveController : MonoBehaviour
    {
        [Tooltip("Action Reference that represents the accelerator control")]
        [SerializeField] private InputActionReference _acceleratorActionReference = null;

        [Tooltip("Action Reference that represents the braking control")]
        [SerializeField] private InputActionReference _brakingActionReference = null;

        [Tooltip("Action Reference that represents the steering control")]
        [SerializeField] private InputActionReference _steeringActionReference = null;

        [Tooltip("Max speed of bicycle")]
        [SerializeField] private float maxSpeed = 6.94f;

        [Tooltip("Steering sensitivity of bicycle")]
        [SerializeField] private float steerSensitivity = 3f;

        [Tooltip("Acceleration rate when accelerator is pressed")]
        [SerializeField] private float accelerationRate = 10f;

        [Tooltip("Deceleration rate when brake is pressed / accelerator is released")]
        [SerializeField] private float decelerationRate = 10f;

        private float currentSpeed = 0f;

        float acceleratorPos = 0f;
        float steerPos = 0f;
        float brakePos = 0f;

        protected virtual void OnEnable()
        {
            if (_acceleratorActionReference == null || _acceleratorActionReference.action == null 
                || _brakingActionReference == null || _brakingActionReference.action == null
                || _steeringActionReference == null || _steeringActionReference.action == null)
                return;

            _acceleratorActionReference.action.started += OnActionStarted;
            _acceleratorActionReference.action.performed += OnActionPerformed;
            _acceleratorActionReference.action.canceled += OnActionCancelled;

            _brakingActionReference.action.started += OnActionStarted;
            _brakingActionReference.action.performed += OnActionPerformed;
            _brakingActionReference.action.canceled += OnActionCancelled;

            _steeringActionReference.action.started += OnActionStarted;
            _steeringActionReference.action.performed += OnActionPerformed;
            _steeringActionReference.action.canceled += OnActionCancelled;
        }

        protected virtual void OnDisable()
        {
            if (_acceleratorActionReference == null || _acceleratorActionReference.action == null
                || _brakingActionReference == null || _brakingActionReference.action == null
                || _steeringActionReference == null || _steeringActionReference.action == null)
                return;

            _acceleratorActionReference.action.started -= OnActionStarted;
            _acceleratorActionReference.action.performed -= OnActionPerformed;
            _acceleratorActionReference.action.canceled -= OnActionCancelled;

            _brakingActionReference.action.started -= OnActionStarted;
            _brakingActionReference.action.performed -= OnActionPerformed;
            _brakingActionReference.action.canceled -= OnActionCancelled;

            _steeringActionReference.action.started -= OnActionStarted;
            _steeringActionReference.action.performed -= OnActionPerformed;
            _steeringActionReference.action.canceled -= OnActionCancelled;
        }

        protected virtual void OnActionStarted(InputAction.CallbackContext ctx)
        {
            UpdateValue(ctx);
        }

        protected virtual void OnActionPerformed(InputAction.CallbackContext ctx) => UpdateValue(ctx);

        protected virtual void OnActionCancelled(InputAction.CallbackContext ctx)
        {
            UpdateValue(ctx);
        }

        private void UpdateValue(InputAction.CallbackContext ctx)
        {
            if (ctx.action == _acceleratorActionReference.action)
            {
                acceleratorPos = ctx.ReadValue<float>();
            }
            else if (ctx.action == _brakingActionReference.action)
            {
                brakePos = ctx.ReadValue<float>();
            }
            else if (ctx.action == _steeringActionReference.action)
            {
                steerPos = ctx.ReadValue<Vector2>().x;
            }

            // Debug.Log(acceleratorPos);
            // Debug.Log(steerPos);
            // Debug.Log(brakePos);
        }

        void Update()
        {
            float newRotationY = transform.localEulerAngles.y + steerPos * steerSensitivity * Time.deltaTime;
            transform.localEulerAngles = new Vector3(0f, newRotationY, 0f);

            if (acceleratorPos > 0)
            {
                // Increase the current speed upto (max movementSpeed)
                currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, acceleratorPos * accelerationRate * Time.deltaTime);
            }
            else if (brakePos > 0)
            {
                // Decrease the current speed / reverse
                currentSpeed = Mathf.MoveTowards(currentSpeed, -maxSpeed, brakePos * decelerationRate * Time.deltaTime);
            }
            else
            {
                // Come to a stop at the same brake rate when accelerator is released
                currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, decelerationRate * Time.deltaTime);
            }

            transform.position = transform.position + (transform.forward * currentSpeed * Time.deltaTime);
        }
    }
}