using UnityEngine;

/// <summary>
/// Configures the demon dogs that are already placed in the scene.
/// No runtime instantiation - dogs are authored at fixed AgentSpawn positions.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    private void Start()
    {
        if (target == null)
        {
            target = ResolveTarget();
        }

        DemonDogController[] dogs = FindObjectsByType<DemonDogController>(FindObjectsSortMode.None);
        if (dogs.Length == 0)
        {
            Debug.LogWarning("[SpawnManager] No DemonDogController instances found in the scene.");
            return;
        }

        for (int i = 0; i < dogs.Length; i++)
        {
            dogs[i].SetTarget(target);

            IntelligentAgent intelligentAgent = dogs[i].GetComponent<IntelligentAgent>();
            if (intelligentAgent != null)
            {
                intelligentAgent.SetTarget(target);
            }
        }

        Debug.Log($"[SpawnManager] Configured {dogs.Length} pre-placed demon dog(s) with target '{(target != null ? target.name : "<none>")}'.");
    }

    private Transform ResolveTarget()
    {
        GameObject hero = null;

        try
        {
            hero = GameObject.FindGameObjectWithTag(targetTag);
        }
        catch (UnityException)
        {
            // Tag may not exist in older scenes.
        }

        if (hero == null)
        {
            hero = GameObject.Find("Player");
        }

        return hero != null ? hero.transform : null;
    }
}
