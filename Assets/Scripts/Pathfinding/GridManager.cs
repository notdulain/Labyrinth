using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Builds a simple 2D grid on the X/Z plane for A* pathfinding.
/// </summary>
public class GridManager : MonoBehaviour
{
    [Header("Grid")]
    public Vector2 gridWorldSize = new Vector2(40f, 40f);
    public float nodeRadius = 0.5f;

    [Header("Detection")]
    public LayerMask obstacleLayers;
    public bool displayGridGizmos = true;
    public float obstacleCheckHeight = 0.5f;
    public float obstacleCheckScale = 0.9f;

    private Node[,] grid;
    private float nodeDiameter;
    private int gridSizeX;
    private int gridSizeY;

    public int GridSizeX
    {
        get { return gridSizeX; }
    }

    public int GridSizeY
    {
        get { return gridSizeY; }
    }

    public int MaxSize
    {
        get { return gridSizeX * gridSizeY; }
    }

    private void Awake()
    {
        CreateGrid();
    }

    [ContextMenu("Rebuild Grid")]
    public void CreateGrid()
    {
        nodeDiameter = nodeRadius * 2f;
        gridSizeX = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.x / nodeDiameter));
        gridSizeY = Mathf.Max(1, Mathf.RoundToInt(gridWorldSize.y / nodeDiameter));
        grid = new Node[gridSizeX, gridSizeY];

        Vector3 worldBottomLeft =
            transform.position
            - Vector3.right * gridWorldSize.x * 0.5f
            - Vector3.forward * gridWorldSize.y * 0.5f;

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Vector3 worldPoint = worldBottomLeft
                    + Vector3.right * (x * nodeDiameter + nodeRadius)
                    + Vector3.forward * (y * nodeDiameter + nodeRadius);

                worldPoint.y = transform.position.y;

                Vector3 obstacleCheckPoint = worldPoint + Vector3.up * obstacleCheckHeight;
                float checkRadius = Mathf.Max(0.01f, nodeRadius * obstacleCheckScale);
                bool blocked = Physics.CheckSphere(
                    obstacleCheckPoint,
                    checkRadius,
                    obstacleLayers,
                    QueryTriggerInteraction.Ignore);

                bool walkable = !blocked;
                grid[x, y] = new Node(walkable, worldPoint, x, y);
            }
        }
    }

    public void ResetNodes()
    {
        if (grid == null)
        {
            return;
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                grid[x, y].ResetPathData();
            }
        }
    }

    public Node NodeFromWorldPoint(Vector3 worldPosition)
    {
        if (grid == null)
        {
            CreateGrid();
        }

        float percentX = Mathf.Clamp01((worldPosition.x - (transform.position.x - gridWorldSize.x * 0.5f)) / gridWorldSize.x);
        float percentY = Mathf.Clamp01((worldPosition.z - (transform.position.z - gridWorldSize.y * 0.5f)) / gridWorldSize.y);

        int x = Mathf.Clamp(Mathf.RoundToInt((gridSizeX - 1) * percentX), 0, gridSizeX - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt((gridSizeY - 1) * percentY), 0, gridSizeY - 1);

        return grid[x, y];
    }

    public List<Node> GetNeighbours(Node node)
    {
        List<Node> neighbours = new List<Node>();

        int[,] directions =
        {
            { 0, 1 },
            { 1, 0 },
            { 0, -1 },
            { -1, 0 }
        };

        for (int i = 0; i < directions.GetLength(0); i++)
        {
            int checkX = node.gridX + directions[i, 0];
            int checkY = node.gridY + directions[i, 1];

            if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
            {
                continue;
            }

            neighbours.Add(grid[checkX, checkY]);
        }

        return neighbours;
    }

    public List<Node> GetNeighbours(Node node, bool allowDiagonalMovement)
    {
        if (!allowDiagonalMovement)
        {
            return GetNeighbours(node);
        }

        List<Node> neighbours = new List<Node>();

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                if (x == 0 && y == 0)
                {
                    continue;
                }

                int checkX = node.gridX + x;
                int checkY = node.gridY + y;

                if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
                {
                    continue;
                }

                neighbours.Add(grid[checkX, checkY]);
            }
        }

        return neighbours;
    }

    public Node GetClosestWalkableNode(Vector3 worldPosition, int maxSearchDistance = 5)
    {
        Node centerNode = NodeFromWorldPoint(worldPosition);
        if (centerNode == null)
        {
            return null;
        }

        if (centerNode.walkable)
        {
            return centerNode;
        }

        Node bestNode = null;
        float bestDistance = float.MaxValue;

        for (int radius = 1; radius <= maxSearchDistance; radius++)
        {
            bool foundWalkableNodeThisRing = false;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    if (Mathf.Abs(x) != radius && Mathf.Abs(y) != radius)
                    {
                        continue;
                    }

                    int checkX = centerNode.gridX + x;
                    int checkY = centerNode.gridY + y;

                    if (checkX < 0 || checkX >= gridSizeX || checkY < 0 || checkY >= gridSizeY)
                    {
                        continue;
                    }

                    Node candidate = grid[checkX, checkY];
                    if (!candidate.walkable)
                    {
                        continue;
                    }

                    foundWalkableNodeThisRing = true;

                    float distance = (candidate.worldPosition - worldPosition).sqrMagnitude;
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestNode = candidate;
                    }
                }
            }

            if (foundWalkableNodeThisRing)
            {
                return bestNode;
            }
        }

        return null;
    }

    private void OnDrawGizmos()
    {
        if (!displayGridGizmos)
        {
            return;
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(transform.position, new Vector3(gridWorldSize.x, 0.1f, gridWorldSize.y));

        if (grid == null)
        {
            return;
        }

        for (int x = 0; x < gridSizeX; x++)
        {
            for (int y = 0; y < gridSizeY; y++)
            {
                Node node = grid[x, y];
                Gizmos.color = node.walkable
                    ? new Color(0.2f, 0.8f, 0.3f, 0.2f)
                    : new Color(0.9f, 0.15f, 0.15f, 0.35f);

                Gizmos.DrawCube(node.worldPosition + Vector3.up * 0.02f, Vector3.one * (nodeDiameter - 0.05f));
            }
        }
    }
}
