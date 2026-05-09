using UnityEngine;

/// <summary>
/// Clean minimal third person controller.
/// - Idle by default
/// - WASD movement (with diagonals)
/// - Left Shift to run
/// - Space to jump
/// Animator parameters required:
///   Float "Speed"      (0 = idle, 0.5 = walk, 1 = run)
///   Bool  "Grounded"
///   Trigger "Jump"
/// </summary>
[RequireComponent(typeof(CharacterController))]
public class SimpleThirdPersonController : MonoBehaviour, IPlayerMotionResettable
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;
    [SerializeField] private float moveAfterTurnAngle = 2f;
    [SerializeField] private float gravity = -20f;
    [SerializeField] private float jumpHeight = 1.2f;

    [Header("Animation")]
    [SerializeField] private Animator animator;

    [Header("Camera")]
    [SerializeField] private Camera followCamera;
    [SerializeField] private float cameraDistance = 3.2f;
    [SerializeField] private float cameraHeight = 2.2f;
    [SerializeField] private float cameraLookHeight = 1.25f;
    [SerializeField] private float cameraFollowSpeed = 12f;
    [SerializeField] private float cameraFieldOfView = 65f;
    [SerializeField] private float mouseSensitivity = 180f;
    [SerializeField] private float minCameraPitch = -20f;
    [SerializeField] private float maxCameraPitch = 55f;

    [Header("Camera Collision")]
    [SerializeField] private LayerMask cameraCollisionMask = ~0;
    [SerializeField] private float cameraCollisionRadius = 0.25f;
    [SerializeField] private float cameraCollisionPadding = 0.2f;

    private CharacterController characterController;
    private float verticalVelocity;
    private float currentSpeedParam; // smoothed value sent to Animator
    private float cameraYaw;
    private float cameraPitch = 18f;

    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash = Animator.StringToHash("Jump");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        if (followCamera == null) followCamera = Camera.main;

        cameraYaw = transform.eulerAngles.y;
        SnapCameraToTarget();
    }

    private void Update()
    {
        // ---- INPUT ----
        float h = Input.GetAxisRaw("Horizontal");   // A/D
        float v = Input.GetAxisRaw("Vertical");     // W/S
        Vector3 input = new Vector3(h, 0f, v);
        bool isMoving = input.sqrMagnitude > 0.01f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // ---- MOVEMENT (camera-relative, world-space) ----
        Vector3 moveDir = Vector3.zero;
        if (isMoving)
        {
            Quaternion yawRotation = Quaternion.Euler(0f, cameraYaw, 0f);
            Vector3 forward = yawRotation * Vector3.forward;
            Vector3 right = yawRotation * Vector3.right;
            moveDir = (forward * input.z + right * input.x).normalized;
        }

        float speed = isRunning ? runSpeed : walkSpeed;

        // ---- GRAVITY + JUMP ----
        bool grounded = characterController.isGrounded;
        if (grounded && verticalVelocity < 0f) verticalVelocity = -1f;

        if (grounded && Input.GetKeyDown(KeyCode.Space))
        {
            if (animator != null)
            {
                animator.ResetTrigger(JumpHash);
                animator.SetTrigger(JumpHash);
            }

            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }

        verticalVelocity += gravity * Time.deltaTime;

        // ---- ROTATE TO FACE MOVEMENT ----
        bool canMoveHorizontally = true;
        if (isMoving)
        {
            Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(
                transform.rotation,
                target,
                rotationSpeed * 90f * Time.deltaTime);

            canMoveHorizontally = Quaternion.Angle(transform.rotation, target) <= moveAfterTurnAngle;
        }

        // ---- APPLY MOVEMENT ----
        Vector3 horizontalVelocity = canMoveHorizontally ? moveDir * speed : Vector3.zero;
        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        // ---- ANIMATOR ----
        // Target: 0 idle, 0.5 walk, 1.0 run
        float targetSpeedParam = 0f;
        if (isMoving) targetSpeedParam = isRunning ? 1f : 0.5f;

        // Smooth so transitions feel natural
        currentSpeedParam = Mathf.Lerp(currentSpeedParam, targetSpeedParam, 10f * Time.deltaTime);

        if (animator != null)
        {
            animator.SetFloat(SpeedHash, currentSpeedParam);
            animator.SetBool(GroundedHash, grounded);
        }
    }

    private void LateUpdate()
    {
        UpdateCamera();
    }

    public void ResetMotionState()
    {
        verticalVelocity = 0f;
        currentSpeedParam = 0f;
        cameraYaw = transform.eulerAngles.y;

        if (animator != null)
        {
            animator.ResetTrigger(JumpHash);
            animator.SetFloat(SpeedHash, 0f);
            animator.SetBool(GroundedHash, true);
        }

        SnapCameraToTarget();
    }

    private void UpdateCamera()
    {
        if (followCamera == null) return;

        cameraYaw += Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        cameraPitch -= Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;
        cameraPitch = Mathf.Clamp(cameraPitch, minCameraPitch, maxCameraPitch);

        Vector3 lookTarget = transform.position + Vector3.up * cameraLookHeight;
        Quaternion orbitRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 desiredPosition = lookTarget + orbitRotation * new Vector3(0f, 0f, -cameraDistance);
        desiredPosition.y = Mathf.Max(desiredPosition.y, transform.position.y + cameraHeight);
        desiredPosition = GetWallSafeCameraPosition(lookTarget, desiredPosition);

        followCamera.transform.position = Vector3.Lerp(
            followCamera.transform.position,
            desiredPosition,
            cameraFollowSpeed * Time.deltaTime);
        followCamera.transform.rotation = Quaternion.LookRotation(lookTarget - followCamera.transform.position, Vector3.up);
        followCamera.orthographic = false;
        followCamera.fieldOfView = cameraFieldOfView;
    }

    private void SnapCameraToTarget()
    {
        if (followCamera == null) return;

        Vector3 lookTarget = transform.position + Vector3.up * cameraLookHeight;
        Quaternion orbitRotation = Quaternion.Euler(cameraPitch, cameraYaw, 0f);
        Vector3 cameraPosition = lookTarget + orbitRotation * new Vector3(0f, 0f, -cameraDistance);
        cameraPosition.y = Mathf.Max(cameraPosition.y, transform.position.y + cameraHeight);
        cameraPosition = GetWallSafeCameraPosition(lookTarget, cameraPosition);

        followCamera.transform.position = cameraPosition;
        followCamera.transform.rotation = Quaternion.LookRotation(lookTarget - followCamera.transform.position, Vector3.up);
        followCamera.orthographic = false;
        followCamera.fieldOfView = cameraFieldOfView;
    }

    private Vector3 GetWallSafeCameraPosition(Vector3 lookTarget, Vector3 desiredPosition)
    {
        Vector3 cameraDirection = desiredPosition - lookTarget;
        float cameraDistanceToCheck = cameraDirection.magnitude;

        if (cameraDistanceToCheck <= 0.001f)
        {
            return desiredPosition;
        }

        cameraDirection /= cameraDistanceToCheck;
        float nearestDistance = cameraDistanceToCheck;
        bool foundObstacle = false;

        RaycastHit[] hits = Physics.SphereCastAll(
            lookTarget,
            cameraCollisionRadius,
            cameraDirection,
            cameraDistanceToCheck,
            cameraCollisionMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == transform || hit.transform.IsChildOf(transform))
            {
                continue;
            }

            if (hit.distance < nearestDistance)
            {
                nearestDistance = hit.distance;
                foundObstacle = true;
            }
        }

        if (!foundObstacle)
        {
            return desiredPosition;
        }

        float safeDistance = Mathf.Max(nearestDistance - cameraCollisionPadding, 0.1f);
        return lookTarget + cameraDirection * safeDistance;
    }

    private void OnValidate()
    {
        cameraCollisionRadius = Mathf.Max(0.01f, cameraCollisionRadius);
        cameraCollisionPadding = Mathf.Max(0f, cameraCollisionPadding);
    }
}
