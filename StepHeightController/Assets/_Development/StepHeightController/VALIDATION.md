# Development validation

Customer root: `Assets/Loags/StepHeightController`. Development-only tools, tests and reports are excluded from customer exports.

## Loags migration checks — October 20, 2026

- Unity 6000.5.2f1: clean compilation, 12/12 EditMode and 20/20 PlayMode tests passed.
- First-person and third-person demo probes: 17/17 checks passed in each scene, including enabled/disabled stations, jump/disable cancellation and reset. The local reset probe checks the immediate reset before allowing normal physics settling.
- Both customer windows open and bind correctly. Rigidbody setup, repeat application, preservation of existing settings, Undo and independent presets passed. Customer exports have no development dependencies.
- The archive audit passes: 85 paths/GUIDs, 65 files, only the four declared registry dependencies. All retained product GUIDs are preserved; the five retired demo assets have no remaining product references.
- Official Asset Store validation reports no compilation errors. Its existing model-orientation advisory for the Starter Assets ramp/stairs remains; their course placement is preserved.

Unity 6000.6.0f1 combined-source validation passes with Step Height Controller added before TweenHelper: all seven product assemblies compile, all three scenes and seven prefabs have no missing scripts, and both products' customer windows open. Step demo readiness is true with Cinemachine 6.6.0, Input System 1.20.0, URP 17.6.0 and uGUI 2.6.0. TweenHelper's explicit setup enables its own dependency symbol; dependency versions, rendering and physics settings remain unchanged by adding the products.

Both Step demo camera smoke checks also pass in that combined project: frames advance, the live Cinemachine Brain follows player movement, orbit and reset work, the humanoid is valid, and the Console has no errors or warnings. These are focused smoke checks, not a repeat of the full movement suite on Unity 6.6.

This is source-composition evidence, separate from archive import. Unity 6.6's missing-signature prompt prevented unattended import of the locally generated archives; neither archive import order nor the reverse source-addition order is claimed as a pass.

## Manual demo review

Open `FirstPersonDemo` and `ThirdPersonDemo` from `Tools > StepHeightController > Setup & Demos`.

- Check walking, sprinting, jumping, sideways/backward approaches and smooth step ascent.
- Compare stepping live with T and reset with R.
- Confirm valid steps, the 0.50 m boundary, rejected height limits, narrow/standard stairs, angled approaches, blocked clearance, the slope and ignored trigger.
- Check animations, camera stability and front-facing depth-tested signs.

## Development tools

Use `Tools > StepHeightController Dev > Open Motion Debugger` to inspect step timing, gaps, velocity and settling. The test menu offers Run All, Run EditMode and Run PlayMode. Keep temporary captures, diagnostics and reports under ignored `ReleaseArtifacts`.

Historical July validation and the pre-refactor October checks do not prove the current artifact. The old `StepHeightDemo` scene and its basic player are intentionally removed; use the current demos for maintained checks.
