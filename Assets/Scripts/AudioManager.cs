using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Audio Sources")]
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;

    [Header("Clips")]
    [SerializeField] private AudioClip uiClickSfx;     // Normal buton sesi
    [SerializeField] private AudioClip quitSfx;        // Çıkış butonu sesi (daha derin / uyarıcı)
    [SerializeField] private AudioClip sliderTickSfx;  // Slider kayma / tıkırtı sesi

    [Header("Slider Settings")]
    [SerializeField] private float sliderPitchMin = 0.9f;
    [SerializeField] private float sliderPitchMax = 1.2f;

    private float lastSliderSoundTime;

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

    public void ToggleSFX()
    {
        if (sfxSource == null) return;
        sfxSource.mute = !sfxSource.mute;
    }

    // Normal butonlar için
    public void PlayClickSound()
    {
        PlayClip(uiClickSfx);
    }

    // Quit butonu için özel ses
    public void PlayQuitSound()
    {
        PlayClip(quitSfx);
    }

    // Slider sürüklendikçe çağrılır (çok sık ses patlamasını engellemek için aralık kısıtlamalı)
    public void PlaySliderSound(float value)
    {
        if (sfxSource == null || sliderTickSfx == null || sfxSource.mute) return;

        // Sesin saniyede 15 kereden fazla tetiklenip gürültü yapmasını önle
        if (Time.unscaledTime - lastSliderSoundTime < 0.06f) return;
        lastSliderSoundTime = Time.unscaledTime;

        // Slider değerine göre (0 - 1) ses tonunu (pitch) hafifçe değiştirerek tatmin edici bir his verir
        sfxSource.pitch = Mathf.Lerp(sliderPitchMin, sliderPitchMax, value);
        sfxSource.PlayOneShot(sliderTickSfx, 0.4f);
        sfxSource.pitch = 1.0f; // Varsayılan tona geri al
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