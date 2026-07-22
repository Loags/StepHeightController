# Public API

Namespace: `LB.StepHeight`

## StepHeightController

Add `StepHeightController` to the same GameObject as the Rigidbody. The component discovers supported colliders on the root and, by default, its children.

### TryStep

```csharp
StepAttemptResult TryStep(Vector3 worldMovementDirection)
```

Attempts to select and begin a step. The vector is a world-space movement intent; its magnitude is ignored. Call from the movement controller's physics loop while grounded and moving.

Immediate results:

- `Started`: A target was accepted and movement began.
- `Disabled`: The component or stepping feature is disabled.
- `AlreadyStepping`: A previous step is still active.
- `InvalidDirection`: The supplied direction has no usable horizontal magnitude.
- `UnsupportedCollider`: No enabled supported player collider is available.
- `NoCandidate`: No obstacle with a valid approach, height, and walkable top was found.
- `Blocked`: Valid step geometry exists, but the player shape does not fit at the target.
- `QueryCapacityExceeded`: More than the supported number of colliders or ray hits occupied a query; no nondeterministic step is performed.

### State and configuration

```csharp
bool IsStepping { get; }
bool SteppingEnabled { get; set; }
float MaximumStepHeight { get; }
LayerMask IgnoredLayers { get; set; }
```

`IsStepping` is the movement arbitration signal. A movement controller should avoid competing Rigidbody position writes while it is true.

Setting `SteppingEnabled` to false cancels an active step with the `Disabled` reason.

### Lifecycle events

```csharp
event Action<StepEventData> StepStarted;
event Action<StepEventData> StepCompleted;
event Action<StepEventData, StepCancellationReason> StepCancelled;
```

`StepEventData` contains the start position, target position, selected surface collider, and vertical height. It can drive animation, sound, camera response, telemetry, or movement locks without exposing internal query data.

Cancellation reasons:

- `Requested`: `CancelStep()` was called.
- `Disabled`: `SteppingEnabled` was set to false.
- `ComponentDisabled`: Unity disabled the component or its GameObject.
- `CollidersRefreshed`: Collider data changed during an active step.

### CancelStep

```csharp
bool CancelStep()
```

Stops an active step and raises `StepCancelled`. Returns false if the controller was already idle.

### RefreshColliderCache

```csharp
void RefreshColliderCache()
```

Rebuilds the player-collider set. Call after adding, removing, enabling, disabling, or resizing colliders at runtime. Refreshing cancels active movement before rebuilding.

### Compatibility API

```csharp
[Obsolete]
StepAttemptResult CheckForStep()
```

This method uses the Rigidbody's current velocity as direction and remains only to ease source migration. New code must call `TryStep` with explicit movement intent.

## Event example

```csharp
private void OnEnable()
{
    stepController.StepStarted += OnStepStarted;
    stepController.StepCompleted += OnStepCompleted;
}

private void OnDisable()
{
    stepController.StepStarted -= OnStepStarted;
    stepController.StepCompleted -= OnStepCompleted;
}

private void OnStepStarted(StepEventData step)
{
    animator.SetBool("Stepping", true);
}

private void OnStepCompleted(StepEventData step)
{
    animator.SetBool("Stepping", false);
}
```
