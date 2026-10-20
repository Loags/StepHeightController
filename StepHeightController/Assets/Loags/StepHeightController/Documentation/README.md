# Step Height Controller

Step Height Controller adds controlled step-up movement to a Rigidbody character. It detects a walkable top surface in the caller's movement direction, verifies that the configured player colliders fit at the target, and moves the Rigidbody smoothly onto the surface.

The runtime does not read input. The included first-person and third-person Rigidbody players, input actions and cameras are sample content only.

## Requirements

- Unity 6.5 or newer for this complete release package. Demo packages: Input System 1.19.0+, Cinemachine 3.x from 3.1.7 (or Unity 6.6's bundled 6.6.x), URP 17.5.0+ and Unity UI 2.5.0+. The setup window checks compatible versions before opening a demo.
- Unity Physics (`com.unity.modules.physics`).
- A Rigidbody and at least one enabled, non-trigger CapsuleCollider, BoxCollider, or SphereCollider in the player hierarchy.
- A movement controller that can call the stepping API from its physics loop.
- Open **Tools > StepHeightController > Setup & Demos** to check demo packages, open Package Manager, and follow the render-pipeline and input settings guidance. Missing demo dependencies disable the sample assembly; the stepping runtime and setup windows remain available.
- The runtime assembly itself has no Input System or render-pipeline dependency.

## Package layout

```text
Assets/Loags/StepHeightController/
  Runtime/         Input-independent runtime and public API
  Editor/          Setup windows, presets and simplified Inspector
  Samples/         Two demos, ready-to-play players and shared course
  Documentation/   Offline setup, API, support, and troubleshooting
```

## Quick start

Updating from 1.0.0? Read [Upgrading to 2.0](UPGRADE.md) before importing into an existing project.

For a guided start, open **Tools > StepHeightController > Setup & Demos** and follow the [quick start](Setup.md). **Configure Rigidbody Player** previews reversible component and physics changes. Your existing movement script remains responsible for calling the stepping API.

1. Add a Rigidbody to the player root.
2. Add a CapsuleCollider, BoxCollider, or SphereCollider to the root or a child.
3. Add `StepHeightController` to the same GameObject as the Rigidbody.
4. Configure Maximum Step Height and Ignored Layers.
5. While the player is grounded and moving, call `TryStep` from `FixedUpdate` with the desired world-space movement direction.

```csharp
using Loags.StepHeight;
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
- **Detection Distance (m):** Additional horizontal search reach beyond the collider bounds. Default: `0.3`.
- **Surface Probe Inset (m):** Distance beyond an obstacle face used to probe its top. Default: `0.1`.

### Movement

- **Step Speed (m/s):** Baseline ascent and forward travel pace, including slow starts and brief movement taps. Faster incoming movement is retained unless the ascent or minimum duration requires slowing. Default: `4.5`.
- **Minimum Step Duration (s):** Lower timing bound that keeps short steps smooth without slowing consecutive stairs. Default: `0.12`.
- **Landing Inset (m):** Distance beyond the detected obstacle face used for final placement. Default: `0.1`.
- **Probe Margin (m):** Vertical padding around the surface probe. Default: `0.02`. Player poses along the ascent use a fixed `0.001` m contact tolerance.

### Collision filtering

- **Ignored Layers:** Excluded from candidate, top-surface, and clearance queries. Put the player layer here in addition to the built-in self-collider filter.
- **Trigger Interaction:** Defaults to Ignore. Use another policy only when triggers deliberately describe step geometry.
- **Include Child Colliders:** Treat supported colliders below the Rigidbody root as one compound player shape.

### Diagnostics

Enable Scene Visualization and select the component to display the detection region, most recent direction, acceptance state, and target. Diagnostics do not write routine messages to the Console.

## Sample

Open **Setup & Demos**, choose the first-person or third-person demo, and enter Play Mode. Both scenes reference the same obstacle-course prefab. Separate ready-to-play player prefabs include explicitly wired input, animation, HUD and Cinemachine cameras.

- Move: WASD
- Look: Mouse
- Sprint: Left Shift
- Jump: Space
- Toggle stepping on this player: T
- Reset player and camera: R
- Release cursor: Esc; click the game to resume

Compare enabled and disabled stepping at labeled stations for valid steps, height limits, narrow and standard stairs, angled approaches and blocked clearance. Jump cancels an active step immediately. Reset clears the active step, velocities, queued jump, animation and camera history; it preserves the stepping toggle.

The first-person player renders animated legs below a stable camera. The third-person player uses a free orbit camera and faces its movement direction. These are Rigidbody demos using Starter Assets visuals, not integrations with Unity's original CharacterController scripts. No gamepad or mobile controls are included.

## Support and release history

For questions, use the publisher's [support contact](https://www.loags.de/contact/). See [release history](CHANGELOG.md), [upgrade guidance](UPGRADE.md), and [third-party notices](Third-Party%20Notices.txt).

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

A valid top was found, but the configured player shapes do not fit along the planned ascent. Check low ceilings, side walls, unused child colliders, and Landing Inset. Enable Scene Visualization to inspect the accepted direction and target.

### The player intersects the step edge

Increase Detection Distance to begin lifting earlier, then check Landing Inset. Surface Probe Inset controls top detection; Probe Margin pads that probe and does not raise the landing position. The ascent checks sampled player poses with a small contact tolerance.

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
