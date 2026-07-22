using System;
using System.Collections.Generic;
using UnityEngine;

namespace LB.StepHeight
{
    internal sealed class ColliderManager
    {
        private const int InitialBufferCapacity = 32;
        private const int MaximumBufferCapacity = 256;

        private readonly Transform _root;
        private readonly Rigidbody _rigidbody;
        private readonly HashSet<Collider> _selfColliders = new HashSet<Collider>();
        private Collider[] _playerColliders = Array.Empty<Collider>();
        private Collider[] _overlapBuffer = new Collider[InitialBufferCapacity];
        private RaycastHit[] _raycastBuffer = new RaycastHit[InitialBufferCapacity];
        private bool _queryCapacityExceeded;

        public ColliderManager(Transform root, Rigidbody rigidbody)
        {
            _root = root;
            _rigidbody = rigidbody;
        }

        public void Refresh(bool includeChildColliders)
        {
            _playerColliders = includeChildColliders
                ? _root.GetComponentsInChildren<Collider>(true)
                : _root.GetComponents<Collider>();

            _selfColliders.Clear();
            foreach (Collider playerCollider in _playerColliders)
            {
                _selfColliders.Add(playerCollider);
            }

            if (!TryGetPlayerBounds(out _))
            {
                throw new InvalidOperationException(
                    "StepHeightController requires at least one enabled, non-trigger CapsuleCollider, BoxCollider, or SphereCollider.");
            }
        }

        public StepAttemptResult TryFindStep(Vector3 movementDirection, StepQuerySettings settings,
            out StepCandidate bestCandidate)
        {
            bestCandidate = default;
            _queryCapacityExceeded = false;
            if (!TryGetPlayerBounds(out Bounds playerBounds)) return StepAttemptResult.UnsupportedCollider;

            float horizontalRadius = Mathf.Max(playerBounds.extents.x, playerBounds.extents.z);
            float searchRadius = horizontalRadius + settings.DetectionDistance;
            Vector3 up = _root.up;
            Vector3 probeCenter = new Vector3(playerBounds.center.x,
                playerBounds.min.y + settings.MaximumStepHeight * 0.5f, playerBounds.center.z);
            int overlapCount = OverlapSphere(probeCenter, searchRadius, settings.QueryMask, settings.TriggerInteraction);
            if (_queryCapacityExceeded) return StepAttemptResult.QueryCapacityExceeded;
            if (overlapCount == 0) return StepAttemptResult.NoCandidate;

            bool foundGeometricCandidate = false;
            bool foundBlockedCandidate = false;
            float bestScore = float.NegativeInfinity;
            Vector3 sideProbe = new Vector3(playerBounds.center.x,
                playerBounds.min.y + settings.MinimumStepHeight, playerBounds.center.z) +
                movementDirection * (horizontalRadius + settings.DetectionDistance);
            Vector3 contactRayOrigin = playerBounds.center + up *
                Vector3.Dot(sideProbe - playerBounds.center, up);

            for (int i = 0; i < overlapCount; i++)
            {
                Collider obstacle = _overlapBuffer[i];
                if (!IsEnvironmentCollider(obstacle)) continue;

                bool hasContactNormal = TryGetContactPoint(obstacle, contactRayOrigin, sideProbe, movementDirection,
                    searchRadius * 2f, out Vector3 contact, out Vector3 contactNormal);
                if (hasContactNormal && Vector3.Angle(contactNormal, up) <= settings.MaximumSurfaceAngle) continue;
                Vector3 toContact = Vector3.ProjectOnPlane(contact - playerBounds.center, up);
                if (toContact.sqrMagnitude <= StepMath.DirectionEpsilon) continue;

                float approachAngle = Vector3.Angle(movementDirection, toContact);
                if (approachAngle > settings.MaximumApproachAngle) continue;

                Vector3 rayOrigin = contact + movementDirection * settings.SurfaceProbeInset +
                    up * (settings.MaximumStepHeight + settings.Clearance);
                float rayDistance = settings.MaximumStepHeight + settings.Clearance * 2f;
                int hitCount = Raycast(rayOrigin, -up, rayDistance, settings.QueryMask, settings.TriggerInteraction);
                if (_queryCapacityExceeded) return StepAttemptResult.QueryCapacityExceeded;

                for (int hitIndex = 0; hitIndex < hitCount; hitIndex++)
                {
                    RaycastHit hit = _raycastBuffer[hitIndex];
                    if (!IsEnvironmentCollider(hit.collider)) continue;

                    float surfaceAngle = Vector3.Angle(hit.normal, up);
                    if (surfaceAngle > settings.MaximumSurfaceAngle) continue;

                    float height = Vector3.Dot(hit.point - new Vector3(playerBounds.center.x, playerBounds.min.y,
                        playerBounds.center.z), up);
                    if (!StepMath.IsHeightWithinRange(height, settings.MinimumStepHeight,
                            settings.MaximumStepHeight)) continue;

                    foundGeometricCandidate = true;
                    float distanceToFace = Mathf.Max(0f, Vector3.Dot(contact - playerBounds.center, movementDirection));
                    Vector3 targetPosition = _rigidbody.position + up * height +
                        movementDirection * (distanceToFace + settings.LandingInset);
                    if (!IsPositionClear(targetPosition, settings))
                    {
                        foundBlockedCandidate = true;
                        continue;
                    }

                    float alignment = Vector3.Dot(movementDirection, toContact.normalized);
                    float score = StepMath.CalculateCandidateScore(alignment, toContact.magnitude, searchRadius,
                        height);
                    if (score <= bestScore) continue;

                    bestScore = score;
                    bestCandidate = new StepCandidate(targetPosition, hit.collider, height);
                }
            }

            if (bestScore > float.NegativeInfinity) return StepAttemptResult.Started;
            if (foundBlockedCandidate) return StepAttemptResult.Blocked;
            return foundGeometricCandidate ? StepAttemptResult.Blocked : StepAttemptResult.NoCandidate;
        }

