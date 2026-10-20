# Release history

## 2.0.0

- Integrate step-up movement with your own Rigidbody controller through explicit movement intent.
- Configure step height, speed, approach and surface limits, with clearance checks for the player shape.
- Use capsule, box, sphere or compound primitive player colliders.
- Coordinate feedback through step results, lifecycle events and cancellation.
- Explore a refreshed demo course with stairs, ledges and rejected obstacle examples.
- Use clearer setup feedback, scene visualization and offline documentation.
- Use a URP-compatible sample player material.
- Start with guided dependency checks and a reversible Rigidbody player setup window.
- Explore first-person and third-person Rigidbody demos with Starter Assets visuals, Cinemachine cameras and one shared course.
- Compare stepping on the same player, reset with one key, and jump immediately out of a step.
- Use independent height and feel presets with detailed tuning under Advanced.
- Keep moving forward while lifting smoothly onto steps, with clearance checks along the ascent.
- Step up promptly when starting close to an obstacle or tapping a movement key.
- Keep demo cameras steady while climbing, including when looking down.
- Use a lower, weightier demo jump and front-facing signs that respect scene depth.
- Keep the walking pose through brief downward steps, while jumps and longer falls use their air animations.

Upgrade note: this complete release package requires Unity 6.5 or newer. The namespace and main integration API have changed. Read [UPGRADE.md](UPGRADE.md) before updating. The demos require Input System, Cinemachine, URP and Unity UI; the stepping runtime remains independent of them.

## 1.0.0

- Initial release with obstacle stepping, a basic demo movement controller and step visualization.
