using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour, IPlayerMotionResettable
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 12f;
    public float moveAfterTurnAngle = 2f;

    [Header("Gravity")]
    public float gravity = -25f;
    public float groundedStickForce = -2f;

    [Header("Camera")]
    public Transform movementCamera;

    [Header("Obstacle Checks")]
    public LayerMask obstacleLayers = ~0;
    public float wallCheckDistance = 0.08f;
    public bool warnAboutObstacleRenderersWithoutColliders = true;

    [Header("Safety")]
    public float maxUnexpectedHeightChange = 5f;

    private CharacterController characterController;
    private Vector3 moveDirection;
    private bool canMoveHorizontally = true;
    private float verticalVelocity;
    private float startingYPosition;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        startingYPosition = transform.position.y;

        if (movementCamera == null && Camera.main != null)
        {
            movementCamera = Camera.main.transform;
        }

        WarnAboutInvalidComponents();
        WarnAboutObstacleRenderersWithoutColliders();
    }

    private void Update()
    {
        ReadCameraRelativeInput();
        ApplyGravity();
        RotateTowardMovement();
        MovePlayer();
        ClampUnexpectedHeight();
    }

    private void ReadCameraRelativeInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 input = new Vector3(horizontalInput, 0f, verticalInput);
        if (input.sqrMagnitude > 1f)
        {
            input.Normalize();
        }

        Vector3 cameraForward = Vector3.forward;
        Vector3 cameraRight = Vector3.right;

        if (movementCamera != null)
        {
            cameraForward = movementCamera.forward;
            cameraRight = movementCamera.right;
        }

        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        moveDirection = cameraForward * input.z + cameraRight * input.x;
        if (moveDirection.sqrMagnitude > 1f)
        {
            moveDirection.Normalize();
        }
    }

    private void ApplyGravity()
    {
        if (characterController.isGrounded && verticalVelocity < 0f)
        {
            verticalVelocity = groundedStickForce;
        }

        verticalVelocity += gravity * Time.deltaTime;
    }

    private void MovePlayer()
    {
        Vector3 requestedHorizontalVelocity = canMoveHorizontally ? moveDirection * moveSpeed : Vector3.zero;
        Vector3 horizontalVelocity = GetWallSafeHorizontalVelocity(requestedHorizontalVelocity);
        Vector3 velocity = horizontalVelocity;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private Vector3 GetWallSafeHorizontalVelocity(Vector3 requestedVelocity)
    {
        Vector3 requestedMove = requestedVelocity * Time.deltaTime;
        requestedMove.y = 0f;

        if (requestedMove.sqrMagnitude <= 0.000001f)
        {
            return Vector3.zero;
        }

        Vector3 moveDirectionToCheck = requestedMove.normalized;
        float moveDistance = requestedMove.magnitude + wallCheckDistance;

        if (!CastPlayerCapsule(moveDirectionToCheck, moveDistance, out RaycastHit hit))
        {
            return requestedVelocity;
        }

        Vector3 slideDirection = Vector3.ProjectOnPlane(requestedVelocity, hit.normal);
        slideDirection.y = 0f;

        if (slideDirection.sqrMagnitude <= 0.000001f)
        {
            return Vector3.zero;
        }

        Vector3 slideMoveDirection = slideDirection.normalized;
        float slideDistance = requestedMove.magnitude + wallCheckDistance;

        if (CastPlayerCapsule(slideMoveDirection, slideDistance, out _))
        {
            return Vector3.zero;
        }

        return slideDirection.normalized * requestedVelocity.magnitude;
    }

    private bool CastPlayerCapsule(Vector3 direction, float distance, out RaycastHit hit)
    {
        GetCapsuleCastPoints(out Vector3 bottom, out Vector3 top, out float radius);

        return Physics.CapsuleCast(
            bottom,
            top,
            radius,
            direction,
            out hit,
            distance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);
    }

    private void GetCapsuleCastPoints(out Vector3 bottom, out Vector3 top, out float radius)
    {
        radius = Mathf.Max(characterController.radius - characterController.skinWidth, 0.01f);
        float halfHeight = Mathf.Max(characterController.height * 0.5f - radius, 0.01f);
        Vector3 center = transform.TransformPoint(characterController.center);

        bottom = center + Vector3.down * halfHeight;
        top = center + Vector3.up * halfHeight;
    }

    private void RotateTowardMovement()
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            canMoveHorizontally = true;
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            rotationSpeed * 90f * Time.deltaTime);

        canMoveHorizontally = Quaternion.Angle(transform.rotation, targetRotation) <= moveAfterTurnAngle;
    }

    private void ClampUnexpectedHeight()
    {
        if (transform.position.y <= startingYPosition + maxUnexpectedHeightChange)
        {
            return;
        }

        Vector3 safePosition = transform.position;
        safePosition.y = startingYPosition;
        transform.position = safePosition;
        verticalVelocity = 0f;

        Debug.LogWarning("Player Y position jumped unexpectedly. Resetting height for safety.", this);
    }

    public void ResetMotionState()
    {
        moveDirection = Vector3.zero;
        canMoveHorizontally = true;
        verticalVelocity = 0f;
        startingYPosition = transform.position.y;
    }

    private void WarnAboutInvalidComponents()
    {
        if (GetComponent<Rigidbody>() != null)
        {
            Debug.LogWarning("Remove Rigidbody from Player. This controller uses CharacterController.", this);
        }

        if (GetComponent<CapsuleCollider>() != null)
        {
            Debug.LogWarning("Remove Capsule Collider from Player. CharacterController already provides collision shape.", this);
        }

        if (GetComponent("NavMeshAgent") != null)
        {
            Debug.LogWarning("Remove NavMeshAgent from Player. This controller uses direct player input.", this);
        }
    }

    private void WarnAboutObstacleRenderersWithoutColliders()
    {
        if (!warnAboutObstacleRenderersWithoutColliders)
        {
            return;
        }

        Renderer[] renderers = FindObjectsByType<Renderer>(FindObjectsSortMode.None);
        for (int i = 0; i < renderers.Length; i++)
        {
            Renderer obstacleRenderer = renderers[i];
            GameObject obstacleObject = obstacleRenderer.gameObject;

            if ((obstacleLayers.value & (1 << obstacleObject.layer)) == 0)
            {
                continue;
            }

            if (obstacleObject.GetComponentInParent<Collider>() == null &&
                obstacleObject.GetComponentInChildren<Collider>() == null)
            {
                Debug.LogWarning(
                    $"Obstacle layer object '{obstacleObject.name}' has a renderer but no 3D Collider. Add a Box Collider or Mesh Collider with Is Trigger off.",
                    obstacleObject);
            }
        }
    }
}
