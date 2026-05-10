using System.Collections.Generic;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Side-by-side comparison of A*, BFS, and Dijkstra on the same graph.
/// Toggle the panel with the C key. Press R to re-run all three.
///
/// Results are sourced from MultiAlgorithmPathfinder so the UI matches the dog.
/// </summary>
public class AlgorithmComparison : MonoBehaviour
{
    [Header("Inputs")]
    [SerializeField] private Transform startTransform;
    [SerializeField] private Transform goalTransform;

    [Header("Hotkeys")]
    [SerializeField] private KeyCode toggleKey = KeyCode.C;
    [SerializeField] private KeyCode rerunKey = KeyCode.R;

    private bool visible = true;
    private readonly List<Result> results = new List<Result>();

    private struct Result
    {
        public string Algorithm;
        public bool Implemented;
        public bool Succeeded;
        public int PathNodes;
        public int VisitedNodes;
        public double Milliseconds;
        public bool Selected;
    }

    private void Start()
    {
        Invoke(nameof(Run), 0.1f);
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            visible = !visible;
        }

        if (Input.GetKeyDown(rerunKey))
        {
            Run();
        }
    }

    public void Run()
    {
        results.Clear();

        MultiAlgorithmPathfinder pathfinder = FindObjectOfType<MultiAlgorithmPathfinder>();
        if (pathfinder == null)
        {
            Debug.LogWarning("[AlgorithmComparison] MultiAlgorithmPathfinder not ready.");
            return;
        }

        Vector3 start = ResolveStart();
        Vector3 goal = ResolveGoal();
        DemonDogController dog = FindObjectOfType<DemonDogController>();
        PathfindingAlgorithm selectedAlgorithm = dog != null ? dog.selectedAlgorithm : PathfindingAlgorithm.AStar;
        List<PathfindingResult> comparisonResults = pathfinder.CompareAll(start, goal, false);

        for (int i = 0; i < comparisonResults.Count; i++)
        {
            PathfindingResult source = comparisonResults[i];
            results.Add(ToChartResult(
                source,
                source.algorithmName == MultiAlgorithmPathfinder.GetAlgorithmName(selectedAlgorithm)));
        }

        foreach (Result result in results)
        {
            if (!result.Implemented)
            {
                Debug.Log($"[AlgorithmComparison] {result.Algorithm}: not implemented yet.");
            }
            else if (!result.Succeeded)
            {
                Debug.Log($"[AlgorithmComparison] {result.Algorithm}: no path found ({result.Milliseconds:F2} ms).");
            }
            else
            {
                Debug.Log($"[AlgorithmComparison] {result.Algorithm}: {result.PathNodes} nodes in {result.Milliseconds:F2} ms.");
            }
        }
    }

    public void UpdateFromDogResult(
        PathfindingResult selectedResult,
        List<PathfindingResult> comparisonResults,
        PathfindingAlgorithm selectedAlgorithm)
    {
        visible = true;
        results.Clear();

        if (comparisonResults != null && comparisonResults.Count > 0)
        {
            for (int i = 0; i < comparisonResults.Count; i++)
            {
                PathfindingResult source = comparisonResults[i];
                results.Add(ToChartResult(
                    source,
                    source.algorithmName == MultiAlgorithmPathfinder.GetAlgorithmName(selectedAlgorithm)));
            }

            return;
        }

        if (selectedResult != null)
        {
            results.Add(ToChartResult(selectedResult, true));
        }
    }

    private static Result ToChartResult(PathfindingResult source, bool selected)
    {
        return new Result
        {
            Algorithm = source.algorithmName,
            Implemented = true,
            Succeeded = source.pathFound,
            PathNodes = source.worldPath != null ? source.worldPath.Count : 0,
            VisitedNodes = source.visitedNodeCount,
            Milliseconds = source.calculationTimeMs,
            Selected = selected
        };
    }

    private Vector3 ResolveStart()
    {
        if (startTransform != null)
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
        if (goalTransform != null)
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

    private void OnGUI()
    {
        if (!visible)
        {
            return;
        }

        GUI.Box(
            new Rect(10, 10, 460, 30 + results.Count * 22 + 30),
            "Algorithm Comparison (D/A/B: select, P: path, C: toggle)");

        int y = 35;
        GUI.Label(new Rect(20, y, 120, 20), "Algorithm");
        GUI.Label(new Rect(145, y, 90, 20), "Path nodes");
        GUI.Label(new Rect(240, y, 90, 20), "Visited");
        GUI.Label(new Rect(330, y, 90, 20), "Time (ms)");
        y += 18;

        foreach (Result result in results)
        {
            string label = result.Selected ? $"> {result.Algorithm}" : result.Algorithm;
            GUI.Label(new Rect(20, y, 120, 20), label);

            if (!result.Implemented)
            {
                GUI.Label(new Rect(145, y, 200, 20), "not implemented");
            }
            else if (!result.Succeeded)
            {
                GUI.Label(new Rect(145, y, 90, 20), "no path");
                GUI.Label(new Rect(240, y, 90, 20), result.VisitedNodes.ToString());
                GUI.Label(new Rect(330, y, 90, 20), result.Milliseconds.ToString("F2"));
            }
            else
            {
                GUI.Label(new Rect(145, y, 90, 20), result.PathNodes.ToString());
                GUI.Label(new Rect(240, y, 90, 20), result.VisitedNodes.ToString());
                GUI.Label(new Rect(330, y, 90, 20), result.Milliseconds.ToString("F2"));
            }

            y += 22;
        }
    }
}
