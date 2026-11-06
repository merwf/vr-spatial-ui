
using UnityEngine;

public class QuitApplication : MonoBehaviour
{
    public void QuitGame()
    {
        Debug.Log("Quit button pressed!");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
