using LB.StepHeight;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

namespace LB.StepHeight.Samples
{
    /// <summary>
    /// Small first-person Rigidbody controller used only by the Step Height Controller sample.
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

        [Header("Movement")]
        [FormerlySerializedAs("walkingSpeed")]
        [SerializeField, Min(0f)] private float moveSpeed = 4.5f;
        [FormerlySerializedAs("runningSpeed")]
        [SerializeField, Min(0f)] private float sprintSpeed = 6.725f;
        [SerializeField, Min(0f)] private float groundAcceleration = 35f;
        [FormerlySerializedAs("jumpForce")]
        [SerializeField, Min(0f)] private float jumpVelocity = 7f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 1f;

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

        private void Awake()
        {
            if (body == null) body = GetComponent<Rigidbody>();
            if (stepController == null) stepController = GetComponent<StepHeightController>();
            if (groundCollider == null) groundCollider = GetComponent<Collider>();
            if (movementOrientation == null) movementOrientation = transform;
            if (viewPitchTransform == null) viewPitchTransform = movementOrientation;
            _input = new InputMap();
        }

        private void OnEnable()
        {
            _input.Player.Movement.performed += OnMovementPerformed;
            _input.Player.Movement.canceled += OnMovementCancelled;
            _input.Player.MouseDelta.performed += OnLookPerformed;
            _input.Player.MouseDelta.canceled += OnLookCancelled;
            _input.Player.Jump.performed += OnJumpPerformed;
            _input.Player.Sprint.performed += OnSprintPerformed;
            _input.Player.Sprint.canceled += OnSprintCancelled;
            _input.Player.Enable();

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

        private void OnDisable()
        {
            _input.Player.Disable();
            _input.Player.Movement.performed -= OnMovementPerformed;
            _input.Player.Movement.canceled -= OnMovementCancelled;
            _input.Player.MouseDelta.performed -= OnLookPerformed;
            _input.Player.MouseDelta.canceled -= OnLookCancelled;
            _input.Player.Jump.performed -= OnJumpPerformed;
            _input.Player.Sprint.performed -= OnSprintPerformed;
            _input.Player.Sprint.canceled -= OnSprintCancelled;
            _moveInput = Vector2.zero;
            _lookInput = Vector2.zero;
            _jumpQueued = false;

            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }

        private void OnDestroy() => _input?.Dispose();

        private void Update()
        {
            float yaw = _lookInput.x * lookSensitivity * LookInputScale;
            _pitch = Mathf.Clamp(_pitch - _lookInput.y * lookSensitivity * LookInputScale, -pitchLimit, pitchLimit);
            movementOrientation.Rotate(0f, yaw, 0f, Space.World);
            viewPitchTransform.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
        }

        private void FixedUpdate()
        {
            bool grounded = IsGrounded();
            Vector3 desiredDirection = movementOrientation.forward * _moveInput.y +
                movementOrientation.right * _moveInput.x;
            desiredDirection = Vector3.ProjectOnPlane(desiredDirection, transform.up);
            if (desiredDirection.sqrMagnitude > 1f) desiredDirection.Normalize();

            if (!stepController.IsStepping)
            {
                float speed = _sprinting ? sprintSpeed : moveSpeed;
                Vector3 verticalVelocity = Vector3.Project(body.linearVelocity, transform.up);
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, transform.up);
                Vector3 targetVelocity = desiredDirection * speed;
                horizontalVelocity = Vector3.MoveTowards(horizontalVelocity, targetVelocity,
                    groundAcceleration * Time.fixedDeltaTime);
                body.linearVelocity = horizontalVelocity + verticalVelocity;

                if (grounded && desiredDirection.sqrMagnitude > 0.0001f)
                {
                    stepController.TryStep(desiredDirection);
                }
            }

            if (_jumpQueued && grounded && !stepController.IsStepping)
            {
                Vector3 horizontalVelocity = Vector3.ProjectOnPlane(body.linearVelocity, transform.up);
                body.linearVelocity = horizontalVelocity + transform.up * jumpVelocity;
            }

            _jumpQueued = false;
            if (stepController.IsStepping) RemoveDownwardVelocity();
            else body.AddForce(Physics.gravity * gravityMultiplier, ForceMode.Acceleration);
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
            return Physics.SphereCast(origin, radius, -transform.up, out _, groundProbeDistance, groundLayers,
                QueryTriggerInteraction.Ignore);
        }

        private void OnMovementPerformed(InputAction.CallbackContext context) =>
            _moveInput = context.ReadValue<Vector2>();

        private void OnMovementCancelled(InputAction.CallbackContext context) => _moveInput = Vector2.zero;

        private void OnLookPerformed(InputAction.CallbackContext context) => _lookInput = context.ReadValue<Vector2>();

        private void OnLookCancelled(InputAction.CallbackContext context) => _lookInput = Vector2.zero;

        private void OnJumpPerformed(InputAction.CallbackContext context) => _jumpQueued = true;

        private void OnSprintPerformed(InputAction.CallbackContext context) => _sprinting = true;

        private void OnSprintCancelled(InputAction.CallbackContext context) => _sprinting = false;
    }
}
