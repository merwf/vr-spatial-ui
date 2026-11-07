using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Hover / press / click feedback for XR ray (or mouse) on uGUI buttons and sliders.
/// Replaces ButtonHoverEffect: do not keep both on the same object.
/// If the object has an Animator driving scale, remove it or it will override the scale.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Selectable))]
public sealed class XRButtonFeedback : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler,
    IPointerUpHandler, IPointerClickHandler
{
    [Header("Scale")]
    [SerializeField] private float hoverScale = 1.08f;
    [SerializeField] private float pressedScale = 0.95f;
    [SerializeField] private float speed = 14f;

    [Header("Label color")]
    [SerializeField] private bool tintLabel = true;
    [SerializeField] private TMP_Text label;   // auto-found in children if empty
    [SerializeField] private Color normalColor = new(0.7f, 0.7f, 0.7f, 1f);
    [SerializeField] private Color hoverColor = Color.white;

    [Header("Glow (optional child Image, alpha only)")]
    [SerializeField] private Image glow;
    [SerializeField, Range(0f, 1f)] private float hoverGlowAlpha = 0.35f;
    [SerializeField, Range(0f, 1f)] private float pressedGlowAlpha = 0.6f;

    [Header("Audio (optional)")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverClip;
    [SerializeField] private AudioClip clickClip;
    [SerializeField, Range(0f, 1f)] private float volume = 0.5f;
    [SerializeField, Range(0f, 0.15f)] private float pitchVariation = 0.05f;

    private Selectable selectable;
    private Vector3 baseScale;
    private float targetScale = 1f;
    private float targetGlow;
    private bool hovered, pressed;

    private void Awake()
    {
        selectable = GetComponent<Selectable>();
        baseScale = transform.localScale;
        if (label == null) label = GetComponentInChildren<TMP_Text>();
        if (tintLabel && label != null) label.color = normalColor;
    }

    private void OnDisable()
    {
        // The menu fades out and gets deactivated; don't leave a stuck hover state.
        hovered = pressed = false;
        targetScale = 1f;
        targetGlow = 0f;
        transform.localScale = baseScale;
        if (tintLabel && label != null) label.color = normalColor;
        SetGlowAlpha(0f);
    }

    private void Update()
    {
        float k = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);

        transform.localScale = Vector3.Lerp(transform.localScale, baseScale * targetScale, k);

        if (tintLabel && label != null)
            label.color = Color.Lerp(label.color, (hovered || pressed) ? hoverColor : normalColor, k);

        if (glow != null)
            SetGlowAlpha(Mathf.Lerp(glow.color.a, targetGlow, k));
    }

    public void OnPointerEnter(PointerEventData e)
    {
        if (!selectable.IsInteractable()) return;
        hovered = true;
        Refresh();
        Play(hoverClip, 0.6f);
    }

    public void OnPointerExit(PointerEventData e)
    {
        hovered = pressed = false;
        Refresh();
    }

    public void OnPointerDown(PointerEventData e)
    {
        if (!selectable.IsInteractable()) return;
        pressed = true;
        Refresh();
    }

    public void OnPointerUp(PointerEventData e)
    {
        pressed = false;
        Refresh();
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (selectable.IsInteractable()) Play(clickClip, 1f);
    }

    private void Refresh()
    {
        targetScale = pressed ? pressedScale : hovered ? hoverScale : 1f;
        targetGlow = pressed ? pressedGlowAlpha : hovered ? hoverGlowAlpha : 0f;
    }

    private void SetGlowAlpha(float a)
    {
        if (glow == null) return;
        Color c = glow.color;
        c.a = a;
        glow.color = c;
    }

    private void Play(AudioClip clip, float gain)
    {
        if (audioSource == null || clip == null) return;
        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(clip, volume * gain);
    }
}