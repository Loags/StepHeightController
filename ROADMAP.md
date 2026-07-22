# Step Height Controller Roadmap

## Roadmap intent

This roadmap turns the current prototype into a stable, reusable Unity Asset Store tool. The work is deliberately split into a product iteration and a later release iteration:

- **Current iteration:** Phases 1-9, including the rebuilt sample experience in Phase 7.
- **Future separate task:** Phase 10, Asset Store release polish. Phase 10 remains visible for planning, but no submission, listing, marketing, licensing review, or release packaging work belongs in the current iteration.

The phases are sequential unless a task is explicitly described as parallel. Each phase must leave the package compiling without warnings or errors introduced by the product before the next phase begins.

## Implementation status — July 22, 2026

- **Phases 1-9:** Implemented and validated in Unity 6.5.2f1, with the trimmed release payload additionally batch-validated in Unity 6.0.67f1. The recorded focused suites pass 12/12 EditMode and 20/20 PlayMode. The earlier 14/14 PlayMode suite also passed with domain reload disabled; the newer motion-parity, consecutive-stair, slope-rejection, and completion-settling cases remain to be rerun in that configuration before release sign-off. The sample Play Mode smoke checks are clean, and the Asset Store Tools engineering preflight is all green.
- **Phase 10:** Still deferred. It remains a separate future release task and has not been started in this iteration.

## Product principles

- The runtime stepping system must not own or require player input.
- The public API must support custom controllers without requiring the included demo controller.
- Required component and prefab references are explicit setup contracts.
- Runtime code, editor tooling, samples, and documentation remain clearly separated.
- Physics queries use one consistent layer and trigger policy.
- Package content stays under one organized root folder and avoids project-only or redundant assets.
- Existing asset GUIDs and `.meta` files are preserved during reorganization wherever practical.

## Current iteration

### Phase 1 - Stabilize the existing implementation

**Goal:** Establish a dependable behavioral baseline before changing architecture.

Work:

- Fix initialization-order hazards between the input manager, demo movement controller, and step controller.
- Correct event subscription ownership so enable/disable cycles do not accumulate anonymous callbacks or retain stale listeners.
- Make stepping lifecycle cleanup deterministic when the component or GameObject is disabled mid-step.
- Apply collision masks consistently to candidate detection, top-surface checks, ground checks, ceiling checks, and completion checks.
- Remove unconditional debug output and keep optional diagnostics intentional and quiet by default.
- Replace avoidable per-query allocations where they affect the steady-state physics loop; make truncated non-alloc query results explicit rather than silently incomplete.
- Correct edge cases discovered in the existing flow, including zero movement, disabled colliders, invalid combined bounds, very low smooth factors, and self-collider hits.
- Capture the current supported behavior and known limitations before architectural changes.

Exit criteria:

- Repeated enable/disable cycles do not duplicate input callbacks or step requests.
- Disabling during a step leaves the controller in a valid, reusable state.
- No package-originated warnings or errors occur in the baseline sample after setup.
- Existing intended stair and ledge behavior remains reproducible.

### Phase 2 - Decouple the core from input

**Goal:** Make the stepping runtime independent of the Unity Input System and any specific movement controller.

Work:

- Remove input subscriptions, `InputManager`, `IMovementInputManager`, and generated input-map dependencies from the runtime stepping core.
- Change the core integration seam to accept movement intent or a world-space movement direction from its caller.
- Keep Rigidbody access and physics detection behind focused collaborators only where that separation improves testability or collider support.
- Move Input System integration into the sample/demo layer.
- Ensure the runtime assembly can compile and operate without `com.unity.inputsystem` when samples are not imported.

Exit criteria:

- A caller can drive stepping using its own input and movement stack.
- Runtime source has no reference to the demo input manager, generated input class, or Input System namespace.
- The demo still drives the same core through the new seam.

### Phase 3 - Establish the public API

**Goal:** Publish a small, intentional, documented integration contract.

Work:

- Define the supported request method using an explicit world-space movement direction or motion intent.
- Expose read-only state needed by integrators, such as whether a step is in progress.
- Define clear step outcomes for accepted, rejected, completed, interrupted, and cancelled attempts without exposing internal physics bookkeeping.
- Add only the lifecycle events needed for animation, audio, camera, or movement coordination.
- Define when collider data is refreshed and expose a refresh operation only if runtime collider changes are supported.
- Decide which types are public; keep detection helpers, wrappers, contact data, and implementation details internal where possible.
- Add XML documentation to every supported public type and member.
- Record compatibility and migration notes for the previous `CheckForStep()` integration.

Exit criteria:

- A third-party controller can integrate without reaching into fields or sample code.
- Public naming, nullability expectations, units, coordinate space, call timing, and side effects are documented.
- The API is narrow enough to support future internal rewrites without unnecessary breaking changes.

### Phase 4 - Reorganize the package

**Goal:** Produce a clean Asset Store package layout with clear assembly boundaries.

Target layout:

```text
Assets/<Publisher>/StepHeightController/
  Runtime/
  Editor/
  Samples/
  Documentation/
```

Work:

