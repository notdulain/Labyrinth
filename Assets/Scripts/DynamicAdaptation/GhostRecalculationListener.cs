using UnityEngine;

/// <summary>
/// Lightweight placeholder listener for future ghost AI path recalculation.
/// </summary>
public class GhostRecalculationListener : MonoBehaviour
{
    /// <summary>
    /// Subscribes to graph recalculation requests while this component is active.
    /// </summary>
    private void OnEnable()
    {
        WallEventInterceptor.OnRecalculationRequired += RecalculatePath;
    }

    /// <summary>
    /// Unsubscribes from graph recalculation requests when this component is disabled.
    /// </summary>
    private void OnDisable()
    {
        WallEventInterceptor.OnRecalculationRequired -= RecalculatePath;
    }

    /// <summary>
    /// Placeholder integration point for future ghost path recalculation logic.
    /// </summary>
    public void RecalculatePath()
    {
        Debug.Log("Ghost path recalculation requested");
    }
}
