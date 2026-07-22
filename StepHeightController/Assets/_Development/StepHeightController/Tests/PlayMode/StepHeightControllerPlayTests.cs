using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LB.StepHeight.Tests
{
    public sealed class StepHeightControllerPlayTests
    {
        private GameObject _player;
        private StepHeightController _controller;
        private readonly List<GameObject> _environment = new List<GameObject>();

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _player = new GameObject("Player");
            Rigidbody body = _player.AddComponent<Rigidbody>();
            body.useGravity = false;
            body.isKinematic = true;
            CapsuleCollider capsule = _player.AddComponent<CapsuleCollider>();
            capsule.center = new Vector3(0f, 0.9f, 0f);
            capsule.height = 1.8f;
            capsule.radius = 0.45f;
            _controller = _player.AddComponent<StepHeightController>();
            yield return null;
        }

        [UnityTearDown]
        public IEnumerator TearDown()
        {
            if (_player != null) Object.Destroy(_player);
            foreach (GameObject environment in _environment)
            {
                if (environment != null) Object.Destroy(environment);
            }

            _environment.Clear();
            yield return null;
        }

        [UnityTest]
        public IEnumerator TryStep_RejectsInvalidDirection()
        {
            Assert.That(_controller.TryStep(Vector3.zero), Is.EqualTo(StepAttemptResult.InvalidDirection));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TryStep_RejectsWhenDisabled()
        {
            _controller.SteppingEnabled = false;

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Disabled));
            yield return null;
        }

        [UnityTest]
        public IEnumerator TryStep_AcceptsLowBoxStep()
        {
            CreateBoxStep(0.3f);
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            Assert.That(_controller.IsStepping, Is.True);
            _controller.CancelStep();
        }

        [UnityTest]
        public IEnumerator CompletedStep_UsesSmoothMinimumDurationAndDoesNotImmediatelyRetrigger()
        {
            CreateBoxStep(0.3f);
            int startCount = 0;
            int completionCount = 0;
            int fixedUpdateCount = 0;
            Vector3 targetPosition = default;
            _controller.StepStarted += step =>
            {
                startCount++;
                targetPosition = step.TargetPosition;
            };
            _controller.StepCompleted += _ => completionCount++;
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            float previousHeight = _player.transform.position.y;
            while (_controller.IsStepping && fixedUpdateCount < 30)
            {
                yield return new WaitForFixedUpdate();
                fixedUpdateCount++;
                Assert.That(_player.transform.position.y, Is.GreaterThanOrEqualTo(previousHeight - 0.0001f));
                previousHeight = _player.transform.position.y;
            }

            Assert.That(fixedUpdateCount, Is.GreaterThanOrEqualTo(5));
            Assert.That(startCount, Is.EqualTo(1));
            Assert.That(completionCount, Is.EqualTo(1));
            Assert.That(Vector3.Distance(_player.transform.position, targetPosition), Is.LessThan(0.0001f));
            Assert.That(_controller.TryStep(Vector3.forward), Is.Not.EqualTo(StepAttemptResult.Started));
        }

        [UnityTest]
        public IEnumerator CompletedStep_ClearsDynamicVerticalVelocity()
        {
            Rigidbody body = _player.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;
            CreateBoxStep(0.3f);
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            yield return new WaitForFixedUpdate();
            body.linearVelocity = Vector3.up * 2f;

            for (int i = 0; i < 30 && _controller.IsStepping; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(Mathf.Abs(Vector3.Dot(body.linearVelocity, _player.transform.up)), Is.LessThan(0.001f));
        }

        [UnityTest]
        public IEnumerator CompletedStep_RemainsSupportedWhenGravityResumes()
        {
            Rigidbody body = _player.GetComponent<Rigidbody>();
            body.isKinematic = false;
            body.useGravity = false;
            CreateBoxStep(0.3f);
            Vector3 targetPosition = default;
            _controller.StepStarted += step => targetPosition = step.TargetPosition;
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            for (int i = 0; i < 30 && _controller.IsStepping; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            float completedHeight = body.position.y;
            Assert.That(targetPosition.y, Is.EqualTo(0.3f).Within(0.001f));

            body.useGravity = true;
            for (int i = 0; i < 5; i++)
            {
                yield return new WaitForFixedUpdate();
            }

            Assert.That(completedHeight - body.position.y, Is.LessThan(0.01f));
        }

        [UnityTest]
        public IEnumerator AcceptedStep_LandsBeyondDetectedObstacleFace()
        {
            CreateBoxStep(0.3f);
            Vector3 targetPosition = default;
            _controller.StepStarted += step => targetPosition = step.TargetPosition;
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            Assert.That(targetPosition.z, Is.GreaterThan(0.45f));
            _controller.CancelStep();
        }

        [UnityTest]
        public IEnumerator ConsecutiveSteps_RearmWithoutAnIdlePhysicsGap()
        {
            CreateEnvironmentBox("Lower Step", new Vector3(0f, 0.1f, 0.72f), new Vector3(2f, 0.2f, 0.6f), false);
            CreateEnvironmentBox("Upper Step", new Vector3(0f, 0.2f, 1.32f), new Vector3(2f, 0.4f, 0.6f), false);
            int startCount = 0;
            int completionCount = 0;
            float firstCompletionTime = 0f;
            float secondStartTime = 0f;
            var attempts = new List<StepAttemptResult>();
            _controller.StepStarted += _ =>
            {
                startCount++;
                if (startCount == 2) secondStartTime = Time.fixedTime;
            };
            _controller.StepCompleted += _ =>
            {
                completionCount++;
                if (completionCount == 1) firstCompletionTime = Time.fixedTime;
            };
            yield return new WaitForFixedUpdate();

            for (int i = 0; i < 60 && completionCount < 2; i++)
            {
                if (!_controller.IsStepping) attempts.Add(_controller.TryStep(Vector3.forward));
                yield return new WaitForFixedUpdate();
            }

            string attemptSummary = string.Join(", ", attempts.GroupBy(result => result)
                .Select(group => $"{group.Key}: {group.Count()}"));
            Assert.That(startCount, Is.EqualTo(2),
                $"Attempts: {attemptSummary}. Position: {_player.transform.position}.");
            Assert.That(completionCount, Is.EqualTo(2));
            Assert.That(secondStartTime - firstCompletionTime, Is.LessThanOrEqualTo(Time.fixedDeltaTime * 1.1f));
        }

        [UnityTest]
        public IEnumerator TryStep_RejectsOverHeightStep()
        {
            CreateBoxStep(0.75f);
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.NoCandidate));
        }

        [UnityTest]
        public IEnumerator TryStep_RejectsBlockedCeiling()
        {
            CreateBoxStep(0.3f);
            CreateEnvironmentBox("Ceiling", new Vector3(0f, 1.6f, 0.72f), new Vector3(2f, 0.2f, 2f), false);
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Blocked));
        }

        [UnityTest]
        public IEnumerator TryStep_RespectsIgnoredLayers()
        {
            GameObject step = CreateBoxStep(0.3f);
            step.layer = 8;
            _controller.IgnoredLayers = 1 << 8;
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.NoCandidate));
        }

        [UnityTest]
        public IEnumerator TryStep_IgnoresTriggersByDefault()
        {
            CreateEnvironmentBox("Trigger Step", new Vector3(0f, 0.15f, 0.72f), new Vector3(2f, 0.3f, 0.6f),
                true);
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.NoCandidate));
        }

        [UnityTest]
        public IEnumerator TryStep_DoesNotTreatWalkableSlopeAsStepRisers()
        {
            GameObject slope = CreateEnvironmentBox("Walkable Slope", new Vector3(0f, 0.37f, 1.9f),
                new Vector3(2f, 0.3f, 3f), false);
            slope.transform.rotation = Quaternion.Euler(-20f, 0f, 0f);
            yield return new WaitForFixedUpdate();

            for (int i = 0; i < 5; i++)
            {
                Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.NoCandidate));
                yield return new WaitForFixedUpdate();
            }
        }

        [UnityTest]
        public IEnumerator TryStep_RejectsObstacleOutsideMovementApproach()
        {
            CreateEnvironmentBox("Side Obstacle", new Vector3(-0.72f, 0.15f, 0f),
                new Vector3(0.6f, 0.3f, 2f), false);
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.right), Is.EqualTo(StepAttemptResult.NoCandidate));
        }

        [UnityTest]
        public IEnumerator TryStep_IgnoresUnsupportedSelfCollider()
        {
            GameObject selfGeometry = GameObject.CreatePrimitive(PrimitiveType.Cube);
            selfGeometry.name = "Unsupported Self Geometry";
            selfGeometry.transform.SetParent(_player.transform, false);
            selfGeometry.transform.localPosition = new Vector3(0f, 0.15f, 0.72f);
            selfGeometry.transform.localScale = new Vector3(2f, 0.3f, 0.6f);
            Mesh mesh = selfGeometry.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(selfGeometry.GetComponent<BoxCollider>());
            yield return null;

            selfGeometry.AddComponent<MeshCollider>().sharedMesh = mesh;
            _controller.RefreshColliderCache();
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.NoCandidate));
        }

        [UnityTest]
        public IEnumerator TryStep_AcceptsCompoundPlayerCollider()
        {
            var child = new GameObject("Head Collider");
            child.transform.SetParent(_player.transform, false);
            SphereCollider sphere = child.AddComponent<SphereCollider>();
            sphere.center = new Vector3(0f, 1.45f, 0f);
            sphere.radius = 0.25f;
            _controller.RefreshColliderCache();
            CreateBoxStep(0.3f);
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            _controller.CancelStep();
        }

        [UnityTest]
        public IEnumerator TryStep_AcceptsMeshEnvironmentStep()
        {
            GameObject step = GameObject.CreatePrimitive(PrimitiveType.Cube);
            step.name = "Mesh Step";
            step.transform.position = new Vector3(0f, 0.15f, 0.72f);
            step.transform.localScale = new Vector3(2f, 0.3f, 0.6f);
            Mesh mesh = step.GetComponent<MeshFilter>().sharedMesh;
            Object.Destroy(step.GetComponent<BoxCollider>());
            _environment.Add(step);
            yield return null;

            step.AddComponent<MeshCollider>().sharedMesh = mesh;
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            _controller.CancelStep();
        }

        [UnityTest]
        public IEnumerator DisablingComponent_CancelsActiveStepOnce()
        {
            CreateBoxStep(0.3f);
            int cancellationCount = 0;
            StepCancellationReason cancellationReason = default;
            _controller.StepCancelled += (_, reason) =>
            {
                cancellationCount++;
                cancellationReason = reason;
            };
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            _controller.enabled = false;

            Assert.That(cancellationCount, Is.EqualTo(1));
            Assert.That(cancellationReason, Is.EqualTo(StepCancellationReason.ComponentDisabled));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RepeatedEnableDisable_CancelsEachAcceptedStepOnce()
        {
            CreateBoxStep(0.3f);
            int cancellationCount = 0;
            _controller.StepCancelled += (_, _) => cancellationCount++;
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            _controller.enabled = false;
            _controller.enabled = true;
            yield return new WaitForFixedUpdate();

            Assert.That(_controller.TryStep(Vector3.forward), Is.EqualTo(StepAttemptResult.Started));
            _controller.enabled = false;

            Assert.That(cancellationCount, Is.EqualTo(2));
            yield return null;
        }

        [UnityTest]
        public IEnumerator BoxAndSpherePlayers_AcceptInitializationContract()
        {
            Object.Destroy(_player);
            yield return null;

            AssertSupportedPlayerCollider<BoxCollider>();
            AssertSupportedPlayerCollider<SphereCollider>();
            yield return null;
        }

        private void AssertSupportedPlayerCollider<T>() where T : Collider
        {
            var player = new GameObject(typeof(T).Name);
            Rigidbody body = player.AddComponent<Rigidbody>();
            body.isKinematic = true;
            player.AddComponent<T>();
            StepHeightController controller = player.AddComponent<StepHeightController>();

            Assert.That(controller.TryStep(Vector3.zero), Is.EqualTo(StepAttemptResult.InvalidDirection));
            Object.Destroy(player);
        }

        private GameObject CreateBoxStep(float height) => CreateEnvironmentBox("Step",
            new Vector3(0f, height * 0.5f, 0.72f), new Vector3(2f, height, 0.6f), false);

        private GameObject CreateEnvironmentBox(string name, Vector3 position, Vector3 scale, bool isTrigger)
        {
            GameObject environment = GameObject.CreatePrimitive(PrimitiveType.Cube);
            environment.name = name;
            environment.transform.position = position;
            environment.transform.localScale = scale;
            environment.GetComponent<BoxCollider>().isTrigger = isTrigger;
            _environment.Add(environment);
            return environment;
        }
    }
}
