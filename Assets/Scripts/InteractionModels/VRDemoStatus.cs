using TMPro;
using UnityEngine;

/// <summary>Tek sahnede ray / direct / gaze denemelerinin son sonucunu gösterir.</summary>
public sealed class VRDemoStatus : MonoBehaviour
{
    [SerializeField] private TMP_Text output;
    private int interactionCount;

    public void Configure(TMP_Text text)
    {
        output = text;
        if (output != null)
            output.text = "ETKILESIM DEMOSU\nRay / Direct / Head-Gaze hazir";
    }

    public void Report(string model, string action)
    {
        interactionCount++;
        if (output != null)
            output.text = "ETKILESIM DEMOSU\n" + model + " : " + action +
                          "\nOlay sayisi: " + interactionCount;
        Debug.Log("[VR Etkilesim] " + model + " / " + action);
    }
}
