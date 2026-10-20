using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.Serialization;

namespace Loags.StepHeight
{
    /// <summary>
    /// Detects nearby walkable steps and moves a Rigidbody onto the selected surface.
    /// The caller owns input, grounded checks, and the surrounding movement controller.
    /// </summary>
    [MovedFrom(true, sourceNamespace: "LB.StepHeight", sourceAssembly: "LB.StepHeight.Runtime", sourceClassName: "StepHeightController")]
    [AddComponentMenu("Loags/Step Height Controller")]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class StepHeightController : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField, Min(0.01f)] private float stepHeight = 0.5f;
        [SerializeField, Min(0.001f)] private float minimumStepHeight = 0.02f;
        [SerializeField, Range(0f, 89f)] private float maximumSurfaceAngle = 50f;
        [FormerlySerializedAs("stepUpAngleThreshold")]
        [SerializeField, Range(0f, 180f)] private float maximumApproachAngle = 65f;
        [SerializeField, Min(0f)] private float detectionDistance = 0.3f;
        [SerializeField, Min(0.001f)] private float surfaceProbeInset = 0.1f;

        [Header("Movement")]
        [FormerlySerializedAs("stepUpSmoothFactor")]
        [SerializeField, Min(0.01f)] private float stepSpeed = 4.5f;
        [SerializeField, Min(0.01f)] private float minimumStepDuration = 0.12f;
        [SerializeField, Min(0f)] private float landingInset = 0.1f;
        [SerializeField, Min(0.001f)] private float clearance = 0.02f;

        [Header("Collision Filtering")]
        [SerializeField] private LayerMask layersToIgnore;
        [SerializeField] private QueryTriggerInteraction triggerInteraction = QueryTriggerInteraction.Ignore;
        [SerializeField] private bool includeChildColliders = true;

        [Header("Diagnostics")]
        [SerializeField] private bool debugVisualization;

        [FormerlySerializedAs("disableStepHeight")]
        [SerializeField, HideInInspector] private bool steppingDisabled;

        private Rigidbody _rigidbody;
        private ColliderManager _colliderManager;
        private Coroutine _stepCoroutine;
        private StepEventData _activeStep;
        private StepAttemptResult _lastAttemptResult;
        private Vector3 _lastAttemptDirection;
        private Vector3 _lastTargetPosition;
        private Vector3 _stepVelocity;
        private float _liftFraction;

        /// <summary>Raised immediately after a step request is accepted.</summary>
        public event Action<StepEventData> StepStarted;

        /// <summary>Raised after the Rigidbody reaches the accepted step target.</summary>
        public event Action<StepEventData> StepCompleted;

        /// <summary>Raised when an active step is cancelled before completion.</summary>
        public event Action<StepEventData, StepCancellationReason> StepCancelled;

        /// <summary>Gets whether a step movement is currently active.</summary>
        public bool IsStepping => _stepCoroutine != null;

        /// <summary>Gets or sets whether this component accepts step requests.</summary>
        public bool SteppingEnabled
        {
            get => !steppingDisabled;
            set
            {
                if (steppingDisabled == !value) return;
                steppingDisabled = !value;
                if (steppingDisabled) CancelStepInternal(StepCancellationReason.Disabled);
            }
        }

        /// <summary>Gets the maximum vertical rise, in world units, accepted as a step.</summary>
        public float MaximumStepHeight => stepHeight;

        /// <summary>Gets or sets the layers excluded from every environment query.</summary>
        public LayerMask IgnoredLayers
        {
            get => layersToIgnore;
            set => layersToIgnore = value;
        }

        private void Awake() => Initialize();

        private void OnDisable() => CancelStepInternal(StepCancellationReason.ComponentDisabled);

        private void OnValidate()
        {
            stepHeight = Mathf.Max(0.01f, stepHeight);
            minimumStepHeight = Mathf.Clamp(minimumStepHeight, 0.001f, stepHeight);
            maximumSurfaceAngle = Mathf.Clamp(maximumSurfaceAngle, 0f, 89f);
            maximumApproachAngle = Mathf.Clamp(maximumApproachAngle, 0f, 180f);
            detectionDistance = Mathf.Max(0f, detectionDistance);
            surfaceProbeInset = Mathf.Max(0.001f, surfaceProbeInset);
            stepSpeed = Mathf.Max(0.01f, stepSpeed);
            minimumStepDuration = Mathf.Max(0.01f, minimumStepDuration);
            landingInset = Mathf.Max(0f, landingInset);
            clearance = Mathf.Max(0.001f, clearance);

            if (Application.isPlaying && _colliderManager != null) RefreshColliderCache();
        }

        /// <summary>
        /// Attempts to start a step in the supplied world-space movement direction.
        /// Call this from the movement controller's physics loop while the character is grounded and moving.
        /// </summary>
        /// <param name="worldMovementDirection">Desired movement direction in world space. Its magnitude is ignored.</param>
        /// <returns>The immediate result of the request.</returns>
        public StepAttemptResult TryStep(Vector3 worldMovementDirection)
        {
            Initialize();

            if (!isActiveAndEnabled || steppingDisabled)
            {
                return RecordAttempt(StepAttemptResult.Disabled, worldMovementDirection);
            }

            if (IsStepping)
            {
                return RecordAttempt(StepAttemptResult.AlreadyStepping, worldMovementDirection);
            }

            Vector3 movementDirection = Vector3.ProjectOnPlane(worldMovementDirection, transform.up);
            if (movementDirection.sqrMagnitude <= StepMath.DirectionEpsilon)
            {
                return RecordAttempt(StepAttemptResult.InvalidDirection, worldMovementDirection);
            }

            movementDirection.Normalize();
            var settings = new StepQuerySettings(stepHeight, minimumStepHeight, maximumSurfaceAngle, maximumApproachAngle,
                detectionDistance, surfaceProbeInset, landingInset, clearance, layersToIgnore, triggerInteraction);
            StepAttemptResult result = _colliderManager.TryFindStep(movementDirection, settings, out StepCandidate candidate);
            RecordAttempt(result, movementDirection);
            if (result != StepAttemptResult.Started) return result;

            _activeStep = new StepEventData(_rigidbody.position, candidate.TargetPosition, candidate.SurfaceCollider,
                candidate.Height);
            _lastTargetPosition = candidate.TargetPosition;
            _stepVelocity = Vector3.ProjectOnPlane(_rigidbody.linearVelocity, transform.up);
            _liftFraction = candidate.LiftFraction;
            _stepCoroutine = StartCoroutine(CompleteStepMovement(_activeStep));
            StepStarted?.Invoke(_activeStep);
            return StepAttemptResult.Started;
        }

        /// <summary>Cancels the active step, if any.</summary>
        /// <returns>True when an active step was cancelled.</returns>
        public bool CancelStep() => CancelStepInternal(StepCancellationReason.Requested);

        /// <summary>
        /// Rebuilds the cached player-collider set. Call this after adding, removing, enabling, disabling, or resizing
        /// player colliders at runtime.
        /// </summary>
        public void RefreshColliderCache()
        {
            Initialize();
            CancelStepInternal(StepCancellationReason.CollidersRefreshed);
            _colliderManager.Refresh(includeChildColliders);
        }

        private void Initialize()
        {
            if (_rigidbody == null) _rigidbody = GetComponent<Rigidbody>();
            if (_colliderManager != null) return;

            _colliderManager = new ColliderManager(transform, _rigidbody);
            _colliderManager.Refresh(includeChildColliders);
        }

        private IEnumerator CompleteStepMovement(StepEventData step)
        {
            Vector3 up = transform.up;
            float duration = StepMath.CalculateStepDuration(step.StartPosition, step.TargetPosition, stepSpeed,
                Mathf.Max(minimumStepDuration, Time.fixedDeltaTime), up, _stepVelocity.magnitude);
            int physicsTicks = Mathf.Max(1, Mathf.CeilToInt(duration / Time.fixedDeltaTime));
            var waitForFixedUpdate = new WaitForFixedUpdate();

            for (int tick = 1; tick <= physicsTicks; tick++)
            {
                float progress = (float)tick / physicsTicks;
                Vector3 stepPosition = StepMath.CalculateStepPosition(step.StartPosition, step.TargetPosition,
                    up, progress, _liftFraction);
                // Velocity must not advance the dynamic body a second time after MovePosition.
                if (!_rigidbody.isKinematic) _rigidbody.linearVelocity = Vector3.zero;
                _rigidbody.MovePosition(stepPosition);

                yield return waitForFixedUpdate;
            }

            RestoreMovementVelocity();
            _stepCoroutine = null;
            StepCompleted?.Invoke(step);
        }

        private void RestoreMovementVelocity()
        {
            if (_rigidbody.isKinematic) return;

            _rigidbody.linearVelocity = _stepVelocity;
        }

        private bool CancelStepInternal(StepCancellationReason reason)
        {
            if (_stepCoroutine == null) return false;

            StopCoroutine(_stepCoroutine);
            RestoreMovementVelocity();
            _stepCoroutine = null;
            StepCancelled?.Invoke(_activeStep, reason);
            return true;
        }

        private StepAttemptResult RecordAttempt(StepAttemptResult result, Vector3 direction)
        {
            _lastAttemptResult = result;
            _lastAttemptDirection = direction.sqrMagnitude > StepMath.DirectionEpsilon ? direction.normalized : Vector3.zero;
            return result;
        }

        private void OnDrawGizmosSelected()
        {
            if (!debugVisualization) return;

            Bounds bounds = default;
            bool hasBounds = false;
            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider playerCollider in colliders)
            {
                if (!playerCollider.enabled || playerCollider.isTrigger) continue;
                if (!hasBounds)
                {
                    bounds = playerCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    bounds.Encapsulate(playerCollider.bounds);
                }
            }

            if (!hasBounds) return;

            float radius = Mathf.Max(bounds.extents.x, bounds.extents.z) + detectionDistance;
            Vector3 probeCenter = new Vector3(bounds.center.x, bounds.min.y + stepHeight * 0.5f, bounds.center.z);
            Gizmos.color = _lastAttemptResult == StepAttemptResult.Started ? Color.green : Color.red;
            Gizmos.DrawWireSphere(probeCenter, radius);

            if (_lastAttemptDirection.sqrMagnitude > StepMath.DirectionEpsilon)
            {
                Gizmos.color = Color.yellow;
                Gizmos.DrawLine(probeCenter, probeCenter + _lastAttemptDirection * radius);
            }

            if (_lastAttemptResult == StepAttemptResult.Started || IsStepping)
            {
                Gizmos.color = Color.cyan;
                Gizmos.DrawWireSphere(_lastTargetPosition, 0.08f);
                Gizmos.DrawLine(transform.position, _lastTargetPosition);
            }
        }
    }
}