        private static bool TryGetContactPoint(Collider obstacle, Vector3 rayOrigin, Vector3 fallbackProbe,
            Vector3 movementDirection, float rayDistance, out Vector3 contact, out Vector3 contactNormal)
        {
            if (obstacle.Raycast(new Ray(rayOrigin, movementDirection), out RaycastHit hit, rayDistance))
            {
                contact = hit.point;
                contactNormal = hit.normal;
                return true;
            }

            if (obstacle is MeshCollider meshCollider && !meshCollider.convex)
            {
                contact = obstacle.bounds.ClosestPoint(fallbackProbe);
                contactNormal = Vector3.zero;
                return false;
            }

            contact = obstacle.ClosestPoint(fallbackProbe);
            contactNormal = Vector3.zero;
            return false;
        }

        private bool TryGetPlayerBounds(out Bounds combinedBounds)
        {
            combinedBounds = default;
            bool hasBounds = false;

            foreach (Collider playerCollider in _playerColliders)
            {
                if (!IsSupportedPlayerCollider(playerCollider)) continue;

                if (!hasBounds)
                {
                    combinedBounds = playerCollider.bounds;
                    hasBounds = true;
                }
                else
                {
                    combinedBounds.Encapsulate(playerCollider.bounds);
                }
            }

            return hasBounds;
        }

        private bool IsPositionClear(Vector3 targetRootPosition, StepQuerySettings settings)
        {
            Vector3 translation = targetRootPosition - _rigidbody.position;

            foreach (Collider playerCollider in _playerColliders)
            {
                if (!IsSupportedPlayerCollider(playerCollider)) continue;

                int overlapCount;
                switch (playerCollider)
                {
                    case CapsuleCollider capsule:
                        GetCapsuleGeometry(capsule, translation, settings.Clearance, out Vector3 pointA,
                            out Vector3 pointB, out float capsuleRadius);
                        overlapCount = OverlapCapsule(pointA, pointB, capsuleRadius, settings.QueryMask,
                            settings.TriggerInteraction);
                        break;
                    case BoxCollider box:
                        Vector3 boxCenter = box.transform.TransformPoint(box.center) + translation;
                        Vector3 halfExtents = StepMath.CalculateBoxHalfExtents(box.size, box.transform.lossyScale,
                            settings.Clearance);
                        overlapCount = OverlapBox(boxCenter, halfExtents, box.transform.rotation, settings.QueryMask,
                            settings.TriggerInteraction);
                        break;
                    case SphereCollider sphere:
                        Vector3 sphereCenter = sphere.transform.TransformPoint(sphere.center) + translation;
                        float sphereRadius = StepMath.CalculateSphereRadius(sphere.radius,
                            sphere.transform.lossyScale, settings.Clearance);
                        overlapCount = OverlapSphere(sphereCenter, sphereRadius, settings.QueryMask,
                            settings.TriggerInteraction);
                        break;
                    default:
                        continue;
                }

                for (int i = 0; i < overlapCount; i++)
                {
                    if (IsEnvironmentCollider(_overlapBuffer[i])) return false;
                }
            }

            return true;
        }

