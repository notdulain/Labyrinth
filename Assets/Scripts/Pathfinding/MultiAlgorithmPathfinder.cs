using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using Debug = UnityEngine.Debug;

/// <summary>
/// Shared pathfinding entry point for the dungeon dog.
/// All algorithms use the same GraphBuilder adjacency graph, so comparisons are fair.
/// </summary>
public class MultiAlgorithmPathfinder : MonoBehaviour
{
    public GraphBuilder graphBuilder;
    public bool logPathNodes;

    private void Awake()
    {
        ResolveGraphBuilder();
    }

    public PathfindingResult FindPath(Vector3 startWorldPosition, Vector3 targetWorldPosition, PathfindingAlgorithm algorithm)
    {
        return FindPath(startWorldPosition, targetWorldPosition, algorithm, false);
    }

    public PathfindingResult FindPathFromNodes(
        Vector3 startNode,
        Vector3 goalNode,
        PathfindingAlgorithm algorithm,
        Vector3 requestedStartPosition,
        Vector3 requestedTargetPosition,
        bool suppressLogging = false)
    {
        ResolveGraphBuilder();
        EnsureGraphBuilt();

        PathfindingResult result = new PathfindingResult
        {
            algorithmName = GetAlgorithmName(algorithm),
            requestedStartPosition = requestedStartPosition,
            requestedTargetPosition = requestedTargetPosition,
            resolvedStartNode = startNode,
            resolvedTargetNode = goalNode,
            usedNearestStartNode = NeedsNearestNodeFallback(requestedStartPosition, startNode),
            usedNearestTargetNode = NeedsNearestNodeFallback(requestedTargetPosition, goalNode)
        };

        if (graphBuilder == null || !graphBuilder.HasGraph)
        {
            result.failureReason = "GraphBuilder is missing or has no graph.";
            LogResult(result, suppressLogging);
            return result;
        }

        Dictionary<Vector3, List<Vector3>> graph = graphBuilder.AdjacencyList;
        result.startPositionBlocked = !graphBuilder.IsWorldPositionWalkable(requestedStartPosition);
        result.targetPositionBlocked = !graphBuilder.IsWorldPositionWalkable(requestedTargetPosition);
        result.startComponentSize = graphBuilder.GetConnectedComponentSize(startNode);
        result.targetComponentSize = graphBuilder.GetConnectedComponentSize(goalNode);

        if (!graph.ContainsKey(startNode) || !graph.ContainsKey(goalNode))
        {
            result.failureReason = "Resolved start or target node is not present in the graph.";
            LogResult(result, suppressLogging);
            return result;
        }

        Stopwatch stopwatch = Stopwatch.StartNew();
        switch (algorithm)
        {
            case PathfindingAlgorithm.Dijkstra:
                result.worldPath = FindDijkstraPath(graph, startNode, goalNode, out result.visitedNodeCount);
                break;
            case PathfindingAlgorithm.BFS:
                result.worldPath = FindBfsPath(graph, startNode, goalNode, out result.visitedNodeCount);
                break;
            default:
                result.worldPath = FindAStarPath(graph, startNode, goalNode, out result.visitedNodeCount);
                break;
        }
        stopwatch.Stop();

        result.calculationTimeMs = (float)stopwatch.Elapsed.TotalMilliseconds;
        result.pathFound = result.worldPath != null && result.worldPath.Count > 0;
        if (!result.pathFound)
        {
            result.failureReason = "The selected algorithm returned an empty path.";
        }

        LogResult(result, suppressLogging);
        return result;
    }

