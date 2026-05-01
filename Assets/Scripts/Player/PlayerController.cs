using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 5f;

    [Header("Rotation")]
    public bool rotateToMovement = true;
    public float rotationSpeed = 12f;

    private Rigidbody playerRigidbody;
    private Vector3 moveDirection;

    private void Awake()
    {
        playerRigidbody = GetComponent<Rigidbody>();
    }

    private void Update()
    {
        float horizontalInput = Input.GetAxisRaw("Horizontal");
        float verticalInput = Input.GetAxisRaw("Vertical");

        moveDirection = new Vector3(
            horizontalInput + verticalInput,
            0f,
            verticalInput - horizontalInput
        ).normalized;
    }

    private void FixedUpdate()
    {
        Vector3 nextPosition = playerRigidbody.position + moveDirection * moveSpeed * Time.fixedDeltaTime;
        playerRigidbody.MovePosition(nextPosition);

        if (rotateToMovement && moveDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
            Quaternion smoothRotation = Quaternion.Slerp(
                playerRigidbody.rotation,
                targetRotation,
                rotationSpeed * Time.fixedDeltaTime
            );

            playerRigidbody.MoveRotation(smoothRotation);
        }
    }
}
