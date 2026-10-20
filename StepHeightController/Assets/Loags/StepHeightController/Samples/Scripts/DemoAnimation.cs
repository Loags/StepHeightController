using UnityEngine;
using UnityEngine.Scripting.APIUpdating;

namespace Loags.StepHeight.Samples
{
    [MovedFrom(true, sourceNamespace: "LB.StepHeight.Samples", sourceAssembly: "LB.StepHeight.Samples", sourceClassName: "DemoAnimation")]
    [DisallowMultipleComponent]
    public sealed class DemoAnimation : MonoBehaviour
    {
        private const float FallGracePeriod = 0.1f;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private static readonly int MotionSpeed = Animator.StringToHash("MotionSpeed");
        private static readonly int Grounded = Animator.StringToHash("Grounded");
        private static readonly int Jump = Animator.StringToHash("Jump");
        private static readonly int FreeFall = Animator.StringToHash("FreeFall");

        [SerializeField] private MovementController movementController;
        [SerializeField] private StepHeightController stepController;
        [SerializeField] private Rigidbody body;
        [SerializeField] private Animator animator;

        private Vector3 _previousPosition;
        private float _jumpHoldUntil;
        private float _fallGraceUntil;

        private void Awake()
        {
            animator.applyRootMotion = false;
            _previousPosition = body.transform.position;
        }

        private void OnEnable()
        {
            movementController.Jumped += OnJumped;
            movementController.PlayerReset += OnPlayerReset;
            _previousPosition = body.transform.position;
            _fallGraceUntil = Time.time;
        }

        private void OnDisable()
        {
            movementController.Jumped -= OnJumped;
            movementController.PlayerReset -= OnPlayerReset;
        }

        private void Update()
        {
            // Measured travel includes Rigidbody stepping, while the Animator only moves the mesh.
            Vector3 position = body.transform.position;
            float speed = Vector3.ProjectOnPlane(position - _previousPosition, body.transform.up).magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            _previousPosition = position;
            bool grounded = Time.time >= _jumpHoldUntil && (stepController.IsStepping || movementController.IsGrounded());
            bool jumping = !grounded && Vector3.Dot(body.linearVelocity, body.transform.up) > 0.1f;
            if (grounded) _fallGraceUntil = Time.time + FallGracePeriod;
            // Brief drops keep the locomotion pose without changing physical grounding.
            bool animationGrounded = grounded || (!jumping && Time.time < _fallGraceUntil);
            animator.SetFloat(Speed, speed, 0.1f, Time.deltaTime);
            animator.SetFloat(MotionSpeed, 1f);
            animator.SetBool(Grounded, animationGrounded);
            animator.SetBool(Jump, jumping);
            animator.SetBool(FreeFall, !animationGrounded && !jumping);
        }

        private void OnJumped()
        {
            _jumpHoldUntil = Time.time + 0.12f;
            _fallGraceUntil = Time.time;
        }

        private void OnPlayerReset(Vector3 displacement)
        {
            _previousPosition = body.transform.position;
            _jumpHoldUntil = 0f;
            _fallGraceUntil = Time.time + FallGracePeriod;
            animator.Rebind();
            animator.SetFloat(Speed, 0f);
            animator.SetFloat(MotionSpeed, 1f);
            animator.SetBool(Grounded, true);
            animator.SetBool(Jump, false);
            animator.SetBool(FreeFall, false);
            animator.Update(0f);
        }
    }
}
