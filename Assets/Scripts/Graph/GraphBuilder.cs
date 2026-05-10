using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Builds a 2D grid-based navigation graph from the scene.
/// Each walkable cell becomes a node; 4-directional neighbours become edges.
/// </summary>
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
        Instance = this;
        BuildGraph();
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

        Debug.Log($"[GraphBuilder] Built graph: {AdjacencyList.Count} walkable nodes");
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
