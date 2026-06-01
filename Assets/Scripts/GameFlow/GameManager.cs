using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Persistent singleton that drives the linear level progression:
///     Forest L1 -> Forest L2 -> Dungeon L1 -> Dungeon L2 -> WIN
/// AtelierTrigger components call Advance() to move to the next stage.
/// </summary>
public class GameManager : MonoBehaviour
{
    public enum Stage
    {
        ForestL1,
        ForestL2,
        DungeonL1,
        DungeonL2,
        Won
    }

    public static GameManager Instance { get; private set; }

    public Stage CurrentStage { get; private set; } = Stage.ForestL1;
    public bool HasWon => CurrentStage == Stage.Won;

    [Header("Level Complete Message")]
    [SerializeField] private float levelCompleteMessageSeconds = 2f;

    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/Forest/Level_1.unity",
        "Assets/Scenes/Forest/Level_2.unity",
        "Assets/Scenes/Dungeon/Level_1.unity",
        "Assets/Scenes/Dungeon/Level_2.unity",
    };

    private static readonly string[] StageNames =
    {
        "Forest Level 1",
        "Forest Level 2",
        "Dungeon Level 1",
        "Dungeon Level 2",
    };

    private bool isTransitioning;
    private string transitionTitle;
    private string transitionMessage;
    private GUIStyle transitionTitleStyle;
    private GUIStyle transitionMessageStyle;
    private Texture2D transitionOverlayTexture;
    private Texture2D transitionBoxTexture;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        go.AddComponent<HomeScreen>();
        go.AddComponent<WinScreen>();
        go.AddComponent<PlayerHealthSystem>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SyncStageFromActiveScene();
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single) return;
        SyncStageFromActiveScene();
    }

    /// <summary>Called by AtelierTrigger when the player enters the centre atelier.</summary>
    public void Advance()
    {
        if (isTransitioning || HasWon)
        {
            return;
        }

        switch (CurrentStage)
        {
            case Stage.ForestL1:
                StartCoroutine(ShowLevelCompleteThenLoad(Stage.ForestL2));
                break;
            case Stage.ForestL2:
                StartCoroutine(ShowLevelCompleteThenLoad(Stage.DungeonL1));
                break;
            case Stage.DungeonL1:
                StartCoroutine(ShowLevelCompleteThenLoad(Stage.DungeonL2));
                break;
            case Stage.DungeonL2:
                StartCoroutine(ShowFinalLevelComplete());
                break;
            case Stage.Won:
                break;
        }
    }

    private IEnumerator ShowLevelCompleteThenLoad(Stage next)
    {
        isTransitioning = true;
        transitionTitle = $"{GetStageName(CurrentStage)} Complete";
        transitionMessage = $"Moving to {GetStageName(next)}...";

        yield return new WaitForSeconds(levelCompleteMessageSeconds);

        isTransitioning = false;
        LoadStage(next);
    }

    private IEnumerator ShowFinalLevelComplete()
    {
        isTransitioning = true;
        transitionTitle = $"{GetStageName(CurrentStage)} Complete";
        transitionMessage = "You won all levels.";

        yield return new WaitForSeconds(levelCompleteMessageSeconds);

        isTransitioning = false;
        CurrentStage = Stage.Won;
        Debug.Log("[GameManager] YOU WIN ALL LEVELS.");
    }

    private void LoadStage(Stage next)
    {
        CurrentStage = next;
        string path = StageScenePaths[(int)next];
        Debug.Log($"[GameManager] Advancing to {next} ({path}).");
        SceneManager.LoadScene(path);
    }

    private void OnGUI()
    {
        if (!isTransitioning)
        {
            return;
        }

        EnsureTransitionGui();

        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), transitionOverlayTexture);

        float boxWidth = Mathf.Min(520f, Screen.width - 40f);
        float boxHeight = 150f;
        Rect boxRect = new Rect(
            (Screen.width - boxWidth) * 0.5f,
            (Screen.height - boxHeight) * 0.5f,
            boxWidth,
            boxHeight);

        GUI.DrawTexture(boxRect, transitionBoxTexture);
        GUI.Label(new Rect(boxRect.x + 20f, boxRect.y + 28f, boxRect.width - 40f, 48f), transitionTitle, transitionTitleStyle);
        GUI.Label(new Rect(boxRect.x + 20f, boxRect.y + 84f, boxRect.width - 40f, 34f), transitionMessage, transitionMessageStyle);
    }

    private void EnsureTransitionGui()
    {
        if (transitionOverlayTexture != null)
        {
            return;
        }

        transitionOverlayTexture = CreateTexture(new Color(0f, 0f, 0f, 0.55f));
        transitionBoxTexture = CreateTexture(new Color(0.06f, 0.07f, 0.06f, 0.92f));

        transitionTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 34,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
        };

        transitionMessageStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 20,
            normal = { textColor = new Color(0.85f, 0.92f, 0.85f, 1f) },
        };
    }

    private static Texture2D CreateTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static string GetStageName(Stage stage)
    {
        int index = (int)stage;
        return index >= 0 && index < StageNames.Length ? StageNames[index] : "Labyrinth";
    }

    private void SyncStageFromActiveScene()
    {
        string path = SceneManager.GetActiveScene().path;
        for (int i = 0; i < StageScenePaths.Length; i++)
        {
            if (path == StageScenePaths[i])
            {
                if (CurrentStage != (Stage)i && CurrentStage != Stage.Won)
                {
                    CurrentStage = (Stage)i;
                }
                return;
            }
        }
    }
}