    public PathfindingResult FindPath(
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition,
        PathfindingAlgorithm algorithm,
        bool suppressLogging)
    {
        ResolveGraphBuilder();
        EnsureGraphBuilt();

        PathfindingResult result = new PathfindingResult
        {
            algorithmName = GetAlgorithmName(algorithm),
            requestedStartPosition = startWorldPosition,
            requestedTargetPosition = targetWorldPosition
        };

        if (graphBuilder == null || !graphBuilder.HasGraph)
        {
            result.failureReason = "GraphBuilder is missing or has no graph.";
            LogResult(result, suppressLogging);
            return result;
        }

        Dictionary<Vector3, List<Vector3>> graph = graphBuilder.AdjacencyList;
        result.startPositionBlocked = !graphBuilder.IsWorldPositionWalkable(startWorldPosition);
        result.targetPositionBlocked = !graphBuilder.IsWorldPositionWalkable(targetWorldPosition);

        if (!graphBuilder.TryGetNearestWalkableNode(startWorldPosition, out Vector3 startNode))
        {
            result.failureReason = "Could not resolve a walkable start node for the dog.";
            LogResult(result, suppressLogging);
            return result;
        }

        if (!graphBuilder.TryGetNearestWalkableNode(targetWorldPosition, out Vector3 goalNode))
        {
            result.failureReason = "Could not resolve a walkable target node for the player.";
            LogResult(result, suppressLogging);
            return result;
        }

        result.resolvedStartNode = startNode;
        result.resolvedTargetNode = goalNode;
        result.usedNearestStartNode = NeedsNearestNodeFallback(startWorldPosition, startNode);
        result.usedNearestTargetNode = NeedsNearestNodeFallback(targetWorldPosition, goalNode);
        result.startComponentSize = graphBuilder.GetConnectedComponentSize(startNode);
        result.targetComponentSize = graphBuilder.GetConnectedComponentSize(goalNode);

        if (!graph.ContainsKey(startNode) || !graph.ContainsKey(goalNode))
        {
            result.failureReason = "Resolved start or target node is not present in the graph.";
            LogResult(result, suppressLogging);
            return result;
        }

        if (!graphBuilder.HasPath(startNode, goalNode))
        {
            HashSet<Vector3> startComponent = graphBuilder.GetConnectedComponentNodes(startNode);
            if (!graphBuilder.TryGetNearestNodeInComponent(targetWorldPosition, startComponent, out Vector3 proxyGoalNode))
            {
                result.failureReason =
                    "Dog start node and player target node are disconnected, and no reachable proxy target could be resolved.";
                LogResult(result, suppressLogging);
                return result;
            }

            goalNode = proxyGoalNode;
            result.resolvedTargetNode = goalNode;
            result.usedReachableTargetProxy = true;
        }

        result = FindPathFromNodes(
            startNode,
            goalNode,
            algorithm,
            startWorldPosition,
            targetWorldPosition,
            suppressLogging);

        if (logPathNodes && result.pathFound && !suppressLogging)
        {
            Debug.Log("Path found: " + FormatPath(result.worldPath), this);
        }

        return result;
    }

    public List<PathfindingResult> CompareAll(Vector3 startWorldPosition, Vector3 targetWorldPosition)
    {
        return CompareAll(startWorldPosition, targetWorldPosition, false);
    }

    public List<PathfindingResult> CompareAllFromNodes(
        Vector3 startNode,
        Vector3 goalNode,
        Vector3 requestedStartPosition,
        Vector3 requestedTargetPosition,
        bool suppressLogging = false)
    {
        return new List<PathfindingResult>
        {
            FindPathFromNodes(startNode, goalNode, PathfindingAlgorithm.AStar, requestedStartPosition, requestedTargetPosition, suppressLogging),
            FindPathFromNodes(startNode, goalNode, PathfindingAlgorithm.Dijkstra, requestedStartPosition, requestedTargetPosition, suppressLogging),
            FindPathFromNodes(startNode, goalNode, PathfindingAlgorithm.BFS, requestedStartPosition, requestedTargetPosition, suppressLogging)
        };
    }

    public List<PathfindingResult> CompareAll(
        Vector3 startWorldPosition,
        Vector3 targetWorldPosition,
        bool suppressLogging)
    {
        return new List<PathfindingResult>
        {
            FindPath(startWorldPosition, targetWorldPosition, PathfindingAlgorithm.AStar, suppressLogging),
            FindPath(startWorldPosition, targetWorldPosition, PathfindingAlgorithm.Dijkstra, suppressLogging),
            FindPath(startWorldPosition, targetWorldPosition, PathfindingAlgorithm.BFS, suppressLogging)
        };
    }

