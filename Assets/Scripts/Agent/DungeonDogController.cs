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
            isChasing = distanceToPlayer > stoppingDistance;
            bool shouldMove = isChasing;

            if (shouldMove)
            {
                pathUpdateTimer += Time.deltaTime;
                if (pathUpdateTimer >= pathUpdateInterval)
                {
                    RecalculatePath();
                }

                FollowPath();
            }
            else
            {
                currentPath.Clear();
                currentPathIndex = 0;
                lastLoggedWaypointIndex = -1;

                if (pathVisualizer != null)
                {
                    pathVisualizer.SetCurrentPath(currentPath, selectedAlgorithm);
                }
            }
        }

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

        EnsureAnimatorRunsOnModel();
        EnsureAnimatorDoesNotUseRootMotion();
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
        if (namedModel != null)
        {
            modelRoot = namedModel;
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
        LogWaypointIndex();

        if (pathVisualizer != null)
        {
            pathVisualizer.SetCurrentPath(currentPath, selectedAlgorithm);
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

    private void FollowPath()
    {
        if (currentPath.Count == 0 || currentPathIndex >= currentPath.Count)
        {
            return;
        }

        LogWaypointIndex();

        while (currentPathIndex < currentPath.Count)
        {
            Vector3 targetPosition = currentPath[currentPathIndex];
            targetPosition.y = transform.position.y;

            Vector3 moveDirection = targetPosition - transform.position;
            moveDirection.y = 0f;

            if (moveDirection.sqrMagnitude <= waypointReachDistance * waypointReachDistance)
            {
                currentPathIndex++;
                LogWaypointIndex();
                continue;
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

            return;
        }
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
