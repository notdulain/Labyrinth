using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Enemy controller for the DemonDog.
/// The user controls the Player; this script only moves the dog toward the Player
/// using the currently selected pathfinding algorithm.
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class DungeonDogController : MonoBehaviour
{
    [Header("Target")]
    public Transform player;
    public float stoppingDistance = 1.2f;

    [Header("Search")]
    public Transform searchStart;
    public bool beginAtSearchStart = true;

    [Header("Movement")]
    public float moveSpeed = 5.5f;
    public float rotationSpeed = 10f;
    public float gravity = -20f;
    public LayerMask obstacleLayers;

    [Header("Close Range Fallback")]
    public float directChaseDistance = 5f;
    public float directStopDistance = 0.45f;

    [Header("Pathfinding")]
    public MultiAlgorithmPathfinder pathfinder;
    public PathfindingAlgorithm selectedAlgorithm = PathfindingAlgorithm.AStar;
    public float pathUpdateInterval = 0.4f;
    public float waypointReachDistance = 0.2f;

    [Header("Animation")]
    public Animator animator;

    [Header("Placeholder Run Animation")]
    public Transform modelRoot;
    public bool useProceduralRunAnimation = true;
    public float runBobHeight = 0.08f;
    public float runStrideTilt = 8f;
    public float runAnimationSpeed = 12f;

    [Header("Debug UI")]
    public PathVisualizer pathVisualizer;
    public AlgorithmComparison algorithmChart;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsChasingHash = Animator.StringToHash("IsChasing");

    private readonly List<Vector3> currentPath = new List<Vector3>();
    private CharacterController characterController;
    private float pathUpdateTimer;
    private float verticalVelocity;
    private int currentPathIndex;
    private Vector3 previousPosition;
    private bool isChasing;
    private bool placedAtSearchStart;
    private PathfindingResult lastResult;
    private Vector3 modelBaseLocalPosition;
    private Quaternion modelBaseLocalRotation;
    private float runAnimationTime;
    private int lastLoggedWaypointIndex = -1;
    private bool setupLogged;
    private bool initialGroundSnapDone;
    private Vector3 lastRecalcPlayerPosition;
    private bool hasRecalcTarget;
    private bool animatorEnsured;


    private const float MinRecalcGap = 0.05f;

    private static PathfindingAlgorithm globalSelectedAlgorithm = PathfindingAlgorithm.AStar;
    private static bool globalPathVisualisation;
    private static int lastInputFrame = -1;

    private void Awake()
    {
        AdoptDetachedMeshyDogModel();
        characterController = GetComponent<CharacterController>();
        AlignCharacterControllerToGround();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        ResolveModelRoot();
        EnsureAnimatorRunsOnModel();
        CacheModelPose();
        ResolveSceneReferences();
        EnsureAnimatorDoesNotUseRootMotion();
        previousPosition = transform.position;
    }

    private void Start()
    {
        ResolveSceneReferences();
        PlaceAtSearchStart();
        SnapToGround();
        LogControllerSetup();
    }

    private void OnEnable()
    {
        pathUpdateTimer = pathUpdateInterval;
        currentPath.Clear();
        currentPathIndex = 0;
        lastLoggedWaypointIndex = -1;
        previousPosition = transform.position;
        CacheModelPose();
    }

    private void OnValidate()
    {
        pathUpdateInterval = Mathf.Clamp(pathUpdateInterval, 0.3f, 0.5f);

        AdoptDetachedMeshyDogModel();

        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
        }

        EnsureAnimatorDoesNotUseRootMotion();
    }

    private void Update()
    {
        ResolveSceneReferences();
        HandleSharedInput();

        if (selectedAlgorithm != globalSelectedAlgorithm)
        {
            selectedAlgorithm = globalSelectedAlgorithm;
            RecalculatePath();
        }

        if (player != null)
        {
            float distanceToPlayer = Vector3.Distance(GetFlatPosition(transform.position), GetFlatPosition(player.position));

            // Hysteresis on isChasing so the dog doesn't toggle between Run / Idle every
            // frame when its distance to the player hovers right at stoppingDistance.
            float chaseEnter = stoppingDistance * 1.15f;
            float chaseExit = stoppingDistance * 0.85f;
            if (isChasing)
            {
                if (distanceToPlayer < chaseExit) isChasing = false;
            }
            else
            {
                if (distanceToPlayer > chaseEnter) isChasing = true;
            }

            if (isChasing)
            {
                pathUpdateTimer += Time.deltaTime;
                bool playerMovedFar = !hasRecalcTarget ||
                    Vector3.Distance(GetFlatPosition(player.position), GetFlatPosition(lastRecalcPlayerPosition)) > 1.5f;
                bool pathExhausted = currentPath.Count == 0 || currentPathIndex >= currentPath.Count;
                bool intervalElapsed = pathUpdateTimer >= pathUpdateInterval;
                bool shouldRecalc = pathExhausted || (intervalElapsed && playerMovedFar);
                // Path-exhaustion recalcs are immediate (no waiting on pathUpdateInterval)
                // so the dog doesn't idle for ~0.4 s at the end of every short path. A
                // small minimum gap prevents runaway recompute if a path resolves in
                // one frame.
                if (shouldRecalc && pathUpdateTimer >= MinRecalcGap)
                {
                    RecalculatePath();
                    lastRecalcPlayerPosition = player.position;
                    hasRecalcTarget = true;
                }
            }
            else
            {
                currentPath.Clear();
                currentPathIndex = 0;
                lastLoggedWaypointIndex = -1;
                hasRecalcTarget = false;

                if (pathVisualizer != null)
                {
                    pathVisualizer.SetCurrentPath(GetInstanceID(), currentPath, selectedAlgorithm);
                }
            }
        }

        ApplyMovementAndGravity();

        UpdateAnimator();
        previousPosition = transform.position;
    }

    public void SetTarget(Transform newTarget)
    {
        player = newTarget;
    }

    public void SetPathfinder(MultiAlgorithmPathfinder newPathfinder)
    {
        pathfinder = newPathfinder;
    }

    private void ResolveSceneReferences()
    {
        AdoptDetachedMeshyDogModel();

        if (player == null)
        {
            GameObject hero = null;

            try
            {
                hero = GameObject.FindGameObjectWithTag("Player");
            }
            catch (UnityException)
            {
                // Older test scenes may not define the Player tag.
            }

            if (hero == null) hero = GameObject.Find("Player");
            if (hero != null) player = hero.transform;
        }

        if (searchStart == null)
        {
            searchStart = ResolveSearchStart();
        }

        if (pathfinder == null)
        {
            pathfinder = FindAnyObjectByType<MultiAlgorithmPathfinder>();
        }

        if (pathVisualizer == null)
        {
            pathVisualizer = FindAnyObjectByType<PathVisualizer>();
        }

        if (algorithmChart == null)
        {
            algorithmChart = FindAnyObjectByType<AlgorithmComparison>();
        }

        if (!animatorEnsured)
        {
            EnsureAnimatorRunsOnModel();
            EnsureAnimatorDoesNotUseRootMotion();
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                animatorEnsured = true;
            }
        }
    }

    private Transform ResolveSearchStart()
    {
        string dogNumber = GetTrailingNumber(name);
        if (!string.IsNullOrEmpty(dogNumber))
        {
            GameObject numberedSpawn = GameObject.Find($"AgentSpawn_{dogNumber}");
            if (numberedSpawn != null)
            {
                return numberedSpawn.transform;
            }
        }

        Transform bestSpawn = null;
        float bestDistanceSqr = float.PositiveInfinity;

        try
        {
            GameObject[] taggedSpawns = GameObject.FindGameObjectsWithTag("AgentSpawn");
            for (int i = 0; i < taggedSpawns.Length; i++)
            {
                ConsiderSearchStartCandidate(taggedSpawns[i].transform, ref bestSpawn, ref bestDistanceSqr);
            }
        }
        catch (UnityException)
        {
            // Older scenes may not define the AgentSpawn tag.
        }

        Transform[] sceneTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Exclude,
            FindObjectsSortMode.None);
        for (int i = 0; i < sceneTransforms.Length; i++)
        {
            Transform candidate = sceneTransforms[i];
            if (candidate.name.StartsWith("AgentSpawn"))
            {
                ConsiderSearchStartCandidate(candidate, ref bestSpawn, ref bestDistanceSqr);
            }
        }

        return bestSpawn;
    }

    private void ConsiderSearchStartCandidate(
        Transform candidate,
        ref Transform bestSpawn,
        ref float bestDistanceSqr)
    {
        if (candidate == null)
        {
            return;
        }

        float distanceSqr = (candidate.position - transform.position).sqrMagnitude;
        if (distanceSqr < bestDistanceSqr)
        {
            bestSpawn = candidate;
            bestDistanceSqr = distanceSqr;
        }
    }

    private static string GetTrailingNumber(string value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        int start = value.Length;
        while (start > 0 && char.IsDigit(value[start - 1]))
        {
            start--;
        }

        return start < value.Length ? value.Substring(start) : string.Empty;
    }

    private void PlaceAtSearchStart()
    {
        if (!beginAtSearchStart || placedAtSearchStart || searchStart == null)
        {
            return;
        }

        Vector3 startPosition = searchStart.position;
        startPosition.y = player != null ? player.position.y : 0f;

        if (characterController != null)
        {
            characterController.enabled = false;
            transform.position = startPosition;
            characterController.enabled = true;
        }
        else
        {
            transform.position = startPosition;
        }

        currentPath.Clear();
        currentPathIndex = 0;
        previousPosition = transform.position;
        placedAtSearchStart = true;

        Debug.Log($"[DemonDog] Starting search from {searchStart.name}.", this);
    }

    private void AlignCharacterControllerToGround()
    {
        if (characterController == null)
        {
            return;
        }

        if (characterController.center.y <= 0.01f)
        {
            characterController.center = new Vector3(
                characterController.center.x,
                characterController.height * 0.5f,
                characterController.center.z);
        }
    }

    private void ResolveModelRoot()
    {
        Transform meshyModel = transform.Find("MeshyDogModel");
        if (meshyModel != null)
        {
            if (modelRoot != meshyModel)
            {
                modelRoot = meshyModel;
            }

            useProceduralRunAnimation = false;
            DisableLegacyDogModel();
            return;
        }

        if (modelRoot != null)
        {
            return;
        }

        Transform namedModel = transform.Find("DogModel");
        if (namedModel != null && namedModel.gameObject.activeInHierarchy)
        {
            modelRoot = namedModel;
            useProceduralRunAnimation = true;
            return;
        }

        MeshRenderer meshRenderer = GetComponentInChildren<MeshRenderer>();
        if (meshRenderer != null && meshRenderer.transform != transform)
        {
            modelRoot = meshRenderer.transform;
        }
    }

    private void AdoptDetachedMeshyDogModel()
    {
        Transform existingChild = transform.Find("MeshyDogModel");
        if (existingChild != null)
        {
            modelRoot = existingChild;
            useProceduralRunAnimation = false;
            return;
        }

        Transform detachedModel = FindDetachedMeshyDogModel();
        if (detachedModel == null)
        {
            return;
        }

        detachedModel.SetParent(transform, false);
        detachedModel.localPosition = Vector3.zero;
        detachedModel.localRotation = Quaternion.identity;

        if (detachedModel.localScale == Vector3.one)
        {
            detachedModel.localScale = Vector3.one * 120f;
        }

        modelRoot = detachedModel;
        useProceduralRunAnimation = false;
        DisableLegacyDogModel();
        Debug.Log("[DemonDog] Adopted detached MeshyDogModel and parented it to DemonDog.", this);
    }

    private void DisableLegacyDogModel()
    {
        Transform oldModel = transform.Find("DogModel");
        if (oldModel != null && oldModel.gameObject.activeSelf)
        {
            oldModel.gameObject.SetActive(false);
        }
    }

    private void EnsureAnimatorRunsOnModel()
    {
        if (modelRoot == null)
        {
            ResolveModelRoot();
        }

        if (modelRoot == null)
        {
            return;
        }

        Animator parentAnimator = GetComponent<Animator>();
        Animator modelAnimator = modelRoot.GetComponent<Animator>();

        RuntimeAnimatorController controller = null;
        Avatar avatar = null;
        AnimatorCullingMode cullingMode = AnimatorCullingMode.AlwaysAnimate;
        AnimatorUpdateMode updateMode = AnimatorUpdateMode.Normal;

        if (modelAnimator != null)
        {
            controller = modelAnimator.runtimeAnimatorController;
            avatar = modelAnimator.avatar;
            cullingMode = modelAnimator.cullingMode;
            updateMode = modelAnimator.updateMode;
        }
        else if (animator != null)
        {
            controller = animator.runtimeAnimatorController;
            avatar = animator.avatar;
            cullingMode = animator.cullingMode;
            updateMode = animator.updateMode;
        }
        else if (parentAnimator != null)
        {
            controller = parentAnimator.runtimeAnimatorController;
            avatar = parentAnimator.avatar;
            cullingMode = parentAnimator.cullingMode;
            updateMode = parentAnimator.updateMode;
        }

        if (modelAnimator == null)
        {
            modelAnimator = modelRoot.gameObject.AddComponent<Animator>();
        }

        if (modelAnimator.runtimeAnimatorController == null && controller != null)
        {
            modelAnimator.runtimeAnimatorController = controller;
        }

        if (modelAnimator.avatar == null && avatar != null)
        {
            modelAnimator.avatar = avatar;
        }

        modelAnimator.cullingMode = cullingMode;
        modelAnimator.updateMode = updateMode;
        modelAnimator.applyRootMotion = false;
        modelAnimator.enabled = true;

        if (parentAnimator != null && parentAnimator != modelAnimator)
        {
            parentAnimator.applyRootMotion = false;
            parentAnimator.enabled = false;
        }

        animator = modelAnimator;

        Debug.Log("[DemonDog] Animator control verified on MeshyDogModel with root motion disabled.", this);
    }

    private Transform FindDetachedMeshyDogModel()
    {
        Transform[] allTransforms = FindObjectsByType<Transform>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None);

        for (int i = 0; i < allTransforms.Length; i++)
        {
            Transform candidate = allTransforms[i];
            if (candidate == null || candidate == transform)
            {
                continue;
            }

            if (candidate.name != "MeshyDogModel")
            {
                continue;
            }

            if (candidate.parent == transform)
            {
                return candidate;
            }

            return candidate;
        }

        return null;
    }

    private void CacheModelPose()
    {
        if (modelRoot == null)
        {
            ResolveModelRoot();
        }

        if (modelRoot == null)
        {
            return;
        }

        modelBaseLocalPosition = modelRoot.localPosition;
        modelBaseLocalRotation = modelRoot.localRotation;
    }

    private void HandleSharedInput()
    {
        if (lastInputFrame == Time.frameCount)
        {
            return;
        }

        bool changedAlgorithm = false;

        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            globalSelectedAlgorithm = PathfindingAlgorithm.AStar;
            Debug.Log("Selected Algorithm: A*");
            changedAlgorithm = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            globalSelectedAlgorithm = PathfindingAlgorithm.Dijkstra;
            Debug.Log("Selected Algorithm: Dijkstra");
            changedAlgorithm = true;
        }
        else if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            globalSelectedAlgorithm = PathfindingAlgorithm.BFS;
            Debug.Log("Selected Algorithm: BFS");
            changedAlgorithm = true;
        }

        bool toggledVisualisation = false;
        if (Input.GetKeyDown(KeyCode.P))
        {
            globalPathVisualisation = !globalPathVisualisation;
            if (pathVisualizer != null)
            {
                pathVisualizer.SetVisible(globalPathVisualisation);
            }

            Debug.Log($"Path Visualisation: {(globalPathVisualisation ? "ON" : "OFF")}");
            toggledVisualisation = true;
        }

        if (changedAlgorithm || toggledVisualisation)
        {
            lastInputFrame = Time.frameCount;
        }
    }

    private void RecalculatePath()
    {
        pathUpdateTimer = 0f;
        currentPath.Clear();
        currentPathIndex = 0;
        lastLoggedWaypointIndex = -1;

        if (pathfinder == null || player == null) return;
        if (GraphBuilder.Instance == null || GraphBuilder.Instance.AdjacencyList == null)
        {
            Debug.LogWarning("[DemonDog] GraphBuilder is missing or has no graph.", this);
            return;
        }

        Vector3 startNode = GraphBuilder.Instance.GetNearestNodeReachableTo(transform.position, player.position);
        Vector3 goalNode = GraphBuilder.Instance.GetNearestNode(player.position);

        lastResult = pathfinder.FindPathFromNodes(
            startNode,
            goalNode,
            selectedAlgorithm,
            transform.position,
            player.position);

        if (lastResult.pathFound)
        {
            currentPath.AddRange(lastResult.worldPath);
        }
        else if (!string.IsNullOrEmpty(lastResult.failureReason))
        {
            Debug.LogWarning($"[DemonDog] Path recalculation failed. {lastResult.failureReason}", this);
        }

        currentPathIndex = GetClosestUsefulPathIndex();
        SkipWaypointsBehindGoal();
        LogWaypointIndex();

        if (pathVisualizer != null)
        {
            pathVisualizer.SetCurrentPath(GetInstanceID(), currentPath, selectedAlgorithm);
        }

        if (algorithmChart != null)
        {
            algorithmChart.UpdateFromDogResult(
                lastResult,
                pathfinder.CompareAllFromNodes(
                    startNode,
                    goalNode,
                    transform.position,
                    player.position,
                    true),
                selectedAlgorithm);
        }
    }

    private void ApplyMovementAndGravity()
    {
        Vector3 horizontalDelta = Vector3.zero;
        Vector3 desiredDirection = Vector3.zero;

        if (isChasing && currentPath.Count > 0 && currentPathIndex < currentPath.Count)
        {
            LogWaypointIndex();

            while (currentPathIndex < currentPath.Count)
            {
                Vector3 waypoint = currentPath[currentPathIndex];
                Vector3 toWaypoint = waypoint - transform.position;
                toWaypoint.y = 0f;

                if (toWaypoint.sqrMagnitude <= waypointReachDistance * waypointReachDistance)
                {
                    currentPathIndex++;
                    LogWaypointIndex();
                    continue;
                }

                desiredDirection = toWaypoint.normalized;
                horizontalDelta = desiredDirection * moveSpeed * Time.deltaTime;
                break;
            }
        }
        else if (isChasing && TryGetDirectChaseDirection(out Vector3 directDirection))
        {
            desiredDirection = directDirection;
            horizontalDelta = desiredDirection * moveSpeed * Time.deltaTime;
        }

        bool grounded = characterController != null && characterController.enabled && characterController.isGrounded;
        if (grounded && verticalVelocity < 0f)
        {
            verticalVelocity = -2f;
        }
        verticalVelocity += gravity * Time.deltaTime;
        Vector3 verticalDelta = Vector3.up * (verticalVelocity * Time.deltaTime);

        Vector3 preMovePosition = transform.position;

        if (characterController != null && characterController.enabled)
        {
            characterController.Move(horizontalDelta + verticalDelta);
        }
        else
        {
            transform.position += horizontalDelta + verticalDelta;
        }

        Vector3 actualHorizontalDelta = transform.position - preMovePosition;
        actualHorizontalDelta.y = 0f;
        float actualHorizontalSpeed = actualHorizontalDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
        if (actualHorizontalSpeed > 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(actualHorizontalDelta.normalized, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRotation,
                rotationSpeed * Time.deltaTime);
        }
    }

    private bool TryGetDirectChaseDirection(out Vector3 direction)
    {
        direction = Vector3.zero;
        if (player == null)
        {
            return false;
        }

        Vector3 toPlayer = GetFlatPosition(player.position) - GetFlatPosition(transform.position);
        float distance = toPlayer.magnitude;
        if (distance <= directStopDistance || distance > directChaseDistance)
        {
            return false;
        }

        if (!HasDirectLineToPlayer(distance))
        {
            return false;
        }

        direction = toPlayer / distance;
        return true;
    }

    private bool HasDirectLineToPlayer(float distance)
    {
        LayerMask blockingLayers = obstacleLayers;
        if (blockingLayers.value == 0 && GraphBuilder.Instance != null)
        {
            blockingLayers = GraphBuilder.Instance.wallLayer;
        }

        if (blockingLayers.value == 0)
        {
            return true;
        }

        Vector3 start = transform.position + Vector3.up * 0.6f;
        Vector3 toPlayer = GetFlatPosition(player.position) - GetFlatPosition(transform.position);
        if (toPlayer.sqrMagnitude <= 0.0001f)
        {
            return true;
        }

        float castRadius = characterController != null ? characterController.radius * 0.8f : 0.35f;
        return !Physics.SphereCast(
            start,
            castRadius,
            toPlayer.normalized,
            out _,
            distance,
            blockingLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void SkipWaypointsBehindGoal()
    {
        if (player == null || currentPath.Count == 0) return;

        Vector3 selfFlat = GetFlatPosition(transform.position);
        Vector3 toGoal = GetFlatPosition(player.position) - selfFlat;
        if (toGoal.sqrMagnitude < 0.01f) return;
        toGoal.Normalize();

        while (currentPathIndex < currentPath.Count - 1)
        {
            Vector3 toWaypoint = GetFlatPosition(currentPath[currentPathIndex]) - selfFlat;
            if (Vector3.Dot(toWaypoint, toGoal) <= 0f ||
                toWaypoint.sqrMagnitude < waypointReachDistance * waypointReachDistance)
            {
                currentPathIndex++;
                continue;
            }
            break;
        }
    }

    private void SnapToGround()
    {
        if (initialGroundSnapDone) return;
        if (characterController == null) return;

        bool ccWasEnabled = characterController.enabled;
        characterController.enabled = false;

        Vector3 origin = transform.position + Vector3.up * 5f;
        if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 50f, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 p = transform.position;
            p.y = hit.point.y;
            transform.position = p;
            verticalVelocity = 0f;
            previousPosition = transform.position;
            Debug.Log($"[DemonDog] Ground-snapped to y={hit.point.y:F2} on '{hit.collider.name}'.", this);
        }
        else
        {
            Debug.LogWarning("[DemonDog] SnapToGround found no floor below spawn position.", this);
        }

        characterController.enabled = ccWasEnabled;
        initialGroundSnapDone = true;
    }

    private void UpdateAnimator()
    {
        if (animator == null) return;

        Vector3 frameDelta = transform.position - previousPosition;
        frameDelta.y = 0f;
        float horizontalSpeed = frameDelta.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);

        animator.SetFloat(SpeedHash, horizontalSpeed);
        animator.SetBool(IsChasingHash, isChasing);

        UpdateProceduralRunAnimation(horizontalSpeed);
    }

    private void UpdateProceduralRunAnimation(float horizontalSpeed)
    {
        if (!useProceduralRunAnimation || modelRoot == null)
        {
            return;
        }

        if (horizontalSpeed <= 0.05f)
        {
            runAnimationTime = 0f;
            modelRoot.localPosition = Vector3.Lerp(
                modelRoot.localPosition,
                modelBaseLocalPosition,
                Time.deltaTime * 8f);
            modelRoot.localRotation = Quaternion.Slerp(
                modelRoot.localRotation,
                modelBaseLocalRotation,
                Time.deltaTime * 8f);
            return;
        }

        float speedMultiplier = Mathf.Clamp(horizontalSpeed / Mathf.Max(moveSpeed, 0.01f), 0.7f, 1.4f);
        runAnimationTime += Time.deltaTime * runAnimationSpeed * speedMultiplier;

        float stride = Mathf.Sin(runAnimationTime);
        float doubleStride = Mathf.Sin(runAnimationTime * 2f);
        float bob = Mathf.Abs(stride) * runBobHeight;

        modelRoot.localPosition = modelBaseLocalPosition + Vector3.up * bob;
        modelRoot.localRotation =
            modelBaseLocalRotation *
            Quaternion.Euler(doubleStride * runStrideTilt * 0.35f, 0f, stride * runStrideTilt);
    }

    private int GetClosestUsefulPathIndex()
    {
        if (currentPath.Count == 0) return 0;

        Vector3 flatPosition = GetFlatPosition(transform.position);
        int closestIndex = 0;
        float closestDistance = float.MaxValue;

        for (int i = 0; i < currentPath.Count; i++)
        {
            float distance = Vector3.Distance(flatPosition, GetFlatPosition(currentPath[i]));
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestIndex = i;
            }
        }

        if (closestIndex < currentPath.Count - 1 && closestDistance <= waypointReachDistance * 1.5f)
        {
            closestIndex++;
        }

        return closestIndex;
    }

    private void EnsureAnimatorDoesNotUseRootMotion()
    {
        if (animator != null && animator.applyRootMotion)
        {
            animator.applyRootMotion = false;
            Debug.Log("[DemonDog] Animator Apply Root Motion was ON and has been disabled.", this);
        }
    }

    private void LogControllerSetup()
    {
        if (setupLogged)
        {
            return;
        }

        int controllerCount = FindObjectsByType<DungeonDogController>(
            FindObjectsInactive.Include,
            FindObjectsSortMode.None).Length;

        Debug.Log(
            $"[DemonDog] Controller object: {name}\n" +
            $"Visible model root: {(modelRoot != null ? modelRoot.name : "None")}\n" +
            $"Model root parent: {(modelRoot != null && modelRoot.parent != null ? modelRoot.parent.name : "None")}\n" +
            $"Player assigned: {(player != null ? player.name : "None")}\n" +
            $"Pathfinder assigned: {(pathfinder != null ? pathfinder.name : "None")}\n" +
            $"GraphBuilder assigned: {(GraphBuilder.Instance != null ? GraphBuilder.Instance.name : "None")}\n" +
            $"CharacterController assigned: {characterController != null}\n" +
            $"Animator assigned: {animator != null}\n" +
            $"Apply Root Motion: {(animator != null && animator.applyRootMotion)}\n" +
            $"Active dog controllers: {controllerCount}",
            this);

        if (modelRoot != null && modelRoot.parent != transform)
        {
            Debug.LogWarning(
                "[DemonDog] The visible dog model is not parented directly under the moving dog object.",
                this);
        }

        if (controllerCount > 1)
        {
            Debug.LogWarning("[DemonDog] Multiple dog controllers are active in the scene.", this);
        }

        setupLogged = true;
    }

    private void LogWaypointIndex()
    {
        if (currentPath.Count == 0)
        {
            return;
        }

        int clampedIndex = Mathf.Clamp(currentPathIndex, 0, currentPath.Count - 1);
        if (clampedIndex == lastLoggedWaypointIndex)
        {
            return;
        }

        lastLoggedWaypointIndex = clampedIndex;
        Debug.Log($"[DemonDog] Current waypoint index: {clampedIndex}/{currentPath.Count - 1}", this);
    }

    private static Vector3 GetFlatPosition(Vector3 position)
    {
        position.y = 0f;
        return position;
    }
}
