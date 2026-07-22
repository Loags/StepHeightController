# Step Height Controller

Step Height Controller adds controlled step-up movement to a Rigidbody character. It detects a walkable top surface in the caller's movement direction, verifies that the configured player colliders fit at the target, and moves the Rigidbody smoothly onto the surface.

The runtime does not read input and does not require the Unity Input System. The included first-person controller and input actions are sample content only.

## Requirements

- Unity 6.0 or newer. The release project is authored with Unity 6.5.2f1 and validated with Unity 6.0.67f1.
- Unity Physics (`com.unity.modules.physics`).
- A Rigidbody and at least one enabled, non-trigger CapsuleCollider, BoxCollider, or SphereCollider in the player hierarchy.
- A movement controller that can call the stepping API from its physics loop.
- The optional sample uses Input System 1.19.0 and the URP version matching the Editor: validated with URP 17.0.4 on Unity 6.0 and URP 17.5.0 on Unity 6.5. The runtime assembly has no Input System or render-pipeline dependency.

## Package layout

```text
Assets/LB/StepHeightController/
  Runtime/         Input-independent runtime and public API
  Editor/          Custom Inspector and setup feedback
  Samples/         Demo scene, player, input, and materials
  Documentation/   Offline setup, API, support, and troubleshooting
```

## Quick start

1. Add a Rigidbody to the player root.
2. Add a CapsuleCollider, BoxCollider, or SphereCollider to the root or a child.
3. Add `StepHeightController` to the same GameObject as the Rigidbody.
4. Configure Maximum Step Height and Ignored Layers.
5. While the player is grounded and moving, call `TryStep` from `FixedUpdate` with the desired world-space movement direction.

```csharp
using LB.StepHeight;
using UnityEngine;

public sealed class PlayerMovement : MonoBehaviour
{
    [SerializeField] private StepHeightController stepController;
    [SerializeField] private Rigidbody body;

    private Vector3 _desiredDirection;

    private void FixedUpdate()
    {
        bool grounded = CheckGrounded();
        if (grounded && _desiredDirection.sqrMagnitude > 0.0001f)
        {
            StepAttemptResult result = stepController.TryStep(_desiredDirection);
            if (result == StepAttemptResult.Started)
            {
                // Avoid issuing competing position changes while IsStepping is true.
            }
        }
    }

    private bool CheckGrounded() => true;
}
```

The caller remains responsible for input, grounded checks, ordinary velocity, jumping, animation, and any movement arbitration. Avoid writing a competing Rigidbody position while `IsStepping` is true. For dynamic Rigidbody controllers, suspend additional downward acceleration during the step or clear its downward vertical component so gravity does not accumulate into a visible landing correction.

## Configuration

### Detection

- **Maximum Step Height (m):** Largest vertical rise accepted as a step. Default: `0.5`.
- **Minimum Step Height (m):** Smaller changes are treated as ground variation. Default: `0.02`.
- **Maximum Surface Angle (degrees):** Largest angle between the candidate surface normal and player up. Default: `50`.
- **Maximum Approach Angle (degrees):** Largest horizontal angle between movement intent and an obstacle. Default: `65`.
- **Detection Distance (m):** Additional horizontal search reach beyond the collider bounds. Default: `0.15`.
- **Surface Probe Inset (m):** Distance beyond an obstacle face used to probe its top. Default: `0.1`.

### Movement

- **Step Speed (m/s):** Maximum average Rigidbody movement speed along the accepted step path. Default: `4.5`.
- **Minimum Step Duration (s):** Lower timing bound that keeps short steps smooth without slowing consecutive stairs. Default: `0.12`.
- **Landing Inset (m):** Distance beyond the detected obstacle face used for final placement. Default: `0.1`.
- **Clearance (m):** Separation used for final-position overlap checks. Default: `0.02`.

### Collision filtering

