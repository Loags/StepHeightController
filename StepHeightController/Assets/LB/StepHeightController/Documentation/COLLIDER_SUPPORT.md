# Collider and environment support

## Player colliders

| Setup | Support | Notes |
| --- | --- | --- |
| CapsuleCollider | Primary | Full offset, direction, rotation, and lossy-scale geometry is used for clearance. |
| BoxCollider | Supported | Center, rotation, size, and non-uniform scale are respected. |
| SphereCollider | Supported | Uses the largest scaled axis as the world radius. |
| Compound supported shapes | Supported | Every enabled, non-trigger supported shape is checked at the target. |
| Child colliders | Supported | Enable Include Child Colliders; colliders may be offset and rotated. |
| MeshCollider as player shape | Not supported | Use a primitive physics collider and keep meshes visual-only. |
| Trigger-only player | Not supported | At least one non-trigger supported collider is required. |

The Rigidbody and StepHeightController belong on the same root. The root must remain aligned to world up; rotations on child collider transforms are supported.

## Environment colliders

Candidate selection uses Unity Collider closest points and raycasts, so BoxCollider, CapsuleCollider, SphereCollider, convex and non-convex MeshCollider, TerrainCollider, and compound environment objects can participate. The candidate top must have a normal within Maximum Surface Angle.

All queries consistently use Ignored Layers and Trigger Interaction. Player colliders and colliders attached to the same Rigidbody are filtered even if the player layer is not ignored.

## Tested scenario categories

- Isolated steps below, at, and above the configured maximum.
- Standard and narrow stair sequences.
- Rotated BoxCollider steps.
- Walkable and too-steep top surfaces.
- Low ceilings and blocked final positions.
- Trigger geometry with Ignore policy.
- Compound CapsuleCollider, BoxCollider, and SphereCollider players.
- Dense overlap results up to the documented query capacity.
- Repeated enable/disable and collider-cache refresh cycles.

## Limits

- Physics queries cap at 256 results. Saturation returns `QueryCapacityExceeded`; increase scene filtering or simplify the local collision density.
- The target is sampled when the request is accepted. Fast moving/rotating platforms can move away during interpolation.
- The system does not transfer platform velocity to the player.
- Very thin treads may not extend beyond Surface Probe Inset. Lower that value carefully for narrow geometry.
- Highly concave mesh geometry can expose a different top collider than its approach face; use simple collision proxies for the most predictable result.
