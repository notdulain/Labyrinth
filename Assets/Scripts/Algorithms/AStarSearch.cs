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

    public List<Vector3> FindPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 startPosition,
        Vector3 targetPosition)
    {
        List<Vector3> emptyPath = new List<Vector3>();

        if (graph == null || graph.Count == 0)
        {
            Debug.LogWarning("AStarSearch needs a graph reference.", this);
            return emptyPath;
        }

        if (!graph.ContainsKey(startPosition) || !graph.ContainsKey(targetPosition))
        {
            Debug.LogWarning("AStarSearch could not find graph start or target nodes.", this);
            return emptyPath;
        }

        List<Vector3> openSet = new List<Vector3> { startPosition };
        HashSet<Vector3> closedSet = new HashSet<Vector3>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        Dictionary<Vector3, float> gScore = new Dictionary<Vector3, float>
        {
            [startPosition] = 0f
        };
        Dictionary<Vector3, float> fScore = new Dictionary<Vector3, float>
        {
            [startPosition] = Heuristic(startPosition, targetPosition)
        };

        while (openSet.Count > 0)
        {
            Vector3 current = GetLowestScoreNode(openSet, fScore);
            if (current == targetPosition)
            {
                List<Vector3> finalPath = RetracePath(cameFrom, current);
                if (logPathToConsole)
                {
                    Debug.Log(BuildVectorPathMessage(finalPath), this);
                }

                return finalPath;
            }

            openSet.Remove(current);
            closedSet.Add(current);

            foreach (Vector3 neighbour in graph[current])
            {
                if (closedSet.Contains(neighbour))
                {
                    continue;
                }

                float tentativeGScore = GetScore(gScore, current) + Vector3.Distance(current, neighbour);
                if (!openSet.Contains(neighbour))
                {
                    openSet.Add(neighbour);
                }
                else if (tentativeGScore >= GetScore(gScore, neighbour))
                {
                    continue;
                }

                cameFrom[neighbour] = current;
                gScore[neighbour] = tentativeGScore;
                fScore[neighbour] = tentativeGScore + Heuristic(neighbour, targetPosition);
            }
        }

        Debug.LogWarning("AStarSearch could not find a graph path.", this);
        return emptyPath;
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
                    Debug.Log(BuildNodePathMessage(finalPath), this);
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

    private List<Vector3> RetracePath(Dictionary<Vector3, Vector3> cameFrom, Vector3 current)
    {
        List<Vector3> path = new List<Vector3> { current };

        while (cameFrom.ContainsKey(current))
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private Vector3 GetLowestScoreNode(List<Vector3> openSet, Dictionary<Vector3, float> fScore)
    {
        Vector3 best = openSet[0];
        float bestScore = GetScore(fScore, best);

        for (int i = 1; i < openSet.Count; i++)
        {
            Vector3 candidate = openSet[i];
            float candidateScore = GetScore(fScore, candidate);
            if (candidateScore < bestScore)
            {
                best = candidate;
                bestScore = candidateScore;
            }
        }

        return best;
    }

    private float GetScore(Dictionary<Vector3, float> scores, Vector3 node)
    {
        return scores.TryGetValue(node, out float score) ? score : float.PositiveInfinity;
    }

    private float Heuristic(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    private int GetDistance(Node a, Node b)
    {
        int distanceX = Mathf.Abs(a.gridX - b.gridX);
        int distanceY = Mathf.Abs(a.gridY - b.gridY);
        return (distanceX + distanceY) * 10;
    }

    private string BuildNodePathMessage(List<Node> path)
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

    private string BuildVectorPathMessage(List<Vector3> path)
    {
        if (path == null || path.Count == 0)
        {
            return "Path found: <empty>";
        }

        StringBuilder builder = new StringBuilder("Path found: ");
        for (int i = 0; i < path.Count; i++)
        {
            builder.Append(FormatPosition(path[i]));

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
