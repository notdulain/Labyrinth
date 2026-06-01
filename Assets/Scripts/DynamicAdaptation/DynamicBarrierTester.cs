using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Standalone runtime tester that simulates a future dynamic barrier and asks
/// the graph adaptation system to sever the nearest affected edge.
/// </summary>
public class DynamicBarrierTester : MonoBehaviour
{
    [Header("Barrier Test")]
    public GameObject barrierPrefab;
    public Transform testOrigin;
    public float nodeOffset = 1.5f;
    public Vector3 fallbackBarrierScale = new Vector3(3.5f, 4f, 0.6f);

    [Header("Scene Barrier Objects")]
    public GameObject barrierOneObject;
    public GameObject barrierTwoObject;
    public bool useSceneBarrierTransforms = true;

    [Header("Barrier Controls")]
    public KeyCode barrierOneKey = KeyCode.B;
    public KeyCode barrierTwoKey = KeyCode.U;

    [Header("Barrier Locations")]
    public Vector3 barrierOneLocalOffset = new Vector3(0f, 0f, 2f);
    public Vector3 barrierTwoLocalOffset = new Vector3(4f, 0f, 2f);
    public Vector3 barrierOneLocalEulerAngles = Vector3.zero;
    public Vector3 barrierTwoLocalEulerAngles = Vector3.zero;

    private const string BarrierOneName = "Dynamic Test Barrier 1";
    private const string BarrierTwoName = "Dynamic Test Barrier 2";

    private GameObject activeBarrierOne;
    private GameObject activeBarrierTwo;
    private bool barrierOneHasSeveredEdge;
    private bool barrierTwoHasSeveredEdge;
    private Vector3 barrierOneNodeA;
    private Vector3 barrierOneNodeB;
    private Vector3 barrierTwoNodeA;
    private Vector3 barrierTwoNodeB;

    /// <summary>
    /// Prepares optional scene barrier objects so they are visible in the
    /// Hierarchy but hidden from gameplay until toggled.
    /// </summary>
    private void Awake()
    {
        PrepareSceneBarrier(barrierOneObject, BarrierOneName);
        PrepareSceneBarrier(barrierTwoObject, BarrierTwoName);
    }

    /// <summary>
    /// Watches for the test key while in Play Mode.
    /// </summary>
    private void Update()
    {
        if (Input.GetKeyDown(barrierOneKey))
            ToggleBarrierOne();

        if (Input.GetKeyDown(barrierTwoKey))
            ToggleBarrierTwo();
    }

    /// <summary>
    /// Toggles barrier one for backwards-compatible test calls.
    /// </summary>
    public void SimulateBarrier()
    {
        ToggleBarrierOne();
    }

    /// <summary>
    /// Toggles the first runtime barrier on or off.
    /// </summary>
    public void ToggleBarrierOne()
    {
        if (activeBarrierOne != null)
        {
            RemoveBarrier(1, ref activeBarrierOne, ref barrierOneHasSeveredEdge, barrierOneNodeA, barrierOneNodeB);
            return;
        }

        SpawnAndSeverBarrier(1, BarrierOneName, barrierOneObject, barrierOneLocalOffset, barrierOneLocalEulerAngles, ref activeBarrierOne, ref barrierOneHasSeveredEdge, out barrierOneNodeA, out barrierOneNodeB);
    }

    /// <summary>
    /// Toggles the second runtime barrier on or off.
    /// </summary>
    public void ToggleBarrierTwo()
    {
        if (activeBarrierTwo != null)
        {
            RemoveBarrier(2, ref activeBarrierTwo, ref barrierTwoHasSeveredEdge, barrierTwoNodeA, barrierTwoNodeB);
            return;
        }

        SpawnAndSeverBarrier(2, BarrierTwoName, barrierTwoObject, barrierTwoLocalOffset, barrierTwoLocalEulerAngles, ref activeBarrierTwo, ref barrierTwoHasSeveredEdge, out barrierTwoNodeA, out barrierTwoNodeB);
    }

