namespace LB.StepHeight
{
    /// <summary>Describes the immediate outcome of a step request.</summary>
    public enum StepAttemptResult
    {
        /// <summary>The request was accepted and step movement began.</summary>
        Started,

        /// <summary>The component or stepping feature is disabled.</summary>
        Disabled,

        /// <summary>A previously accepted step is still in progress.</summary>
        AlreadyStepping,

        /// <summary>The supplied direction has no usable horizontal component.</summary>
        InvalidDirection,

        /// <summary>No enabled CapsuleCollider, BoxCollider, or SphereCollider is available.</summary>
        UnsupportedCollider,

        /// <summary>No walkable step matched the configured limits.</summary>
        NoCandidate,

        /// <summary>A candidate was found, but the player shape does not fit at the target.</summary>
        Blocked,

        /// <summary>A physics query reached its safety capacity, so the request was rejected deterministically.</summary>
        QueryCapacityExceeded
    }
}
