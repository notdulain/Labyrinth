using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A simple sphere agent that uses A* to chase the player.
/// </summary>
public class IntelligentAgent : MonoBehaviour
{
    public Transform player;
    public AStarSearch pathfinder;
    public float moveSpeed = 3f;
    public float stoppingDistance = 1.25f;
    public float pathUpdateInterval = 0.4f;

    [Header("Movement")]
    public float waypointReachDistance = 0.15f;
    public float rotationSpeed = 8f;

    private readonly List<Vector3> currentPath = new List<Vector3>();

    private float pathUpdateTimer;
    private int currentPathIndex;

    private void Awake()
    {
        if (pathfinder == null)
        {
            ResolvePathfinder();
        }
    }

    private void OnEnable()
    {
        pathUpdateTimer = pathUpdateInterval;
        currentPath.Clear();
        currentPathIndex = 0;
    }

    private void Update()
    {
        if (player == null)
        {
            ResolvePlayer();
        }

        if (pathfinder == null)
        {
            ResolvePathfinder();
        }

        if (player == null || pathfinder == null)
        {
            return;
        }

        pathUpdateTimer += Time.deltaTime;

        if (pathUpdateTimer >= pathUpdateInterval)
        {
            RecalculatePath();
        }

        if (Vector3.Distance(transform.position, player.position) <= stoppingDistance)
        {
            DrawCurrentPath();
            return;
        }

        FollowPath();
        DrawCurrentPath();
    }

    private void RecalculatePath()
    {
        pathUpdateTimer = 0f;
        currentPath.Clear();

        if (GraphBuilder.Instance != null && GraphBuilder.Instance.AdjacencyList != null)
        {
            Vector3 startNode = GraphBuilder.Instance.GetNearestNodeReachableTo(transform.position, player.position);
            Vector3 goalNode = GraphBuilder.Instance.GetNearestNode(player.position);
            currentPath.AddRange(pathfinder.FindPath(
                GraphBuilder.Instance.AdjacencyList,
                startNode,
                goalNode));
        }
        else
        {
            List<Node> nodePath = pathfinder.FindPath(transform.position, player.position);
            for (int i = 0; i < nodePath.Count; i++)
            {
                currentPath.Add(nodePath[i].worldPosition);
            }
        }

        currentPathIndex = GetBestPathIndexForCurrentPosition();
    }

    private void FollowPath()
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            return;
        }

        Vector3 targetPosition = currentPath[currentPathIndex];
        targetPosition.y = transform.position.y;

        Vector3 moveDirection = targetPosition - transform.position;
        moveDirection.y = 0f;

        if (moveDirection.sqrMagnitude <= waypointReachDistance * waypointReachDistance)
        {
            currentPathIndex++;
            return;
        }

        Vector3 normalizedDirection = moveDirection.normalized;
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPosition,
            moveSpeed * Time.deltaTime);

        if (normalizedDirection.sqrMagnitude > 0.0001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(normalizedDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }

    private void DrawCurrentPath()
    {
        if (currentPath.Count < 2)
        {
            return;
        }

        for (int i = 1; i < currentPath.Count; i++)
        {
            Vector3 start = currentPath[i - 1] + Vector3.up * 0.2f;
            Vector3 end = currentPath[i] + Vector3.up * 0.2f;
            Debug.DrawLine(start, end, Color.red);
        }
    }

    private Vector3 GetFlatPosition(Vector3 position)
    {
        position.y = 0f;
        return position;
    }

    private int GetBestPathIndexForCurrentPosition()
    {
        if (currentPath.Count == 0)
        {
            return 0;
        }

        Vector3 flatAgentPosition = GetFlatPosition(transform.position);
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < currentPath.Count; i++)
        {
            float distance = Vector3.Distance(flatAgentPosition, GetFlatPosition(currentPath[i]));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        while (closestIndex < currentPath.Count - 1)
        {
            float distanceToCurrentNode = Vector3.Distance(
                flatAgentPosition,
                GetFlatPosition(currentPath[closestIndex]));

            if (distanceToCurrentNode > waypointReachDistance * 1.5f)
            {
                break;
            }

            closestIndex++;
        }

        return closestIndex;
    }

    public void SetTarget(Transform newPlayer)
    {
        player = newPlayer;
    }

    public void SetPathfinder(AStarSearch newPathfinder)
    {
        pathfinder = newPathfinder;
    }

    private void ResolvePlayer()
    {
        GameObject hero = null;

        try
        {
            hero = GameObject.FindGameObjectWithTag("Player");
        }
        catch (UnityException)
        {
            // Tag may not exist in older scenes.
        }

        if (hero == null)
        {
            hero = GameObject.Find("Player");
        }

        if (hero != null)
        {
            player = hero.transform;
        }
    }

    private void ResolvePathfinder()
    {
        pathfinder = FindFirstObjectByType<AStarSearch>();
        if (pathfinder == null)
        {
            pathfinder = gameObject.AddComponent<AStarSearch>();
        }
    }
}
