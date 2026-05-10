using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Runs A* pathfinding on the maze grid.
/// </summary>
public class AStarSearch : MonoBehaviour
{
    public GridManager gridManager;
    public bool logPathToConsole = true;

    private readonly PriorityQueue openSet = new PriorityQueue();
    private readonly HashSet<Node> closedSet = new HashSet<Node>();

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }
    }

    public List<Node> FindPath(Vector3 startPosition, Vector3 targetPosition)
    {
        List<Node> emptyPath = new List<Node>();

        if (gridManager == null)
        {
            Debug.LogWarning("AStarSearch needs a GridManager reference.", this);
            return emptyPath;
        }

        gridManager.CreateGrid();
        gridManager.ResetNodes();
        openSet.Clear();
        closedSet.Clear();

        Node startNode = gridManager.NodeFromWorldPoint(startPosition);
        Node targetNode = gridManager.NodeFromWorldPoint(targetPosition);

        if (!startNode.walkable)
        {
            startNode = gridManager.GetClosestWalkableNode(startPosition);
        }

        if (!targetNode.walkable)
        {
            targetNode = gridManager.GetClosestWalkableNode(targetPosition);
        }

        if (startNode == null || targetNode == null)
        {
            Debug.LogWarning("AStarSearch could not find walkable start or target nodes.", this);
            return emptyPath;
        }

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        openSet.Enqueue(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet.Dequeue();
            if (currentNode == null)
            {
                break;
            }

            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                List<Node> finalPath = RetracePath(startNode, targetNode);
                if (logPathToConsole)
                {
                    Debug.Log(BuildPathMessage(finalPath), this);
                }

                return finalPath;
            }

            List<Node> neighbours = gridManager.GetNeighbours(currentNode);
            for (int i = 0; i < neighbours.Count; i++)
            {
                Node neighbour = neighbours[i];

                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                int newMovementCost = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCost < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCost;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Enqueue(neighbour);
                    }
                    else
                    {
                        openSet.UpdateItem(neighbour);
                    }
                }
            }
        }

        Debug.LogWarning("AStarSearch could not find a path.", this);
        return emptyPath;
    }

    private List<Node> RetracePath(Node startNode, Node endNode)
    {
        List<Node> path = new List<Node>();
        Node currentNode = endNode;

        while (currentNode != startNode)
        {
            path.Add(currentNode);
            currentNode = currentNode.parent;

            if (currentNode == null)
            {
                return new List<Node>();
            }
        }

        path.Add(startNode);
        path.Reverse();
        return path;
    }

    private int GetDistance(Node a, Node b)
    {
        int distanceX = Mathf.Abs(a.gridX - b.gridX);
        int distanceY = Mathf.Abs(a.gridY - b.gridY);
        return (distanceX + distanceY) * 10;
    }

    private string BuildPathMessage(List<Node> path)
    {
        if (path == null || path.Count == 0)
        {
            return "Path found: <empty>";
        }

        StringBuilder builder = new StringBuilder("Path found: ");
        for (int i = 0; i < path.Count; i++)
        {
            builder.Append(FormatPosition(path[i].worldPosition));

            if (i < path.Count - 1)
            {
                builder.Append(" -> ");
            }
        }

        return builder.ToString();
    }

    private string FormatPosition(Vector3 position)
    {
        return $"({Mathf.RoundToInt(position.x)},{Mathf.RoundToInt(position.y)},{Mathf.RoundToInt(position.z)})";
    }
}
