using Unity.Cinemachine;
using UnityEngine;

namespace LB.StepHeight.Samples
{
    [DisallowMultipleComponent]
    public sealed class DemoCameraReset : MonoBehaviour
    {
        [SerializeField] private MovementController movementController;
        [SerializeField] private CinemachineCamera demoCamera;

        private void OnEnable() => movementController.PlayerReset += ResetCamera;

        private void OnDisable() => movementController.PlayerReset -= ResetCamera;

        private void ResetCamera(Vector3 displacement)
        {
            // A reset is a teleport, so the camera must discard its previous damping history.
            demoCamera.OnTargetObjectWarped(demoCamera.Follow, displacement);
            demoCamera.PreviousStateIsValid = false;
        }
    }
}
