using UnityEngine;

/// <summary>
/// Lightweight OnGUI overlay shown when the GameManager has reached the Won state.
/// Replace with proper UI later.
/// </summary>
public class WinScreen : MonoBehaviour
{
    private GUIStyle bigStyle;
    private GUIStyle subStyle;

    private void OnGUI()
    {
        if (GameManager.Instance == null || !GameManager.Instance.HasWon) return;

        if (bigStyle == null)
        {
            bigStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 80,
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.white },
            };
            subStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 24,
                normal = { textColor = Color.white },
            };
        }

        // Full-screen black-ish overlay.
        var overlay = new Texture2D(1, 1);
        overlay.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.85f));
        overlay.Apply();
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlay);

        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 120), "YOU WIN", bigStyle);
        GUI.Label(new Rect(0, Screen.height * 0.55f, Screen.width, 40), "Press Esc to quit.", subStyle);

        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Escape)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
