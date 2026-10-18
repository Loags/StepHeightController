using UnityEngine;

namespace LB.StepHeight
{
    internal readonly struct StepCandidate
    {
        public StepCandidate(Vector3 targetPosition, Collider surfaceCollider, float height, float liftFraction)
        {
            TargetPosition = targetPosition;
            SurfaceCollider = surfaceCollider;
            Height = height;
            LiftFraction = liftFraction;
        }

        public Vector3 TargetPosition { get; }
        public Collider SurfaceCollider { get; }
        public float Height { get; }
        public float LiftFraction { get; }
    }
}
