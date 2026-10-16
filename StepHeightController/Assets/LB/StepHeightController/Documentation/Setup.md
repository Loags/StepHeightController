# Quick start

Open **Tools > Step Height Controller > Setup & Demos**.

1. Open each missing demo dependency in Package Manager and choose Install. Tested versions are shown beside the installed versions.
2. Use Universal RP for the demos. If you need an asset, right-click in the Project window and choose **Create > Rendering > URP Asset (with Universal Renderer)**. Open **Graphics** from the setup window and assign it as **Default Render Pipeline**. Open **Quality** and assign the same asset for the quality levels you use, or clear their override to use the Graphics default.
3. In Player > Other Settings, choose **Input System (New)** or **Both** and restart Unity if prompted.
4. Refresh, open either demo, and press Play. Each demo uses the same obstacle course.

The stepping runtime needs only Unity physics. Cinemachine, Input System, Universal RP and Unity UI are demo requirements. The demo players use our Rigidbody movement with Starter Assets visuals; they do not integrate Unity's CharacterController scripts.

## Your own player

Open **Configure Rigidbody Player**, choose the scene object holding your non-kinematic Rigidbody, and review the changes before applying. Supported shapes are enabled, non-trigger Capsule, Box and Sphere colliders, including compound primitive shapes. Keep the physics root upright and rotate the visual model separately.

The tool adds the stepping component if needed. Optional interpolation and upright rotation settings are off until you select them. Existing controllers, input, cameras and tuning are preserved. Repeat runs make no extra changes. One Undo reverses an Apply action.

Your movement script still owns movement and jumping. Assign its Step Height Controller reference explicitly and adapt this pattern in its physics loop:

```csharp
// Handle a jump before requesting another step.
if (jumpRequested && (isGrounded || stepController.IsStepping))
{
    stepController.CancelStep();
    Jump();
    return;
}

if (!stepController.IsStepping)
{
    ApplyMovement();
    if (isGrounded && isMoving) stepController.TryStep(worldMovementDirection);
}
```

These names represent your existing motor's state and methods. While stepping, let Step Height Controller move the Rigidbody. Do not overwrite its position or horizontal velocity. Suspend extra downward acceleration during the step, or clear its downward velocity; resume normal gravity afterward.

## Tune the feel

The Inspector shows height, speed and ignored layers first. Height presets change the height range; feel presets change speed and minimum duration. Each group preserves the other group and collision filtering. Detailed detection, collision and diagnostic controls live under **Advanced**.

## Ready-to-play players

Use **Select player prefab** to inspect either demo player, or **Add player** to create its instance in your active scene. Each prefab includes its configured controls and camera rig. Keep one active player/camera rig. Adding a player supports Undo and does not replace scene objects.
