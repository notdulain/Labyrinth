using UnityEngine;

/// <summary>
/// Animates a single guide dot with gentle bobbing and pulsing.
/// </summary>
public class GuideDot : MonoBehaviour
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly MaterialPropertyBlock propertyBlock = new MaterialPropertyBlock();

    private Renderer[] cachedRenderers;
    private Vector3 anchorPosition;
    private float baseScale = 0.1f;
    private float animationSpeed = 2f;
    private float pulseAmount = 0.08f;
    private float bobAmount = 0.03f;
    private int trailIndex;

    private void Awake()
    {
        cachedRenderers = GetComponentsInChildren<Renderer>(true);
        anchorPosition = transform.position;
    }

    public void Configure(float scale, float newAnimationSpeed, float newPulseAmount, float newBobAmount, int index)
    {
        baseScale = Mathf.Max(0.001f, scale);
        animationSpeed = Mathf.Max(0.01f, newAnimationSpeed);
        pulseAmount = Mathf.Max(0f, newPulseAmount);
        bobAmount = Mathf.Max(0f, newBobAmount);
        trailIndex = index;
    }

    public void SetAnchorPosition(Vector3 worldPosition)
    {
        anchorPosition = worldPosition;
        transform.position = anchorPosition;
    }

    private void Update()
    {
        float time = Time.time * animationSpeed + trailIndex * 0.35f;
        float bobOffset = Mathf.Sin(time) * bobAmount;
        float pulse = 1f + Mathf.Sin(time + 1.2f) * pulseAmount;

        transform.position = anchorPosition + Vector3.up * bobOffset;
        transform.localScale = Vector3.one * (baseScale * pulse);

        UpdateColorPulse(time);
    }

    private void UpdateColorPulse(float time)
    {
        if (cachedRenderers == null || cachedRenderers.Length == 0)
        {
            return;
        }

        float brightness = 0.75f + (Mathf.Sin(time + 0.8f) * 0.5f + 0.5f) * 0.25f;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            Renderer currentRenderer = cachedRenderers[i];
            if (currentRenderer == null || currentRenderer.sharedMaterial == null)
            {
                continue;
            }

            Material sharedMaterial = currentRenderer.sharedMaterial;
            int colorPropertyId;

            if (sharedMaterial.HasProperty(BaseColorId))
            {
                colorPropertyId = BaseColorId;
            }
            else if (sharedMaterial.HasProperty(ColorId))
            {
                colorPropertyId = ColorId;
            }
            else
            {
                continue;
            }

            Color baseColor = sharedMaterial.GetColor(colorPropertyId);
            Color pulsedColor = baseColor * brightness;
            pulsedColor.a = baseColor.a;

            currentRenderer.GetPropertyBlock(propertyBlock);
            propertyBlock.SetColor(colorPropertyId, pulsedColor);
            currentRenderer.SetPropertyBlock(propertyBlock);
        }
    }
}
