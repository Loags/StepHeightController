using UnityEngine;
using UnityEngine.Scripting.APIUpdating;
using UnityEngine.UI;

namespace Loags.StepHeight.Samples
{
    [MovedFrom(true, sourceNamespace: "LB.StepHeight.Samples", sourceAssembly: "LB.StepHeight.Samples", sourceClassName: "DemoHud")]
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