- Move all deliverable content beneath one named root folder.
- Separate runtime, editor-only code, sample scripts, sample input actions, scenes, prefabs, materials, and documentation.
- Add focused runtime, editor, and sample assembly definitions; keep optional dependencies out of the runtime assembly.
- Use a publisher-owned namespace that does not include Unity trademarks.
- Move the demo movement controller and all Input System assets out of runtime.
- Remove redundant, unused, project-generated, or unrelated assets from the intended export set.
- Keep paths below the Asset Store's 150-character limit.
- Preserve GUIDs and `.meta` files through every move so prefab and scene references survive.
- Define the exact export root and exclude `Packages`, `ProjectSettings`, Asset Store Tools, and other host-project content from the product payload.

Exit criteria:

- Importing only the product root into a clean supported project compiles successfully.
- Runtime users do not inherit sample-only dependencies.
- All prefabs, scenes, scripts, and assembly references survive the move.
- The export set contains one organized root and no duplicate or unusable files.

### Phase 5 - Improve configuration and Inspector UX

**Goal:** Make correct setup and tuning obvious without hiding invalid mandatory wiring.

Work:

- Group settings by detection, movement, collision filtering, and diagnostics.
- Give every setting a clear label, tooltip, unit, safe default, and meaningful range or minimum.
- Replace negatively worded toggles such as `disableStepHeight` with a clear enabled-state contract where compatibility permits.
- Define whether settings are per-component or reusable through a configuration asset; introduce a ScriptableObject only if reuse materially improves workflows.
- Add a focused custom Inspector only for information or controls that Unity's standard Inspector cannot express cleanly.
- Validate configuration once through `OnValidate`, `Awake`, or the Inspector as appropriate; do not add repeated runtime fallback logic for required setup.
- Make debug visualization editor-friendly, inexpensive when disabled, and visually distinguish detection, rejection, clearance, and target results.
- Preserve serialized values during field renames with Unity serialization migration attributes where needed.

Exit criteria:

- A new user can configure a typical capsule player without reading source code.
- Invalid numeric combinations are prevented or surfaced at one clear validation point.
- Upgrading from the prior component does not silently reset serialized tuning.

### Phase 6 - Strengthen collider and environment support

**Goal:** Make stepping predictable across common player colliders and level geometry.

Support matrix to define and verify:

- CapsuleCollider as the primary supported player shape.
- BoxCollider and SphereCollider where behavior can be made reliable.
- Compound player colliders with an explicit rule for included colliders.
- Offset, rotated, and non-uniformly scaled hierarchies within documented limits.
- Box, mesh, terrain, convex, and compound environment colliders where Unity physics queries support reliable results.
- Slopes, stair sequences, narrow treads, wall-adjacent steps, overhead clearance, moving or rotated surfaces, and trigger-heavy scenes.

Work:

- Replace bounds-only assumptions with collider-shape-aware measurements where necessary.
- Use explicit self-collider filtering without repeated LINQ allocations in physics hot paths.
- Make query capacity behavior robust in dense scenes.
- Apply the configured environment mask and trigger policy to every query.
- Distinguish walkable top surfaces from walls, ceilings, steep slopes, and false positives behind the player.
- Define behavior for moving platforms and Rigidbody obstacles; support them or document the limitation explicitly.
- Define minimum tread depth, maximum surface angle, clearance, and approach constraints.
- Keep unsupported shapes or setups explicit rather than silently falling back to guessed dimensions.

Exit criteria:

- The documented collider matrix matches observed behavior.
- No self-hit, ignored-layer, trigger, or ceiling regression remains in the validation scenarios.
- Unsupported configurations fail clearly during setup or are stated as limitations.

### Phase 7 - Rebuild the sample experience

**Goal:** Provide a focused, professional demonstration of setup, integration, and supported behavior.

Work:

- Rebuild the sample scene around the public API rather than internal implementation details.
- Include clearly labeled obstacles covering valid steps, over-height rejection, steep or shallow approach angles, slopes, narrow treads, ceilings, compound geometry, and relevant collider types.
- Keep the demo movement/input controller visibly sample-only and easy to replace.
- Provide a clean player prefab with sane defaults and no missing references.
- Add an in-scene or Inspector-accessible way to compare important configurations without turning the sample into product runtime code.
- Use only redistributable content owned by the publisher or content explicitly permitted for Asset Store demos.
- Verify prefab transforms, scale, colliders, materials, and render-pipeline setup.

Exit criteria:

- Opening the scene makes the product's purpose and limits understandable within minutes.
- Every supported configuration has at least one demonstrable scenario.
- The sample produces no package-originated Console warnings or errors after documented setup.

### Phase 8 - Rewrite documentation

**Goal:** Deliver comprehensive offline documentation for setup, integration, configuration, behavior, and troubleshooting.

Documents:

- Overview and feature boundaries.
- Installation/import and dependency requirements.
- Quick start using the included prefab and sample.
- Integration guide for custom Rigidbody movement controllers.
- Complete public API reference with call timing and code examples.
- Configuration reference with units, defaults, tuning guidance, and performance tradeoffs.
- Collider/environment support matrix and known limitations.
- Troubleshooting guide for missed steps, false positives, ceilings, masks, triggers, and movement conflicts.
- Upgrade/migration notes from the original implementation.
- Changelog and support/contact information placeholders for release completion.
- Third-Party Notices file only if the audited package includes third-party content; the final legal review remains Phase 10.

