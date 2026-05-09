using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Drives a demon dog along a Dijkstra-computed path toward a target.
///
/// GV polish:
///   - Catmull-Rom spline smooths sharp grid corners into natural curves.
///   - Speed ramp (acceleration / deceleration) eases the dog out of rest
///     and brings it to a stop near the goal.
///   - Defensive Animator hookup: if an Animator with a "speed" float
///     parameter is present, it gets driven from current movement speed
///     each frame. Activates automatically when the demon dog .fbx /
///     Animator Controller drops in; harmless on the capsule placeholder.
///   - OnDrawGizmos visualises the active smoothed path in a per-instance
///     colour so multiple spawned dogs are easy to tell apart in Scene view.
/// </summary>
public class DemonDogController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float acceleration = 6f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float arrivalThreshold = 0.2f;
    [SerializeField] private float stopThreshold = 0.5f;

    [Header("Path Smoothing")]
    [SerializeField] private int smoothingSubdivisions = 6;

    [Header("Pathfinding")]
    [SerializeField] private Transform target;
    [SerializeField] private float repathInterval = 2f;
    [SerializeField] private bool useMockPathOnStart = false;

    [Header("Animation (optional)")]
    [SerializeField] private Animator animator;
    [SerializeField] private string speedParameter = "speed";

    [Header("Debug")]
    [SerializeField] private bool drawPathGizmo = true;

    private readonly List<Vector3> path = new List<Vector3>();
    private int currentWaypointIndex;
    private float currentSpeed;
    private Color gizmoColor;
    private int speedParamHash;

    private void Awake()
    {
        // Cache an animator if one exists on this GameObject and the
        // inspector slot was left empty.
        if (animator == null) TryGetComponent(out animator);
        speedParamHash = Animator.StringToHash(speedParameter);

        // Stable per-instance colour for path gizmos so multiple spawned
        // dogs draw paths in distinct hues.
        gizmoColor = Color.HSVToRGB(Random.value, 0.85f, 1f);
    }

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
        DriveAnimator();
    }

    public void SetPath(List<Vector3> newPath)
    {
        path.Clear();
        if (newPath == null || newPath.Count == 0)
        {
            currentWaypointIndex = 0;
            return;
        }

        path.AddRange(SmoothPath(newPath, smoothingSubdivisions));
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
            // No path or finished: ease the dog to a stop.
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, acceleration * Time.deltaTime);
            return;
        }

        Vector3 waypoint = path[currentWaypointIndex];
        Vector3 flatDirection = waypoint - transform.position;
        flatDirection.y = 0f;

        // Smooth rotation toward the next waypoint.
        if (flatDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(flatDirection.normalized);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }

        // Speed ramp: accelerate up to moveSpeed normally; decelerate when
        // approaching the final waypoint of the current path.
        float targetSpeed = moveSpeed;
        int waypointsLeft = path.Count - currentWaypointIndex;
        if (waypointsLeft <= 1)
        {
            float distToGoal = Vector3.Distance(transform.position, waypoint);
            if (distToGoal < stopThreshold)
            {
                targetSpeed = Mathf.Lerp(0f, moveSpeed, distToGoal / stopThreshold);
            }
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, acceleration * Time.deltaTime);

        transform.position = Vector3.MoveTowards(
            transform.position,
            waypoint,
            currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, waypoint) < arrivalThreshold)
        {
            currentWaypointIndex++;
        }
    }

    private void DriveAnimator()
    {
        if (animator == null) return;
        animator.SetFloat(speedParamHash, currentSpeed);
    }

    /// <summary>
    /// Returns a Catmull-Rom subdivided copy of the input polyline so corners
    /// are interpolated as smooth arcs instead of sharp 90° pivots.
    /// </summary>
    private static List<Vector3> SmoothPath(List<Vector3> raw, int subdivisionsPerSegment)
    {
        if (raw == null || raw.Count < 3 || subdivisionsPerSegment <= 1)
        {
            return new List<Vector3>(raw ?? new List<Vector3>());
        }

        var result = new List<Vector3>();
        for (int i = 0; i < raw.Count - 1; i++)
        {
            Vector3 p0 = raw[Mathf.Max(i - 1, 0)];
            Vector3 p1 = raw[i];
            Vector3 p2 = raw[i + 1];
            Vector3 p3 = raw[Mathf.Min(i + 2, raw.Count - 1)];

            for (int s = 0; s < subdivisionsPerSegment; s++)
            {
                float t = s / (float)subdivisionsPerSegment;
                result.Add(CatmullRom(p0, p1, p2, p3, t));
            }
        }
        result.Add(raw[raw.Count - 1]);
        return result;
    }

    private static Vector3 CatmullRom(Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3, float t)
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * (
            (2f * p1) +
            (-p0 + p2) * t +
            (2f * p0 - 5f * p1 + 4f * p2 - p3) * t2 +
            (-p0 + 3f * p1 - 3f * p2 + p3) * t3);
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

    private void OnDrawGizmos()
    {
        if (!drawPathGizmo || path == null || path.Count < 2) return;

        Gizmos.color = gizmoColor.a == 0f ? Color.red : gizmoColor;
        for (int i = currentWaypointIndex; i < path.Count - 1; i++)
        {
            Gizmos.DrawLine(path[i], path[i + 1]);
        }

        // Highlight the immediate next waypoint.
        if (currentWaypointIndex < path.Count)
        {
            Gizmos.DrawWireSphere(path[currentWaypointIndex], 0.15f);
        }
    }
}
