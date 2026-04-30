using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Moves a demon dog along a waypoint path.
/// </summary>
public class DemonDogController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float rotationSpeed = 8f;
    [SerializeField] private float arrivalThreshold = 0.1f;
    [SerializeField] private bool useMockPathOnStart = true;

    private readonly List<Vector3> path = new List<Vector3>();
    private int currentWaypointIndex;

    private void Start()
    {
        if (useMockPathOnStart)
        {
            SetPath(CreateMockPathFromCurrentPosition());
        }
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
