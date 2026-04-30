using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Performs Dijkstra shortest-path search for labyrinth navigation.
/// </summary>
public class DijkstraSearch : MonoBehaviour
{
    public static DijkstraSearch Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    public List<Vector3> FindPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal)
    {
        if (graph == null || graph.Count == 0)
        {
            return new List<Vector3>();
        }

        if (!graph.ContainsKey(start) || !graph.ContainsKey(goal))
        {
            return new List<Vector3>();
        }

        if (start == goal)
        {
            return new List<Vector3> { start };
        }

        var frontier = new PriorityQueue<Vector3>();
        var costs = new Dictionary<Vector3, float>();
        var cameFrom = new Dictionary<Vector3, Vector3>();
        var visited = new HashSet<Vector3>();

        foreach (Vector3 node in graph.Keys)
        {
            costs[node] = float.PositiveInfinity;
        }

        costs[start] = 0f;
        frontier.Enqueue(start, 0f);

        while (frontier.Count > 0)
        {
            Vector3 current = frontier.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == goal)
            {
                break;
            }

            if (!graph.TryGetValue(current, out List<Vector3> neighbors))
            {
                continue;
            }

            foreach (Vector3 next in neighbors)
            {
                if (!costs.ContainsKey(next))
                {
                    continue;
                }

                float newCost = costs[current] + 1f;
                if (newCost >= costs[next])
                {
                    continue;
                }

                costs[next] = newCost;
                cameFrom[next] = current;
                frontier.Enqueue(next, newCost);
            }
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    [ContextMenu("Run Dijkstra Hardcoded Test")]
    public void RunHardcodedTest()
    {
        var graph = new Dictionary<Vector3, List<Vector3>>();

        Vector3 a = new Vector3(0f, 0f, 0f);
        Vector3 b = new Vector3(1f, 0f, 0f);
        Vector3 c = new Vector3(2f, 0f, 0f);
        Vector3 d = new Vector3(1f, 0f, 1f);

        graph[a] = new List<Vector3> { b };
        graph[b] = new List<Vector3> { a, c, d };
        graph[c] = new List<Vector3> { b };
        graph[d] = new List<Vector3> { b, c };

        List<Vector3> path = FindPath(graph, a, c);
        Debug.Log(path.Count == 0
            ? "Dijkstra test failed: no path found."
            : $"Dijkstra test path: {string.Join(" -> ", path)}");
    }

    private List<Vector3> ReconstructPath(
        Dictionary<Vector3, Vector3> cameFrom,
        Vector3 start,
        Vector3 goal)
    {
        if (!cameFrom.ContainsKey(goal))
        {
            return new List<Vector3>();
        }

        var path = new List<Vector3> { goal };
        Vector3 current = goal;

        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