- **Ignored Layers:** Excluded from candidate, top-surface, and clearance queries. Put the player layer here in addition to the built-in self-collider filter.
- **Trigger Interaction:** Defaults to Ignore. Use another policy only when triggers deliberately describe step geometry.
- **Include Child Colliders:** Treat supported colliders below the Rigidbody root as one compound player shape.

### Diagnostics

Enable Scene Visualization and select the component to display the detection region, most recent direction, acceptance state, and target. Diagnostics do not write routine messages to the Console.

## Sample

Open `Samples/Scenes/StepHeightDemo.unity` and enter Play Mode.

- Move: WASD
- Look: Mouse
- Sprint: Left Shift
- Jump: Space

The scene labels sections by expected result: valid 0.30 m steps, boundary 0.50 m steps, rejected over-height obstacles, narrow and standard stairs, irregular geometry, a rotated step, a low ceiling, a slope, and an ignored trigger.

## Public API

See [Public API](API.md) for request results, lifecycle events, cancellation, collider-cache refresh, and an integration example.

## Collider and environment support

| Setup | Support | Notes |
| --- | --- | --- |
| CapsuleCollider player | Primary | Offset, direction, rotation, and lossy scale are respected. |
| BoxCollider player | Supported | Center, rotation, size, and non-uniform scale are respected. |
| SphereCollider player | Supported | The largest scaled axis defines the world radius. |
| Compound primitive player | Supported | Every enabled, non-trigger supported shape is checked at the target. |
| Child player colliders | Supported | Enable Include Child Colliders. |
| MeshCollider player | Not supported | Keep meshes visual-only and use primitive physics colliders. |
| Environment colliders | Supported | Primitive, MeshCollider, TerrainCollider, and compound geometry can participate. |

The Rigidbody and `StepHeightController` must be on the same root. The root must remain aligned to world up, although child colliders and environment geometry may be offset or rotated. Candidate selection uses collider closest points and raycasts; the selected top must satisfy Maximum Surface Angle.

All queries consistently apply Ignored Layers and Trigger Interaction. Player colliders and colliders attached to the same Rigidbody are filtered automatically.

## Troubleshooting

### TryStep returns NoCandidate

- Confirm the caller is close enough and the world-space direction points toward the obstacle.
- Check Detection Distance, Ignored Layers, Maximum Step Height, Maximum Surface Angle, and Maximum Approach Angle.
- Reduce Surface Probe Inset carefully when valid treads are very thin.

### TryStep returns Blocked

A valid top was found, but the configured player shapes do not fit at the target. Check low ceilings, side walls, unused child colliders, Landing Inset, and Clearance. Enable Scene Visualization to inspect the accepted direction and target.

### The player intersects the step edge

Increase Landing Inset in small increments. Surface Probe Inset controls top detection; Clearance only changes the final overlap query and does not raise the landing position.

### The player drops, jitters, or fights the step

Do not write competing Rigidbody positions while `IsStepping` is true. For dynamic bodies, suspend additional downward acceleration during the step so gravity does not accumulate into a landing correction. The included sample demonstrates this ownership handoff.

### A slope starts repeated tiny steps

The approach probe rejects walkable faces. Ensure the slope collider has accurate surface normals and is not constructed from many vertical micro-faces.

### Runtime collider changes are ignored

Call `RefreshColliderCache()` after adding, removing, enabling, disabling, or resizing player colliders. Refreshing intentionally cancels an active step.

### The sample input does not respond

Install Input System 1.19.0 or a compatible version and ensure Active Input Handling includes the Input System. This dependency belongs only to the sample assembly.

## Limitations

- Physics queries are capped at 256 results. Saturation returns `QueryCapacityExceeded`; use collision layers or simpler local collision geometry.
- An accepted target is sampled once. Fast moving or rotating platforms are not continuously retargeted, and their velocity is not transferred to the player.
- Highly concave MeshCollider environments can expose a different top collider than their approach face. Simple collision proxies are the most predictable setup.
- This is a step-up system, not climbing, vaulting, ledge grabbing, or complete character locomotion.
