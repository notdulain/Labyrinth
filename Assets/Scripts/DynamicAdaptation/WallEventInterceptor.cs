using UnityEngine;
using System;

/// <summary>
/// Broadcasts dynamic labyrinth topology events without depending on any
/// specific player, enemy, or maze implementation.
/// </summary>
public class WallEventInterceptor : MonoBehaviour
{
    /// <summary>
    /// Raised when an edge between two graph node positions should be removed.
    /// </summary>
    public static event Action<Vector3, Vector3> OnEdgeSevered;

    /// <summary>
    /// Raised when pathfinding agents should recalculate their current paths.
    /// </summary>
    public static event Action OnRecalculationRequired;

    /// <summary>
    /// Requests that the graph edge between two nodes be severed, then asks
    /// listeners to recalculate paths against the updated graph.
    /// </summary>
    /// <param name="nodeA">The first node world position.</param>
    /// <param name="nodeB">The second node world position.</param>
    public static void TriggerEdgeSever(Vector3 nodeA, Vector3 nodeB)
    {
        Debug.Log($"[WallEventInterceptor] Edge sever requested between {nodeA} and {nodeB}.");
        OnEdgeSevered?.Invoke(nodeA, nodeB);
        TriggerRecalculation();
    }

    /// <summary>
    /// Requests that pathfinding listeners refresh their paths.
    /// </summary>
    public static void TriggerRecalculation()
    {
        Debug.Log("[WallEventInterceptor] Recalculation requested.");
        OnRecalculationRequired?.Invoke();
    }
}
