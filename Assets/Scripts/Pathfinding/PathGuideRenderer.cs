using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Rebuilds and renders a subtle dotted guide trail from the player to the temple.
/// </summary>
public class PathGuideRenderer : MonoBehaviour
{
    [Header("References")]
    public AStarPathfinder pathfinder;
    public Transform player;
    public Transform temple;
    public GuideDot guideDotPrefab;

    [Header("Path Guide")]
    public float dotSpacing = 0.6f;
    public float dotHeightOffset = 0.08f;
    public int maxDots = 80;
    public float updateInterval = 0.4f;
    public float playerMoveThreshold = 0.45f;
    public float targetMoveThreshold = 0.2f;
    public LayerMask guideGroundLayer;

    [Header("Dot Animation")]
    public float dotScale = 0.1f;
    public float animationSpeed = 2f;
    public float pulseAmount = 0.08f;
    public float bobAmount = 0.03f;

    [Header("Debug")]
    public bool logWarnings = true;
    public bool drawDebugPath = false;

    private readonly List<GuideDot> dotPool = new List<GuideDot>();
    private readonly List<Vector3> currentPathPoints = new List<Vector3>();
    private readonly List<Vector3> sampledDotPositions = new List<Vector3>();

    private float updateTimer;
    private Vector3 lastPlayerPosition;
    private Vector3 lastTemplePosition;
    private bool hasLastPositions;
    private bool missingPathWarningShown;

    private void Awake()
    {
        if (pathfinder == null)
        {
            pathfinder = FindFirstObjectByType<AStarPathfinder>();
        }

        if (pathfinder != null)
        {
            if (player == null)
            {
                player = pathfinder.player;
            }

            if (temple == null)
            {
                temple = pathfinder.temple;
            }
        }
    }

    private void OnEnable()
    {
        updateTimer = updateInterval;
        hasLastPositions = false;
    }

    private void OnDisable()
    {
        HideAllDots();
    }

    private void Update()
    {
        if (!CanRenderGuide())
        {
            HideAllDots();
            return;
        }

        updateTimer += Time.deltaTime;

        bool shouldUpdateBecauseNoPath = currentPathPoints.Count == 0;
        bool shouldUpdateBecauseMoved = HasMovedEnough();

        if (updateTimer >= updateInterval && (shouldUpdateBecauseNoPath || shouldUpdateBecauseMoved))
        {
            RecalculateGuidePath();
        }
    }

    private bool CanRenderGuide()
    {
        return pathfinder != null && player != null && temple != null && guideDotPrefab != null;
    }

    private bool HasMovedEnough()
    {
        if (!hasLastPositions)
        {
            return true;
        }

        float playerThresholdSqr = playerMoveThreshold * playerMoveThreshold;
        float targetThresholdSqr = targetMoveThreshold * targetMoveThreshold;

        bool playerMovedEnough = (player.position - lastPlayerPosition).sqrMagnitude >= playerThresholdSqr;
        bool templeMovedEnough = (temple.position - lastTemplePosition).sqrMagnitude >= targetThresholdSqr;

        return playerMovedEnough || templeMovedEnough;
    }

    private void RecalculateGuidePath()
    {
        updateTimer = 0f;
        currentPathPoints.Clear();
        currentPathPoints.AddRange(pathfinder.FindWorldPath(player.position, temple.position));

        lastPlayerPosition = player.position;
        lastTemplePosition = temple.position;
        hasLastPositions = true;

        if (currentPathPoints.Count == 0)
        {
            HideAllDots();

            if (logWarnings && !missingPathWarningShown)
            {
                Debug.LogWarning("PathGuideRenderer could not find a walkable path from the player to the temple.", this);
                missingPathWarningShown = true;
            }

            return;
        }

        missingPathWarningShown = false;
        BuildDotPositions(currentPathPoints, sampledDotPositions);
        RenderDots(sampledDotPositions);
    }

    private void BuildDotPositions(List<Vector3> pathPoints, List<Vector3> output)
    {
        output.Clear();
        if (pathPoints.Count == 0)
        {
            return;
        }

        float pathLengthSoFar = 0f;
        float nextDotDistance = 0f;

        for (int i = 1; i < pathPoints.Count && output.Count < maxDots; i++)
        {
            Vector3 segmentStart = pathPoints[i - 1];
            Vector3 segmentEnd = pathPoints[i];
            float segmentLength = Vector3.Distance(segmentStart, segmentEnd);

            if (segmentLength <= 0.0001f)
            {
                continue;
            }

            while (nextDotDistance <= pathLengthSoFar + segmentLength && output.Count < maxDots)
            {
                float distanceIntoSegment = nextDotDistance - pathLengthSoFar;
                float lerpValue = Mathf.Clamp01(distanceIntoSegment / segmentLength);
                Vector3 rawPoint = Vector3.Lerp(segmentStart, segmentEnd, lerpValue);
                output.Add(GetGuidePoint(rawPoint));
                nextDotDistance += Mathf.Max(0.05f, dotSpacing);
            }

            pathLengthSoFar += segmentLength;
        }

        if (output.Count == 0)
        {
            output.Add(GetGuidePoint(pathPoints[0]));
        }

        Vector3 templePoint = GetGuidePoint(pathPoints[pathPoints.Count - 1]);
        if (output.Count < maxDots && (output[output.Count - 1] - templePoint).sqrMagnitude > 0.04f)
        {
            output.Add(templePoint);
        }
    }

    private Vector3 GetGuidePoint(Vector3 rawPoint)
    {
        if (guideGroundLayer.value != 0)
        {
            Vector3 rayOrigin = rawPoint + Vector3.up * 2f;
            if (Physics.Raycast(
                rayOrigin,
                Vector3.down,
                out RaycastHit hit,
                4f,
                guideGroundLayer,
                QueryTriggerInteraction.Ignore))
            {
                rawPoint = hit.point;
            }
        }

        return rawPoint + Vector3.up * dotHeightOffset;
    }

    private void RenderDots(List<Vector3> dotPositions)
    {
        EnsurePoolSize(dotPositions.Count);

        for (int i = 0; i < dotPool.Count; i++)
        {
            bool shouldBeActive = i < dotPositions.Count;
            dotPool[i].gameObject.SetActive(shouldBeActive);

            if (!shouldBeActive)
            {
                continue;
            }

            dotPool[i].Configure(dotScale, animationSpeed, pulseAmount, bobAmount, i);
            dotPool[i].SetAnchorPosition(dotPositions[i]);
        }
    }

    private void EnsurePoolSize(int targetCount)
    {
        int desiredCount = Mathf.Min(targetCount, maxDots);

        while (dotPool.Count < desiredCount)
        {
            GuideDot dot = Instantiate(guideDotPrefab, transform);
            dot.gameObject.SetActive(false);
            dotPool.Add(dot);
        }
    }

    private void HideAllDots()
    {
        for (int i = 0; i < dotPool.Count; i++)
        {
            if (dotPool[i] != null)
            {
                dotPool[i].gameObject.SetActive(false);
            }
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (!drawDebugPath || currentPathPoints.Count < 2)
        {
            return;
        }

        Gizmos.color = new Color(0.2f, 0.9f, 1f, 0.75f);
        for (int i = 1; i < currentPathPoints.Count; i++)
        {
            Gizmos.DrawLine(currentPathPoints[i - 1] + Vector3.up * 0.1f, currentPathPoints[i] + Vector3.up * 0.1f);
        }
    }
}
