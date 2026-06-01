using UnityEngine;

/// <summary>
/// Place on the centre atelier object in each level. When the player enters the
/// trigger volume the GameManager advances to the next stage. Fires exactly once.
/// </summary>
[RequireComponent(typeof(Collider))]
public class AtelierTrigger : MonoBehaviour
{
    [Tooltip("Tag the trigger looks for. If the Player object isn't tagged, the name fallback below is used.")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("Fallback exact name to match if the tag isn't set on the Player.")]
    [SerializeField] private string playerNameFallback = "Player";

    private bool fired;

    private void Reset()
    {
        var col = GetComponent<Collider>();
        if (col != null) col.isTrigger = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (fired) return;
        if (!IsPlayer(other)) return;
        fired = true;
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Advance();
        }
        else
        {
            Debug.LogWarning("[AtelierTrigger] No GameManager.Instance available.");
        }
    }

    private bool IsPlayer(Collider other)
    {
        try
        {
            if (other.CompareTag(playerTag)) return true;
        }
        catch (UnityException)
        {
            // Tag may not be defined in this scene.
        }
        return other.gameObject.name == playerNameFallback ||
               other.transform.root.name == playerNameFallback;
    }
}
