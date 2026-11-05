using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// Buton/slider üzerine XR ışını (veya fare) geldiğinde büyüme + renk değişimi.
/// Hem panel butonlarında hem ana menüde aynı script kullanılabilir.
/// NOT: Bu objede Animator varsa kaldırın, yoksa scale'i her karede ezer.
/// </summary>
[DisallowMultipleComponent]
public class ButtonHoverEffect : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Ölçek")]
    [SerializeField] float scaleAmount = 1.15f;
    [SerializeField] float speed = 10f;

    [Header("Yazı Rengi")]
    [SerializeField] Color normalColor = new Color(0.7f, 0.7f, 0.7f);
    [SerializeField] Color hoverColor = Color.white;

    [Header("Ses (isteğe bağlı)")]
    [SerializeField] AudioSource audioSource;
    [SerializeField] AudioClip hoverClip;

    Vector3 originalScale;
    TMP_Text label;
    bool isHovered;

    void Awake()
    {
        originalScale = transform.localScale;
        label = GetComponentInChildren<TMP_Text>();
        if (label != null) label.color = normalColor;
    }

    void OnDisable()
    {
        // Panel kapanıp açılırsa buton büyük kalmasın
        isHovered = false;
        transform.localScale = originalScale;
        if (label != null) label.color = normalColor;
    }

    void Update()
    {
        // Kare hızından bağımsız yumuşatma
        float t = 1f - Mathf.Exp(-speed * Time.unscaledDeltaTime);

        Vector3 targetScale = isHovered ? originalScale * scaleAmount : originalScale;
        transform.localScale = Vector3.Lerp(transform.localScale, targetScale, t);

        if (label != null)
            label.color = Color.Lerp(label.color, isHovered ? hoverColor : normalColor, t);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovered = true;
        if (audioSource != null && hoverClip != null)
            audioSource.PlayOneShot(hoverClip);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovered = false;
    }
}