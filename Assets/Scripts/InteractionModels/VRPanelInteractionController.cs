using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

/// <summary>
/// XR ray UI / grab / thumbstick zoom / head-gaze reset / menu toggle / follow mode (auto-manual)
/// </summary>
[DisallowMultipleComponent]
public sealed class VRPanelInteractionController : MonoBehaviour
{
    [Header("Mevcut sahne referanslari")]
    [SerializeField] private RectTransform worldCanvas;
    [SerializeField] private GameObject existingPanel;
    [SerializeField] private Camera headsetCamera;

    [Header("Yeni kontroller")]
    [SerializeField] private XRSimpleInteractable grabHandle;
    [SerializeField] private Image grabHandleImage;
    [SerializeField] private TMP_Text grabHandleText;
    [SerializeField] private Button closeMenuButton;
    [SerializeField] private Button openMenuButton;
    [SerializeField] private Button resetButton;
    [SerializeField] private Collider resetGazeCollider;
    [SerializeField] private Image gazeProgressFill;
    [SerializeField] private TMP_Text statusText;

    [Header("Starter Assets Input Actions")]
    [SerializeField] private InputActionAsset xriInputActions;

    [Header("Konfor ve hareket")]
    [SerializeField, Range(1.0f, 4.0f)] private float resetForwardDistance = 2.35f;
    [SerializeField] private float resetVerticalOffset = -2.0f;
    [SerializeField] private float resetPitchDegrees = 25.0f;
    [SerializeField, Range(0.4f, 2.5f)] private float joystickZoomMetersPerSecond = 0.8f;
    [SerializeField, Min(0.25f)] private float minimumForwardDistance = 1.15f;
    [SerializeField, Min(1.0f)] private float maximumForwardDistance = 4.5f;
    [SerializeField, Range(0.5f, 4.0f)] private float gazeDwellSeconds = 2.0f;
    
    [Header("Durum Mesajlari")]

    [SerializeField, Min(0.1f)]
    private float statusDisplaySeconds = 3f;

    private Coroutine statusHideCoroutine;

    private static readonly Color ReadyColor = new(0.12f, 0.43f, 0.72f, 0.94f);
    private static readonly Color HoverColor = new (0.90f, 0.61f, 0.12f, 0.96f);
    private static readonly Color GrabColor = new (0.10f, 0.64f, 0.49f, 0.96f);

    private InputAction leftThumbstick;
    private InputAction rightThumbstick;
    private bool enabledLeftByUs;
    private bool enabledRightByUs;
    private InputAction selectedThumbstick;
    private Transform grabbingInteractor;
    private Vector3 gripStartInteractorPosition;
    private Vector3 gripStartPanelPosition;
    private float gripDepthOffset;
    private bool grabbed;
    private bool panelVisible = true;
    private float gazeElapsed;
    private bool gazeTriggered;
    private RectTransform gazeCircleRect;
    private Texture2D gazeCircleTexture;
    private Sprite gazeCircleSprite;
    private MonoBehaviour[] originalFollowers;
    private bool[] originalFollowerStates;
    private readonly List<Behaviour> suppressedLocomotion = new();

    #region Reset Panel Smoothness
    [Header("Reset Animation")]
    [SerializeField, Min(0.05f)]
    private float resetAnimationDuration = 0.45f;

    private Coroutine resetAnimationCoroutine;
    private bool isResetting;
    private bool grabHandleWasEnabledBeforeReset;
    #endregion
    #region Panel Smoothness
    [Header("Menu Transition")]
    [SerializeField, Min(0.05f)]
    private float menuTransitionDuration = 0.5f;

    [SerializeField, Range(0.8f, 1f)]
    private float hiddenScaleMultiplier = 0.84f;

    private CanvasGroup panelCanvasGroup;
    private Vector3 panelVisibleScale;
    private Coroutine menuTransitionCoroutine;
    private bool originalPanelInteractable;
    private bool originalPanelBlocksRaycasts;

    private Vector3 hiddenVelocity;
    #endregion

