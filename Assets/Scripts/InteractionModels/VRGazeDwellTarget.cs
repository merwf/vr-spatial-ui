using TMPro;
using UnityEngine;

/// <summary>
/// Bakisla secilen nesnenin ilerlemesini ve renkle geri bildirimini yonetir.
/// Gercek eye-tracking DEGIL, bas/kamera yonu ile calisir.
/// </summary>
public sealed class VRGazeDwellTarget : MonoBehaviour
{
    [SerializeField] private Renderer visual;
    [SerializeField] private Transform progressFill;
    [SerializeField] private TMP_Text label;
    [SerializeField] private VRDemoStatus status;
    [SerializeField] private float barWidth = 0.36f;

    private readonly Color idle = new Color(0.72f, 0.32f, 0.95f);
    private readonly Color hover = new Color(1f, 0.78f, 0.18f);
    private readonly Color selected = new Color(0.25f, 0.98f, 0.50f);
    private MaterialPropertyBlock block;
    private bool active;

    public void Configure(Renderer targetVisual, Transform fill, TMP_Text targetLabel, VRDemoStatus reporter)
    {
        visual = targetVisual;
        progressFill = fill;
        label = targetLabel;
        status = reporter;
        ApplyColor(idle);
        SetProgress(0);
    }

    private void Start()
    {
        ResetTarget();
    }

    public void SetFocused(bool focused)
    {
        if (!focused)
        {
            ResetTarget();
            return;
        }
        if (!active)
        {
            ApplyColor(hover);
            if (label != null) label.text = "HEAD-GAZE - BAKIS TUTULUYOR";
        }
    }

    public void SetProgress(float progress)
    {
        float p = Mathf.Clamp01(progress);
        if (progressFill != null)
        {
            Vector3 s = progressFill.localScale;
            s.x = barWidth * Mathf.Max(0.001f, p);
            progressFill.localScale = s;

            Vector3 pos = progressFill.localPosition;
            pos.x = -barWidth * 0.5f + s.x * 0.5f;
            progressFill.localPosition = pos;
        }
        if (!active && label != null && p > 0f)
            label.text = "HEAD-GAZE - %" + Mathf.RoundToInt(p * 100);
    }

    public void Activate()
    {
        if (active) return;
        active = true;
        ApplyColor(selected);
        if (label != null) label.text = "HEAD-GAZE - SECILDI";
        if (status != null)
            status.Report("Head-Gaze", "2 sn sabit bakisla secildi");
    }

    private void ResetTarget()
    {
        active = false;
        ApplyColor(idle);
        SetProgress(0f);
        if (label != null) label.text = "HEAD-GAZE - 2 SN BAK";
    }

    private void ApplyColor(Color color)
    {
        if (visual == null) return;
        if (block == null) block = new MaterialPropertyBlock();
        visual.GetPropertyBlock(block);
        Material material = visual.sharedMaterial;
        if (material != null && material.HasProperty("_BaseColor"))
            block.SetColor(Shader.PropertyToID("_BaseColor"), color);
        if (material != null && material.HasProperty("_Color"))
            block.SetColor(Shader.PropertyToID("_Color"), color);
        visual.SetPropertyBlock(block);
    }
}
