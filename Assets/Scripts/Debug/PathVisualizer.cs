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

    [Header("Game-view line rendering")]
    [SerializeField] private float lineWidth = 0.15f;
    [SerializeField] private float lineHeightOffset = 0.4f;

    private bool isVisible;
    private bool hasCurrentDogPath;
    private float refreshTimer;
    private PathfindingAlgorithm currentAlgorithm = PathfindingAlgorithm.AStar;
    private List<Vector3> bfsPath = new List<Vector3>();
    private List<Vector3> astarPath = new List<Vector3>();
    private List<Vector3> dijkstraPath = new List<Vector3>();
    private List<Vector3> currentDogPath = new List<Vector3>();
    private MultiAlgorithmPathfinder pathfinder;

    private LineRenderer bfsLine;
    private LineRenderer astarLine;
    private LineRenderer dijkstraLine;
    private LineRenderer dogLine;

    private void Start()
    {
        pathfinder = FindObjectOfType<MultiAlgorithmPathfinder>();
        EnsureLineRenderers();
        ApplyVisibilityToLineRenderers();
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
        UpdateLineRenderer(dogLine, currentDogPath, GetAlgorithmColor(currentAlgorithm));
        ApplyVisibilityToLineRenderers();
    }

    public void SetVisible(bool visible)
    {
        isVisible = visible;
        EnsureLineRenderers();
        if (isVisible)
        {
            refreshTimer = 0f;
            if (!hasCurrentDogPath)
            {
                RefreshAllPaths();
            }
            ApplyVisibilityToLineRenderers();
        }
        else
        {
            ApplyVisibilityToLineRenderers();
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

        UpdateLineRenderer(bfsLine, bfsPath, Color.blue);
        UpdateLineRenderer(astarLine, astarPath, Color.green);
        UpdateLineRenderer(dijkstraLine, dijkstraPath, Color.yellow);
        ApplyVisibilityToLineRenderers();

        Debug.Log(
            $"[PathVisualizer] ON  |  " +
            $"BFS: {bfsPath.Count} nodes (blue)  |  " +
            $"A*: {astarPath.Count} nodes (green)  |  " +
            $"Dijkstra: {dijkstraPath.Count} nodes (yellow)");
    }

    private void EnsureLineRenderers()
    {
        if (bfsLine == null) bfsLine = CreateLineRenderer("_LR_BFS", Color.blue);
        if (astarLine == null) astarLine = CreateLineRenderer("_LR_AStar", Color.green);
        if (dijkstraLine == null) dijkstraLine = CreateLineRenderer("_LR_Dijkstra", Color.yellow);
        if (dogLine == null) dogLine = CreateLineRenderer("_LR_Dog", Color.green);
    }

    private LineRenderer CreateLineRenderer(string childName, Color color)
    {
        Transform existing = transform.Find(childName);
        GameObject go = existing != null ? existing.gameObject : new GameObject(childName);
        go.transform.SetParent(transform, false);
        LineRenderer lr = go.GetComponent<LineRenderer>();
        if (lr == null) lr = go.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 0;
        lr.startWidth = lineWidth;
        lr.endWidth = lineWidth;
        lr.numCapVertices = 2;
        lr.numCornerVertices = 2;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lr.receiveShadows = false;
        lr.alignment = LineAlignment.View;

        Shader shader = Shader.Find("Sprites/Default");
        if (shader == null) shader = Shader.Find("Unlit/Color");
        if (shader == null) shader = Shader.Find("Hidden/Internal-Colored");
        Material mat = new Material(shader) { color = color };
        lr.material = mat;
        lr.startColor = color;
        lr.endColor = color;
        lr.enabled = false;
        return lr;
    }

    private void UpdateLineRenderer(LineRenderer lr, List<Vector3> path, Color color)
    {
        if (lr == null) return;
        lr.startColor = color;
        lr.endColor = color;
        if (lr.material != null) lr.material.color = color;

        if (path == null || path.Count < 2)
        {
            lr.positionCount = 0;
            return;
        }

        lr.positionCount = path.Count;
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i];
            p.y += lineHeightOffset;
            lr.SetPosition(i, p);
        }
    }

    private void ApplyVisibilityToLineRenderers()
    {
        if (bfsLine == null || astarLine == null || dijkstraLine == null || dogLine == null) return;

        if (!isVisible)
        {
            bfsLine.enabled = false;
            astarLine.enabled = false;
            dijkstraLine.enabled = false;
            dogLine.enabled = false;
            return;
        }

        if (hasCurrentDogPath)
        {
            bfsLine.enabled = false;
            astarLine.enabled = false;
            dijkstraLine.enabled = false;
            dogLine.enabled = dogLine.positionCount >= 2;
        }
        else
        {
            dogLine.enabled = false;
            bfsLine.enabled = bfsLine.positionCount >= 2;
            astarLine.enabled = astarLine.positionCount >= 2;
            dijkstraLine.enabled = dijkstraLine.positionCount >= 2;
        }
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
