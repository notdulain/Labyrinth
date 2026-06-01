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

    private static readonly string[] StageScenePaths =
    {
        "Assets/Scenes/Forest/Level_1.unity",
        "Assets/Scenes/Forest/Level_2.unity",
        "Assets/Scenes/Dungeon/Level_1.unity",
        "Assets/Scenes/Dungeon/Level_2.unity",
    };

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Bootstrap()
    {
        if (Instance != null) return;
        var go = new GameObject("GameManager");
        go.AddComponent<GameManager>();
        go.AddComponent<WinScreen>();
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
        switch (CurrentStage)
        {
            case Stage.ForestL1:
                LoadStage(Stage.ForestL2);
                break;
            case Stage.ForestL2:
                LoadStage(Stage.DungeonL1);
                break;
            case Stage.DungeonL1:
                LoadStage(Stage.DungeonL2);
                break;
            case Stage.DungeonL2:
                CurrentStage = Stage.Won;
                Debug.Log("[GameManager] YOU WIN.");
                break;
            case Stage.Won:
                break;
        }
    }

    private void LoadStage(Stage next)
    {
        CurrentStage = next;
        string path = StageScenePaths[(int)next];
        Debug.Log($"[GameManager] Advancing to {next} ({path}).");
        SceneManager.LoadScene(path);
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