    #region Follow Mode
    // AUTO   = Canvas'taki LazyFollow acik, panel kullaniciyi takip eder. Tutamac ve reset gizlenir.
    // MANUAL = LazyFollow kapali, panel tutamac (grip) ile tasinir. Mevcut davranis.
    public enum FollowMode { Manual, Auto }

    [Header("Takip Modu (Auto = LazyFollow, Manual = Grip)")]
    [SerializeField] private FollowMode followMode = FollowMode.Manual;
    [SerializeField] private Button followModeButton;
    [SerializeField] private TMP_Text followModeLabel;
    #endregion

    #region Feedback
    private enum HandleState { Ready, Hover, Grabbed }
    private Color handleTargetColor = new(0.12f, 0.43f, 0.72f, 0.94f);
    private float handleTargetScale = 1f;
    private Vector3 handleBaseScale = Vector3.one;
    #endregion

    /// <summary>Editor kurulum aracı referansları sahneye kalıcı olarak bağlar.</summary>
    public void Configure(RectTransform canvas, GameObject panel, Camera camera,
        XRSimpleInteractable handle, Image handleImage, TMP_Text handleLabel,
        Button closeButton, Button openButton, Button gazeResetButton,
        Collider gazeCollider, Image progress, TMP_Text status, InputActionAsset inputActions)
    {
        worldCanvas = canvas;
        existingPanel = panel;
        headsetCamera = camera;
        grabHandle = handle;
        grabHandleImage = handleImage;
        grabHandleText = handleLabel;
        closeMenuButton = closeButton;
        openMenuButton = openButton;
        resetButton = gazeResetButton;
        resetGazeCollider = gazeCollider;
        gazeProgressFill = progress;
        statusText = status;
        xriInputActions = inputActions;
    }

    private void Start()
    {
        if (worldCanvas == null) worldCanvas = transform as RectTransform;
        if (headsetCamera == null) headsetCamera = Camera.main;
        if (worldCanvas == null || headsetCamera == null || existingPanel == null)
        {
            Debug.LogError("[VR Menu] Canvas, Panel ve XR Camera referanslari gerekli.", this);
            enabled = false;
            return;
        }

        // Mevcut LazyFollow/LazyFollowYawOnly paneli geri çekmesin.
        // Orijinal durumlar saklanır; sahne/prefab dosyası değiştirilmez.
        // (AUTO modunda SetFollowMode bu orijinal durumlari geri yukler.)
        var behaviours = worldCanvas.GetComponents<MonoBehaviour>();
        var followers = new List<MonoBehaviour>();
        foreach (var behaviour in behaviours)
        {
            if (behaviour == null || behaviour == this) continue;
            if (behaviour.GetType().Name.Contains("LazyFollow")) followers.Add(behaviour);
        }
        originalFollowers = followers.ToArray();
        originalFollowerStates = new bool[originalFollowers.Length];
        for (int i = 0; i < originalFollowers.Length; i++)
        {
            originalFollowerStates[i] = originalFollowers[i].enabled;
            originalFollowers[i].enabled = false;
        }

        if (xriInputActions != null)
        {
            var leftMap = xriInputActions.FindActionMap("XRI Left", false);
            var rightMap = xriInputActions.FindActionMap("XRI Right", false);
            leftThumbstick = leftMap?.FindAction("Thumbstick", false);
            rightThumbstick = rightMap?.FindAction("Thumbstick", false);
            if (leftThumbstick != null && !leftThumbstick.enabled) { leftThumbstick.Enable(); enabledLeftByUs = true; }
            if (rightThumbstick != null && !rightThumbstick.enabled) { rightThumbstick.Enable(); enabledRightByUs = true; }
        }

        if (grabHandle != null)
        {
            grabHandle.hoverEntered.AddListener(OnHoverEnter);
            grabHandle.hoverExited.AddListener(OnHoverExit);
            grabHandle.selectEntered.AddListener(OnGrabEnter);
            grabHandle.selectExited.AddListener(OnGrabExit);
        }
        if (closeMenuButton != null) closeMenuButton.onClick.AddListener(CloseMenu);
        if (openMenuButton != null) openMenuButton.onClick.AddListener(OpenMenu);
        if (followModeButton != null) followModeButton.onClick.AddListener(ToggleFollowMode);

        InitializeGazeCircle();
        ResetMenuPosition();

        // Initialization of panel smoothness references
        panelCanvasGroup = existingPanel.GetComponent<CanvasGroup>();

        if (panelCanvasGroup == null)
            panelCanvasGroup = existingPanel.AddComponent<CanvasGroup>();

        panelVisibleScale = existingPanel.transform.localScale;

        originalPanelInteractable = panelCanvasGroup.interactable;
        originalPanelBlocksRaycasts = panelCanvasGroup.blocksRaycasts;
        
        SetMenuVisible(true);
        if (grabHandleImage != null) handleBaseScale = grabHandleImage.rectTransform.localScale;
        SetHandleState(HandleState.Ready);
        SetFollowMode(followMode, false);
        SetStatus("MENU HAZIR");
    }

