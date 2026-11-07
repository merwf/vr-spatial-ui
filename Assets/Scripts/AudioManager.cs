using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    // YENİ: SFX kapatılınca birlikte susturulacak diğer kaynaklar
    // (Canvas'taki, XRButtonFeedback'in kullandığı AudioSource)
    [SerializeField] private AudioSource[] extraSfxSources;

    [Header("Clips")]
    [SerializeField] private AudioClip uiClickSfx;
    [SerializeField] private AudioClip quitSfx;
    [SerializeField] private AudioClip sliderTickSfx;

    [Header("Slider Settings")]
    [SerializeField] private float sliderPitchMin = 0.9f;
    [SerializeField] private float sliderPitchMax = 1.2f;

    private float lastSliderSoundTime;

    // YENİ: butonların ikon durumunu okuyabilmesi için
    public bool IsMusicMuted => musicSource != null && musicSource.mute;
    public bool IsSfxMuted => sfxSource != null && sfxSource.mute;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void ToggleMusic()
    {
        if (musicSource == null) return;
        musicSource.mute = !musicSource.mute;
    }

    // YENİ: artık ekstra kaynakları da birlikte susturuyor
    public void ToggleSFX()
    {
        if (sfxSource == null) return;
        bool mute = !sfxSource.mute;
        sfxSource.mute = mute;

        if (extraSfxSources == null) return;
        foreach (var source in extraSfxSources)
            if (source != null) source.mute = mute;
    }

    public void PlayClickSound()
    {
        PlayClip(uiClickSfx);
    }

    public void PlayQuitSound()
    {
        PlayClip(quitSfx);
    }

    public void PlaySliderSound(float value)
    {
        if (sfxSource == null || sliderTickSfx == null || sfxSource.mute) return;

        if (Time.unscaledTime - lastSliderSoundTime < 0.06f) return;
        lastSliderSoundTime = Time.unscaledTime;

        sfxSource.pitch = Mathf.Lerp(sliderPitchMin, sliderPitchMax, value);
        sfxSource.PlayOneShot(sliderTickSfx, 0.4f);
        sfxSource.pitch = 1.0f;
    }

    private void PlayClip(AudioClip clip)
    {
        if (sfxSource != null && clip != null && !sfxSource.mute)
        {
            sfxSource.pitch = 1.0f;
            sfxSource.PlayOneShot(clip);
        }
    }
}