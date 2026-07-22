# Validation matrix

## Verification record

Validated on July 22, 2026 with Unity 6.5.2f1 and Unity 6.0.67f1:

- Runtime, Editor, sample, and test assemblies compiled with no product-originated errors or warnings.
- EditMode: 12/12 focused cases passed.
- PlayMode: 20/20 focused tests passed with standard Play Mode settings.
- Fast Enter Play Mode: 14/14 focused PlayMode tests passed with domain reload disabled before the motion-parity additions; rerun this configuration before release sign-off.
- `StepHeightDemo` entered Play Mode with no product-originated warnings or errors under both standard and domain-reload-disabled settings.
- Asset Store Tools current-project validation completed with every check green (manual validator run).
- The runtime assembly has no Input System or render-pipeline reference; those dependencies remain sample-only.
- Unity 6.0.67f1 batch validation completed with exit code 0 for the isolated release payload using Input System 1.19.0 and URP 17.0.4.
- The Unity 6.0 batch check loaded the demo scene and player prefab with no missing scripts, resolved both sample material shaders, and reported no product-originated compiler, shader, or import findings.

## Automated EditMode coverage

- Height boundaries.
- Step-duration calculation and minimum physics duration.
- Ignored-layer mask construction.
- Direction validation.
- Public event data integrity.
- Minimum-duration enforcement for short step paths.
- Linear horizontal and smoothed vertical trajectory calculation.

## Automated PlayMode coverage

- Supported CapsuleCollider, BoxCollider, and SphereCollider initialization.
- Invalid direction and disabled-state results.
- Accepted low box step.
- Over-height rejection.
- Ceiling/final-position blockage.
- Ignored-layer and trigger behavior.
- Cancellation on component disable.
- Repeated enable/disable without retained input callbacks in runtime.
- Smooth minimum-duration ascent, exact final placement, and immediate re-trigger rejection.
- Landing placement beyond the detected obstacle face.
- Immediate consecutive-step acceptance for aligned stair colliders.
- Walkable slope rejection without repeated micro-step events.
- Dynamic Rigidbody vertical-velocity cleanup after step completion.
- Direct support placement and bounded settling when gravity resumes after completion.

## Development motion debugger

Open `Tools > Step Height Controller > Open Motion Debugger`, select the player, and enter Play Mode.

- Cyan Scene View segments show active step movement.
- Orange segments show movement between accepted steps.
- Green spheres show completed targets; yellow is active and red is cancelled.
- Each target label records step height and the completion-to-next-start gap.
- The window shows Rigidbody velocity, horizontal and vertical velocity, step duration, gap duration, distance and average speed travelled during the gap, and post-step drop/rise during a bounded 100 ms settling window.
- Copy Recorded Steps exports the complete chronological trace and latest settling metrics to the system clipboard.

The debugger is stored under `Assets/_Development` and is not part of the customer payload.

## Manual sample checks

Open `Samples/Scenes/StepHeightDemo.unity` and verify:

- 0.30 m steps and stairs are traversable.
- 0.50 m boundary cases match configured clearance and approach.
- 0.75 m and 1.00 m obstacles are rejected.
- Rotated step, slope, ceiling, and trigger labels match behavior.
- Scene Visualization is quiet when disabled and useful when enabled.
- No product-originated warnings or errors appear after documented setup.

## Compatibility checks

- Minimum-version validation: Unity 6.0.67f1 with URP 17.0.4.
- Authoring validation: Unity 6.5.2f1 with URP 17.5.0.
- Runtime assembly compiles without Input System or render-pipeline references.
- Sample assembly owns the Input System dependency.
- Domain Reload disabled validation is included in the recorded focused PlayMode pass.

The final clean-project import, release payload validation, and submission checks remain gates for the separate Phase 10 release task.
