using System.Collections;
using UnityEngine;

public class QuitDialogAnimator : MonoBehaviour
{
    [Header("Panels / Groups")]
    [SerializeField] private CanvasGroup mainContentGroup;  // Ana butonların olduğu grup
    [SerializeField] private CanvasGroup quitDialogGroup;   // Çıkış onay penceresi

    [Header("Animation Settings")]
    [SerializeField] private float fadeDuration = 0.25f;

    private Coroutine activeTransition;

    private void Awake()
    {
        // Başlangıç durumu: Ana menü görünür, onay penceresi gizli ve tıklanamaz
        if (mainContentGroup != null)
        {
            mainContentGroup.alpha = 1f;
            SetGroupInteractable(mainContentGroup, true);
        }

        if (quitDialogGroup != null)
        {
            quitDialogGroup.alpha = 0f;
            SetGroupInteractable(quitDialogGroup, false);
            quitDialogGroup.gameObject.SetActive(false);
        }
    }

    // Kırmızı Quit butonuna basıldığında çağrılır
    public void OpenQuitDialog()
    {
        StartTransition(showQuitDialog: true);
    }

    // "CANCEL" butonuna basıldığında çağrılır
    public void CloseQuitDialog()
    {
        StartTransition(showQuitDialog: false);
    }

    private void StartTransition(bool showQuitDialog)
    {
        if (activeTransition != null)
            StopCoroutine(activeTransition);

        activeTransition = StartCoroutine(FadeRoutine(showQuitDialog));
    }

    private IEnumerator FadeRoutine(bool showQuitDialog)
    {
        float elapsed = 0f;

        float mainStart = mainContentGroup != null ? mainContentGroup.alpha : 1f;
        float mainTarget = showQuitDialog ? 0f : 1f;

        float quitStart = quitDialogGroup != null ? quitDialogGroup.alpha : 0f;
        float quitTarget = showQuitDialog ? 1f : 0f;

        if (showQuitDialog && quitDialogGroup != null)
        {
            quitDialogGroup.gameObject.SetActive(true);
        }

        // Geçiş sırasında yanlış tıklamaları önlemek için etkileşimleri kapat
        if (mainContentGroup != null) SetGroupInteractable(mainContentGroup, false);
        if (quitDialogGroup != null) SetGroupInteractable(quitDialogGroup, false);

        while (elapsed < fadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / fadeDuration);

            if (mainContentGroup != null)
                mainContentGroup.alpha = Mathf.Lerp(mainStart, mainTarget, t);

            if (quitDialogGroup != null)
                quitDialogGroup.alpha = Mathf.Lerp(quitStart, quitTarget, t);

            yield return null;
        }

        // Bitiş değerlerini sabitle ve etkileşimi aç
        if (mainContentGroup != null)
        {
            mainContentGroup.alpha = mainTarget;
            SetGroupInteractable(mainContentGroup, !showQuitDialog);
        }

        if (quitDialogGroup != null)
        {
            quitDialogGroup.alpha = quitTarget;
            SetGroupInteractable(quitDialogGroup, showQuitDialog);

            if (!showQuitDialog)
                quitDialogGroup.gameObject.SetActive(false);
        }

        activeTransition = null;
    }

    private void SetGroupInteractable(CanvasGroup group, bool state)
    {
        group.interactable = state;
        group.blocksRaycasts = state;
    }
}