    /// <summary>
    /// Creates a visible test barrier and requests an edge sever around it.
    /// </summary>
    /// <param name="barrierIndex">The barrier slot number for logging.</param>
    /// <param name="barrierName">The name to assign to the spawned barrier.</param>
    /// <param name="sceneBarrier">The optional pre-placed scene barrier object.</param>
    /// <param name="localOffset">The local offset from the test origin.</param>
    /// <param name="localEulerAngles">The local rotation from the test origin.</param>
    /// <param name="activeBarrier">The active barrier reference to update.</param>
    /// <param name="hasSeveredEdge">Whether this barrier severed an edge.</param>
    /// <param name="storedNodeA">The first severed node.</param>
    /// <param name="storedNodeB">The second severed node.</param>
    private void SpawnAndSeverBarrier(
        int barrierIndex,
        string barrierName,
        GameObject sceneBarrier,
        Vector3 localOffset,
        Vector3 localEulerAngles,
        ref GameObject activeBarrier,
        ref bool hasSeveredEdge,
        out Vector3 storedNodeA,
        out Vector3 storedNodeB)
    {
        storedNodeA = Vector3.zero;
        storedNodeB = Vector3.zero;
        hasSeveredEdge = false;

        Transform origin = ResolveOrigin();
        Vector3 barrierPosition;
        Quaternion barrierRotation;

        if (sceneBarrier != null && useSceneBarrierTransforms)
        {
            barrierPosition = sceneBarrier.transform.position;
            barrierRotation = sceneBarrier.transform.rotation;
        }
        else
        {
            barrierRotation = origin.rotation * Quaternion.Euler(localEulerAngles);
            barrierPosition = origin.TransformPoint(localOffset);
        }

        Vector3 forward = barrierRotation * Vector3.forward;

        activeBarrier = SpawnBarrier(barrierName, sceneBarrier, barrierPosition, barrierRotation);
        Debug.Log($"[DynamicBarrierTester] Spawned barrier {barrierIndex} at {barrierPosition}. Object: {activeBarrier.name}.");

        if (TryFindAffectedEdge(barrierPosition, forward, out Vector3 nodeA, out Vector3 nodeB))
        {
            storedNodeA = nodeA;
            storedNodeB = nodeB;
            hasSeveredEdge = true;
            Debug.Log($"[DynamicBarrierTester] Barrier {barrierIndex} requesting edge sever between nearest opposite-side nodes {nodeA} and {nodeB}.");
            WallEventInterceptor.TriggerEdgeSever(nodeA, nodeB);
        }
        else
        {
            Debug.LogWarning($"[DynamicBarrierTester] Barrier {barrierIndex} could not find a valid graph edge to sever. Recalculation will still be requested for listener testing.");
            WallEventInterceptor.TriggerRecalculation();
        }
    }

    /// <summary>
    /// Removes a runtime barrier and restores the edge it severed when possible.
    /// </summary>
    /// <param name="barrierIndex">The barrier slot number for logging.</param>
    /// <param name="activeBarrier">The active barrier reference to clear.</param>
    /// <param name="hasSeveredEdge">Whether this barrier severed an edge.</param>
    /// <param name="nodeA">The first severed node.</param>
    /// <param name="nodeB">The second severed node.</param>
    private void RemoveBarrier(int barrierIndex, ref GameObject activeBarrier, ref bool hasSeveredEdge, Vector3 nodeA, Vector3 nodeB)
    {
        if (activeBarrier != null)
        {
            Debug.Log($"[DynamicBarrierTester] Removing barrier {barrierIndex}: {activeBarrier.name}.");
            if (IsReusableSceneBarrier(activeBarrier))
                activeBarrier.SetActive(false);
            else
                Destroy(activeBarrier);

            activeBarrier = null;
        }

        if (hasSeveredEdge)
        {
            if (IsEdgeHeldByOtherActiveBarrier(barrierIndex, nodeA, nodeB))
            {
                Debug.Log($"[DynamicBarrierTester] Barrier {barrierIndex} removed, but its edge remains severed by another active barrier.");
            }
            else
            {
                RestoreEdge(nodeA, nodeB);
            }
        }

        hasSeveredEdge = false;
        WallEventInterceptor.TriggerRecalculation();
    }

    /// <summary>
    /// Names and hides a pre-placed scene barrier until it is toggled on.
    /// </summary>
    /// <param name="sceneBarrier">The scene barrier to prepare.</param>
    /// <param name="barrierName">The stable barrier name.</param>
    private static void PrepareSceneBarrier(GameObject sceneBarrier, string barrierName)
    {
        if (sceneBarrier == null)
            return;

        sceneBarrier.name = barrierName;
        sceneBarrier.SetActive(false);
    }

    /// <summary>
    /// Returns the configured origin, or this tester's transform as a safe fallback.
    /// </summary>
    /// <returns>The transform used as the barrier origin.</returns>
    private Transform ResolveOrigin()
    {
        if (testOrigin != null)
            return testOrigin;

        Debug.Log("[DynamicBarrierTester] No test origin assigned; using this GameObject transform.");
        return transform;
    }

