using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Locomotion.Turning;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;
public class LunarPanelController : MonoBehaviour
{
    [Header("Yerçekimi")]
    [SerializeField] private TMP_Text gravityLabel;
    private const float MoonGravity = -1.62f;
    private const float EarthGravity = -9.81f;
    private bool isMoon = true;

    [Header("Işık")]
    [SerializeField] private Light directionalLight;
    [SerializeField] private Slider lightSlider;
    [SerializeField] private float maxIntensity = 2f;

    [Header("Dönüş")]
    [SerializeField] private SnapTurnProvider snapTurnProvider;
    [SerializeField] private ContinuousTurnProvider continuousTurnProvider;
    [Tooltip("Left ve Right Controller objelerindeki ControllerInputActionManager")]
    [SerializeField] private ControllerInputActionManager[] controllerManagers;
    [SerializeField] private TMP_Text snapTurnLabel;
    private bool snapTurnOn = true;

    private void Start()
    {
        ApplyGravity();
        ApplySnapTurn();

        if (lightSlider != null && directionalLight != null)
        {
            lightSlider.SetValueWithoutNotify(directionalLight.intensity / maxIntensity);
            lightSlider.onValueChanged.AddListener(SetLightIntensity);
        }
    }

    private void OnDestroy()
    {
        if (lightSlider != null)
        {
            lightSlider.onValueChanged.RemoveListener(SetLightIntensity);
        }
    }

    public void ToggleGravity()
    {
        isMoon = !isMoon;
        ApplyGravity();
    }

    private void ApplyGravity()
    {
        Physics.gravity = new Vector3(0f, isMoon ? MoonGravity : EarthGravity, 0f);
        if (gravityLabel != null)
        {
            gravityLabel.text = isMoon ? "Gravity: MOON" : "Gravity: EARTH";
        }
    }

    public void SetLightIntensity(float value)
    {
        if (directionalLight != null)
        {
            directionalLight.intensity = value * maxIntensity;
        }
    }

    public void ToggleSnapTurn()
    {
        snapTurnOn = !snapTurnOn;
        ApplySnapTurn();
    }

    private void ApplySnapTurn()
    {
        // Input action'ları doğru olana yönlendirir (asıl gerekli kısım)
        foreach (var manager in controllerManagers)
            if (manager != null)
                manager.smoothTurnEnabled = !snapTurnOn;

        // Ek güvence: aktif olmayan provider tamamen sussun
        if (snapTurnProvider != null) snapTurnProvider.enabled = snapTurnOn;
        if (continuousTurnProvider != null) continuousTurnProvider.enabled = !snapTurnOn;

        if (snapTurnLabel != null)
            snapTurnLabel.text = snapTurnOn ? "Snap Turn: ON" : "Snap Turn: OFF";
    }
}