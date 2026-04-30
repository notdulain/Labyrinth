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
public class SimpleThirdPersonController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float walkSpeed = 2.5f;
    [SerializeField] private float runSpeed = 5f;
    [SerializeField] private float rotationSpeed = 12f;
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

    private CharacterController characterController;
    private float verticalVelocity;
    private float currentSpeedParam; // smoothed value sent to Animator
    private float cameraYaw;
    private float cameraPitch = 18f;

    private static readonly int SpeedHash    = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash     = Animator.StringToHash("Jump");

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
            verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null) animator.SetTrigger(JumpHash);
        }

        verticalVelocity += gravity * Time.deltaTime;

        // ---- APPLY MOVEMENT ----
        Vector3 velocity = moveDir * speed;
        velocity.y = verticalVelocity;
        characterController.Move(velocity * Time.deltaTime);

        // ---- ROTATE TO FACE MOVEMENT ----
        if (isMoving)
        {
            Quaternion target = Quaternion.LookRotation(moveDir, Vector3.up);
            transform.rotation = Quaternion.Slerp(transform.rotation, target, rotationSpeed * Time.deltaTime);
        }

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

        followCamera.transform.position = cameraPosition;
        followCamera.transform.rotation = Quaternion.LookRotation(lookTarget - followCamera.transform.position, Vector3.up);
        followCamera.orthographic = false;
        followCamera.fieldOfView = cameraFieldOfView;
    }
}
