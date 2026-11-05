using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.XR.Interaction.Toolkit.Samples.StarterAssets;

/// <summary>
/// LUNAR INTERFACE panelinin tüm işlevleri:
/// Gravity (Ay/Dünya), Light Intensity (slider), Snap Turn (açık/kapalı).
/// Canvas'a ekleyin, alanları Inspector'dan atayın.
/// </summary>
public class LunarPanelController : MonoBehaviour
{
    [Header("Yerçekimi")]
    [SerializeField] TMP_Text gravityLabel;
    const float MoonGravity = -1.62f;
    const float EarthGravity = -9.81f;
    bool isMoon = true;

    [Header("Işık")]
    [SerializeField] Light directionalLight;
    [SerializeField] Slider lightSlider;      // Min 0, Max 1
    [SerializeField] float maxIntensity = 2f;

    [Header("Dönüş (Snap / Continuous)")]
    [Tooltip("XR Origin altındaki Left ve Right Controller objelerindeki ControllerInputActionManager")]
    [SerializeField] ControllerInputActionManager[] controllerManagers;
    [SerializeField] TMP_Text snapTurnLabel;
    bool snapTurnOn = true;

    void Start()
    {
        ApplyGravity();
        ApplySnapTurn();

        if (lightSlider != null && directionalLight != null)
        {
            lightSlider.SetValueWithoutNotify(directionalLight.intensity / maxIntensity);
            lightSlider.onValueChanged.AddListener(SetLightIntensity);
        }
    }

    // ---- Gravity: Button1 OnClick'e bağlanır ----
    public void ToggleGravity()
    {
        isMoon = !isMoon;
        ApplyGravity();
    }

    void ApplyGravity()
    {
        Physics.gravity = new Vector3(0f, isMoon ? MoonGravity : EarthGravity, 0f);
        if (gravityLabel != null)
            gravityLabel.text = isMoon ? "Gravity: MOON" : "Gravity: EARTH";
    }

    // ---- Light: kod içinden slider'a bağlanıyor, Inspector'da ayrıca bağlamayın ----
    public void SetLightIntensity(float value)
    {
        if (directionalLight != null)
            directionalLight.intensity = value * maxIntensity;
    }

    // ---- Snap Turn: Button2 OnClick'e bağlanır ----
    public void ToggleSnapTurn()
    {
        snapTurnOn = !snapTurnOn;
        ApplySnapTurn();
    }

    void ApplySnapTurn()
    {
        foreach (var manager in controllerManagers)
            if (manager != null)
                manager.smoothTurnEnabled = !snapTurnOn;

        if (snapTurnLabel != null)
            snapTurnLabel.text = snapTurnOn ? "Snap Turn: ON" : "Snap Turn: OFF";
    }
}