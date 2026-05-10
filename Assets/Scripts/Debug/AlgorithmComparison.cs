using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using Debug = UnityEngine.Debug;

/// <summary>
/// Side-by-side comparison of A*, BFS, and Dijkstra on the same graph.
/// Toggle the panel with the C key. Press R to re-run all three.
///
/// A* and BFS are discovered via reflection so this script keeps working
/// while teammates have not yet implemented their FindPath methods.
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
        public double Milliseconds;
    }

    private void Start()
    {
        Run();
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey)) visible = !visible;
        if (Input.GetKeyDown(rerunKey)) Run();
    }

    public void Run()
    {
        results.Clear();

        if (GraphBuilder.Instance == null || GraphBuilder.Instance.AdjacencyList == null)
        {
            Debug.LogWarning("[AlgorithmComparison] GraphBuilder not ready.");
            return;
        }

        Vector3 start = ResolveStart();
        Vector3 goal = ResolveGoal();
        Vector3 startNode = GraphBuilder.Instance.GetNearestNode(start);
        Vector3 goalNode = GraphBuilder.Instance.GetNearestNode(goal);
        var graph = GraphBuilder.Instance.AdjacencyList;

        results.Add(Measure("A*",       FindObjectOfType<AStarSearch>(),    graph, startNode, goalNode));
        results.Add(Measure("BFS",      FindObjectOfType<BFSSearch>(),      graph, startNode, goalNode));
        results.Add(Measure("Dijkstra", FindObjectOfType<DijkstraSearch>(), graph, startNode, goalNode));

        foreach (var r in results)
        {
            if (!r.Implemented)
                Debug.Log($"[AlgorithmComparison] {r.Algorithm}: not implemented yet.");
            else if (!r.Succeeded)
                Debug.Log($"[AlgorithmComparison] {r.Algorithm}: no path found ({r.Milliseconds:F2} ms).");
            else
                Debug.Log($"[AlgorithmComparison] {r.Algorithm}: {r.PathNodes} nodes in {r.Milliseconds:F2} ms.");
        }
    }

    private Vector3 ResolveStart()
    {
        if (startTransform != null) return startTransform.position;
        var dog = FindObjectOfType<DemonDogController>();
        return dog != null ? dog.transform.position : Vector3.zero;
    }

    private Vector3 ResolveGoal()
    {
        if (goalTransform != null) return goalTransform.position;
        GameObject hero = GameObject.FindGameObjectWithTag("Player");
        return hero != null ? hero.transform.position : Vector3.zero;
    }

    private static Result Measure(
        string label,
        MonoBehaviour searcher,
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal)
    {
        var result = new Result { Algorithm = label };

        if (searcher == null)
        {
            return result;
        }

        MethodInfo findPath = searcher.GetType().GetMethod(
            "FindPath",
            new[] { typeof(Dictionary<Vector3, List<Vector3>>), typeof(Vector3), typeof(Vector3) });

        if (findPath == null)
        {
            return result;
        }

        result.Implemented = true;

        var sw = Stopwatch.StartNew();
        var path = findPath.Invoke(searcher, new object[] { graph, start, goal }) as List<Vector3>;
        sw.Stop();

        result.Milliseconds = sw.Elapsed.TotalMilliseconds;
        result.Succeeded = path != null && path.Count > 0;
        result.PathNodes = path != null ? path.Count : 0;
        return result;
    }

    private void OnGUI()
    {
        if (!visible) return;

        GUI.Box(new Rect(10, 10, 320, 30 + results.Count * 22 + 30),
            "Algorithm Comparison (C: toggle, R: rerun)");

        int y = 35;
        GUI.Label(new Rect(20, y, 90, 20), "Algorithm");
        GUI.Label(new Rect(120, y, 90, 20), "Path nodes");
        GUI.Label(new Rect(220, y, 90, 20), "Time (ms)");
        y += 18;

        foreach (var r in results)
        {
            GUI.Label(new Rect(20, y, 90, 20), r.Algorithm);

            if (!r.Implemented)
            {
                GUI.Label(new Rect(120, y, 200, 20), "not implemented");
            }
            else if (!r.Succeeded)
            {
                GUI.Label(new Rect(120, y, 90, 20), "no path");
                GUI.Label(new Rect(220, y, 90, 20), r.Milliseconds.ToString("F2"));
            }
            else
            {
                GUI.Label(new Rect(120, y, 90, 20), r.PathNodes.ToString());
                GUI.Label(new Rect(220, y, 90, 20), r.Milliseconds.ToString("F2"));
            }
            y += 22;
        }
    }
}
