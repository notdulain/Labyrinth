using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Removes graph edges when dynamic obstacles block labyrinth paths.
/// </summary>
public class EdgeSeverer : MonoBehaviour
{
    private const float NodeMatchTolerance = 0.25f;

    /// <summary>
    /// Subscribes to runtime wall events while this component is active.
    /// </summary>
    private void OnEnable()
    {
        WallEventInterceptor.OnEdgeSevered += HandleEdgeSevered;
    }

    /// <summary>
    /// Unsubscribes from runtime wall events when this component is disabled.
    /// </summary>
    private void OnDisable()
    {
        WallEventInterceptor.OnEdgeSevered -= HandleEdgeSevered;
    }

    /// <summary>
    /// Removes the requested connection from the active graph in both directions.
    /// </summary>
    /// <param name="nodeA">The first node world position.</param>
    /// <param name="nodeB">The second node world position.</param>
    private void HandleEdgeSevered(Vector3 nodeA, Vector3 nodeB)
    {
        GraphBuilder graphBuilder = GraphBuilder.Instance;
        if (graphBuilder == null)
        {
            Debug.LogWarning("[EdgeSeverer] Cannot sever edge because GraphBuilder.Instance is missing.");
            return;
        }

        Dictionary<Vector3, List<Vector3>> adjacencyList = graphBuilder.AdjacencyList;
        if (adjacencyList == null || adjacencyList.Count == 0)
        {
            Debug.LogWarning("[EdgeSeverer] Cannot sever edge because the graph adjacency list is empty.");
            return;
        }

        if (!TryResolveNode(adjacencyList, nodeA, out Vector3 resolvedA) ||
            !TryResolveNode(adjacencyList, nodeB, out Vector3 resolvedB))
        {
            Debug.LogWarning($"[EdgeSeverer] Failed to resolve requested edge nodes: {nodeA} -> {nodeB}.");
            return;
        }

        bool removedForward = RemoveConnection(adjacencyList, resolvedA, resolvedB);
        bool removedBackward = RemoveConnection(adjacencyList, resolvedB, resolvedA);

        if (removedForward && removedBackward)
        {
            Debug.Log($"[EdgeSeverer] Severed graph edge between {resolvedA} and {resolvedB}.");
        }
        else if (removedForward || removedBackward)
        {
            Debug.LogWarning($"[EdgeSeverer] Partially severed graph edge between {resolvedA} and {resolvedB}. Forward removed: {removedForward}, backward removed: {removedBackward}.");
        }
        else
        {
            Debug.LogWarning($"[EdgeSeverer] No existing graph edge found between {resolvedA} and {resolvedB}.");
        }
    }

    /// <summary>
    /// Removes one directed connection from the adjacency list.
    /// </summary>
    /// <param name="adjacencyList">The graph adjacency list to modify.</param>
    /// <param name="from">The source node.</param>
    /// <param name="to">The target node.</param>
    /// <returns>True when a connection was removed.</returns>
    private static bool RemoveConnection(Dictionary<Vector3, List<Vector3>> adjacencyList, Vector3 from, Vector3 to)
    {
        if (!adjacencyList.TryGetValue(from, out List<Vector3> neighbours) || neighbours == null)
            return false;

        return neighbours.Remove(to);
    }

    /// <summary>
    /// Resolves an incoming node position to a graph key, allowing small floating
    /// point differences from future systems.
    /// </summary>
    /// <param name="adjacencyList">The graph adjacency list to search.</param>
    /// <param name="requestedNode">The requested node position.</param>
    /// <param name="resolvedNode">The matching graph key.</param>
    /// <returns>True when a matching graph node was found.</returns>
    private static bool TryResolveNode(Dictionary<Vector3, List<Vector3>> adjacencyList, Vector3 requestedNode, out Vector3 resolvedNode)
    {
        if (adjacencyList.ContainsKey(requestedNode))
        {
            resolvedNode = requestedNode;
            return true;
        }

        float bestDistance = NodeMatchTolerance * NodeMatchTolerance;
        resolvedNode = requestedNode;

        foreach (Vector3 node in adjacencyList.Keys)
        {
            float distance = (node - requestedNode).sqrMagnitude;
            if (distance <= bestDistance)
            {
                bestDistance = distance;
                resolvedNode = node;
            }
        }

        return bestDistance < NodeMatchTolerance * NodeMatchTolerance;
    }
}