    /// <summary>
    /// Spawns the configured prefab or a primitive cube fallback.
    /// </summary>
    /// <param name="barrierName">The name to assign to the spawned barrier.</param>
    /// <param name="sceneBarrier">The optional pre-placed scene barrier object.</param>
    /// <param name="position">The world position for the barrier.</param>
    /// <param name="rotation">The world rotation for the barrier.</param>
    /// <returns>The spawned barrier GameObject.</returns>
    private GameObject SpawnBarrier(string barrierName, GameObject sceneBarrier, Vector3 position, Quaternion rotation)
    {
        if (sceneBarrier != null)
        {
            sceneBarrier.name = barrierName;
            sceneBarrier.transform.SetPositionAndRotation(position, rotation);
            sceneBarrier.SetActive(true);
            Debug.Log("[DynamicBarrierTester] Enabled pre-placed scene barrier object.");
            return sceneBarrier;
        }

        GameObject barrier;
        if (barrierPrefab != null)
        {
            barrier = Instantiate(barrierPrefab, position, rotation);
            barrier.name = barrierName;
            return barrier;
        }

        barrier = GameObject.CreatePrimitive(PrimitiveType.Cube);
        barrier.name = barrierName;
        barrier.transform.SetPositionAndRotation(position, rotation);
        barrier.transform.localScale = fallbackBarrierScale;
        barrier.transform.position = new Vector3(position.x, fallbackBarrierScale.y * 0.5f, position.z);
        Debug.Log("[DynamicBarrierTester] No barrier prefab assigned; spawned primitive cube fallback.");
        return barrier;
    }

    /// <summary>
    /// Checks whether a barrier is one of the reusable scene objects.
    /// </summary>
    /// <param name="barrier">The barrier object to check.</param>
    /// <returns>True when the barrier should be hidden instead of destroyed.</returns>
    private bool IsReusableSceneBarrier(GameObject barrier)
    {
        return barrier != null && (barrier == barrierOneObject || barrier == barrierTwoObject);
    }

    /// <summary>
    /// Restores a severed graph edge in both directions when the graph still has
    /// both endpoint nodes.
    /// </summary>
    /// <param name="nodeA">The first graph node.</param>
    /// <param name="nodeB">The second graph node.</param>
    private static void RestoreEdge(Vector3 nodeA, Vector3 nodeB)
    {
        GraphBuilder graphBuilder = GraphBuilder.Instance;
        if (graphBuilder == null || graphBuilder.AdjacencyList == null)
        {
            Debug.LogWarning("[DynamicBarrierTester] Cannot restore edge because GraphBuilder.Instance or its adjacency list is missing.");
            return;
        }

        Dictionary<Vector3, List<Vector3>> adjacencyList = graphBuilder.AdjacencyList;
        bool restoredForward = RestoreConnection(adjacencyList, nodeA, nodeB);
        bool restoredBackward = RestoreConnection(adjacencyList, nodeB, nodeA);

        Debug.Log($"[DynamicBarrierTester] Restored graph edge between {nodeA} and {nodeB}. Forward restored: {restoredForward}, backward restored: {restoredBackward}.");
    }

    /// <summary>
    /// Restores one directed graph connection if the source and target nodes
    /// still exist and the connection is not already present.
    /// </summary>
    /// <param name="adjacencyList">The graph adjacency list to modify.</param>
    /// <param name="from">The source node.</param>
    /// <param name="to">The target node.</param>
    /// <returns>True when a connection was added.</returns>
    private static bool RestoreConnection(Dictionary<Vector3, List<Vector3>> adjacencyList, Vector3 from, Vector3 to)
    {
        if (!adjacencyList.ContainsKey(to))
            return false;

        if (!adjacencyList.TryGetValue(from, out List<Vector3> neighbours) || neighbours == null)
            return false;

        if (neighbours.Contains(to))
            return false;

        neighbours.Add(to);
        return true;
    }

    /// <summary>
    /// Checks whether the other active barrier still owns the same severed edge.
    /// </summary>
    /// <param name="barrierIndex">The barrier currently being removed.</param>
    /// <param name="nodeA">The first graph node.</param>
    /// <param name="nodeB">The second graph node.</param>
    /// <returns>True when the other active barrier uses the same edge.</returns>
    private bool IsEdgeHeldByOtherActiveBarrier(int barrierIndex, Vector3 nodeA, Vector3 nodeB)
    {
        if (barrierIndex != 1 && activeBarrierOne != null && barrierOneHasSeveredEdge)
            return IsSameUndirectedEdge(nodeA, nodeB, barrierOneNodeA, barrierOneNodeB);

        if (barrierIndex != 2 && activeBarrierTwo != null && barrierTwoHasSeveredEdge)
            return IsSameUndirectedEdge(nodeA, nodeB, barrierTwoNodeA, barrierTwoNodeB);

        return false;
    }

