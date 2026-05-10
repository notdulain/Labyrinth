using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Spawns multiple demon dogs at random walkable graph nodes.
/// Each spawned dog runs its own DemonDogController, so they all
/// pathfind independently with Dijkstra against the same target.
/// </summary>
public class SpawnManager : MonoBehaviour
{
    [Header("Spawn Settings")]
    [SerializeField] private GameObject demonDogPrefab;
    [SerializeField] private int spawnCount = 3;
    [SerializeField] private float minDistanceFromTarget = 8f;

    [Header("Target")]
    [SerializeField] private Transform target;
    [SerializeField] private string targetTag = "Player";

    private readonly List<GameObject> spawnedDogs = new List<GameObject>();

    private void Start()
    {
        if (target == null)
        {
            GameObject hero = GameObject.FindGameObjectWithTag(targetTag);
            if (hero != null) target = hero.transform;
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

        int actual = Mathf.Min(spawnCount, candidates.Count);
        for (int i = 0; i < actual; i++)
        {
            Vector3 spawnPos = candidates[i];
            GameObject dog = Instantiate(demonDogPrefab, spawnPos, Quaternion.identity);
            dog.name = $"DemonDog_{i + 1}";

            DemonDogController controller = dog.GetComponent<DemonDogController>();
            if (controller != null && target != null)
            {
                // Use reflection-free assignment via a public setter we'll add to the controller,
                // or rely on the controller's auto-find by tag. Auto-find covers us here.
            }

            spawnedDogs.Add(dog);
        }

        Debug.Log($"[SpawnManager] Spawned {actual} demon dogs.");
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
