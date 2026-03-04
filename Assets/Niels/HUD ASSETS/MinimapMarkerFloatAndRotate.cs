using UnityEngine;

public class MinimapMarkerFloatRotateBlink : MonoBehaviour
{
    [Header("Floating Settings")]
    public float floatHeight = 0.5f;
    public float floatSpeed = 1f;

    [Header("Rotation Settings")]
    public float rotationSpeed = 50f;

    [Header("Emission Pulse (HDRP Lit)")]
    public Renderer targetRenderer;
    public float blinkSpeed = 2f;
    public float minIntensity = 18f;
    public float maxIntensity = 22f;

    private Vector3 startPosition;
    private Material materialInstance;
    private Color baseEmissiveColor;

    void Start()
    {
        startPosition = transform.position;

        if (targetRenderer != null)
        {
            materialInstance = targetRenderer.material;

            // Cache original emissive color
            baseEmissiveColor = materialInstance.GetColor("_EmissiveColor");
        }
    }

    void Update()
    {
        // Floating
        float newY = startPosition.y + Mathf.Sin(Time.time * floatSpeed) * floatHeight;
        transform.position = new Vector3(startPosition.x, newY, startPosition.z);

        // Rotation
        transform.Rotate(Vector3.up * rotationSpeed * Time.deltaTime);

        // Emission pulse
        if (materialInstance != null)
        {
            float intensity = Mathf.Lerp(
                minIntensity,
                maxIntensity,
                (Mathf.Sin(Time.time * blinkSpeed) + 1f) / 2f
            );

            materialInstance.SetColor("_EmissiveColor", baseEmissiveColor * intensity);
        }
    }
}
