# Sample experience

Open `Scenes/StepHeightDemo.unity`.

The `MovementController` is deliberately sample-only. It owns Input System callbacks, performs grounding and ordinary Rigidbody movement, and calls the runtime `TryStep` API with world-space intent.

Controls:

- WASD: Move
- Mouse: Look
- Left Shift: Sprint
- Space: Jump

Scene groups beginning with Valid or Boundary are expected to be traversable with the default 0.5 m maximum. Groups beginning with Rejected demonstrate height or clearance constraints. Focused Validation Scenarios additionally cover a rotated box, a slope, and an ignored trigger.
