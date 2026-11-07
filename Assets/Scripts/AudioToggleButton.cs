using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Müzik veya SFX'i açıp kapatan buton. İkonu duruma göre değiştirir.
/// OnClick'e elle bir şey bağlamaya gerek yok, script kendisi bağlanır.
/// </summary>
[RequireComponent(typeof(Button))]
public class AudioToggleButton : MonoBehaviour
{
    public enum Channel { Music, SFX }

    [SerializeField] private Channel channel = Channel.Music;
    [SerializeField] private Image icon;        // değişecek ikon Image'ı
    [SerializeField] private Sprite onSprite;   // ses açıkken
    [SerializeField] private Sprite offSprite;  // ses kapalıyken

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void Start()
    {
        Refresh();
    }

    private void Toggle()
    {
        var am = AudioManager.Instance;
        if (am == null) return;

        if (channel == Channel.Music) am.ToggleMusic();
        else am.ToggleSFX();

        Refresh();
    }

    private void Refresh()
    {
        var am = AudioManager.Instance;
        if (am == null || icon == null) return;

        bool muted = channel == Channel.Music ? am.IsMusicMuted : am.IsSfxMuted;
        icon.sprite = muted ? offSprite : onSprite;
    }
}