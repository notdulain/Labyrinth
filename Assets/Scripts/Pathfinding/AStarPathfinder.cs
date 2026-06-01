using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Finds a shortest walkable path on the GridManager using A*.
/// </summary>
public class AStarPathfinder : MonoBehaviour
{
    [Header("References")]
    public GridManager gridManager;
    public Transform player;
    public Transform temple;

    [Header("Path Settings")]
    public bool allowDiagonalMovement = false;
    public bool simplifyWorldPath = true;
    public bool logConfigurationWarnings = true;

    private void Awake()
    {
        if (gridManager == null)
        {
            gridManager = FindFirstObjectByType<GridManager>();
        }
    }

    public List<Node> FindPathToTemple()
    {
        if (player == null || temple == null)
        {
            if (logConfigurationWarnings)
            {
                Debug.LogWarning("AStarPathfinder is missing the Player or Temple reference.", this);
            }

            return new List<Node>();
        }

        return FindPath(player.position, temple.position);
    }

    public List<Vector3> FindWorldPathToTemple()
    {
        if (player == null || temple == null)
        {
            return new List<Vector3>();
        }

        return FindWorldPath(player.position, temple.position);
    }

    public List<Vector3> FindWorldPath(Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        List<Node> nodePath = FindPath(startWorldPosition, targetWorldPosition);
        if (nodePath.Count == 0)
        {
            return new List<Vector3>();
        }

        if (!simplifyWorldPath)
        {
            List<Vector3> fullPath = new List<Vector3>();
            for (int i = 0; i < nodePath.Count; i++)
            {
                fullPath.Add(nodePath[i].worldPosition);
            }

            return fullPath;
        }

        return SimplifyPath(nodePath);
    }

    public List<Node> FindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        List<Node> emptyPath = new List<Node>();

        if (gridManager == null)
        {
            if (logConfigurationWarnings)
            {
                Debug.LogWarning("AStarPathfinder could not find a GridManager in the scene.", this);
            }

            return emptyPath;
        }

        gridManager.CreateGrid();
        gridManager.ResetNodes();

        Node startNode = gridManager.GetClosestWalkableNode(startWorldPosition);
        Node targetNode = gridManager.GetClosestWalkableNode(targetWorldPosition);

        if (startNode == null || targetNode == null)
        {
            return emptyPath;
        }

        List<Node> openSet = new List<Node>();
        HashSet<Node> closedSet = new HashSet<Node>();

        startNode.gCost = 0;
        startNode.hCost = GetDistance(startNode, targetNode);
        openSet.Add(startNode);

        while (openSet.Count > 0)
        {
            Node currentNode = openSet[0];

            for (int i = 1; i < openSet.Count; i++)
            {
                Node candidateNode = openSet[i];
                if (candidateNode.fCost < currentNode.fCost ||
                    candidateNode.fCost == currentNode.fCost && candidateNode.hCost < currentNode.hCost)
                {
                    currentNode = candidateNode;
                }
            }

            openSet.Remove(currentNode);
            closedSet.Add(currentNode);

            if (currentNode == targetNode)
            {
                return RetracePath(startNode, targetNode);
            }

            List<Node> neighbours = gridManager.GetNeighbours(currentNode, allowDiagonalMovement);
            for (int i = 0; i < neighbours.Count; i++)
            {
                Node neighbour = neighbours[i];
                if (!neighbour.walkable || closedSet.Contains(neighbour))
                {
                    continue;
                }

                int newMovementCostToNeighbour = currentNode.gCost + GetDistance(currentNode, neighbour);
                if (newMovementCostToNeighbour < neighbour.gCost || !openSet.Contains(neighbour))
                {
                    neighbour.gCost = newMovementCostToNeighbour;
                    neighbour.hCost = GetDistance(neighbour, targetNode);
                    neighbour.parent = currentNode;

                    if (!openSet.Contains(neighbour))
                    {
                        openSet.Add(neighbour);
                    }
                }
            }
        }

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

    private List<Vector3> SimplifyPath(List<Node> path)
    {
        List<Vector3> simplifiedPath = new List<Vector3>();
        if (path.Count == 0)
        {
            return simplifiedPath;
        }

        simplifiedPath.Add(path[0].worldPosition);

        Vector2Int previousDirection = Vector2Int.zero;
        for (int i = 1; i < path.Count; i++)
        {
            Vector2Int newDirection = new Vector2Int(
                path[i].gridX - path[i - 1].gridX,
                path[i].gridY - path[i - 1].gridY);

            if (newDirection != previousDirection)
            {
                simplifiedPath.Add(path[i - 1].worldPosition);
            }

            previousDirection = newDirection;
        }

        simplifiedPath.Add(path[path.Count - 1].worldPosition);
        return RemoveDuplicatePoints(simplifiedPath);
    }

    private List<Vector3> RemoveDuplicatePoints(List<Vector3> points)
    {
        List<Vector3> cleanedPoints = new List<Vector3>();

        for (int i = 0; i < points.Count; i++)
        {
            if (cleanedPoints.Count == 0 || cleanedPoints[cleanedPoints.Count - 1] != points[i])
            {
                cleanedPoints.Add(points[i]);
            }
        }

        return cleanedPoints;
    }

    private int GetDistance(Node a, Node b)
    {
        int distanceX = Mathf.Abs(a.gridX - b.gridX);
        int distanceY = Mathf.Abs(a.gridY - b.gridY);

        if (!allowDiagonalMovement)
        {
            return (distanceX + distanceY) * 10;
        }

        int diagonalSteps = Mathf.Min(distanceX, distanceY);
        int straightSteps = Mathf.Abs(distanceX - distanceY);
        return diagonalSteps * 14 + straightSteps * 10;
    }
}
