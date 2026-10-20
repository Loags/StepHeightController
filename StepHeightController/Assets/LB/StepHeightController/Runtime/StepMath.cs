using UnityEngine;

namespace LB.StepHeight
{
    internal static class StepMath
    {
        public const float DirectionEpsilon = 0.0001f;
        private const float HeightTolerance = 0.0001f;

        public static bool IsHeightWithinRange(float height, float minimumHeight, float maximumHeight) =>
            height >= minimumHeight - HeightTolerance && height <= maximumHeight + HeightTolerance;

        public static float CalculateStepDuration(Vector3 start, Vector3 target, float speed, float minimumDuration, Vector3 up, float horizontalSpeed = 0f)
        {
            Vector3 displacement = target - start;
            float liftTime = Mathf.Abs(Vector3.Dot(displacement, up.normalized)) / Mathf.Max(speed, 0.01f);
            // A slow startup tick must not dictate the pace of the entire committed step.
            float travelSpeed = Mathf.Max(horizontalSpeed, speed);
            float travelTime = Vector3.ProjectOnPlane(displacement, up).magnitude / Mathf.Max(travelSpeed, 0.01f);
            return Mathf.Max(liftTime, Mathf.Max(travelTime, minimumDuration));
        }

        public static Vector3 CalculateStepPosition(Vector3 start, Vector3 target, Vector3 up, float progress, float liftFraction = 1f)
        {
            float clampedProgress = Mathf.Clamp01(progress);
            Vector3 normalizedUp = up.normalized;
            Vector3 displacement = target - start;
            Vector3 verticalDisplacement = Vector3.Project(displacement, normalizedUp);
            Vector3 horizontalDisplacement = displacement - verticalDisplacement;
            // Keep forward travel continuous; only the lift eases and can finish early to clear the riser.
            float verticalProgress = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(clampedProgress / Mathf.Max(liftFraction, 0.001f)));
            return start + horizontalDisplacement * clampedProgress + verticalDisplacement * verticalProgress;
        }

        public static float CalculateCandidateScore(float alignment, float distance, float searchRadius, float height) =>
            alignment * 2f - distance / Mathf.Max(searchRadius, 0.001f) + height * 0.01f;

        public static Vector3 CalculateBoxHalfExtents(Vector3 size, Vector3 lossyScale, float clearance)
        {
            Vector3 scale = Abs(lossyScale);
            Vector3 halfExtents = Vector3.Scale(size * 0.5f, scale) - Vector3.one * clearance;
            return Max(halfExtents, Vector3.one * 0.001f);
        }

        public static float CalculateSphereRadius(float radius, Vector3 lossyScale, float clearance)
        {
            Vector3 scale = Abs(lossyScale);
            float maximumScale = Mathf.Max(scale.x, Mathf.Max(scale.y, scale.z));
            return Mathf.Max(0.001f, radius * maximumScale - clearance);
        }

        private static Vector3 Abs(Vector3 value) =>
            new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        private static Vector3 Max(Vector3 left, Vector3 right) =>
            new Vector3(Mathf.Max(left.x, right.x), Mathf.Max(left.y, right.y), Mathf.Max(left.z, right.z));
    }
}
