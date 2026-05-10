using UnityEngine;
using System.Collections.Generic;
using System.Reflection;

/// <summary>
/// Visualizes computed navigation paths for debugging in the labyrinth scenes.
///
/// Press P (default) to toggle visualization on/off.
/// When enabled, all three pathfinding algorithms run from the same start/goal
/// and their results are drawn simultaneously in different colours:
///
///   BFS      → Blue
///   A*       → Green
///   Dijkstra → Yellow
///
/// Scene view  : coloured spheres + connecting lines drawn via OnDrawGizmos.
/// Game view   : same lines drawn via Debug.DrawLine every frame while active.
///
/// A* (Sasindi) is called via reflection so this script keeps working even when
/// AStarSearch.FindPath has not been implemented yet.
/// </summary>
public class PathVisualizer : MonoBehaviour
{
    // -------------------------------------------------------------------------
    // Inspector settings
    // -------------------------------------------------------------------------

    [Header("Hotkey")]
    [Tooltip("Key that toggles path visualization on / off.")]
    [SerializeField] private KeyCode toggleKey = KeyCode.P;

    [Header("Optional: pin specific start / goal positions")]
    [Tooltip("Leave null to auto-detect DemonDog position as start.")]
    [SerializeField] private Transform startTransform;

    [Tooltip("Leave null to auto-detect the Player as goal.")]
    [SerializeField] private Transform goalTransform;

    [Header("Sphere size")]
    [SerializeField] private float nodeSphereRadius = 0.25f;

    // -------------------------------------------------------------------------
    // Private state
    // -------------------------------------------------------------------------

    private bool isVisible = false;

    // Cached paths — refreshed each time we toggle on
    private List<Vector3> bfsPath      = new List<Vector3>();
    private List<Vector3> astarPath    = new List<Vector3>();
    private List<Vector3> dijkstraPath = new List<Vector3>();

    // References to sibling algorithm scripts in the scene
    private BFSSearch      bfsSearcher;
    private DijkstraSearch dijkstraSearcher;
    private MonoBehaviour  astarSearcher;   // kept as MonoBehaviour — may be null

    // -------------------------------------------------------------------------
    // Unity lifecycle
    // -------------------------------------------------------------------------

    private void Start()
    {
        // Find algorithm scripts that are already in the scene
        bfsSearcher      = FindObjectOfType<BFSSearch>();
        dijkstraSearcher = FindObjectOfType<DijkstraSearch>();

        // A* is Sasindi's script — use FindObjectOfType generically
        // so we don't break if the class is empty or not yet attached
        AStarSearch foundAStar = FindObjectOfType<AStarSearch>();
        if (foundAStar == null)
        {
            foundAStar = gameObject.AddComponent<AStarSearch>();
        }

        astarSearcher = foundAStar;

        Debug.Log("[PathVisualizer] Ready. Press P to toggle path visualization.");
    }

    // How often (in seconds) to recalculate paths while visualizer is ON.
    // Every frame would be too expensive — 0.5s is smooth enough for debugging.
    [Header("Refresh rate")]
    [SerializeField] private float refreshInterval = 0.5f;
    private float refreshTimer = 0f;

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isVisible = !isVisible;