        private void GetCapsuleGeometry(CapsuleCollider capsule, Vector3 translation, float clearance,
            out Vector3 pointA, out Vector3 pointB, out float radius)
        {
            Vector3 scale = Abs(capsule.transform.lossyScale);
            Vector3 localAxis;
            float heightScale;
            float radiusScale;

            switch (capsule.direction)
            {
                case 0:
                    localAxis = Vector3.right;
                    heightScale = scale.x;
                    radiusScale = Mathf.Max(scale.y, scale.z);
                    break;
                case 2:
                    localAxis = Vector3.forward;
                    heightScale = scale.z;
                    radiusScale = Mathf.Max(scale.x, scale.y);
                    break;
                default:
                    localAxis = Vector3.up;
                    heightScale = scale.y;
                    radiusScale = Mathf.Max(scale.x, scale.z);
                    break;
            }

            Vector3 center = capsule.transform.TransformPoint(capsule.center) + translation;
            Vector3 axis = capsule.transform.TransformDirection(localAxis).normalized;
            radius = Mathf.Max(0.001f, capsule.radius * radiusScale - clearance);
            float height = Mathf.Max(radius * 2f, capsule.height * heightScale - clearance * 2f);
            float segmentHalfLength = Mathf.Max(0f, height * 0.5f - radius);
            pointA = center + axis * segmentHalfLength;
            pointB = center - axis * segmentHalfLength;
        }

        private int OverlapSphere(Vector3 position, float radius, int layerMask,
            QueryTriggerInteraction triggerInteraction)
        {
            while (true)
            {
                int count = Physics.OverlapSphereNonAlloc(position, radius, _overlapBuffer, layerMask,
                    triggerInteraction);
                if (count < _overlapBuffer.Length) return count;
                if (_overlapBuffer.Length >= MaximumBufferCapacity)
                {
                    _queryCapacityExceeded = true;
                    return count;
                }

                Array.Resize(ref _overlapBuffer, Mathf.Min(_overlapBuffer.Length * 2, MaximumBufferCapacity));
            }
        }

        private int OverlapCapsule(Vector3 pointA, Vector3 pointB, float radius, int layerMask,
            QueryTriggerInteraction triggerInteraction)
        {
            while (true)
            {
                int count = Physics.OverlapCapsuleNonAlloc(pointA, pointB, radius, _overlapBuffer, layerMask,
                    triggerInteraction);
                if (count < _overlapBuffer.Length) return count;
                if (_overlapBuffer.Length >= MaximumBufferCapacity)
                {
                    _queryCapacityExceeded = true;
                    return count;
                }

                Array.Resize(ref _overlapBuffer, Mathf.Min(_overlapBuffer.Length * 2, MaximumBufferCapacity));
            }
        }

        private int OverlapBox(Vector3 center, Vector3 halfExtents, Quaternion orientation, int layerMask,
            QueryTriggerInteraction triggerInteraction)
        {
            while (true)
            {
                int count = Physics.OverlapBoxNonAlloc(center, halfExtents, _overlapBuffer, orientation, layerMask,
                    triggerInteraction);
                if (count < _overlapBuffer.Length) return count;
                if (_overlapBuffer.Length >= MaximumBufferCapacity)
                {
                    _queryCapacityExceeded = true;
                    return count;
                }

                Array.Resize(ref _overlapBuffer, Mathf.Min(_overlapBuffer.Length * 2, MaximumBufferCapacity));
            }
        }

        private int Raycast(Vector3 origin, Vector3 direction, float distance, int layerMask,
            QueryTriggerInteraction triggerInteraction)
        {
            while (true)
            {
                int count = Physics.RaycastNonAlloc(origin, direction, _raycastBuffer, distance, layerMask,
                    triggerInteraction);
                if (count < _raycastBuffer.Length)
                {
                    Array.Sort(_raycastBuffer, 0, count, RaycastHitDistanceComparer.Instance);
                    return count;
                }

                if (_raycastBuffer.Length >= MaximumBufferCapacity)
                {
                    _queryCapacityExceeded = true;
                    return count;
                }

                Array.Resize(ref _raycastBuffer, Mathf.Min(_raycastBuffer.Length * 2, MaximumBufferCapacity));
            }
        }

        private bool IsEnvironmentCollider(Collider collider) =>
            collider != null && collider.enabled && !_selfColliders.Contains(collider) &&
            (collider.attachedRigidbody == null || collider.attachedRigidbody != _rigidbody);

        private static bool IsSupportedPlayerCollider(Collider collider) =>
            collider != null && collider.enabled && !collider.isTrigger &&
            (collider is CapsuleCollider || collider is BoxCollider || collider is SphereCollider);

        private static Vector3 Abs(Vector3 value) =>
            new Vector3(Mathf.Abs(value.x), Mathf.Abs(value.y), Mathf.Abs(value.z));

        private sealed class RaycastHitDistanceComparer : IComparer<RaycastHit>
        {
            public static readonly RaycastHitDistanceComparer Instance = new RaycastHitDistanceComparer();

            public int Compare(RaycastHit left, RaycastHit right) => left.distance.CompareTo(right.distance);
        }
    }
}
