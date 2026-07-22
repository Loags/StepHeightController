using UnityEngine;

namespace LB.StepHeight
{
    internal readonly struct StepQuerySettings
    {
        public StepQuerySettings(float maximumStepHeight, float minimumStepHeight, float maximumSurfaceAngle,
            float maximumApproachAngle, float detectionDistance, float surfaceProbeInset, float landingInset, float clearance,
            LayerMask ignoredLayers, QueryTriggerInteraction triggerInteraction)
        {
            MaximumStepHeight = maximumStepHeight;
            MinimumStepHeight = minimumStepHeight;
            MaximumSurfaceAngle = maximumSurfaceAngle;
            MaximumApproachAngle = maximumApproachAngle;
            DetectionDistance = detectionDistance;
            SurfaceProbeInset = surfaceProbeInset;
            LandingInset = landingInset;
            Clearance = clearance;
            QueryMask = ~ignoredLayers.value;
            TriggerInteraction = triggerInteraction;
        }

        public float MaximumStepHeight { get; }
        public float MinimumStepHeight { get; }
        public float MaximumSurfaceAngle { get; }
        public float MaximumApproachAngle { get; }
        public float DetectionDistance { get; }
        public float SurfaceProbeInset { get; }
        public float LandingInset { get; }
        public float Clearance { get; }
        public int QueryMask { get; }
        public QueryTriggerInteraction TriggerInteraction { get; }
    }
}
