using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 4f;
    public float rotationSpeed = 12f;
    public float gravity = -25f;
    public float groundedStickForce = -2f;

    [Header("Safety")]
    public float maxAllowedYPosition = 5f;

    private CharacterController characterController;
    private Vector3 moveDirection;
    private float verticalVelocity;
    private float startingYPosition;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        startingYPosition = transform.position.y;
    }

    private void Update()
    {
        ReadMovementInput();
        ApplyGravity();
        MovePlayer();
        RotateTowardMovement();
        ClampUnexpectedHeight();
    }

    private void ReadMovementInput()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        Vector3 inputDirection = new Vector3(horizontalInput, 0f, verticalInput);

        if (inputDirection.sqrMagnitude > 1f)
        {
            inputDirection.Normalize();
        }

        moveDirection = new Vector3(
            inputDirection.x + inputDirection.z,
            0f,
            inputDirection.z - inputDirection.x
        ).normalized;
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
        Vector3 velocity = moveDirection * moveSpeed;
        velocity.y = verticalVelocity;

        characterController.Move(velocity * Time.deltaTime);
    }

    private void RotateTowardMovement()
    {
        if (moveDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            rotationSpeed * Time.deltaTime
        );
    }

    private void ClampUnexpectedHeight()
    {
        if (transform.position.y <= startingYPosition + maxAllowedYPosition)
        {
            return;
        }

        Vector3 safePosition = transform.position;
        safePosition.y = startingYPosition;
        transform.position = safePosition;
        verticalVelocity = 0f;
    }
}
