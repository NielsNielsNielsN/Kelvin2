using UnityEngine;

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

    private float targetIntensity;
    private float timer;

    void Start()
    {
        if (targetLight == null)
            targetLight = GetComponent<Light>();

        // Pick an initial target
        targetIntensity = Random.Range(minIntensity, maxIntensity);

        if (targetLight != null)
            targetLight.intensity = targetIntensity;
    }

    void Update()
    {
        if (targetLight == null) return;

        // Smoothly move towards the target intensity
        targetLight.intensity = Mathf.Lerp(targetLight.intensity, targetIntensity, transitionSpeed * Time.deltaTime);

        // Count up and pick a new random target when the interval is reached
        timer += Time.deltaTime;
        if (timer >= interval)
        {
            timer = 0f;
            targetIntensity = Random.Range(minIntensity, maxIntensity);
        }
    }
}