Work:

- Replace the current text README and fix encoding, grammar, outdated API references, and ambiguous claims.
- Keep a complete offline `.md`, `.txt`, `.pdf`, `.html`, or `.rtf` documentation path inside the product root.
- Ensure all examples compile against the established public API.
- State dependencies, supported Unity versions, render pipelines, platforms, and limitations accurately.
- Link to online videos only as supplemental material; do not embed video files in the package.

Exit criteria:

- A new integrator can install, configure, and call the system without inspecting source or the sample controller.
- Documentation and shipped behavior agree on API, dependencies, support, and limitations.
- Spelling, grammar, file links, and code examples pass review.

### Phase 9 - Add focused validation

**Goal:** Protect the public contract and the physics cases most likely to regress.

Work:

- Add EditMode tests for pure calculations, configuration constraints, candidate ranking, mask construction, and collider-dimension logic extracted from MonoBehaviours.
- Add focused PlayMode tests for accepted steps, over-height rejection, approach-angle rejection, ceiling clearance, ignored layers, triggers, self-colliders, interrupted steps, and repeated enable/disable cycles.
- Add representative tests for each collider/environment combination claimed as supported.
- Validate clean import and compilation in the minimum supported Unity version and the release-authoring version.
- Validate with Domain Reload disabled to protect Fast Enter Play Mode compatibility, especially if the release is authored with Unity 6.6 or newer.
- Check that runtime assemblies compile without sample and Input System content.
- Run the Asset Store Publishing Tools validator as an engineering preflight and record findings; final release validation and submission remain Phase 10.
- Perform a final static audit for namespace ownership, obsolete APIs, editor/runtime assembly leakage, missing `.meta` files, path length, duplicate content, and package-originated warnings/errors.

Exit criteria:

- Focused tests pass in the supported validation matrix.
- A clean import has no product-originated compile errors or warnings.
- Validator findings are either fixed in Phases 1-9 or explicitly assigned to the future Phase 10 release task.
- Claimed support in documentation is backed by a test or a recorded manual scenario.

## Future separate task - not part of the current iteration

### Phase 10 - Asset Store release polish

> **OUT OF SCOPE NOW.** Phase 10 must be scheduled and executed as a separate future task after Phases 1-9 are accepted. Do not begin release packaging, Publisher Portal work, marketing production, or submission during the current iteration.

Future work:

- Re-read the live Asset Store Submission Guidelines at release time and reconcile any changes since this roadmap was written.
- Freeze the supported Unity version, render-pipeline, platform, and dependency matrix.
- Perform the final legal and provenance audit for all source, meshes, materials, fonts, audio, images, and other third-party content.
- Create or finalize `Third-Party Notices.txt` and license files when required, and mirror required disclosures in the listing.
- Prepare the exact clean export payload and verify size, paths, root folder, metadata, `.meta` completeness, and absence of redundant/project-only content.
- Run the final Asset Store Publishing Tools validation in a clean project and resolve all findings.
- Produce listing copy, technical details, keywords, screenshots, artwork, and optional external video that accurately represent the shipped product.
- Disclose dependencies, limitations, render-pipeline requirements, supported Unity versions, and any setup requirements in the Publisher Portal.
- Verify publisher email, support links, and website are active.
- Preview the listing, submit the package, and handle reviewer feedback as release work.

Release gate:

- Phase 10 starts only after Phases 1-9 have signed-off acceptance results.
- Submission approval is never assumed; the package remains subject to Unity's review.

## Asset Store guideline alignment

The roadmap was checked against the [Unity Asset Store Submission Guidelines](https://assetstore.unity.com/publishing/submission-guidelines), last updated May 20, 2026 when this roadmap was prepared. Relevant requirements include:

| Guideline area | Roadmap coverage |
| --- | --- |
| Professional quality; no package-originated errors or warnings (1.1.a-b) | Phases 1, 7, and 9 |
| Dependencies and limitations disclosed (1.1.c, 3.1.a) | Phases 4 and 8; final listing disclosure in Phase 10 |
| Demo content where applicable (1.1.f) | Phase 7 |
| Unity version and render-pipeline compatibility (1.3) | Phases 4, 7, 8, and 9; final declaration in Phase 10 |
| Single root, organized content, no redundant files, path limits (2.1) | Phase 4; final export audit in Phase 10 |
| Comprehensive documentation in an accepted format (2.3) | Phase 8 |
| User-declared namespaces, readable and consistent source, supported APIs (2.5) | Phases 1-4 and 9 |
| Fast Enter Play Mode support when applicable (2.5.h) | Phase 9 |
| Accurate product marketing and publisher information (3.1, 4.1) | Reserved for Phase 10 |
| Asset Store validation, upload, and review workflow | Engineering preflight in Phase 9; final validation and submission in Phase 10 |

The guidelines are a live release dependency. This mapping is planning guidance, not a substitute for the final Phase 10 compliance review.