    private List<Vector3> FindAStarPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal,
        out int visitedCount)
    {
        visitedCount = 0;
        if (!CanSearch(graph, start, goal)) return new List<Vector3>();
        if (start == goal) return new List<Vector3> { start };

        List<Vector3> openSet = new List<Vector3> { start };
        HashSet<Vector3> closedSet = new HashSet<Vector3>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        Dictionary<Vector3, float> gScore = new Dictionary<Vector3, float> { [start] = 0f };
        Dictionary<Vector3, float> fScore = new Dictionary<Vector3, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            Vector3 current = GetLowestScoreNode(openSet, fScore);
            if (current == goal) return ReconstructPath(cameFrom, start, goal);

            openSet.Remove(current);
            if (!closedSet.Add(current)) continue;
            visitedCount++;

            foreach (Vector3 neighbour in graph[current])
            {
                if (closedSet.Contains(neighbour)) continue;

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
                fScore[neighbour] = tentativeGScore + Heuristic(neighbour, goal);
            }
        }

        return new List<Vector3>();
    }

    private List<Vector3> FindDijkstraPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal,
        out int visitedCount)
    {
        visitedCount = 0;
        if (!CanSearch(graph, start, goal)) return new List<Vector3>();
        if (start == goal) return new List<Vector3> { start };

        PriorityQueue<Vector3> frontier = new PriorityQueue<Vector3>();
        Dictionary<Vector3, float> costs = new Dictionary<Vector3, float>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();
        HashSet<Vector3> visited = new HashSet<Vector3>();

        foreach (Vector3 node in graph.Keys)
        {
            costs[node] = float.PositiveInfinity;
        }

        costs[start] = 0f;
        frontier.Enqueue(start, 0f);

        while (frontier.Count > 0)
        {
            Vector3 current = frontier.Dequeue();
            if (!visited.Add(current)) continue;
            visitedCount++;

            if (current == goal) break;

            foreach (Vector3 next in graph[current])
            {
                float newCost = costs[current] + Vector3.Distance(current, next);
                if (newCost >= costs[next]) continue;

                costs[next] = newCost;
                cameFrom[next] = current;
                frontier.Enqueue(next, newCost);
            }
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    private List<Vector3> FindBfsPath(
        Dictionary<Vector3, List<Vector3>> graph,
        Vector3 start,
        Vector3 goal,
        out int visitedCount)
    {
        visitedCount = 0;
        if (!CanSearch(graph, start, goal)) return new List<Vector3>();
        if (start == goal) return new List<Vector3> { start };

        Queue<Vector3> frontier = new Queue<Vector3>();
        HashSet<Vector3> visited = new HashSet<Vector3>();
        Dictionary<Vector3, Vector3> cameFrom = new Dictionary<Vector3, Vector3>();

        frontier.Enqueue(start);
        visited.Add(start);

        while (frontier.Count > 0)
        {
            Vector3 current = frontier.Dequeue();
            visitedCount++;

            if (current == goal) break;

            foreach (Vector3 next in graph[current])
            {
                if (!visited.Add(next)) continue;

                cameFrom[next] = current;
                frontier.Enqueue(next);
            }
        }

        return ReconstructPath(cameFrom, start, goal);
    }

    private void ResolveGraphBuilder()
    {
        if (graphBuilder == null)
        {
            graphBuilder = GraphBuilder.Instance != null ? GraphBuilder.Instance : FindAnyObjectByType<GraphBuilder>();
        }
    }

    private void EnsureGraphBuilt()
    {
        if (graphBuilder == null)
        {
            return;
        }

        if (graphBuilder.AdjacencyList == null || graphBuilder.AdjacencyList.Count == 0)
        {
            graphBuilder.BuildGraph();
        }
    }

    private static bool CanSearch(Dictionary<Vector3, List<Vector3>> graph, Vector3 start, Vector3 goal)
    {
        return graph != null && graph.ContainsKey(start) && graph.ContainsKey(goal);
    }

    private void LogResult(PathfindingResult result, bool suppressLogging)
    {
        if (suppressLogging)
        {
            return;
        }

        int pathCount = result.worldPath != null ? result.worldPath.Count : 0;
        Debug.Log(
            $"[MultiAlgorithmPathfinder] Selected algorithm: {result.algorithmName}\n" +
            $"Requested start: {FormatVector(result.requestedStartPosition)}\n" +
            $"Requested target: {FormatVector(result.requestedTargetPosition)}\n" +
            $"Resolved start node: {FormatVector(result.resolvedStartNode)}\n" +
            $"Resolved target node: {FormatVector(result.resolvedTargetNode)}\n" +
            $"Start blocked: {result.startPositionBlocked}\n" +
            $"Target blocked: {result.targetPositionBlocked}\n" +
            $"Used nearest walkable start node: {result.usedNearestStartNode}\n" +
            $"Used nearest walkable target node: {result.usedNearestTargetNode}\n" +
            $"Used reachable target proxy: {result.usedReachableTargetProxy}\n" +
            $"Start component size: {result.startComponentSize}\n" +
            $"Target component size: {result.targetComponentSize}\n" +
            $"Path found: {result.pathFound}\n" +
            $"Path node count: {pathCount}\n" +
            $"Visited node count: {result.visitedNodeCount}\n" +
            $"Calculation time: {result.calculationTimeMs:F2} ms",
            this);

        if (result.usedReachableTargetProxy)
        {
            Debug.LogWarning(
                "[MultiAlgorithmPathfinder] Player node was disconnected from the dog. Using the closest reachable target node instead.",
                this);
        }

        if (!string.IsNullOrEmpty(result.failureReason))
        {
            Debug.LogWarning($"[MultiAlgorithmPathfinder] {result.failureReason}", this);
        }
    }

    private static bool NeedsNearestNodeFallback(Vector3 worldPosition, Vector3 resolvedNode)
    {
        Vector3 delta = worldPosition - resolvedNode;
        delta.y = 0f;
        return delta.sqrMagnitude > 0.01f;
    }

    private static List<Vector3> ReconstructPath(Dictionary<Vector3, Vector3> cameFrom, Vector3 start, Vector3 goal)
    {
        if (!cameFrom.ContainsKey(goal)) return new List<Vector3>();

        List<Vector3> path = new List<Vector3> { goal };
        Vector3 current = goal;

        while (current != start)
        {
            current = cameFrom[current];
            path.Add(current);
        }

        path.Reverse();
        return path;
    }

    private static Vector3 GetLowestScoreNode(List<Vector3> openSet, Dictionary<Vector3, float> fScore)
    {
        Vector3 best = openSet[0];
        float bestScore = GetScore(fScore, best);

        for (int i = 1; i < openSet.Count; i++)
        {
            float score = GetScore(fScore, openSet[i]);
            if (score < bestScore)
            {
                best = openSet[i];
                bestScore = score;
            }
        }

        return best;
    }

    private static float GetScore(Dictionary<Vector3, float> scores, Vector3 node)
    {
        return scores.TryGetValue(node, out float score) ? score : float.PositiveInfinity;
    }

    private static float Heuristic(Vector3 a, Vector3 b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.z - b.z);
    }

    public static string GetAlgorithmName(PathfindingAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case PathfindingAlgorithm.Dijkstra:
                return "Dijkstra";
            case PathfindingAlgorithm.BFS:
                return "BFS";
            default:
                return "A*";
        }
    }

    private static string FormatPath(List<Vector3> path)
    {
        List<string> parts = new List<string>();
        for (int i = 0; i < path.Count; i++)
        {
            Vector3 p = path[i];
            parts.Add($"({Mathf.RoundToInt(p.x)},{Mathf.RoundToInt(p.y)},{Mathf.RoundToInt(p.z)})");
        }

        return string.Join(" -> ", parts);
    }

    private static string FormatVector(Vector3 value)
    {
        return $"({value.x:F2}, {value.y:F2}, {value.z:F2})";
    }
}
