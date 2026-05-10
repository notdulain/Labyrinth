using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns demon dogs at the AgentSpawn markers placed in the level.
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
            Debug.LogError("[SpawnManager] No demonDogPrefab assigned.");
            return;
        }

        if (GraphBuilder.Instance == null || GraphBuilder.Instance.AdjacencyList == null)
        {
            Debug.LogWarning("[SpawnManager] GraphBuilder not ready; cannot spawn.");
            return;
        }

        SpawnDogs();
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
        HashSet<Vector3> usedNodes = new HashSet<Vector3>();
        Vector3 anchorPosition = target != null ? target.position : spawnPoints[0].position;
        int actual = Mathf.Min(spawnCount, spawnPoints.Count);
        for (int i = 0; i < actual; i++)
        {
            Transform spawnPoint = spawnPoints[i];
            Vector3 spawnPosition = GraphBuilder.Instance.GetNearestNodeReachableTo(
                spawnPoint.position,
                anchorPosition,
                usedNodes);
            usedNodes.Add(spawnPosition);

            GameObject dog = Instantiate(demonDogPrefab, spawnPosition, spawnPoint.rotation);
            dog.name = $"DemonDog_{i + 1}";
            ConfigureSpawnedAgent(dog);
            spawnedDogs.Add(dog);

            if (Vector3.Distance(spawnPoint.position, spawnPosition) > GraphBuilder.Instance.cellSize)
            {
                Debug.LogWarning(
                    $"[SpawnManager] {spawnPoint.name} was not on the player's reachable graph. " +
                    $"Spawned {dog.name} at nearest reachable node {spawnPosition}.");
            }
        }

        Debug.Log($"[SpawnManager] Spawned {actual} demon dogs at defined AgentSpawn points.");
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
            dog.name = $"DemonDog_{i + 1}";
            ConfigureSpawnedAgent(dog);
            spawnedDogs.Add(dog);
        }

        Debug.Log($"[SpawnManager] Spawned {actual} demon dogs at fallback graph nodes.");
    }

    private List<Transform> FindSpawnPoints()
    {
        List<Transform> spawnPoints = new List<Transform>();

        try
        {
            GameObject[] taggedPoints = GameObject.FindGameObjectsWithTag(spawnPointTag);
            foreach (GameObject point in taggedPoints)
            {
                if (point != null)
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
            Transform[] allTransforms = FindObjectsOfType<Transform>();
            foreach (Transform candidate in allTransforms)
            {
                if (candidate.name.StartsWith(spawnPointNamePrefix))
                {
                    spawnPoints.Add(candidate);
                }
            }
        }

        spawnPoints.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
        return spawnPoints;
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
