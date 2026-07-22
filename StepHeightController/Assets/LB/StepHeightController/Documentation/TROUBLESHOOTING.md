# Troubleshooting

## TryStep returns InvalidDirection

Pass world-space movement intent, not raw input axes. Project camera-relative input into the world before calling. Do not call with a zero vector.

## TryStep always returns NoCandidate

- Confirm the caller invokes it while close to the obstacle.
- Confirm the requested movement direction points toward the obstacle.
- Increase Detection Distance slightly.
- Check that the obstacle layer is not in Ignored Layers.
- Check that the top is within Maximum Step Height and Maximum Surface Angle.
- For thin treads, reduce Surface Probe Inset.

## TryStep returns Blocked

A geometrically valid top was found, but one of the compound player shapes overlaps environment geometry at the predicted target. Check overhead clearance, side walls, unused child colliders, and the Clearance value. Enable Scene Visualization to inspect the target.

## The player intersects the step edge

Increase Landing Inset in small increments. Surface Probe Inset controls top-surface detection rather than final placement. Increase Clearance only when the final overlap check needs additional separation. Ensure the player uses primitive physics colliders and that the Rigidbody root scale is sensible.

## Stepping looks like two upward pops

Increase Minimum Step Duration so the movement is distributed across more physics updates. The default `0.12` seconds keeps short steps above the two-tick range without introducing a pronounced pause between consecutive stairs. If `StepStarted` is raised twice for one ledge, confirm the caller does not invoke multiple controller instances.

## Consecutive stairs feel like stop-and-start movement

Step movement keeps horizontal progress linear while smoothing only the vertical lift. If stairs still feel separated, reduce Minimum Step Duration slightly. Do not increase Landing Inset to solve timing; it controls placement rather than cadence.

## The Rigidbody drops or jitters immediately after a step

Do not accumulate additional downward acceleration while `IsStepping` is true. A dynamic Rigidbody can otherwise retain gravity velocity while `MovePosition` raises it, then apply that velocity as soon as the step completes. The sample suspends gravity and clears downward vertical velocity during the active step. The runtime removes the remaining vertical velocity when a dynamic-body step completes so the accepted target does not produce a second upward or downward correction.

## A walkable slope repeatedly starts tiny steps

The approach probe rejects faces within Maximum Surface Angle because they are continuous walkable ground rather than step risers. Ensure the slope collider has an accurate surface normal and is not built from many vertical micro-faces.

## The player rises but ordinary movement fights the step

Do not write a competing Rigidbody position while `IsStepping` is true. The sample continues gravity but suspends horizontal velocity changes during a step.

## Steps work from the wrong direction

Pass desired movement direction rather than current velocity when knockback, conveyors, or external forces can dominate velocity. Lower Maximum Approach Angle for stricter directional selection.

## Trigger volumes become steps

Set Trigger Interaction to Ignore. This is the default.

## Runtime collider changes are ignored

Call `RefreshColliderCache()` after enabling, disabling, adding, removing, or resizing player colliders. The refresh intentionally cancels any active step.

## UnsupportedCollider or an initialization exception

Add an enabled, non-trigger CapsuleCollider, BoxCollider, or SphereCollider to the root or a child. MeshCollider is not a supported player shape.

## QueryCapacityExceeded

More than 256 results occupied a local overlap or ray query. Use collision layers to exclude unrelated objects, merge unnecessarily fragmented collision, or reduce local collider density.

## Sample input does not respond

The sample requires the Unity Input System package. The runtime does not. Ensure Input System 1.19.0 or a compatible version is installed and the project's active input handling includes the Input System.