    private void OnDestroy()
    {
        if (grabHandle != null)
        {
            grabHandle.hoverEntered.RemoveListener(OnHoverEnter);
            grabHandle.hoverExited.RemoveListener(OnHoverExit);
            grabHandle.selectEntered.RemoveListener(OnGrabEnter);
            grabHandle.selectExited.RemoveListener(OnGrabExit);
        }
        if (closeMenuButton != null) closeMenuButton.onClick.RemoveListener(CloseMenu);
        if (openMenuButton != null) openMenuButton.onClick.RemoveListener(OpenMenu);
        if (followModeButton != null) followModeButton.onClick.RemoveListener(ToggleFollowMode);
        ReleaseLocomotion();
        if (originalFollowers != null)
        {
            for (int i = 0; i < originalFollowers.Length; i++)
                if (originalFollowers[i] != null) originalFollowers[i].enabled = originalFollowerStates[i];
        }
        if (gazeCircleSprite != null) Destroy(gazeCircleSprite);
        if (gazeCircleTexture != null) Destroy(gazeCircleTexture);
        if (enabledLeftByUs && leftThumbstick != null) leftThumbstick.Disable();
        if (enabledRightByUs && rightThumbstick != null) rightThumbstick.Disable();
    }

    private void Update()
    {
        AnimateHandle();
        if (!panelVisible || grabbed || isResetting || followMode == FollowMode.Auto ||
            resetGazeCollider == null || headsetCamera == null)
        {
            ClearGaze();
            return;
        }

        Ray ray = new (headsetCamera.transform.position, headsetCamera.transform.forward);
        // Gaze düğmesi Ignore Raycast katmanındadır; XR kontrolcü ışını UI tıklamasını engellemez.
        const int gazeLayerMask = 1 << 2;
        bool focused = Physics.Raycast(ray, out RaycastHit hit, 8f, gazeLayerMask, QueryTriggerInteraction.Collide)
                       && hit.collider == resetGazeCollider;

        if (!focused)
        {
            ClearGaze();
            return;
        }

        if (gazeTriggered) return; // Başka yöne bakana kadar tekrar reset atma.
        gazeElapsed += Time.deltaTime;
        UpdateGazeCircle(Mathf.Clamp01(gazeElapsed / Mathf.Max(0.01f, gazeDwellSeconds)));
        if (gazeElapsed >= gazeDwellSeconds)
        {
            gazeTriggered = true;
            ResetMenuPosition();
            SetStatus("HEAD-GAZE | 2 saniye bakis ile menu sifirlandi");
        }
    }

