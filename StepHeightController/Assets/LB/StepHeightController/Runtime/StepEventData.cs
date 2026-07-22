using UnityEngine;

namespace LB.StepHeight
{
    /// <summary>Immutable context supplied by step lifecycle events.</summary>
    public readonly struct StepEventData
    {
        /// <summary>Creates lifecycle context for an accepted step.</summary>
        /// <param name="startPosition">Rigidbody position when the request was accepted.</param>
        /// <param name="targetPosition">Accepted Rigidbody target position.</param>
        /// <param name="surfaceCollider">Collider that supplied the walkable top surface.</param>
        /// <param name="height">Vertical rise in world units.</param>
        public StepEventData(Vector3 startPosition, Vector3 targetPosition, Collider surfaceCollider, float height)
        {
            StartPosition = startPosition;
            TargetPosition = targetPosition;
            SurfaceCollider = surfaceCollider;
            Height = height;
        }

        /// <summary>Gets the Rigidbody position at the time the step was accepted.</summary>
        public Vector3 StartPosition { get; }

        /// <summary>Gets the accepted Rigidbody target position.</summary>
        public Vector3 TargetPosition { get; }

        /// <summary>Gets the walkable surface selected for the step.</summary>
        public Collider SurfaceCollider { get; }

        /// <summary>Gets the vertical rise in world units.</summary>
        public float Height { get; }
    }
}
