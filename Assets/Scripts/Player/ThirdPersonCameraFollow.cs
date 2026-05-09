using UnityEngine;

public class ThirdPersonCameraFollow : MonoBehaviour
{
    [Header("Target")]
    public Transform target;
    public float distance = 5f;
    public float height = 2.5f;
    public float lookHeight = 1.4f;

    [Header("Smoothing")]
    public float smoothTime = 0.08f;
    public float rotationLerpSpeed = 15f;

    [Header("Collision")]
    public LayerMask obstacleLayers = ~0;
    public float collisionRadius = 0.25f;
    public float collisionOffset = 0.2f;
    public float minCameraDistance = 1.2f;

    [Header("Overhead Fallback")]
    public float overheadHeight = 7f;
    public float overheadBackOffset = 1f;
    public float overheadAngle = 70f;

    private Vector3 followVelocity;
    private bool usingOverheadView;

    private void Awake()
    {
        WarnAboutInvalidComponents();
    }

    private void LateUpdate()
    {
        if (target == null)
        {
            return;
        }

        Vector3 lookTarget = target.position + Vector3.up * lookHeight;
        Vector3 normalPosition = target.position - target.forward * distance + Vector3.up * height;
        Vector3 desiredPosition = GetCameraPosition(lookTarget, normalPosition);

        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPosition,
            ref followVelocity,
            smoothTime
        );

        RotateTowardLookTarget(lookTarget);
    }

    private Vector3 GetCameraPosition(Vector3 lookTarget, Vector3 normalPosition)
    {
        Vector3 safeThirdPersonPosition = GetPositionBeforeObstacle(lookTarget, normalPosition);
        float safeDistanceFromPlayer = Vector3.Distance(lookTarget, safeThirdPersonPosition);

        if (safeDistanceFromPlayer >= minCameraDistance)
        {
            usingOverheadView = false;
            return safeThirdPersonPosition;
        }

        usingOverheadView = true;
        return GetOverheadPosition();
    }

    private Vector3 GetPositionBeforeObstacle(Vector3 lookTarget, Vector3 desiredPosition)
    {
        Vector3 castDirection = desiredPosition - lookTarget;
        float castDistance = castDirection.magnitude;

        if (castDistance <= 0.001f)
        {
            return desiredPosition;
        }

        castDirection /= castDistance;

        RaycastHit[] hits = Physics.SphereCastAll(
            lookTarget,
            collisionRadius,
            castDirection,
            castDistance,
            obstacleLayers,
            QueryTriggerInteraction.Ignore);

        float nearestDistance = castDistance;
        bool foundObstacle = false;

        for (int i = 0; i < hits.Length; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform == target || hit.transform.IsChildOf(target))
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

        float safeDistance = Mathf.Max(nearestDistance - collisionOffset, 0.1f);
        return lookTarget + castDirection * safeDistance;
    }

    private Vector3 GetOverheadPosition()
    {
        return target.position + Vector3.up * overheadHeight - target.forward * overheadBackOffset;
    }

    private void RotateTowardLookTarget(Vector3 lookTarget)
    {
        Vector3 lookDirection = lookTarget - transform.position;
        if (lookDirection.sqrMagnitude <= 0.001f)
        {
            return;
        }

        Quaternion lookAtRotation = Quaternion.LookRotation(lookDirection, Vector3.up);
        Quaternion overheadRotation = Quaternion.Euler(overheadAngle, target.eulerAngles.y, 0f);
        Quaternion desiredRotation = usingOverheadView ? overheadRotation : lookAtRotation;

        transform.rotation = Quaternion.Lerp(
            transform.rotation,
            desiredRotation,
            rotationLerpSpeed * Time.deltaTime
        );
    }

    private void WarnAboutInvalidComponents()
    {
        if (GetComponent<Rigidbody>() != null)
        {
            Debug.LogWarning("Main Camera should not have a Rigidbody. Remove it for stable camera follow.", this);
        }

        if (GetComponent<Collider>() != null)
        {
            Debug.LogWarning("Main Camera should not have a Collider. Remove it for stable camera follow.", this);
        }
    }
}
