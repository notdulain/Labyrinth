using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Performs Dijkstra shortest-path search over an adjacency-list graph.
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

        var queue = new PriorityQueue<Vector3>();
        var distances = new Dictionary<Vector3, float>();
        var previous = new Dictionary<Vector3, Vector3>();
        var visited = new HashSet<Vector3>();

        foreach (var node in graph.Keys)
        {
            distances[node] = float.PositiveInfinity;
        }

        distances[start] = 0f;
        queue.Enqueue(start, 0f);

        while (queue.Count > 0)
        {
            Vector3 current = queue.Dequeue();
            if (!visited.Add(current))
            {
                continue;
            }

            if (current == goal)
            {
                break;
            }

            if (!graph.TryGetValue(current, out var neighbors))
            {
                continue;
            }

            foreach (var neighbor in neighbors)
            {
                if (!distances.ContainsKey(neighbor))
                {
                    continue;
                }

                float newCost = distances[current] + 1f;
                if (newCost >= distances[neighbor])
                {
                    continue;
                }

                distances[neighbor] = newCost;
                previous[neighbor] = current;
                queue.Enqueue(neighbor, newCost);
            }
        }

        return ReconstructPath(previous, start, goal);
    }

    private List<Vector3> ReconstructPath(
        Dictionary<Vector3, Vector3> previous,
        Vector3 start,
        Vector3 goal)
    {
        if (!previous.ContainsKey(goal))
        {
            return new List<Vector3>();
        }

        var path = new List<Vector3> { goal };
        Vector3 current = goal;

        while (current != start)
        {
            current = previous[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }
}