    private void LateUpdate()
    {
        if (worldCanvas == null || headsetCamera == null) return;

        if (!panelVisible)
        {
            // Tekrar aç düğmesi görüş alanında kalsın.
            // AUTO modunda bunu zaten LazyFollow yapiyor; ikisi cekismesin.
            if (followMode == FollowMode.Manual) ApplyDefaultPose(true);
            return;
        }

        if (!grabbed || grabbingInteractor == null) return;

        if (isResetting) return;

        float axis = selectedThumbstick != null ? selectedThumbstick.ReadValue<Vector2>().y : 0f;
        if (Mathf.Abs(axis) < 0.18f)
        {
            // Kullanılan elde stick tanımlı değilse diğer el fallback.
            InputAction other = selectedThumbstick == leftThumbstick ? rightThumbstick : leftThumbstick;
            if (other != null) axis = other.ReadValue<Vector2>().y;
        }

        if (Keyboard.current != null)
        {

            float keyboardAxis = 0f;

            if (Keyboard.current.pageUpKey.isPressed){
                keyboardAxis += 1f;
            }

            if (Keyboard.current.pageDownKey.isPressed){
                keyboardAxis -= 1f;
            }

            if (Mathf.Abs(keyboardAxis) > 0.01f)
                axis = keyboardAxis;
        }

        if (Mathf.Abs(axis) > 0.18f)
            gripDepthOffset -= axis * joystickZoomMetersPerSecond * Time.deltaTime;

        Vector3 headForward = HorizontalForward();

        Vector3 interactorDelta =
            grabbingInteractor.position - gripStartInteractorPosition;

        float forwardDelta = Vector3.Dot(interactorDelta, headForward);

        Vector3 candidate =
            gripStartPanelPosition + headForward * (forwardDelta + gripDepthOffset);

        float depth = Vector3.Dot(
            candidate - headsetCamera.transform.position,
            headForward
        );

        float clampedDepth = Mathf.Clamp(
            depth,
            minimumForwardDistance,
            maximumForwardDistance
        );

        worldCanvas.position =
            candidate + headForward * (clampedDepth - depth);
    }

    private Vector3 HorizontalForward()
    {
        Vector3 v = Vector3.ProjectOnPlane(headsetCamera.transform.forward, Vector3.up);
        return v.sqrMagnitude > 0.001f ? v.normalized : Vector3.forward;
    }
    
    private void ApplyDefaultPose(bool smooth = false)
    {
        Vector3 forward = HorizontalForward();
        Vector3 targetPos = headsetCamera.transform.position +
                            forward * resetForwardDistance + Vector3.up * resetVerticalOffset;
        Quaternion targetRot = Quaternion.LookRotation(forward, Vector3.up) *
                            Quaternion.Euler(resetPitchDegrees, 0f, 0f);

        if (smooth)
        {
            worldCanvas.position = Vector3.SmoothDamp(
                worldCanvas.position, targetPos, ref hiddenVelocity, 0.25f);
            worldCanvas.rotation = Quaternion.Slerp(
                worldCanvas.rotation, targetRot, 1f - Mathf.Exp(-8f * Time.unscaledDeltaTime));
        }
        else
        {
            worldCanvas.SetPositionAndRotation(targetPos, targetRot);
            hiddenVelocity = Vector3.zero;
        }
    }

    public void ResetMenuPosition()
    {
        EndGrab();

        // AUTO modunda paneli LazyFollow konumlandiriyor; reset animasyonu onunla cekismesin.
        if (followMode == FollowMode.Auto)
            return;

        if (worldCanvas == null || headsetCamera == null)
            return;

        // Cancel an existing reset without losing the original handle state.
        if (resetAnimationCoroutine != null)
        {
            StopCoroutine(resetAnimationCoroutine);
            resetAnimationCoroutine = null;

            if (grabHandle != null)
                grabHandle.enabled = grabHandleWasEnabledBeforeReset;
        }

        isResetting = true;
        ClearGaze();

        if (grabHandle != null)
        {
            grabHandleWasEnabledBeforeReset = grabHandle.enabled;
            grabHandle.enabled = false;
        }

        resetAnimationCoroutine = StartCoroutine(AnimateMenuReset());

        SetStatus("RESET | Menu varsayilan konuma tasiniyor");
    }
    private IEnumerator AnimateMenuReset()
    {
        Vector3 startPosition = worldCanvas.position;
        Quaternion startRotation = worldCanvas.rotation;

        Vector3 forward = HorizontalForward();

        Vector3 targetPosition =
            headsetCamera.transform.position +
            forward * resetForwardDistance +
            Vector3.up * resetVerticalOffset;

        Quaternion targetRotation =
            Quaternion.LookRotation(forward, Vector3.up) *
            Quaternion.Euler(resetPitchDegrees, 0f, 0f);

        float elapsed = 0f;

        while (elapsed < resetAnimationDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(elapsed / resetAnimationDuration);

            // Smoothstep easing.
            float eased = t * t * (3f - 2f * t);

            worldCanvas.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                eased
            );

            worldCanvas.rotation = Quaternion.Slerp(
                startRotation,
                targetRotation,
                eased
            );

            yield return null;
        }

