using UnityEngine;
using UnityEngine.UI;

namespace LB.StepHeight.Samples
{
    [DisallowMultipleComponent]
    public sealed class DemoHud : MonoBehaviour
    {
        [SerializeField] private StepHeightController stepController;
        [SerializeField] private Text statusLabel;
        [SerializeField] private Text controlsLabel;

        private void Awake() => controlsLabel.text = "WASD Move   Mouse Look   Shift Sprint   Space Jump\nT Toggle stepping   R Reset   Esc Release cursor   Click Resume";

        private void Update()
        {
            statusLabel.text = stepController.SteppingEnabled ? "STEPPING ON" : "STEPPING OFF";
        }
    }
}
