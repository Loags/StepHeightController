namespace LB.StepHeight
{
    /// <summary>Describes why an accepted step ended before completion.</summary>
    public enum StepCancellationReason
    {
        /// <summary>The caller invoked <see cref="StepHeightController.CancelStep"/>.</summary>
        Requested,

        /// <summary>Stepping was disabled through <see cref="StepHeightController.SteppingEnabled"/>.</summary>
        Disabled,

        /// <summary>The controller component or its GameObject was disabled.</summary>
        ComponentDisabled,

        /// <summary>The player collider cache was refreshed during an active step.</summary>
        CollidersRefreshed
    }
}
