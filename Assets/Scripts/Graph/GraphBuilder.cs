using System.Collections.Generic;
using UnityEngine;

public class GraphBuilder : MonoBehaviour
{
    public static GraphBuilder Instance;

    [Header("Grid Settings")]
    public int gridWidth = 10;
    public int gridDepth = 10;
    public float cellSize = 1f;
    public Vector3 gridOrigin = Vector3.zero;
    public LayerMask wallLayer;

    public Dictionary<Vector3, List<Vector3>> AdjacencyList { get; private set; }

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        BuildGraph();
    }

    public void BuildGraph()
    {
        AdjacencyList = new Dictionary<Vector3, List<Vector3>>();

        // Pass 1: collect walkable nodes
        for (int x = 0; x < gridWidth; x++)
        {
            for (int z = 0; z < gridDepth; z++)
            {
                Vector3 worldPos = GridToWorld(x, z);
                if (IsWalkable(worldPos))
                    AdjacencyList[worldPos] = new List<Vector3>();
            }
        }

        // Pass 2: connect 4-directional neighbours
        int[] dx = { 1, -1, 0, 0 };
        int[] dz = { 0, 0, 1, -1 };

        foreach (Vector3 node in new List<Vector3>(AdjacencyList.Keys))
        {
            int x = Mathf.RoundToInt((node.x - gridOrigin.x) / cellSize);
            int z = Mathf.RoundToInt((node.z - gridOrigin.z) / cellSize);

            for (int i = 0; i < 4; i++)
            {
                int nx = x + dx[i];
                int nz = z + dz[i];
                if (nx < 0 || nx >= gridWidth || nz < 0 || nz >= gridDepth) continue;

                Vector3 neighbour = GridToWorld(nx, nz);
                if (AdjacencyList.ContainsKey(neighbour))
                    AdjacencyList[node].Add(neighbour);
            }
        }

        Debug.Log($"[GraphBuilder] Built graph: {AdjacencyList.Count} walkable nodes.");
        PrintAdjacencyList();
    }

    bool IsWalkable(Vector3 worldPos)
    {
        // Sample at Y=0.5 to detect wall colliders standing on the floor
        Vector3 checkCenter = new Vector3(worldPos.x, 0.5f, worldPos.z);
        Vector3 halfExtents = new Vector3(cellSize * 0.4f, 0.4f, cellSize * 0.4f);
        return !Physics.CheckBox(checkCenter, halfExtents, Quaternion.identity, wallLayer);
    }

    Vector3 GridToWorld(int x, int z)
    {
        return new Vector3(gridOrigin.x + x * cellSize, 0f, gridOrigin.z + z * cellSize);
    }

    void PrintAdjacencyList()
    {
        int shown = 0;
        foreach (var kvp in AdjacencyList)
        {
            if (shown >= 20)
            {
                Debug.Log("[GraphBuilder] ... (truncated — attach debugger for full output)");
                break;
            }
            Debug.Log($"  {kvp.Key} -> [{string.Join(", ", kvp.Value)}]");
            shown++;
        }
    }
}