    /// <summary>
    /// Compares two graph edges without caring about node order.
    /// </summary>
    /// <param name="a1">The first node of the first edge.</param>
    /// <param name="b1">The second node of the first edge.</param>
    /// <param name="a2">The first node of the second edge.</param>
    /// <param name="b2">The second node of the second edge.</param>
    /// <returns>True when the edges have the same endpoints.</returns>
    private static bool IsSameUndirectedEdge(Vector3 a1, Vector3 b1, Vector3 a2, Vector3 b2)
    {
        return (a1 == a2 && b1 == b2) || (a1 == b2 && b1 == a2);
    }

    /// <summary>
    /// Finds the graph edge nearest to the barrier that crosses from one side of
    /// the barrier plane to the other.
    /// </summary>
    /// <param name="barrierPosition">The barrier world position.</param>
    /// <param name="forward">The forward axis that defines opposite sides.</param>
    /// <param name="nodeA">The first affected graph node.</param>
    /// <param name="nodeB">The second affected graph node.</param>
    /// <returns>True when a connected edge was found.</returns>
    private bool TryFindAffectedEdge(Vector3 barrierPosition, Vector3 forward, out Vector3 nodeA, out Vector3 nodeB)
    {
        nodeA = Vector3.zero;
        nodeB = Vector3.zero;

        GraphBuilder graphBuilder = GraphBuilder.Instance;
        if (graphBuilder == null)
        {
            Debug.LogWarning("[DynamicBarrierTester] GraphBuilder.Instance is missing; cannot locate graph nodes.");
            return false;
        }

        Dictionary<Vector3, List<Vector3>> adjacencyList = graphBuilder.AdjacencyList;
        if (adjacencyList == null || adjacencyList.Count == 0)
        {
            Debug.LogWarning("[DynamicBarrierTester] Graph adjacency list is empty; cannot locate graph nodes.");
            return false;
        }

        if (TryFindNearestCrossingEdge(adjacencyList, barrierPosition, forward, out nodeA, out nodeB))
            return true;

        Vector3 sampledA = graphBuilder.GetNearestNode(barrierPosition - forward * Mathf.Max(0.1f, nodeOffset));
        Vector3 sampledB = graphBuilder.GetNearestNode(barrierPosition + forward * Mathf.Max(0.1f, nodeOffset));

        if (sampledA == sampledB)
        {
            Debug.LogWarning("[DynamicBarrierTester] Opposite-side samples resolved to the same graph node.");
            return false;
        }

        nodeA = sampledA;
        nodeB = sampledB;
        Debug.LogWarning("[DynamicBarrierTester] No crossing graph edge found near barrier; using nearest sampled nodes as fallback.");
        return true;
    }

    /// <summary>
    /// Finds the closest existing graph connection that crosses the barrier plane.
    /// </summary>
    /// <param name="adjacencyList">The graph adjacency list.</param>
    /// <param name="barrierPosition">The barrier world position.</param>
    /// <param name="forward">The forward axis that defines opposite sides.</param>
    /// <param name="nodeA">The first edge node.</param>
    /// <param name="nodeB">The second edge node.</param>
    /// <returns>True when a crossing edge was found.</returns>
    private static bool TryFindNearestCrossingEdge(Dictionary<Vector3, List<Vector3>> adjacencyList, Vector3 barrierPosition, Vector3 forward, out Vector3 nodeA, out Vector3 nodeB)
    {
        nodeA = Vector3.zero;
        nodeB = Vector3.zero;
        float bestScore = float.PositiveInfinity;

        foreach (KeyValuePair<Vector3, List<Vector3>> entry in adjacencyList)
        {
            if (entry.Value == null)
                continue;

            foreach (Vector3 neighbour in entry.Value)
            {
                Vector3 edgeMidpoint = (entry.Key + neighbour) * 0.5f;
                float sideA = Vector3.Dot(entry.Key - barrierPosition, forward);
                float sideB = Vector3.Dot(neighbour - barrierPosition, forward);

                if (sideA * sideB >= 0f)
                    continue;

                float score = (edgeMidpoint - barrierPosition).sqrMagnitude;
                if (score < bestScore)
                {
                    bestScore = score;
                    nodeA = entry.Key;
                    nodeB = neighbour;
                }
            }
        }

        return bestScore < float.PositiveInfinity;
    }
}
