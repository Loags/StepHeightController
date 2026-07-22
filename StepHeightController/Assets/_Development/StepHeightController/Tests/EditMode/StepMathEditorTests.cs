using NUnit.Framework;
using UnityEngine;

namespace LB.StepHeight.Tests
{
    public sealed class StepMathEditorTests
    {
        [TestCase(0.02f, 0.02f, 0.5f, true)]
        [TestCase(0.5f, 0.02f, 0.5f, true)]
        [TestCase(0.019f, 0.02f, 0.5f, false)]
        [TestCase(0.501f, 0.02f, 0.5f, false)]
        public void IsHeightWithinRange_UsesInclusiveBoundaries(float height, float minimum, float maximum,
            bool expected)
        {
            Assert.That(StepMath.IsHeightWithinRange(height, minimum, maximum), Is.EqualTo(expected));
        }

        [Test]
        public void CalculateStepDuration_UsesDistanceAndSpeed()
        {
            float duration = StepMath.CalculateStepDuration(Vector3.zero, Vector3.up * 0.5f, 2f, 0.02f);

            Assert.That(duration, Is.EqualTo(0.25f).Within(0.0001f));
        }

        [Test]
        public void CalculateStepDuration_NeverFallsBelowPhysicsDuration()
        {
            float duration = StepMath.CalculateStepDuration(Vector3.zero, Vector3.up * 0.001f, 100f, 0.12f);

            Assert.That(duration, Is.EqualTo(0.12f).Within(0.0001f));
        }

        [Test]
        public void CalculateStepPosition_UsesLinearHorizontalAndSmoothedVerticalProgress()
        {
            Vector3 position = StepMath.CalculateStepPosition(Vector3.zero, new Vector3(1f, 1f, 0f), Vector3.up,
                0.25f);

            Assert.That(position.x, Is.EqualTo(0.25f).Within(0.0001f));
            Assert.That(position.y, Is.EqualTo(0.15625f).Within(0.0001f));
        }

        [Test]
        public void StepQuerySettings_InvertsIgnoredLayers()
        {
            var settings = new StepQuerySettings(0.5f, 0.02f, 50f, 65f, 0.15f, 0.1f, 0.02f,
                0.02f, (LayerMask)(1 << 8), QueryTriggerInteraction.Ignore);

            Assert.That((settings.QueryMask & (1 << 8)) == 0, Is.True);
            Assert.That((settings.QueryMask & 1) != 0, Is.True);
        }

        [Test]
        public void StepEventData_PreservesPublicContext()
        {
            var surface = new GameObject("Surface").AddComponent<BoxCollider>();
            var data = new StepEventData(Vector3.one, Vector3.one * 2f, surface, 0.3f);

            Assert.That(data.StartPosition, Is.EqualTo(Vector3.one));
            Assert.That(data.TargetPosition, Is.EqualTo(Vector3.one * 2f));
            Assert.That(data.SurfaceCollider, Is.SameAs(surface));
            Assert.That(data.Height, Is.EqualTo(0.3f));

            Object.DestroyImmediate(surface.gameObject);
        }

        [Test]
        public void CalculateCandidateScore_PrefersAlignedNearbyCandidates()
        {
            float alignedNear = StepMath.CalculateCandidateScore(1f, 0.2f, 1f, 0.3f);
            float angledFar = StepMath.CalculateCandidateScore(0.5f, 0.8f, 1f, 0.4f);

            Assert.That(alignedNear, Is.GreaterThan(angledFar));
        }

        [Test]
        public void CalculateBoxHalfExtents_UsesAbsoluteNonUniformScaleAndClearance()
        {
            Vector3 halfExtents = StepMath.CalculateBoxHalfExtents(new Vector3(2f, 4f, 6f),
                new Vector3(-2f, 0.5f, 3f), 0.1f);

            Assert.That(halfExtents, Is.EqualTo(new Vector3(1.9f, 0.9f, 8.9f)));
        }

        [Test]
        public void CalculateSphereRadius_UsesLargestScaleAxisAndClearance()
        {
            float radius = StepMath.CalculateSphereRadius(0.5f, new Vector3(1f, -3f, 2f), 0.1f);

            Assert.That(radius, Is.EqualTo(1.4f).Within(0.0001f));
        }
    }
}
