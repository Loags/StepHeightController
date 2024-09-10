using UnityEngine;
using UnityEngine.InputSystem;
using LB.Player.Movement.StepHeight;
using LB.Player.Input;

namespace LB.Player.Movement
{
	[RequireComponent(typeof(Rigidbody))]
	public class MovementController : MonoBehaviour
	{
		#region Variables

		[HideInInspector] public StepHeightController stepHeightController;
		[HideInInspector] public new Rigidbody rigidbody;
		private SpringJoint magnetJoint;

		private bool isSwinging = false;
		private Vector3 previousInputInfluence = Vector3.zero;

		[Header("Movement")] [SerializeField] private float walkingSpeed = 4.5f;
		[SerializeField] private float runningSpeed = 6.725f;
		[SerializeField] [Range(1f, 10)] private float swingInfluenceFactor = 5;
		private bool onStair;
		private bool hasInput;
		[HideInInspector] public bool blockMovement;

		[Header("Rotation")] [SerializeField] private float lookSpeed = 2.0f;
		[SerializeField] private float lookXLimit = 90;
		private float rotationX = 0;
		[SerializeField] private Transform xRotationTransform;
		[SerializeField] private Transform yRotationTransform;
		private float rotationModifier;

		[Header("Jumping")] [SerializeField] private float jumpForce = 10f;
		[SerializeField] private MeshCollider feet;
		private float distanceToGround;

		private Vector2 movementInput = Vector2.zero;
		private Vector2 mouseDelta = Vector2.zero;
		private bool sprinting;
		private Vector3 lastInputVector;

		[Header("Gravity")] [SerializeField] private float gravityStrength;
		[SerializeField] private Vector3 gravityDirection;

		#endregion

		#region (De)initialization

		private void OnEnable()
		{
			InputManager.inputMap.Player.MouseDelta.performed += ctx => mouseDelta = ctx.ReadValue<Vector2>();
			InputManager.inputMap.Player.MouseDelta.Enable();
			InputManager.inputMap.Player.Movement.performed += ctx => movementInput = ctx.ReadValue<Vector2>();
			InputManager.inputMap.Player.Movement.Enable();
			InputManager.inputMap.Player.Jump.performed += OnJumpPerformed;
			InputManager.inputMap.Player.Jump.Enable();
			InputManager.inputMap.Player.Sprint.performed += ctx => sprinting = ctx.ReadValueAsButton();
			InputManager.inputMap.Player.Sprint.Enable();
			InputManager.inputMap.Player.Enable();
		}

		private void OnDisable()
		{
			InputManager.inputMap.Player.Jump.performed -= OnJumpPerformed;
			InputManager.inputMap.Player.MouseDelta.Disable();
			InputManager.inputMap.Player.Movement.Disable();
			InputManager.inputMap.Player.Jump.Disable();
			InputManager.inputMap.Player.Sprint.Disable();
			InputManager.inputMap.Player.Disable();
		}

		private void Awake()
		{
			stepHeightController = GetComponent<StepHeightController>();
			rigidbody = GetComponent<Rigidbody>();
			distanceToGround = feet.bounds.extents.y;

			rigidbody.isKinematic = true;
			Cursor.visible = false;
			Cursor.lockState = CursorLockMode.Locked;

			SetRotationModifier(1f);
		}

		private void Start()
		{
			gravityDirection = Vector3.down;
			rigidbody.isKinematic = false;
		}

		#endregion

		private void Update()
		{
			InputManager.inputMap.Player.Enable();
			bool grounded = IsGrounded();

			if (blockMovement)
			{
				Vector3 currentMovement = GetModifiedVelocity(false);
				if (grounded)
				{
					currentMovement.x *= .5f;
					currentMovement.z *= .5f;
				}

				rigidbody.velocity = currentMovement;
				if (rigidbody.velocity.magnitude < .3f && grounded && onStair && !magnetJoint)
				{
					rigidbody.isKinematic = true;
				}

				return;
			}

			DoRotation();
			DoMovement(grounded);
		}

		private void FixedUpdate()
		{
			rigidbody.AddForce(rigidbody.mass * rigidbody.mass * gravityStrength * gravityDirection.normalized);
		}

		public void SetRotationModifier(float _value)
		{
			rotationModifier = _value;
		}

