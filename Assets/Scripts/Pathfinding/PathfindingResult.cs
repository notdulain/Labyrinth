using System.Collections.Generic;
using UnityEngine;

public class PathfindingResult
{
    public List<Node> path = new List<Node>();
    public List<Vector3> worldPath = new List<Vector3>();
    public int visitedNodeCount;
    public float calculationTimeMs;
    public bool pathFound;
    public string algorithmName;
    public string failureReason;
    public Vector3 requestedStartPosition;
    public Vector3 requestedTargetPosition;
    public Vector3 resolvedStartNode;
    public Vector3 resolvedTargetNode;
    public bool startPositionBlocked;
    public bool targetPositionBlocked;
    public bool usedNearestStartNode;
    public bool usedNearestTargetNode;
    public bool usedReachableTargetProxy;
    public int startComponentSize;
    public int targetComponentSize;
}
