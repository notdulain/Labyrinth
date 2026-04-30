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

    private CharacterController characterController;
    private float verticalVelocity;
    private float currentSpeedParam; // smoothed value sent to Animator

    private static readonly int SpeedHash    = Animator.StringToHash("Speed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int JumpHash     = Animator.StringToHash("Jump");

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    private void Update()
    {
        // ---- INPUT ----
        float h = Input.GetAxisRaw("Horizontal");   // A/D
        float v = Input.GetAxisRaw("Vertical");     // W/S
        Vector3 input = new Vector3(h, 0f, v);
        bool isMoving = input.sqrMagnitude > 0.01f;
        bool isRunning = Input.GetKey(KeyCode.LeftShift);

        // ---- MOVEMENT (camera-independent, world-space) ----
        Vector3 moveDir = Vector3.zero;
        if (isMoving) moveDir = input.normalized;

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
}