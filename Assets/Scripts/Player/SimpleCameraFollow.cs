using UnityEngine;

public class SimpleCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public Vector3 followOffset = new Vector3(0f, 6f, -8f);
    public Vector3 lookAtOffset = new Vector3(0f, 1.4f, 0f);

    [Header("Smoothing")]
    public float smoothTime = 0.12f;
    public float rotationLerpSpeed = 12f;

    [Header("Collision")]
    public bool preventClipping = true;
    public float cameraRadius = 0.3f;
    public float cameraCollisionPadding = 0.15f;
    public LayerMask collisionMask = ~0;

    private Vector3 followVelocity;

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookTarget = target.position + lookAtOffset;
        Vector3 desiredPosition = target.position + followOffset;

        if (preventClipping)
        {
            desiredPosition = GetCollisionSafePosition(lookTarget, desiredPosition);
        }

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            smoothTime
        );

        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.sqrMagnitude > 0.001f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
            transform.rotation = Quaternion.Lerp(
                transform.rotation,
                targetRotation,
                rotationLerpSpeed * Time.deltaTime
            );
        }
    }

    private Vector3 GetCollisionSafePosition(Vector3 lookTarget, Vector3 desiredPosition)
    {
        Vector3 cameraDirection = desiredPosition - lookTarget;
        float cameraDistance = cameraDirection.magnitude;

        if (cameraDistance <= 0.001f)
        {
            return desiredPosition;
        }

        cameraDirection /= cameraDistance;

        if (Physics.SphereCast(
            lookTarget,
            cameraRadius,
            cameraDirection,
            out RaycastHit hit,
            cameraDistance,
            collisionMask,
            QueryTriggerInteraction.Ignore))
        {
            float safeDistance = Mathf.Max(hit.distance - cameraCollisionPadding, 0.1f);
            return lookTarget + cameraDirection * safeDistance;
        }

        return desiredPosition;
    }
}
