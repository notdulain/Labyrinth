using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Performs breadth-first search over the labyrinth graph.
///
/// BFS explores nodes level by level (nearest first) using a Queue.
/// It finds the shortest path measured by number of hops (nodes visited),
/// not by distance weight — making it faster than Dijkstra on uniform grids.
///
/// Time Complexity : O(V + E)  where V = nodes, E = edges
/// Space Complexity: O(V)      for the visited set and cameFrom table
/// </summary>
public class BFSSearch : MonoBehaviour
{
    public static BFSSearch Instance { get; private set; }

    private void Awake()
    {
        // Singleton — only one BFSSearch should exist in the scene
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    /// <summary>
    /// Finds the shortest path (by hop count) from start to goal using BFS.
    /// Returns an empty list when no path exists.
    /// </summary>
    /// <param name="graph">Adjacency list built by GraphBuilder.</param>
    /// <param name="start">World-space position of the start node.</param>
    /// <param name="goal">World-space position of the goal node.</param>
    public List<Vector3> FindPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal)
    {
        // --- Guard checks ---
        if (graph == null || graph.Count == 0)
        {
            Debug.LogWarning("[BFSSearch] Graph is empty or null.");
            return new List<Vector3>();
        }

        if (!graph.ContainsKey(start) || !graph.ContainsKey(goal))
        {
            Debug.LogWarning("[BFSSearch] Start or goal node not found in graph.");
            return new List<Vector3>();
        }

        // If we are already at the goal, return immediately
        if (start == goal)
        {
            return new List<Vector3> { start };
        }

        // --- BFS Core ---

        // Queue holds nodes to explore next (FIFO — first in, first out)
        var frontier = new Queue<Vector3>();

        // visited tracks nodes we have already seen so we don't revisit them
        var visited = new HashSet<Vector3>();

        // cameFrom records how we got to each node — used to reconstruct the path at the end
        var cameFrom = new Dictionary<Vector3, Vector3>();

        // Begin at the start node
        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            // Take the next node from the front of the queue
            Vector3 current = frontier.Dequeue();

            // Did we reach the goal?
            if (current == goal)
            {
                break;
            }

            // Look at every neighbour of the current node
            if (!graph.TryGetValue(current, out List<Vector3> neighbors))
            {
                continue;
            }

            foreach (Vector3 next in neighbors)
            {
                // Only process this neighbour if we haven't visited it yet
                if (visited.Contains(next))
                {
                    continue;
                }

                visited.Add(next);
                cameFrom[next] = current;   // remember how we got here
                frontier.Enqueue(next);     // schedule it to be explored
            }
        }

        // If the goal was never recorded in cameFrom, no path exists
        return ReconstructPath(cameFrom, start, goal);
    }

    /// <summary>
    /// Walks backwards through cameFrom from goal → start, then reverses to get
    /// the correct start → goal order.
    /// </summary>
    private List<Vector3> ReconstructPath(
        Dictionary<Vector3, Vector3> cameFrom,
        Vector3 start,
        Vector3 goal)
    {
        if (!cameFrom.ContainsKey(goal))
        {
            // Goal was never reached
            return new List<Vector3>();
        }

        var path = new List<Vector3> { goal };
        Vector3 current = goal;

        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();   // we built it backwards, so flip it
        return path;
    }

    /// <summary>
    /// Right-click this component in the Unity Inspector and choose
    /// "Run BFS Hardcoded Test" to verify the algorithm without the full scene.
    /// </summary>
    [ContextMenu("Run BFS Hardcoded Test")]
    public void RunHardcodedTest()
    {
        // Build a tiny 4-node graph:  A - B - C
        //                                 |
        //                                 D
        var graph = new Dictionary<Vector3, List<Vector3>>();

        Vector3 a = new Vector3(0f, 0f, 0f);
        Vector3 b = new Vector3(1f, 0f, 0f);
        Vector3 c = new Vector3(2f, 0f, 0f);
        Vector3 d = new Vector3(1f, 0f, 1f);

        graph[a] = new List<Vector3> { b };
        graph[b] = new List<Vector3> { a, c, d };
        graph[c] = new List<Vector3> { b };
        graph[d] = new List<Vector3> { b };

        // Expected shortest path: A → B → C  (2 hops)
        List<Vector3> path = FindPath(graph, a, c);

        if (path.Count == 0)
            Debug.LogError("[BFSSearch] Test FAILED: no path found.");
        else
            Debug.Log($"[BFSSearch] Test PASSED: {string.Join(" -> ", path)}");
    }
}
