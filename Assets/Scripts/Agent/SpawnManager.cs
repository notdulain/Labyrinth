using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

/// <summary>
/// Spawns agents at the AgentSpawn markers placed in the level.
/// Falls back to graph nodes only when a scene has no spawn markers.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject demonDogPrefab;
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private float minDistanceFromTarget = 8f;
    [SerializeField] private string spawnPointTag = "AgentSpawn";
    [SerializeField] private string spawnPointNamePrefix = "AgentSpawn";

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    private readonly List<GameObject> spawnedDogs = new List<GameObject>();

    private void Start()
    {
        if (target == null)
        {
            target = ResolveTarget();
        }

        if (demonDogPrefab == null)
        {
            Debug.LogError("[SpawnManager] No agent prefab assigned.");
            return;
        }

        if (UseExistingSceneDogs())
        {
            return;
        }

        if (GraphBuilder.Instance == null || GraphBuilder.Instance.AdjacencyList == null)
        {
            Debug.LogWarning("[SpawnManager] GraphBuilder not ready; cannot spawn.");
            return;
        }

        SpawnDogs();
    }

    private bool UseExistingSceneDogs()
    {
        DemonDogController[] existingDogs = FindObjectsByType<DemonDogController>(FindObjectsSortMode.None);
        if (existingDogs == null || existingDogs.Length == 0)
        {
            return false;
        }

        for (int i = 0; i < existingDogs.Length; i++)
        {
            ConfigureSpawnedAgent(existingDogs[i].gameObject);
            spawnedDogs.Add(existingDogs[i].gameObject);
        }

        Debug.Log($"[SpawnManager] Using {existingDogs.Length} demon dog(s) already placed in the scene.");
        return true;
    }

    private void SpawnDogs()
    {
        List<Transform> spawnPoints = FindSpawnPoints();
        if (spawnPoints.Count > 0)
        {
            SpawnAtDefinedPoints(spawnPoints);
            return;
        }

        SpawnAtGraphNodes();
    }

    private void SpawnAtDefinedPoints(List<Transform> spawnPoints)
    {
        Vector3 anchorPosition = target != null ? target.position : spawnPoints[0].position;
        int actual = Mathf.Min(spawnCount, spawnPoints.Count);
        for (int i = 0; i < actual; i++)
        {
            Transform spawnPoint = spawnPoints[i];

            GameObject dog = Instantiate(demonDogPrefab, spawnPoint.position, spawnPoint.rotation);
            dog.name = $"{GetSpawnedAgentNamePrefix()}_{i + 1}";
            ConfigureSpawnedAgent(dog);
            dog.transform.SetPositionAndRotation(spawnPoint.position, spawnPoint.rotation);
            spawnedDogs.Add(dog);

            Vector3 nearestReachable = GraphBuilder.Instance.GetNearestNodeReachableTo(
                spawnPoint.position,
                anchorPosition);
            if (Vector3.Distance(spawnPoint.position, nearestReachable) > GraphBuilder.Instance.cellSize)
            {
                Debug.LogWarning(
                    $"[SpawnManager] {spawnPoint.name} is not on the player's reachable graph. " +
                    $"{dog.name} will spawn at the marker but may be unable to path to the target.");
            }
        }

        Debug.Log($"[SpawnManager] Spawned {actual} agent(s) at defined AgentSpawn points.");
    }

    private void SpawnAtGraphNodes()
    {
        List<Vector3> walkableNodes = new List<Vector3>(GraphBuilder.Instance.AdjacencyList.Keys);
        if (walkableNodes.Count == 0)
        {
            Debug.LogWarning("[SpawnManager] No walkable nodes in graph.");
            return;
        }

        Vector3 targetPos = target != null ? target.position : Vector3.zero;
        List<Vector3> candidates = new List<Vector3>();
        foreach (Vector3 node in walkableNodes)
        {
            if (target == null || Vector3.Distance(node, targetPos) >= minDistanceFromTarget)
            {
                candidates.Add(node);
            }
        }

        if (candidates.Count == 0)
        {
            // Fall back: use any walkable node if min-distance filter excluded everything.
            candidates = walkableNodes;
        }

        Shuffle(candidates);

        HashSet<Vector3> usedNodes = new HashSet<Vector3>();
        int actual = Mathf.Min(spawnCount, candidates.Count);
        for (int i = 0; i < actual; i++)
        {
            Vector3 spawnPos = GraphBuilder.Instance.GetNearestNodeReachableTo(
                candidates[i],
                targetPos,
                usedNodes);
            usedNodes.Add(spawnPos);
            GameObject dog = Instantiate(demonDogPrefab, spawnPos, Quaternion.identity);
            dog.name = $"{GetSpawnedAgentNamePrefix()}_{i + 1}";
            ConfigureSpawnedAgent(dog);
            spawnedDogs.Add(dog);
        }

        Debug.Log($"[SpawnManager] Spawned {actual} agent(s) at fallback graph nodes.");
    }

    private List<Transform> FindSpawnPoints()
    {
        List<Transform> spawnPoints = new List<Transform>();
        Scene ownerScene = gameObject.scene;

        try
        {
            GameObject[] taggedPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);
            foreach (GameObject point in taggedPoints)
            {
                if (IsSceneSpawnPoint(point != null ? point.transform : null, ownerScene))
                {
                    spawnPoints.Add(point.transform);
                }
            }
        }
        catch (UnityException)
        {
            // Tag may not exist in older scenes; name fallback below still works.
        }

        if (spawnPoints.Count == 0)
        {
            Transform[] allTransforms = FindObjectsByType<Transform>(
                FindObjectsInactive.Exclude,
                FindObjectsSortMode.None);
            foreach (Transform candidate in allTransforms)
            {
                if (IsSceneSpawnPoint(candidate, ownerScene))
                {
                    spawnPoints.Add(candidate);
                }
            }
        }

        spawnPoints.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return spawnPoints;
    }

    private bool IsSceneSpawnPoint(Transform candidate, Scene ownerScene)
    {
        if (candidate == null || !candidate.gameObject.activeInHierarchy)
            return false;

        if (candidate.gameObject.scene != ownerScene)
            return false;

        if (candidate.name.StartsWith(spawnPointNamePrefix))
            return true;

        try
        {
            return candidate.CompareTag(spawnPointTag);
        }
        catch (UnityException)
        {
            return false;
        }
    }

    private void ConfigureSpawnedAgent(GameObject dog)
    {
        if (dog == null)
        {
            return;
        }

        DemonDogController dogController = dog.GetComponent<DemonDogController>();
        if (dogController != null)
        {
            dogController.SetTarget(target);
        }

        IntelligentAgent intelligentAgent = dog.GetComponent<IntelligentAgent>();
        if (intelligentAgent != null)
        {
            intelligentAgent.SetTarget(target);
        }
    }

    private string GetSpawnedAgentNamePrefix()
    {
        return demonDogPrefab != null ? demonDogPrefab.name : "DemonDog";
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

    private static void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
