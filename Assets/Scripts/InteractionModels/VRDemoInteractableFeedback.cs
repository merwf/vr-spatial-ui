using TMPro;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// XR Interaction Toolkit hover/select olaylarina renk ve bilgi geri bildirimi ekler.
/// XRSimpleInteractable ve XRGrabInteractable ile kullanilabilir.
/// </summary>
[RequireComponent(typeof(XRBaseInteractable))]
public sealed class VRDemoInteractableFeedback : MonoBehaviour
{
    [SerializeField] private Renderer visual;
    [SerializeField] private VRDemoStatus status;
    [SerializeField] private TMP_Text description;
    [SerializeField] private string modeName = "Ray";
    [SerializeField] private Color idleColor = new Color(0.10f, 0.55f, 0.90f);
    [SerializeField] private Color hoverColor = new Color(1.00f, 0.80f, 0.20f);
    [SerializeField] private Color selectedColor = new Color(0.20f, 1.00f, 0.50f);

    private XRBaseInteractable interactable;
    private MaterialPropertyBlock propertyBlock;
    private bool isHovered;
    private bool isSelected;
    private int baseColorId;
    private int legacyColorId;

    public void Configure(Renderer target, VRDemoStatus reporter, TMP_Text label,
                          string mode, Color normal)
    {
        visual = target;
        status = reporter;
        description = label;
        modeName = mode;
        idleColor = normal;
        ApplyColor(idleColor);
    }

    private void Awake()
    {
        interactable = GetComponent<XRBaseInteractable>();
        if (visual == null)
            visual = GetComponentInChildren<Renderer>();
        baseColorId = Shader.PropertyToID("_BaseColor");
        legacyColorId = Shader.PropertyToID("_Color");
    }

    private void OnEnable()
    {
        if (interactable == null)
            interactable = GetComponent<XRBaseInteractable>();
        interactable.hoverEntered.AddListener(OnHoverEntered);
        interactable.hoverExited.AddListener(OnHoverExited);
        interactable.selectEntered.AddListener(OnSelectEntered);
        interactable.selectExited.AddListener(OnSelectExited);
        ApplyColor(idleColor);
    }

    private void OnDisable()
    {
        if (interactable == null) return;
        interactable.hoverEntered.RemoveListener(OnHoverEntered);
        interactable.hoverExited.RemoveListener(OnHoverExited);
        interactable.selectEntered.RemoveListener(OnSelectEntered);
        interactable.selectExited.RemoveListener(OnSelectExited);
        isHovered = false;
        isSelected = false;
    }

    private void OnHoverEntered(HoverEnterEventArgs args)
    {
        isHovered = true;
        RefreshColor();
        if (description != null)
            description.text = modeName + " - HEDEF ALGILANDI";
        if (status != null) status.Report(modeName, "Hover / hedef algilandi");
    }

    private void OnHoverExited(HoverExitEventArgs args)
    {
        isHovered = interactable.isHovered;
        RefreshColor();
        if (!isSelected && description != null)
            description.text = modeName + " - HAZIR";
    }

    private void OnSelectEntered(SelectEnterEventArgs args)
    {
        isSelected = true;
        RefreshColor();
        if (description != null)
            description.text = modeName + " - SECILDI";
        if (status != null) status.Report(modeName, "Select / secildi");
    }

    private void OnSelectExited(SelectExitEventArgs args)
    {
        isSelected = interactable.isSelected;
        isHovered = interactable.isHovered;
        RefreshColor();
        if (description != null)
            description.text = modeName + (isHovered ? " - HEDEF ALGILANDI" : " - HAZIR");
        if (status != null) status.Report(modeName, "Select / birakildi");
    }

    private void RefreshColor()
    {
        ApplyColor(isSelected ? selectedColor : isHovered ? hoverColor : idleColor);
    }

    private void ApplyColor(Color color)
    {
        if (visual == null) return;
        if (propertyBlock == null) propertyBlock = new MaterialPropertyBlock();
        // Bu yontem sharedMaterial'i degistirmez; proje genelindeki materyalleri korur.
        visual.GetPropertyBlock(propertyBlock);
        Material material = visual.sharedMaterial;
        if (material != null && material.HasProperty("_BaseColor"))
            propertyBlock.SetColor(Shader.PropertyToID("_BaseColor"), color);
        if (material != null && material.HasProperty("_Color"))
            propertyBlock.SetColor(Shader.PropertyToID("_Color"), color);
        visual.SetPropertyBlock(propertyBlock);
    }
}
