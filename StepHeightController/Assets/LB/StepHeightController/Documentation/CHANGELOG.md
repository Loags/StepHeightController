# Changelog

## Unreleased

- Restored the original step timing character with a configurable minimum duration.
- Added a dedicated landing inset so accepted targets advance beyond the detected obstacle face.
- Kept the step active until the final Rigidbody position has been processed by physics.
- Kept horizontal travel linear across each step while smoothing the vertical lift for continuous stair movement.
- Made sample look sensitivity frame-rate independent and updated its default to `5`.
- Rejected walkable approach faces so ramps are not interpreted as repeated small steps.
- Prevented the sample Rigidbody from accumulating downward gravity velocity during active step movement.
- Cleared dynamic Rigidbody vertical velocity at completion to prevent post-step overshoot or settling.
- Landed accepted targets directly on their support surface instead of adding query clearance as physical height.
- Prevented the final dynamic Rigidbody placement from generating a post-step contact impulse.

## Unreleased

### Added

- Input-independent `TryStep(Vector3)` public API.
- Step lifecycle state, events, cancellation, and collider refresh operations.
- CapsuleCollider, BoxCollider, SphereCollider, and compound-player clearance support.
- Consistent layer and trigger filtering with bounded non-alloc query buffers.
- Custom Inspector with units, grouped configuration, setup feedback, and runtime controls.
- Focused sample scenarios, with automated validation retained outside the release payload.
- Offline API, collider support, troubleshooting, and migration documentation.
- Unity 6.0.67f1 compatibility validation for runtime, editor tooling, sample scripts, scene, prefab, and URP 17.0.4 materials.

### Changed

- Reorganized all deliverable content below one package root.
- Moved Input System and first-person movement code into the sample assembly.
- Replaced the sample player MeshCollider with a primitive CapsuleCollider.
- Replaced routine debug logging with optional Scene visualization.

### Removed

- Runtime InputManager and generated-input dependencies.
- Prototype wrapper and manager interfaces that were not part of the supported API.
- Redundant sample InputManager prefab and player-collider FBX.
