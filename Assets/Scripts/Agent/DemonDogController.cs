using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Drives a demon dog along a Dijkstra-computed path toward a target.
/// Falls back to a hardcoded mock path when graph or target is missing.
/// </summary>
public class DemonDogController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float arrivalThreshold = 0.2f;

    [Header("Pathfinding")]
    [SerializeField] private Transform target;
    [SerializeField] private float repathInterval = 2f;
    [SerializeField] private bool useMockPathOnStart = false;

    private readonly List<Vector3> path = new List<Vector3>();
    private int currentWaypointIndex;

    private void Start()
    {
        if (target == null)
        {
            GameObject hero = GameObject.FindGameObjectWithTag("Player");
            if (hero != null) target = hero.transform;
        }

        if (useMockPathOnStart)
        {
            SetPath(CreateMockPathFromCurrentPosition());
        }

        InvokeRepeating(nameof(Repath), 0f, repathInterval);
    }

    private void Update()
    {
        FollowPath();
    }

    public void SetPath(List<Vector3> newPath)
    {
        path.Clear();
        if (newPath == null || newPath.Count == 0)
        {
            currentWaypointIndex = 0;
            return;
        }

        path.AddRange(newPath);
        currentWaypointIndex = 0;
    }

    private void Repath()
    {
        if (target == null) return;
        if (GraphBuilder.Instance == null || GraphBuilder.Instance.AdjacencyList == null) return;
        if (DijkstraSearch.Instance == null) return;

        Vector3 startNode = GraphBuilder.Instance.GetNearestNode(transform.position);
        Vector3 goalNode = GraphBuilder.Instance.GetNearestNode(target.position);

        List<Vector3> newPath = DijkstraSearch.Instance.FindPath(
            GraphBuilder.Instance.AdjacencyList,
            startNode,
            goalNode);

        if (newPath != null && newPath.Count > 0)
        {
            SetPath(newPath);
        }
    }

    private void FollowPath()
    {
        if (path.Count == 0 || currentWaypointIndex >= path.Count)
        {
            return;
        }

        Vector3 target = path[currentWaypointIndex];
        Vector3 flatDirection = target - transform.position;
        flatDirection.y = 0f;

        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            moveSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < arrivalThreshold)
        {
            currentWaypointIndex++;
        }
    }

    private List<Vector3> CreateMockPathFromCurrentPosition()
    {
        Vector3 start = transform.position;
        return new List<Vector3>
        {
            start,
            start + new Vector3(2f, 0f, 0f),
            start + new Vector3(2f, 0f, 2f),
            start + new Vector3(0f, 0f, 2f),
            start
        };
    }
}
