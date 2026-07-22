using UnityEngine;

namespace LB.StepHeight
{
    internal readonly struct StepCandidate
    {
        public StepCandidate(Vector3 targetPosition, Collider surfaceCollider, float height)
        {
            TargetPosition = targetPosition;
            SurfaceCollider = surfaceCollider;
            Height = height;
        }

        public Vector3 TargetPosition { get; }
        public Collider SurfaceCollider { get; }
        public float Height { get; }
    }
}
