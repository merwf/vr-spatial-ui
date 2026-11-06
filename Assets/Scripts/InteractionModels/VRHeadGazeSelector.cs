using UnityEngine;

/// <summary>
/// Kamera (HEAD-GAZE) dogrultusunda Physics.Raycast yapar.
/// Eye-tracking olmadiginda da XR Device Simulator ile test edilebilir.
/// </summary>
public sealed class VRHeadGazeSelector : MonoBehaviour
{
    [SerializeField] private Camera xrCamera;
    [SerializeField] private VRGazeDwellTarget target;
    [SerializeField, Min(0.1f)] private float dwellSeconds = 2f;
    [SerializeField, Min(0.5f)] private float rayDistance = 6f;

    private float elapsed;
    private bool focused;
    private bool triggered;

    public void Configure(Camera camera, VRGazeDwellTarget gazeTarget, float seconds = 2f)
    {
        xrCamera = camera;
        target = gazeTarget;
        dwellSeconds = Mathf.Max(0.1f, seconds);
    }

    private void Update()
    {
        if (xrCamera == null || target == null) return;

        Ray gazeRay = new Ray(xrCamera.transform.position, xrCamera.transform.forward);
        bool hitTarget = Physics.Raycast(gazeRay, out RaycastHit hit, rayDistance,
                                        Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore)
                         && hit.collider.GetComponentInParent<VRGazeDwellTarget>() == target;

        if (!hitTarget)
        {
            if (focused) target.SetFocused(false);
            focused = false;
            triggered = false;
            elapsed = 0f;
            return;
        }

        if (!focused)
        {
            focused = true;
            target.SetFocused(true);
        }

        if (triggered) return;
        elapsed += Time.deltaTime;
        target.SetProgress(elapsed / dwellSeconds);
        if (elapsed >= dwellSeconds)
        {
            triggered = true;
            target.Activate();
        }
    }
}
