using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

public class LightFlicker : MonoBehaviour
{
    [Header("Light Settings")]
    [SerializeField] private Light targetLight;

    [Header("Intensity Range")]
    [SerializeField] private float minIntensity = 0.5f;
    [SerializeField] private float maxIntensity = 2f;

    [Header("Timing")]
    [Tooltip("Seconds between each intensity target change")]
    [SerializeField] private float interval = 0.15f;
    [Tooltip("Speed at which intensity smoothly lerps to the new target")]
    [SerializeField] private float transitionSpeed = 5f;

    private HDAdditionalLightData hdLight;
    private float targetIntensity;
    private float currentIntensity;
    private float timer;

    void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        if (targetLight != null)
            hdLight = targetLight.GetComponent<HDAdditionalLightData>();

        targetIntensity = Random.Range(minIntensity, maxIntensity);
        currentIntensity = targetIntensity;
        ApplyIntensity(currentIntensity);
    }

    void Update()
    {
        if (targetLight == null) return;

        // Smoothly move towards the target intensity
        currentIntensity = Mathf.Lerp(currentIntensity, targetIntensity, transitionSpeed * Time.deltaTime);
        ApplyIntensity(currentIntensity);

        // Pick a new random target when the interval is reached
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            targetIntensity = Random.Range(minIntensity, maxIntensity);
        }
    }

    private void ApplyIntensity(float value)
    {
        if (hdLight != null)
            hdLight.SetIntensity(value);
        else if (targetLight != null)
            targetLight.intensity = value;
    }
}

