# Upgrading from 1.0.0 to 2.0.0

Version 2.0.0 changes the integration API. This complete release package requires Unity 6.5 or newer. Back up your project or commit it to source control before updating. Upgrade in a copy first and check your own player movement and scenes before adopting the update.

## Custom movement integrations

- Change imports from `LB.Player.Movement.StepHeight` to `LB.StepHeight`. The included demo controller now uses `LB.StepHeight.Samples`.
- Call `TryStep(worldSpaceMovementDirection)` from your grounded physics update. Your controller owns input, grounded checks, ordinary movement and jumping.
- While `IsStepping` is true, avoid competing Rigidbody position changes. Avoid accumulating additional downward acceleration during the step; the sample demonstrates this handoff.
- Use the returned result and lifecycle events to coordinate animation, audio or camera feedback. See [the API guide](API.md).
- `CheckForStep()` remains an obsolete adapter that derives direction from Rigidbody velocity. Update callers to `TryStep` for explicit movement intent.

## Existing scenes and prefabs

The main component script GUID is preserved, and selected renamed fields retain migration attributes. This does not guarantee a complete automatic upgrade of custom setups. Review required Rigidbody and collider references, layers, detection limits and movement settings. Retune step speed and minimum duration for your controller.

Legacy package files may remain at their former paths after a Unity package update. In your backed-up project, compare old and new package folders and remove obsolete product files deliberately if they cause duplicate types or stale references. Do not delete your own assets or unrelated packages.

The complete package now includes an Input System sample with URP materials. Install the documented dependencies and use Input System input handling for the sample. The core runtime remains independent of input and rendering packages.

This update has not been verified as an in-place import over every historical 1.0.0 setup. Use [Quick start](README.md) and the new sample as the integration reference.
