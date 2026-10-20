using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace LB.StepHeight.Samples
{
    /// <summary>
    /// Small keyboard-and-mouse Rigidbody controller used only by the sample scenes.
    /// Production integrations should drive <see cref="StepHeightController.TryStep"/> from their own movement code.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class MovementController : MonoBehaviour
    {
        private const float LookInputScale = 0.02f;

        [Header("References")]
        [FormerlySerializedAs("stepHeightController")]
        [SerializeField] private StepHeightController stepController;
        [FormerlySerializedAs("rigidbody")]
        [SerializeField] private Rigidbody body;
        [FormerlySerializedAs("feet")]
        [SerializeField] private Collider groundCollider;
        [FormerlySerializedAs("yRotationTransform")]
        [SerializeField] private Transform movementOrientation;
        [FormerlySerializedAs("xRotationTransform")]
        [SerializeField] private Transform viewPitchTransform;
        [SerializeField] private Transform visualRoot;
        [SerializeField] private bool turnVisualToMovement;
        [SerializeField, Min(0f)] private float visualTurnSpeed = 720f;

        [Header("Movement")]
        [FormerlySerializedAs("walkingSpeed")]
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [FormerlySerializedAs("runningSpeed")]
        [SerializeField, Min(0f)] private float sprintSpeed = 6.725f;
        [SerializeField, Min(0f)] private float groundAcceleration = 35f;
        [FormerlySerializedAs("jumpForce")]
        [SerializeField, Min(0f)] private float jumpVelocity = 8.15f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 2f;

        [Header("Look")]
        [FormerlySerializedAs("lookSpeed")]
        [SerializeField, Min(0f)] private float lookSensitivity = 5f;
        [FormerlySerializedAs("lookXLimit")]
        [SerializeField, Range(0f, 90f)] private float pitchLimit = 90f;

        [Header("Grounding")]
        [SerializeField, Min(0.001f)] private float groundProbeDistance = 0.12f;
        [SerializeField] private LayerMask groundLayers = ~0;

        private InputMap _input;
        private Vector2 _moveInput;
        private Vector2 _lookInput;
        private bool _sprinting;
        private bool _jumpQueued;
        private float _pitch;
        private float _initialPitch;
        private Vector3 _spawnPosition;
        private Quaternion _spawnRotation;
        private Quaternion _initialYawRotation;
        private Quaternion _initialPitchRotation;
        private Quaternion _initialVisualRotation;

        public event Action Jumped;
        public event Action<Vector3> PlayerReset;
        public bool IsSprinting => _sprinting;
        public bool HasMovementInput => _moveInput.sqrMagnitude > 0.0001f;

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (stepController == null) stepController = GetComponent<StepHeightController>();
            if (groundCollider == null) groundCollider = GetComponent<Collider>();
            if (movementOrientation == null) movementOrientation = transform;
            if (viewPitchTransform == null) viewPitchTransform = movementOrientation;
            _spawnPosition = body.position;
            _spawnRotation = body.rotation;
            _initialYawRotation = movementOrientation.localRotation;
            _initialPitchRotation = viewPitchTransform.localRotation;
            _initialPitch = Mathf.DeltaAngle(0f, viewPitchTransform.localEulerAngles.x);
            _pitch = _initialPitch;
            if (visualRoot != null) _initialVisualRotation = visualRoot.localRotation;
        }

        private void OnEnable()
        {
            _input ??= new InputMap();
            _input.Player.MouseDelta.performed += OnLookPerformed;
            _input.Player.MouseDelta.canceled += OnLookCancelled;
            _input.Player.Jump.performed += OnJumpPerformed;
            _input.Player.Enable();

            CaptureCursor();
        }

        private void OnDisable()
        {
            _input.Player.Disable();
            _input.Player.MouseDelta.performed -= OnLookPerformed;
            _input.Player.MouseDelta.canceled -= OnLookCancelled;
            _input.Player.Jump.performed -= OnJumpPerformed;
            ReleaseCursor();
        }

        private void OnDestroy() => _input?.Dispose();

        private void Update()
        {
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ReleaseCursor();
            if (Cursor.lockState != CursorLockMode.Locked)
            {
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame) CaptureCursor();
                return;
            }

            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            {
                stepController.SteppingEnabled = !stepController.SteppingEnabled;
            }
            if (Keyboard.current != null && Keyboard.current.rKey.wasPressedThisFrame)
            {
                ResetPlayer();
                return;
            }

            _moveInput = _input.Player.Movement.ReadValue<Vector2>();
            _sprinting = _input.Player.Sprint.IsPressed();
            float yaw = _lookInput.x * lookSensitivity * LookInputScale;
            _pitch = Mathf.Clamp(_pitch - _lookInput.y * lookSensitivity * LookInputScale, -pitchLimit, pitchLimit);
            // Camera anchors turn independently of the Rigidbody and animated character.
            movementOrientation.Rotate(0f, yaw, 0f, Space.World);
            viewPitchTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            UpdateVisualFacing();
        }

        private void FixedUpdate()
        {
            bool grounded = IsGrounded() && Vector3.Dot(body.linearVelocity, transform.up) <= 0.1f;
            Vector3 desiredDirection = GetMovementDirection();

            // Jump wins over both an active ascent and a new step on this physics tick.
            bool jumped = _jumpQueued && (grounded || stepController.IsStepping);
            _jumpQueued = false;
            if (jumped)
            {
                stepController.CancelStep();
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, transform.up);
                body.linearVelocity = horizontalVelocity + transform.up * jumpVelocity;
                Jumped?.Invoke();
            }

            if (!stepController.IsStepping)
            {
                float speed = _sprinting ? sprintSpeed : moveSpeed;
                Vector3 verticalVelocity = Vector3.Project(body.linearVelocity, transform.up);
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, transform.up);
                Vector3 targetVelocity = desiredDirection * speed;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity, groundAcceleration * Time.fixedDeltaTime);
                body.linearVelocity = horizontalVelocity + verticalVelocity;

                if (!jumped && grounded && desiredDirection.sqrMagnitude > 0.0001f)
                {
                    stepController.TryStep(desiredDirection);
                }
            }

            if (stepController.IsStepping) RemoveDownwardVelocity();
            else body.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
        }

        private Vector3 GetMovementDirection()
        {
            Vector3 direction = movementOrientation.forward * _moveInput.y + movementOrientation.right * _moveInput.x;
            return Vector3.ClampMagnitude(Vector3.ProjectOnPlane(direction, transform.up), 1f);
        }

        private void UpdateVisualFacing()
        {
            if (visualRoot == null) return;
            Vector3 direction = turnVisualToMovement ? GetMovementDirection() : movementOrientation.forward;
            if (direction.sqrMagnitude < 0.0001f) return;
            Quaternion facing = Quaternion.LookRotation(direction, transform.up);
            visualRoot.rotation = turnVisualToMovement ? Quaternion.RotateTowards(visualRoot.rotation, facing, visualTurnSpeed * Time.deltaTime) : facing;
        }

        public void ResetPlayer()
        {
            stepController.CancelStep();
            Vector3 displacement = _spawnPosition - body.position;
            body.position = _spawnPosition;
            body.rotation = _spawnRotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            movementOrientation.localRotation = _initialYawRotation;
            viewPitchTransform.localRotation = _initialPitchRotation;
            if (visualRoot != null) visualRoot.localRotation = _initialVisualRotation;
            _pitch = _initialPitch;
            ClearInput();
            PlayerReset?.Invoke(displacement);
        }

        private void OnApplicationFocus(bool focused)
        {
            if (!focused) ReleaseCursor();
        }

        private void CaptureCursor()
        {
            _lookInput = Vector2.zero;
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void ReleaseCursor()
        {
            ClearInput();
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void ClearInput()
        {
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _sprinting = false;
            _jumpQueued = false;
        }

        private void RemoveDownwardVelocity()
        {
            Vector3 verticalVelocity = Vector3.Project(body.linearVelocity, transform.up);
            if (Vector3.Dot(verticalVelocity, transform.up) >= 0f) return;

            body.linearVelocity -= verticalVelocity;
        }

        /// <summary>Returns whether the sample player's primary collider is immediately above a walkable surface.</summary>
        public bool IsGrounded()
        {
            Bounds bounds = groundCollider.bounds;
            float radius = Mathf.Max(0.01f, Mathf.Min(bounds.extents.x, bounds.extents.z) - 0.02f);
            Vector3 origin = new Vector3(bounds.center.x, bounds.min.y + radius + 0.01f, bounds.center.z);
            return Physics.SphereCast(origin, radius, -transform.up, out RaycastHit hit, groundProbeDistance, groundLayers, QueryTriggerInteraction.Ignore) && Vector3.Dot(hit.normal, transform.up) >= 0.5f;
        }

        private void OnLookPerformed(InputAction.CallbackContext context)
        {
            if (Cursor.lockState == CursorLockMode.Locked) _lookInput = context.ReadValue<Vector2>();
        }

        private void OnLookCancelled(InputAction.CallbackContext context) => _lookInput = Vector2.zero;

        private void OnJumpPerformed(InputAction.CallbackContext context)
        {
            if (Cursor.lockState == CursorLockMode.Locked) _jumpQueued = true;
        }

    }
}
