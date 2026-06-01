using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// First-screen instructions shown before the player starts Forest Level 1.
/// </summary>
public class HomeScreen : MonoBehaviour
{
    private bool isShowing;
    private bool hasShown;
    private float previousTimeScale = 1f;
    private GUIStyle titleStyle;
    private GUIStyle headingStyle;
    private GUIStyle bodyStyle;
    private GUIStyle promptStyle;
    private Texture2D overlayTexture;
    private Texture2D panelTexture;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        TryShowForCurrentScene();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode == LoadSceneMode.Single)
        {
            TryShowForCurrentScene();
        }
    }

    private void Update()
    {
        if (!isShowing)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
        {
            StartGame();
        }
    }

    private void OnGUI()
    {
        if (!isShowing)
        {
            return;
        }

        EnsureGui();

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture);

        float panelWidth = Mathf.Min(760f, Screen.width - 48f);
        float panelHeight = Mathf.Min(560f, Screen.height - 48f);
        Rect panelRect = new Rect(
            (Screen.width - panelWidth) * 0.5f,
            (Screen.height - panelHeight) * 0.5f,
            panelWidth,
            panelHeight);

        GUI.DrawTexture(panelRect, panelTexture);

        float x = panelRect.x + 34f;
        float y = panelRect.y + 26f;
        float width = panelRect.width - 68f;

        GUI.Label(new Rect(x, y, width, 58f), "Labyrinth", titleStyle);
        y += 70f;

        GUI.Label(new Rect(x, y, width, 32f), "Forest", headingStyle);
        y += 38f;
        GUI.Label(
            new Rect(x, y, width, 92f),
            "Level 1: Find Jacob's Fountain to win.\nLevel 2: Find Jacob's Fountain while dogs chase you.\nUse the U and B keys to place barriers and block their path.",
            bodyStyle);
        y += 118f;

        GUI.Label(new Rect(x, y, width, 32f), "Dungeon", headingStyle);
        y += 38f;
        GUI.Label(
            new Rect(x, y, width, 70f),
            "Level 1: Find the altar.\nLevel 2: Find the altar while dogs chase you.",
            bodyStyle);

        GUI.Label(
            new Rect(panelRect.x + 20f, panelRect.yMax - 82f, panelRect.width - 40f, 46f),
            "Press Enter to Start",
            promptStyle);
    }

    private void TryShowForCurrentScene()
    {
        if (hasShown || isShowing)
        {
            return;
        }

        if (SceneManager.GetActiveScene().path != "Assets/Scenes/Forest/Level_1.unity")
        {
            return;
        }

        hasShown = true;
        isShowing = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
    }

    private void StartGame()
    {
        isShowing = false;
        Time.timeScale = previousTimeScale;
    }

    private void EnsureGui()
    {
        if (overlayTexture != null)
        {
            return;
        }

        overlayTexture = CreateTexture(new Color(0f, 0f, 0f, 0.72f));
        panelTexture = CreateTexture(new Color(0.06f, 0.08f, 0.07f, 0.96f));

        titleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 48,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
        };

        headingStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleLeft,
            fontSize = 26,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.75f, 1f, 0.78f, 1f) },
        };

        bodyStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperLeft,
            fontSize = 20,
            wordWrap = true,
            normal = { textColor = Color.white },
        };

        promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 22,
            fontStyle = FontStyle.Bold,
            normal = { textColor = new Color(0.75f, 1f, 0.78f, 1f) },
        };
    }

    private static Texture2D CreateTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }
}