		private void DoMovement(bool grounded)
		{
			if (blockMovement) return;

			float speed = sprinting ? runningSpeed : walkingSpeed;

			Vector2 curSpeed = new(movementInput.y, movementInput.x);
			hasInput = curSpeed.x != 0 || curSpeed.y != 0;
			if (hasInput && rigidbody.isKinematic) rigidbody.isKinematic = false;

			if (curSpeed.magnitude > 1)
			{
				curSpeed = curSpeed.normalized;
			}

			curSpeed *= speed;

			Vector3 currentMovement = GetModifiedVelocity(hasInput);
			Vector3 newMovement = (yRotationTransform.forward * curSpeed.x) + (yRotationTransform.right * curSpeed.y);
			lastInputVector = newMovement;

			if (grounded)
			{
				if (!isSwinging)
				{
					currentMovement.x *= 0.5f;
					currentMovement.z *= 0.5f;
				}

				if (!hasInput)
				{
					previousInputInfluence = Vector3.zero;
				}

				if (hasInput)
				{
					stepHeightController.CheckForStep();
				}
			}
			else
			{
				if (hasInput)
				{
					if (Vector3.Angle(lastInputVector, newMovement) < 20)
					{
						float angle = Vector3.Angle(currentMovement, newMovement);
						bool inFrontOfWall =
							Physics.SphereCast(yRotationTransform.position - .5f * yRotationTransform.forward, .5f,
								yRotationTransform.forward, out RaycastHit _, 1.5f, ~LayerMask.GetMask("Player"),
								QueryTriggerInteraction.Ignore);

						if (inFrontOfWall)
						{
							if (angle > 20)
							{
								currentMovement = new Vector3(0, currentMovement.y, 0);
							}
							else
							{
								currentMovement *= (1 - (angle / 20));
							}
						}
					}

					float factor = swingInfluenceFactor * Time.deltaTime;
					newMovement = previousInputInfluence * (1 - factor) + newMovement * factor;
				}
			}

			if (hasInput)
			{
				previousInputInfluence = newMovement;
			}

			rigidbody.velocity = currentMovement + newMovement;

			float clampedX = Mathf.Clamp(rigidbody.velocity.x, -runningSpeed, runningSpeed);
			float clampedZ = Mathf.Clamp(rigidbody.velocity.z, -runningSpeed, runningSpeed);
			rigidbody.velocity = new Vector3(clampedX, rigidbody.velocity.y, clampedZ);

			if (!hasInput)
			{
				rigidbody.velocity = Vector3.zero;
			}

			if (!hasInput && rigidbody.velocity.magnitude < .3f && grounded && onStair && !magnetJoint)
			{
				rigidbody.isKinematic = true;
			}
		}

		private void DoRotation()
		{
			if (blockMovement) return;
			float deltaTime = Mathf.Min(Time.deltaTime, .05f);

			rotationX +=
				-mouseDelta.y * lookSpeed * deltaTime * 10 *
				rotationModifier;
			rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
			xRotationTransform.localRotation = Quaternion.Euler(rotationX, 0, 0);
			yRotationTransform.rotation *=
				Quaternion.Euler(0,
					mouseDelta.x * lookSpeed * deltaTime *
					10 * rotationModifier, 0);
		}

		/// <summary> Completely removes the previous input influence on the players velocity if new input is provided </summary>
		/// <param name="_hasInput"> Is new input provided? </param>
		/// <returns> The velocity of the player without input influences (if input is provided - else the unmodified velocity) </returns>
		private Vector3 GetModifiedVelocity(bool _hasInput)
		{
			Vector3 rawVelocity = rigidbody.velocity;

			if (_hasInput)
			{
				Vector3 proj = Vector3.Project(rawVelocity, previousInputInfluence);
				if (proj.sqrMagnitude > previousInputInfluence.sqrMagnitude)
				{
					proj = proj.normalized * previousInputInfluence.magnitude;
				}

				return rawVelocity - proj;
			}

			return rawVelocity;
		}

		public bool IsGrounded()
		{
			Vector3 fPos = feet.transform.position;
			Vector3[] positions = new Vector3[4];
			Vector3 fwd = 0.4f * feet.bounds.extents.z * Vector3.forward;
			Vector3 right = 0.4f * feet.bounds.extents.x * Vector3.right;

			positions[0] = fPos + fwd + right;
			positions[1] = fPos + fwd - right;
			positions[2] = fPos - fwd + right;
			positions[3] = fPos - fwd - right;

			foreach (Vector3 v in positions)
			{
				if (Physics.Raycast(v, -transform.up, out RaycastHit ray, distanceToGround + .5f,
					    ~LayerMask.GetMask("Player"), QueryTriggerInteraction.Ignore))
				{
					onStair = ray.collider.CompareTag("Stairs");
					if (!magnetJoint)
					{
						isSwinging = false;
					}

					return true;
				}
			}

			onStair = false;
			return false;
		}

		private void OnJumpPerformed(InputAction.CallbackContext ctx)
		{
			if (!IsGrounded()) return;
			if (blockMovement) return;


			rigidbody.isKinematic = false;
			Vector3 vel = transform.InverseTransformDirection(rigidbody.velocity);
			vel.y = jumpForce;
			rigidbody.velocity = transform.TransformDirection(vel);
		}
	}
}