        // Ensure the final pose is exact.
        worldCanvas.SetPositionAndRotation(
            targetPosition,
            targetRotation
        );

        if (grabHandle != null)
            grabHandle.enabled = grabHandleWasEnabledBeforeReset;

        isResetting = false;
        resetAnimationCoroutine = null;

        ClearGaze();

        SetStatus("RESET | Menu varsayilan konuma getirildi");
    }

    // ---------------------------------------------------------------
    // Follow Mode (AUTO / MANUAL)
    // ---------------------------------------------------------------

    /// <summary>Follow mode butonu bu metodu cagirir: AUTO <-> MANUAL.</summary>
    public void ToggleFollowMode()
    {
        SetFollowMode(followMode == FollowMode.Auto ? FollowMode.Manual : FollowMode.Auto, true);
    }

    private void SetFollowMode(FollowMode mode, bool announce)
    {
        followMode = mode;
        bool auto = mode == FollowMode.Auto;

        if (auto)
        {
            // Tutma veya reset animasyonu devam ediyorsa temizce bitir.
            EndGrab();
            ClearGaze();
            if (resetAnimationCoroutine != null)
            {
                StopCoroutine(resetAnimationCoroutine);
                resetAnimationCoroutine = null;
                isResetting = false;
                if (grabHandle != null) grabHandle.enabled = grabHandleWasEnabledBeforeReset;
            }
        }

        // LazyFollow: AUTO'da sahnedeki orijinal durumuna doner, MANUAL'da kapali.
        if (originalFollowers != null)
        {
            for (int i = 0; i < originalFollowers.Length; i++)
                if (originalFollowers[i] != null)
                    originalFollowers[i].enabled = auto && originalFollowerStates[i];
        }

        // AUTO modunda tutamac ve reset butonuna gerek yok.
        if (grabHandle != null) grabHandle.gameObject.SetActive(!auto);
        if (resetButton != null) resetButton.gameObject.SetActive(!auto);

        if (followModeLabel != null)
            followModeLabel.text = auto ? "MODE: AUTO" : "MODE: MANUAL";

        if (announce)
            SetStatus(auto
                ? "AUTO FOLLOW | Menu seni takip ediyor"
                : "MANUAL | Tutamaci grip ile tutarak tasi");
    }

    private void SetMenuVisible(bool visible)
    {
        panelVisible = visible;

        if (openMenuButton != null)
            openMenuButton.gameObject.SetActive(!visible);

        if (existingPanel == null) return;

        if (menuTransitionCoroutine != null)
        {
            StopCoroutine(menuTransitionCoroutine);
            menuTransitionCoroutine = null;
        }

        if (visible)
        {
            existingPanel.SetActive(true);
        }
        else
        {
            ClearGaze();
        }

        panelCanvasGroup.interactable = false;
        panelCanvasGroup.blocksRaycasts = false;

        menuTransitionCoroutine = StartCoroutine(
            AnimatePanelTransition(visible)
        );
    }

    private IEnumerator AnimatePanelTransition(bool show)
    {
        float startAlpha = panelCanvasGroup.alpha;
        Vector3 startScale = existingPanel.transform.localScale;

        float targetAlpha = show ? 1f : 0f;

        Vector3 targetScale = show
            ? panelVisibleScale
            : panelVisibleScale * hiddenScaleMultiplier;

        float elapsed = 0f;

        while (elapsed < menuTransitionDuration)
        {
            elapsed += Time.unscaledDeltaTime;

            float t = Mathf.Clamp01(
                elapsed / menuTransitionDuration
            );

            // Smoothstep easing
            float eased = t * t * (3f - 2f * t);

            panelCanvasGroup.alpha = Mathf.Lerp(
                startAlpha, targetAlpha, eased
            );

            existingPanel.transform.localScale = Vector3.Lerp(
                startScale, targetScale, eased
            );

            yield return null;
        }

        panelCanvasGroup.alpha = targetAlpha;
        existingPanel.transform.localScale = targetScale;

        if (show)
        {
            panelCanvasGroup.interactable = originalPanelInteractable;
            panelCanvasGroup.blocksRaycasts = originalPanelBlocksRaycasts;
        }
        else
        {
            existingPanel.SetActive(false);
        }

        menuTransitionCoroutine = null;
    }

    public void CloseMenu()
    {
        EndGrab();
        SetMenuVisible(false);
    }

    public void OpenMenu()
    {
        SetMenuVisible(true);
        ResetMenuPosition();
        SetStatus("MENU ACILDI | Ray ile butonlara basabilirsin");
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (!grabbed) SetHandleState(HandleState.Hover);
        if (!grabbed) SetStatus("HOVER | Tutamac hedeflendi");
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        if (!grabbed) SetHandleState(HandleState.Ready);
    }

    private void OnGrabEnter(SelectEnterEventArgs args)
    {
        if (followMode == FollowMode.Auto) return;
        if (isResetting || !panelVisible || args.interactorObject == null) return;
        grabbingInteractor = args.interactorObject.transform;
        gripStartInteractorPosition = grabbingInteractor.position;
        gripStartPanelPosition = worldCanvas.position;
        gripDepthOffset = 0f;
        selectedThumbstick = FindThumbstickForInteractor(grabbingInteractor);
        grabbed = true;
        ClearGaze();
        SuppressLocomotion();
        SetHandleState(HandleState.Grabbed);
        if (grabHandleText != null) grabHandleText.text = "TUTULUYOR | STICK / PgUp-PgDn";
        SetStatus("DIRECT GRAB | Kumandayi hareket ettir, stick ile mesafeyi ayarla");
    }

    private void OnGrabExit(SelectExitEventArgs args)
    {
        EndGrab();
        SetStatus("DIRECT GRAB | Menu birakildi");
    }

    private InputAction FindThumbstickForInteractor(Transform interactor)
    {
        for (Transform current = interactor; current != null; current = current.parent)
        {
            if (current.name.IndexOf("left", StringComparison.OrdinalIgnoreCase) >= 0) return leftThumbstick;
            if (current.name.IndexOf("right", StringComparison.OrdinalIgnoreCase) >= 0) return rightThumbstick;
        }
        return rightThumbstick ?? leftThumbstick;
    }

    private void EndGrab()
    {
        if (!grabbed) return;
        grabbed = false;
        grabbingInteractor = null;
        selectedThumbstick = null;
        gripDepthOffset = 0f;
        ReleaseLocomotion();
        SetHandleState(HandleState.Ready);
        if (grabHandleText != null) grabHandleText.text = "TUTAMAC | GRIP ILE TUT";
    }

    // Joystick'le zoom yaparken aynı zamanda yürüyüp dönmemek için
    // yalnızca grab süresince çalışan locomotion provider'larını askıya alır.
    private void SuppressLocomotion()
    {
        ReleaseLocomotion();
        foreach (var component in FindObjectsByType<MonoBehaviour>())
        {
            if (component == null || !component.enabled || component == this) continue;
            string n = component.GetType().Name;
            if (!n.Contains("MoveProvider") && !n.Contains("TurnProvider")) continue;
            suppressedLocomotion.Add(component);
            component.enabled = false;
        }
    }

    private void ReleaseLocomotion()
    {
        foreach (var component in suppressedLocomotion)
            if (component != null) component.enabled = true;
        suppressedLocomotion.Clear();
    }

    // The existing serialized gazeProgressFill Image is reused to avoid breaking scene references.
    // A soft circular sprite is generated in memory only during Play; no external texture is needed.
    private void InitializeGazeCircle()
    {
        if (gazeProgressFill == null) return;
        const int textureSize = 128;
        gazeCircleTexture = new(textureSize, textureSize, TextureFormat.RGBA32, false)
        {
            name = "VR Gaze Soft Circle (Runtime)",
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        Color32[] pixels = new Color32[textureSize * textureSize];
        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x + 0.5f - textureSize * 0.5f) / (textureSize * 0.5f);
                float dy = (y + 0.5f - textureSize * 0.5f) / (textureSize * 0.5f);
                float radius = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha = 1f - Mathf.SmoothStep(0.78f, 1.0f, radius);
                pixels[y * textureSize + x] = new Color(1f, 1f, 1f, alpha);
            }
        }
        gazeCircleTexture.SetPixels32(pixels);
        gazeCircleTexture.Apply(false, false);
        gazeCircleSprite = Sprite.Create(gazeCircleTexture,
            new Rect(0f, 0f, textureSize, textureSize), new Vector2(0.5f, 0.5f), 100f);
        gazeCircleSprite.name = "VR Gaze Soft Circle Sprite (Runtime)";
        gazeProgressFill.sprite = gazeCircleSprite;
        gazeProgressFill.type = Image.Type.Simple;
        gazeProgressFill.preserveAspect = true;
        gazeProgressFill.raycastTarget = false;
        gazeProgressFill.color = new Color(0.05f, 0.84f, 1.0f, 0.39f);
        gazeCircleRect = gazeProgressFill.rectTransform;
        UpdateGazeCircle(0f);
    }

    private void UpdateGazeCircle(float progress)
    {
        if (gazeProgressFill == null) return;
        progress = Mathf.Clamp01(progress);
        // Smoothstep is perceptually smoother than a linear size change.
        float eased = progress * progress * (3f - 2f * progress);
        if (gazeCircleRect != null)
            gazeCircleRect.localScale = Vector3.one * Mathf.Max(0.001f, eased);
        gazeProgressFill.enabled = progress > 0f;
    }

    private void ClearGaze()
    {
        gazeElapsed = 0f;
        gazeTriggered = false;
        UpdateGazeCircle(0f);
    }

   private void SetHandleState(HandleState state)
    {
        switch (state)
        {
            case HandleState.Ready:   handleTargetColor = ReadyColor; handleTargetScale = 1.00f; break;
            case HandleState.Hover:   handleTargetColor = HoverColor; handleTargetScale = 1.05f; break;
            case HandleState.Grabbed: handleTargetColor = GrabColor;  handleTargetScale = 1.10f; break;
        }
    }

    private void AnimateHandle()
    {
        if (grabHandleImage == null) return;
        float k = 1f - Mathf.Exp(-14f * Time.unscaledDeltaTime);
        grabHandleImage.color = Color.Lerp(grabHandleImage.color, handleTargetColor, k);
        var rt = grabHandleImage.rectTransform;
        rt.localScale = Vector3.Lerp(rt.localScale, handleBaseScale * handleTargetScale, k);
    }

    
    private void SetStatus(string message)
    {
        if (statusText == null) return;

        // Yeni mesaji goster.
        statusText.text = message;

        Debug.Log("[VR Menu] " + message, this);

        // Onceki mesajin zamanlayicisini iptal et.
        if (statusHideCoroutine != null)
        {
            StopCoroutine(statusHideCoroutine);
            statusHideCoroutine = null;
        }

        // Yeni mesaj icin zamanlayiciyi baslat.
        if (isActiveAndEnabled)
        {
            statusHideCoroutine = StartCoroutine(HideStatusAfterDelay());
        }
    }

    private IEnumerator HideStatusAfterDelay()
    {
        yield return new WaitForSecondsRealtime(statusDisplaySeconds);

        if (statusText != null)
        {
            statusText.text = string.Empty;
        }

        statusHideCoroutine = null;
    }

}