            if (isVisible)
            {
                RefreshAllPaths();   // immediate refresh on toggle-on
                refreshTimer = 0f;
            }
            else
            {
                Debug.Log("[PathVisualizer] Visualization OFF.");
            }
        }

        if (isVisible)
        {
            // Count down and recalculate — paths follow the moving player
            refreshTimer -= Time.deltaTime;
            if (refreshTimer <= 0f)
            {
                RefreshAllPaths();
                refreshTimer = refreshInterval;
            }

            // Draw lines every frame (Debug.DrawLine lasts only one frame)
            DrawDebugLines(bfsPath,      Color.blue);
            DrawDebugLines(astarPath,    Color.green);
            DrawDebugLines(dijkstraPath, Color.yellow);
        }
    }

    // -------------------------------------------------------------------------
    // Path calculation
    // -------------------------------------------------------------------------

    private void RefreshAllPaths()
    {
        if (GraphBuilder.Instance == null || GraphBuilder.Instance.AdjacencyList == null)
        {
            Debug.LogWarning("[PathVisualizer] GraphBuilder is not ready yet.");
            return;
        }

        var graph = GraphBuilder.Instance.AdjacencyList;

        // Snap real-world positions to the nearest graph node
        Vector3 startNode = GraphBuilder.Instance.GetNearestNode(ResolveStart());
        Vector3 goalNode  = GraphBuilder.Instance.GetNearestNode(ResolveGoal());

        // --- BFS (your script) ---
        bfsPath = (bfsSearcher != null)
            ? bfsSearcher.FindPath(graph, startNode, goalNode)
            : new List<Vector3>();

        // --- Dijkstra (Luchitha's script) ---
        dijkstraPath = (dijkstraSearcher != null)
            ? dijkstraSearcher.FindPath(graph, startNode, goalNode)
            : new List<Vector3>();

        // --- A* (Sasindi's script) — called via reflection so we don't crash
        //     if FindPath hasn't been written yet ---
        astarPath = InvokeAStarFindPath(graph, startNode, goalNode);

        // Print a summary so we can verify in the Console
        Debug.Log(
            $"[PathVisualizer] ON  |  " +
            $"BFS: {bfsPath.Count} nodes (blue)  |  " +
            $"A*: {(astarPath.Count > 0 ? astarPath.Count + " nodes" : "not ready")} (green)  |  " +
            $"Dijkstra: {dijkstraPath.Count} nodes (yellow)");
    }

    /// <summary>
    /// Calls AStarSearch.FindPath via reflection.
    /// Returns an empty list if A* is not yet implemented.
    /// </summary>
    private List<Vector3> InvokeAStarFindPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal)
    {
        if (astarSearcher == null)
            return new List<Vector3>();

        // Look for a method with the exact signature FindPath(graph, start, goal)
        MethodInfo method = astarSearcher.GetType().GetMethod(
            "FindPath",
            new[] {
                typeof(Dictionary<Vector3, List<Vector3>>),
                typeof(Vector3),
                typeof(Vector3)
            });

        if (method == null)
        {
            // A* exists in the scene but FindPath is not implemented yet
            return new List<Vector3>();
        }

        var result = method.Invoke(astarSearcher, new object[] { graph, start, goal })
                     as List<Vector3>;

        return result ?? new List<Vector3>();
    }

    // -------------------------------------------------------------------------
    // Resolve start / goal positions
    // -------------------------------------------------------------------------

    private Vector3 ResolveStart()
    {
        // Priority: pinned Transform → DemonDog in scene → origin
        if (startTransform != null) return startTransform.position;

        DemonDogController dog = FindObjectOfType<DemonDogController>();
        return dog != null ? dog.transform.position : Vector3.zero;
    }

    private Vector3 ResolveGoal()
    {
        // Priority: pinned Transform → tagged Player → origin
        if (goalTransform != null) return goalTransform.position;

        GameObject hero = GameObject.FindGameObjectWithTag("Player");
        return hero != null ? hero.transform.position : Vector3.zero;
    }

    // -------------------------------------------------------------------------
    // Drawing — Game view (Debug.DrawLine, called every Update frame)
    // -------------------------------------------------------------------------

    private void DrawDebugLines(List<Vector3> path, Color color)
    {
        if (path == null || path.Count < 2) return;

        for (int i = 0; i < path.Count - 1; i++)
        {
            Debug.DrawLine(path[i], path[i + 1], color);
        }
    }

    // -------------------------------------------------------------------------
    // Drawing — Scene view (Gizmos, called by the editor every repaint)
    // -------------------------------------------------------------------------

    private void OnDrawGizmos()
    {
        if (!isVisible) return;

        DrawGizmosPath(bfsPath,      Color.blue);
        DrawGizmosPath(astarPath,    Color.green);
        DrawGizmosPath(dijkstraPath, Color.yellow);
    }

    /// <summary>
    /// Draws a sphere at every node and a line between consecutive nodes.
    /// </summary>
    private void DrawGizmosPath(List<Vector3> path, Color color)
    {
        if (path == null || path.Count == 0) return;

        Gizmos.color = color;

        for (int i = 0; i < path.Count; i++)
        {
            // Sphere at this node
            Gizmos.DrawSphere(path[i], nodeSphereRadius);

            // Line to the next node
            if (i < path.Count - 1)
            {
                Gizmos.DrawLine(path[i], path[i + 1]);
            }
        }
    }
}
