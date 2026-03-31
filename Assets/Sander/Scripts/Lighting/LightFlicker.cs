using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class LightFlicker : MonoBehaviour
{
    [Header("Light Settings")]
    public Light targetLight;
    public float lowIntensity = 0.5f;
    public float highIntensity = 3f;

    [Header("Blink Settings")]
    public float blinkSpeed = 1f; // seconds per switch

    private float timer;
    private bool isHigh;

    void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        targetLight.intensity = lowIntensity;
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (timer >= blinkSpeed)
        {
            timer = 0f;
            isHigh = !isHigh;

            targetLight.intensity = isHigh ? highIntensity : lowIntensity;
        }
    }
}