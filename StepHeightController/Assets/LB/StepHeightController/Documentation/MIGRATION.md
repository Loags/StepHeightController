# Migration from the original prototype

## Namespace changes

Replace prototype namespaces such as:

```csharp
using LB.Player.Movement.StepHeight;
```

with:

```csharp
using LB.StepHeight;
```

The main script GUID is preserved, so existing prefab and scene component references migrate with the asset move.

## Input ownership

The runtime no longer creates or subscribes to `InputMap`, `InputManager`, or `IMovementInputManager`. Remove the old InputManager prefab from scenes. Own input in the movement controller and pass world-space intent explicitly:

```csharp
stepController.TryStep(desiredWorldDirection);
```

The old parameterless `CheckForStep()` remains obsolete for source migration and uses Rigidbody velocity. Replace it rather than building new integrations around it.

## Serialized settings

Unity serialization migration attributes preserve the original values for:

- Step Up Smooth Factor to Step Speed.
- Step Up Angle Threshold to Maximum Approach Angle.
- Disable Step Height to the internal disabled state.
- Demo movement speed, look, jump, and transform references.

Step Up Smooth Factor migrates to Step Speed for serialization continuity, but the values use different units. The
new Minimum Step Duration defaults to `0.12` seconds so short steps receive enough physics updates without slowing a
continuous stair sequence. Horizontal travel remains linear while the vertical lift uses SmoothStep. Landing Inset now
controls final placement independently from Surface Probe Inset.

## Collider setup

Replace MeshCollider player shapes with CapsuleCollider, BoxCollider, or SphereCollider. The included Player prefab now uses a root CapsuleCollider. Environment MeshColliders remain supported.

## Movement arbitration

Use `IsStepping` to avoid competing Rigidbody position writes. Subscribe to lifecycle events when animation, sound, or camera systems need step state.

## Package paths

Content now resides below:

```text
Assets/LB/StepHeightController/
```

Runtime, editor, sample, and documentation content is separated inside the export root. Development-only test assemblies remain outside the package and are not part of the customer payload. Do not reference sample types from production code.
