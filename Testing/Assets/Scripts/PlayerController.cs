using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float moveSpeed = 5f;

    private void Update()
    {
        float horizontalInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");

        Vector3 moveDirection = new Vector3(
            horizontalInput + verticalInput,
            0f,
            verticalInput - horizontalInput
        );

        moveDirection = moveDirection.normalized;

        transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
    }
}
