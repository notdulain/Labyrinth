using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(CharacterController))]
public class PlayerWorldBounds : MonoBehaviour
{
    [Header("Bounds")]
    [SerializeField] private Collider boundsSource;
    [SerializeField] private float edgePadding = 0.05f;

    [Header("Respawn")]
    [SerializeField] private Transform respawnPoint;
    [SerializeField] private float fallDistanceBelowBounds = 5f;

    private const float DefaultGroundOffset = 0.05f;

    private CharacterController characterController;

    private void Reset()
    {
        characterController = GetComponent<CharacterController>();
        ResolveDefaultReferences();
    }

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        ResolveDefaultReferences();
    }

    private void LateUpdate()
    {
        if (boundsSource == null)
        {
            return;
        }

        Bounds worldBounds = boundsSource.bounds;
        if (transform.position.y < worldBounds.min.y - fallDistanceBelowBounds)
        {
            Respawn(worldBounds);
            return;
        }

        ClampToWorldBounds(worldBounds);
    }

    private void ClampToWorldBounds(Bounds worldBounds)
    {
        Vector3 clampedPosition = GetClampedPosition(transform.position, worldBounds);
        if ((clampedPosition - transform.position).sqrMagnitude <= 0.000001f)
        {
            return;
        }

        SetControllerPosition(clampedPosition);
    }

    private Vector3 GetClampedPosition(Vector3 position, Bounds worldBounds)
    {
        float inset = edgePadding;
        if (characterController != null)
        {
            inset += characterController.radius;
        }

        float minX = worldBounds.min.x + inset;
        float maxX = worldBounds.max.x - inset;
        float minZ = worldBounds.min.z + inset;
        float maxZ = worldBounds.max.z - inset;

        if (minX > maxX)
        {
            minX = maxX = worldBounds.center.x;
        }

        if (minZ > maxZ)
        {
            minZ = maxZ = worldBounds.center.z;
        }

        position.x = Mathf.Clamp(position.x, minX, maxX);
        position.z = Mathf.Clamp(position.z, minZ, maxZ);
        return position;
    }

    private void Respawn(Bounds worldBounds)
    {
        Vector3 targetPosition = respawnPoint != null
            ? respawnPoint.position
            : new Vector3(worldBounds.center.x, worldBounds.max.y + DefaultGroundOffset, worldBounds.center.z);

        Quaternion targetRotation = respawnPoint != null
            ? respawnPoint.rotation
            : transform.rotation;

        targetPosition = GetClampedPosition(targetPosition, worldBounds);
        SetControllerPose(targetPosition, targetRotation);
        ResetMotionState();
    }

    private void SetControllerPosition(Vector3 position)
    {
        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled)
        {
            characterController.enabled = false;
        }

        transform.position = position;

        if (wasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private void SetControllerPose(Vector3 position, Quaternion rotation)
    {
        bool wasEnabled = characterController != null && characterController.enabled;
        if (wasEnabled)
        {
            characterController.enabled = false;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (wasEnabled)
        {
            characterController.enabled = true;
        }
    }

    private void ResetMotionState()
    {
        MonoBehaviour[] components = GetComponents<MonoBehaviour>();
        for (int i = 0; i < components.Length; i++)
        {
            if (components[i] is IPlayerMotionResettable resettable)
            {
                resettable.ResetMotionState();
            }
        }
    }

    private void ResolveDefaultReferences()
    {
        if (boundsSource == null)
        {
            GameObject defaultBoundsSource = GameObject.Find("DungeonMaze/DungeonFloor_WideMaze");
            if (defaultBoundsSource != null)
            {
                boundsSource = defaultBoundsSource.GetComponent<Collider>();
            }
        }

        if (respawnPoint == null)
        {
            GameObject spawn = GameObject.FindWithTag("PlayerSpawn");
            if (spawn == null)
            {
                spawn = GameObject.Find("PlayerSpawn");
            }

            if (spawn != null)
            {
                respawnPoint = spawn.transform;
            }
        }
    }

    private void OnValidate()
    {
        edgePadding = Mathf.Max(0f, edgePadding);
        fallDistanceBelowBounds = Mathf.Max(0f, fallDistanceBelowBounds);
    }
}
