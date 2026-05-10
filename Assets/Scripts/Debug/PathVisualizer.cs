using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Visualizes computed navigation paths for debugging in the labyrinth scenes.
///
/// Press P (default) to toggle visualization on/off.
/// When enabled, all three pathfinding algorithms run from the same start/goal
/// and their results are drawn simultaneously in different colours:
///
///   BFS      -> Blue
///   A*       -> Green
///   Dijkstra -> Yellow
///
/// Scene view: coloured spheres + connecting lines drawn via OnDrawGizmos.
/// Game view: same lines drawn via Debug.DrawLine every frame while active.
///
/// Uses MultiAlgorithmPathfinder so the visualized paths match the dog's movement.
/// </summary>
public class PathVisualizer : MonoBehaviour
{
    [Header("Hotkey")]
    [Tooltip("Key that toggles path visualization on / off.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.P;
    [SerializeField] private bool handleInput = false;

    [Header("Optional: pin specific start / goal positions")]
    [SerializeField] private bool usePinnedTransforms = false;
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform goalTransform;

    [Header("Sphere size")]
    [SerializeField] private float nodeSphereRadius = 0.25f;

    [Header("Refresh rate")]
    [SerializeField] private float refreshInterval = 0.5f;

    private bool isVisible;
    private bool hasCurrentDogPath;
    private float refreshTimer;
    private PathfindingAlgorithm currentAlgorithm = PathfindingAlgorithm.AStar;
    private List<Vector3> bfsPath = new List<Vector3>();
    private List<Vector3> astarPath = new List<Vector3>();
    private List<Vector3> dijkstraPath = new List<Vector3>();
    private List<Vector3> currentDogPath = new List<Vector3>();
    private MultiAlgorithmPathfinder pathfinder;

    private void Start()
    {
        pathfinder = FindObjectOfType<MultiAlgorithmPathfinder>();
        Debug.Log("[PathVisualizer] Ready. Press P to toggle path visualization.");
    }

    private void Update()
    {
        if (handleInput && Input.GetKeyDown(toggleKey))
        {
            ToggleVisible();
        }

        if (!isVisible)
        {
            return;
        }

        refreshTimer -= Time.deltaTime;
        if (refreshTimer <= 0f)
        {
            RefreshAllPaths();
            refreshTimer = refreshInterval;
        }

        if (hasCurrentDogPath)
        {
            DrawDebugLines(currentDogPath, GetAlgorithmColor(currentAlgorithm));
            return;
        }

        DrawDebugLines(bfsPath, Color.blue);
        DrawDebugLines(astarPath, Color.green);
        DrawDebugLines(dijkstraPath, Color.yellow);
    }

    public void SetCurrentPath(List<Vector3> path, PathfindingAlgorithm algorithm)
    {
        currentDogPath = path != null ? new List<Vector3>(path) : new List<Vector3>();
        currentAlgorithm = algorithm;
        hasCurrentDogPath = currentDogPath.Count > 0;
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (isVisible)
        {
            refreshTimer = 0f;
            if (!hasCurrentDogPath)
            {
                RefreshAllPaths();
            }
        }
        else
        {
            Debug.Log("[PathVisualizer] Visualization OFF.");
        }
    }

    public void ToggleVisible()
    {
        SetVisible(!isVisible);
    }

    private void RefreshAllPaths()
    {
        if (pathfinder == null)
        {
            pathfinder = FindObjectOfType<MultiAlgorithmPathfinder>();
        }

        if (pathfinder == null)
        {
            Debug.LogWarning("[PathVisualizer] MultiAlgorithmPathfinder is not ready yet.");
            return;
        }

        Vector3 start = ResolveStart();
        Vector3 goal = ResolveGoal();
        List<PathfindingResult> results = pathfinder.CompareAll(start, goal, true);

        bfsPath = new List<Vector3>();
        astarPath = new List<Vector3>();
        dijkstraPath = new List<Vector3>();

        for (int i = 0; i < results.Count; i++)
        {
            PathfindingResult result = results[i];
            if (result == null || result.worldPath == null)
            {
                continue;
            }

            switch (result.algorithmName)
            {
                case "BFS":
                    bfsPath = new List<Vector3>(result.worldPath);
                    break;
                case "Dijkstra":
                    dijkstraPath = new List<Vector3>(result.worldPath);
                    break;
                default:
                    astarPath = new List<Vector3>(result.worldPath);
                    break;
            }
        }

        Debug.Log(
            $"[PathVisualizer] ON  |  " +
            $"BFS: {bfsPath.Count} nodes (blue)  |  " +
            $"A*: {astarPath.Count} nodes (green)  |  " +
            $"Dijkstra: {dijkstraPath.Count} nodes (yellow)");
    }

    private Vector3 ResolveStart()
    {
        if (usePinnedTransforms && startTransform != null)
        {
            return startTransform.position;
        }

        DemonDogController dog = FindObjectOfType<DemonDogController>();
        if (dog != null)
        {
            return dog.transform.position;
        }

        IntelligentAgent intelligentAgent = FindObjectOfType<IntelligentAgent>();
        if (intelligentAgent != null)
        {
            return intelligentAgent.transform.position;
        }

        Transform spawnPoint = FindAgentSpawnPoint();
        return spawnPoint != null ? spawnPoint.position : Vector3.zero;
    }

    private Vector3 ResolveGoal()
    {
        if (usePinnedTransforms && goalTransform != null)
        {
            return goalTransform.position;
        }

        GameObject hero = null;

        try
        {
            hero = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
        }

        if (hero == null)
        {
            hero = GameObject.Find("Player");
        }

        return hero != null ? hero.transform.position : Vector3.zero;
    }

    private Transform FindAgentSpawnPoint()
    {
        try
        {
            GameObject[] taggedPoints = GameObject.FindGameObjectsWithTag("AgentSpawn");
            if (taggedPoints.Length > 0)
            {
                return taggedPoints[0].transform;
            }
        }
        catch (UnityException)
        {
        }

        Transform[] allTransforms = FindObjectsOfType<Transform>();
        foreach (Transform candidate in allTransforms)
        {
            if (candidate.name.StartsWith("AgentSpawn"))
            {
                return candidate;
            }
        }

        return null;
    }

    private void DrawDebugLines(List<Vector3> path, Color color)
    {
        if (path == null || path.Count < 2)
        {
            return;
        }

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawLine(path[i], path[i + 1], color);
        }
    }

    private void OnDrawGizmos()
    {
        if (!isVisible)
        {
            return;
        }

        if (hasCurrentDogPath)
        {
            DrawGizmosPath(currentDogPath, GetAlgorithmColor(currentAlgorithm));
            return;
        }

        DrawGizmosPath(bfsPath, Color.blue);
        DrawGizmosPath(astarPath, Color.green);
        DrawGizmosPath(dijkstraPath, Color.yellow);
    }

    private void DrawGizmosPath(List<Vector3> path, Color color)
    {
        if (path == null || path.Count == 0)
        {
            return;
        }

        Gizmos.color = color;

        for (int i = 0; i < path.Count; i++)
        {
            Gizmos.DrawSphere(path[i], nodeSphereRadius);

            if (i < path.Count - 1)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }

    private static Color GetAlgorithmColor(PathfindingAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case PathfindingAlgorithm.Dijkstra:
                return Color.yellow;
            case PathfindingAlgorithm.BFS:
                return Color.blue;
            default:
                return Color.green;
        }
    }
}
