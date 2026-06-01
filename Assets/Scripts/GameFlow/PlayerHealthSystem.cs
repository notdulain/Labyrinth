using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Forest L2 and Dungeon L2 player health overlay and damage handling.
/// Any dog in attack range drains the player over ten total seconds of contact.
/// </summary>
public class PlayerHealthSystem : MonoBehaviour
{
    [Header("Health")]
    [SerializeField] private float maxHealth = 10f;
    [SerializeField] private float secondsOfDogContactToDie = 10f;
    [SerializeField] private float dogAttackDistance = 1.6f;

    [Header("UI")]
    [SerializeField] private int barWidth = 320;
    [SerializeField] private int barHeight = 24;
    [SerializeField] private int barTop = 22;

    private Transform player;
    private DungeonDogController[] dogs = new DungeonDogController[0];
    private float currentHealth;
    private bool isHealthLevel;
    private bool isDead;
    private GUIStyle labelStyle;
    private GUIStyle deathTitleStyle;
    private GUIStyle deathHintStyle;
    private Texture2D backgroundTexture;
    private Texture2D fillTexture;
    private Texture2D dangerFillTexture;
    private Texture2D overlayTexture;

    private float DamagePerSecond => maxHealth / Mathf.Max(secondsOfDogContactToDie, 0.01f);

    private void Awake()
    {
        currentHealth = maxHealth;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void Start()
    {
        RefreshSceneState();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (mode != LoadSceneMode.Single)
        {
            return;
        }

        RefreshSceneState();
    }

    private void Update()
    {
        if (!isHealthLevel || isDead)
        {
            return;
        }

        ResolveTargetsIfNeeded();

        if (IsAnyDogAttacking())
        {
            currentHealth = Mathf.Max(0f, currentHealth - DamagePerSecond * Time.deltaTime);
            if (currentHealth <= 0f)
            {
                KillPlayer();
            }
        }
    }

    private void OnGUI()
    {
        if (!isHealthLevel)
        {
            return;
        }

        EnsureGuiAssets();
        DrawHealthBar();

        if (isDead)
        {
            DrawDeathOverlay();
        }
    }

    private void RefreshSceneState()
    {
        isHealthLevel = IsHealthEnabledScene(SceneManager.GetActiveScene().path);
        player = null;
        dogs = new DungeonDogController[0];
        isDead = false;
        currentHealth = maxHealth;

        if (isHealthLevel)
        {
            ResolveTargetsIfNeeded();
        }
    }

    private void ResolveTargetsIfNeeded()
    {
        if (player == null)
        {
            GameObject hero = null;
            try
            {
                hero = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                // Some scenes may not define the Player tag in older branches.
            }

            if (hero == null)
            {
                hero = GameObject.Find("Player");
            }

            if (hero != null)
            {
                player = hero.transform;
            }
        }

        if (dogs.Length == 0)
        {
            dogs = FindObjectsByType<DungeonDogController>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
        }
    }

    private bool IsAnyDogAttacking()
    {
        if (player == null)
        {
            return false;
        }

        float attackDistanceSqr = dogAttackDistance * dogAttackDistance;
        Vector3 playerPosition = Flatten(player.position);

        for (int i = 0; i < dogs.Length; i++)
        {
            DungeonDogController dog = dogs[i];
            if (dog == null || !dog.isActiveAndEnabled)
            {
                continue;
            }

            if ((Flatten(dog.transform.position) - playerPosition).sqrMagnitude <= attackDistanceSqr)
            {
                return true;
            }
        }

        return false;
    }

    private void KillPlayer()
    {
        isDead = true;

        PlayerController playerController = player != null ? player.GetComponent<PlayerController>() : null;
        if (playerController != null)
        {
            playerController.enabled = false;
        }

        SimpleThirdPersonController simpleController =
            player != null ? player.GetComponent<SimpleThirdPersonController>() : null;
        if (simpleController != null)
        {
            simpleController.enabled = false;
        }

        DungeonDogController[] activeDogs = FindObjectsByType<DungeonDogController>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < activeDogs.Length; i++)
        {
            activeDogs[i].enabled = false;
        }

        Debug.Log("[PlayerHealth] Player died after sustained dog attacks.");
    }

    private void DrawHealthBar()
    {
        int x = Mathf.RoundToInt((Screen.width - barWidth) * 0.5f);
        Rect outerRect = new Rect(x, barTop, barWidth, barHeight);
        Rect innerRect = new Rect(x + 3, barTop + 3, barWidth - 6, barHeight - 6);
        float healthPercent = Mathf.Clamp01(currentHealth / Mathf.Max(maxHealth, 0.01f));
        Rect fillRect = new Rect(innerRect.x, innerRect.y, innerRect.width * healthPercent, innerRect.height);

        GUI.DrawTexture(outerRect, backgroundTexture);
        GUI.DrawTexture(fillRect, healthPercent <= 0.35f ? dangerFillTexture : fillTexture);
        GUI.Label(
            new Rect(x, barTop + barHeight + 4, barWidth, 22),
            $"Health {Mathf.CeilToInt(currentHealth)}/{Mathf.CeilToInt(maxHealth)}",
            labelStyle);
    }

    private void DrawDeathOverlay()
    {
        GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), overlayTexture);
        GUI.Label(new Rect(0, Screen.height * 0.35f, Screen.width, 100), "YOU DIED", deathTitleStyle);
        GUI.Label(new Rect(0, Screen.height * 0.52f, Screen.width, 40), "The dogs caught you.", deathHintStyle);
    }

    private void EnsureGuiAssets()
    {
        if (backgroundTexture != null)
        {
            return;
        }

        backgroundTexture = CreateTexture(new Color(0.05f, 0.05f, 0.05f, 0.85f));
        fillTexture = CreateTexture(new Color(0.16f, 0.78f, 0.35f, 0.95f));
        dangerFillTexture = CreateTexture(new Color(0.9f, 0.16f, 0.12f, 0.95f));
        overlayTexture = CreateTexture(new Color(0f, 0f, 0f, 0.78f));

        labelStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = 16,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
        };

        deathTitleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 72,
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.white },
        };

        deathHintStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 24,
            normal = { textColor = Color.white },
        };
    }

    private static Texture2D CreateTexture(Color color)
    {
        var texture = new Texture2D(1, 1);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return texture;
    }

    private static Vector3 Flatten(Vector3 position)
    {
        position.y = 0f;
        return position;
    }

    private static bool IsHealthEnabledScene(string scenePath)
    {
        return scenePath == "Assets/Scenes/Forest/Level_2.unity" ||
            scenePath == "Assets/Scenes/Dungeon/Level_2.unity";
    }
}
