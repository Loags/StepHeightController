# Upgrading from 1.0.0 to 2.0.0

Version 2.0.0 changes the integration API. This complete release package requires Unity 6.5 or newer. Back up your project or commit it to source control before updating. Upgrade in a copy first and check your own player movement and scenes before adopting the update.

## Custom movement integrations

- Change imports from `LB.Player.Movement.StepHeight` to `Loags.StepHeight`. The included demo controller now uses `Loags.StepHeight.Samples`.
- Call `TryStep(worldSpaceMovementDirection)` from your grounded physics update. Your controller owns input, grounded checks, ordinary movement and jumping.
- While `IsStepping` is true, avoid competing Rigidbody position changes. Avoid accumulating additional downward acceleration during the step; the sample demonstrates this handoff.
- Use the returned result and lifecycle events to coordinate animation, audio or camera feedback. See [the API guide](API.md).
- Replace `CheckForStep()` calls with `TryStep(worldSpaceMovementDirection)`. The obsolete velocity-based adapter has been removed.

## Earlier 2.0 previews

The package now lives at `Assets/Loags/StepHeightController`. Earlier preview namespaces and assemblies used `LB.StepHeight`; update imports and assembly references to `Loags.StepHeight` and `Loags.StepHeight.Runtime`, `.Editor` or `.Samples` as appropriate. Product script and asset GUIDs are preserved, and Unity type-migration attributes describe the previous component identities. External source code still needs its imports and assembly references updated.

In a backed-up copy of an earlier preview project:

1. Before importing, use Unity's Project window to move the existing `Assets/LB/StepHeightController` folder into `Assets/Loags/StepHeightController`. Move the folder with its metadata; preserve your custom assets.
2. Import the update, then update your own source imports and assembly references. Do not save scenes during a temporary missing-script state.
3. Keep one product root. Check for obsolete `LB.StepHeight.*.asmdef` files or a generated `InputMap.cs` left at the former path; remove only stale product files after confirming the new assemblies and wrapper exist. Unity package imports do not reliably delete retired files.
4. Remove the five retired sample assets listed below only if they remain and your own content does not use them. Reopen and check your scenes and prefabs before adopting the update.

This migration is not verified against every historical preview. For heavily customized older layouts, use a clean project import as the reference.

The retired assets are `Samples/Scenes/StepHeightDemo.unity`, `Samples/Prefabs/Player.prefab`, and `Samples/Materials/Mat_Brown.mat`, `Mat_Grey.mat`, `Mat_Player.mat`. These five assets are removed. Use `FirstPersonDemo`, `ThirdPersonDemo`, and their named player prefabs.

## Existing scenes and prefabs

The main component script GUID is preserved, and selected renamed fields retain migration attributes. This does not guarantee a complete automatic upgrade of custom setups. Review required Rigidbody and collider references, layers, detection limits and movement settings. Retune step speed and minimum duration for your controller.

Legacy package files may remain at their former paths after a Unity package update. In your backed-up project, compare old and new package folders and remove obsolete product files deliberately if they cause duplicate types or stale references. Do not delete your own assets or unrelated packages.

The complete package now includes an Input System sample with URP materials. Install the documented dependencies and use Input System input handling for the sample. The core runtime remains independent of input and rendering packages.

This update has not been verified as an in-place import over every historical 1.0.0 setup. Use [Quick start](README.md) and the new sample as the integration reference.
