using UnityEngine;
using System.Collections.Generic;

public class GraphBuilder : MonoBehaviour
{
    public static GraphBuilder Instance;

    [Header("Grid Settings")]
    public Vector3 gridOrigin = new Vector3(-26.2f, 0f, -35.8f);
    public float cellSize = 2.85f;
    public int gridCols = 19;
    public int gridRows = 26;

    [Header("Wall Detection")]
    public LayerMask wallLayer;
    public float checkBoxRadius = 1.0f;

    public Dictionary<Vector3, List<Vector3>> AdjacencyList { get; private set; }

    void Awake()
    {
        BuildGraph();
        RegisterInstance();
    }

    public void BuildGraph()
    {
        AdjacencyList = new Dictionary<Vector3, List<Vector3>>();

        List<Vector3> walkableNodes = new List<Vector3>();
        for (int col = 0; col < gridCols; col++)
        {
            for (int row = 0; row < gridRows; row++)
            {
                Vector3 worldPos = GridToWorld(col, row);
                if (IsWalkable(worldPos))
                    walkableNodes.Add(worldPos);
            }
        }

        foreach (var node in walkableNodes)
        {
            AdjacencyList[node] = new List<Vector3>();
            Vector3[] neighbours = {
                node + new Vector3(cellSize, 0, 0),
                node + new Vector3(-cellSize, 0, 0),
                node + new Vector3(0, 0, cellSize),
                node + new Vector3(0, 0, -cellSize)
            };

            foreach (var n in neighbours)
            {
                foreach (var existing in walkableNodes)
                {
                    if (Vector3.Distance(n, existing) < 0.1f)
                    {
                        AdjacencyList[node].Add(existing);
                        break;
                    }
                }
            }
        }

        Debug.Log($"[GraphBuilder] Built graph on {name}: {AdjacencyList.Count} walkable nodes");
    }

    public Vector3 GetNearestNode(Vector3 worldPos)
    {
        if (AdjacencyList == null || AdjacencyList.Count == 0)
            return worldPos;

        Vector3 best = worldPos;
        float bestDist = float.PositiveInfinity;
        foreach (var node in AdjacencyList.Keys)
        {
            float d = (node - worldPos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = node;
            }
        }

        return best;
    }

    public Vector3 GetNearestNodeReachableTo(Vector3 worldPos, Vector3 anchorWorldPos)
    {
        HashSet<Vector3> component = GetConnectedComponent(GetNearestNode(anchorWorldPos));
        if (component.Count == 0)
            return GetNearestNode(worldPos);

        return GetNearestNodeInSet(worldPos, component, null);
    }

    public Vector3 GetNearestNodeReachableTo(
        Vector3 worldPos,
        Vector3 anchorWorldPos,
        HashSet<Vector3> excludedNodes)
    {
        HashSet<Vector3> component = GetConnectedComponent(GetNearestNode(anchorWorldPos));
        if (component.Count == 0)
            return GetNearestNode(worldPos);

        return GetNearestNodeInSet(worldPos, component, excludedNodes);
    }

    public bool HasPath(Vector3 start, Vector3 goal)
    {
        if (AdjacencyList == null || !AdjacencyList.ContainsKey(start) || !AdjacencyList.ContainsKey(goal))
            return false;

        HashSet<Vector3> component = GetConnectedComponent(start);
        return component.Contains(goal);
    }

    private void RegisterInstance()
    {
        if (Instance == null || ShouldReplaceInstance(Instance, this))
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning(
                    $"[GraphBuilder] Multiple GraphBuilders found. Using {name} and ignoring {Instance.name}.");
            }

            Instance = this;
        }
        else if (Instance != this)
        {
            Debug.LogWarning(
                $"[GraphBuilder] Multiple GraphBuilders found. Keeping active graph {Instance.name}; {name} is ignored.");
        }
    }

    private static bool ShouldReplaceInstance(GraphBuilder current, GraphBuilder candidate)
    {
        bool currentDetectsWalls = current.wallLayer.value != 0;
        bool candidateDetectsWalls = candidate.wallLayer.value != 0;

        if (candidateDetectsWalls != currentDetectsWalls)
            return candidateDetectsWalls;

        float currentWalkableRatio = current.GetWalkableRatio();
        float candidateWalkableRatio = candidate.GetWalkableRatio();
        if (candidateWalkableRatio < currentWalkableRatio - 0.05f)
            return true;

        return false;
    }

    private float GetWalkableRatio()
    {
        int totalCells = Mathf.Max(1, gridCols * gridRows);
        int walkableCells = AdjacencyList != null ? AdjacencyList.Count : 0;
        return walkableCells / (float)totalCells;
    }

    private HashSet<Vector3> GetConnectedComponent(Vector3 start)
    {
        HashSet<Vector3> visited = new HashSet<Vector3>();
        if (AdjacencyList == null || !AdjacencyList.ContainsKey(start))
            return visited;

        Queue<Vector3> frontier = new Queue<Vector3>();
        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            Vector3 current = frontier.Dequeue();
            foreach (Vector3 next in AdjacencyList[current])
            {
                if (visited.Add(next))
                {
                    frontier.Enqueue(next);
                }
            }
        }

        return visited;
    }

    private Vector3 GetNearestNodeInSet(
        Vector3 worldPos,
        HashSet<Vector3> candidates,
        HashSet<Vector3> excludedNodes)
    {
        Vector3 best = worldPos;
        float bestDist = float.PositiveInfinity;

        foreach (Vector3 node in candidates)
        {
            if (excludedNodes != null && excludedNodes.Contains(node))
                continue;

            float d = (node - worldPos).sqrMagnitude;
            if (d < bestDist)
            {
                bestDist = d;
                best = node;
            }
        }

        return bestDist < float.PositiveInfinity ? best : GetNearestNode(worldPos);
    }

    Vector3 GridToWorld(int col, int row)
    {
        return new Vector3(
            gridOrigin.x + col * cellSize + cellSize * 0.5f,
            0.5f,
            gridOrigin.z + row * cellSize + cellSize * 0.5f
        );
    }

    bool IsWalkable(Vector3 worldPos)
    {
        Vector3 halfExtents = Vector3.one * (checkBoxRadius * 0.5f);
        return !Physics.CheckBox(worldPos, halfExtents, Quaternion.identity, wallLayer);
    }

    void OnDrawGizmos()
    {
        if (AdjacencyList == null) return;
        Gizmos.color = Color.green;
        foreach (var kvp in AdjacencyList)
        {
            Gizmos.DrawSphere(kvp.Key, 0.2f);
            foreach (var neighbour in kvp.Value)
                Gizmos.DrawLine(kvp.Key, neighbour);
        }
    }
}
