using UnityEngine;
using UnityEngine.UI;

public class SharedTimerSliders : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Total time in seconds")]
    [SerializeField] private float totalTime = 30f;

    [Tooltip("Start counting down automatically on scene load?")]
    [SerializeField] private bool autoStart = true;

    [Header("Sliders (decrease from max to min)")]
    [SerializeField] private Slider slider1;
    [SerializeField] private Slider slider2;

    [Header("Images to Fade In - each with its own target alpha")]
    [SerializeField] private Image image1;
    [Range(0f, 1f)][SerializeField] private float image1TargetAlpha = 1f;

    [SerializeField] private Image image2;
    [Range(0f, 1f)][SerializeField] private float image2TargetAlpha = 0.85f;

    [SerializeField] private Image image3;
    [Range(0f, 1f)][SerializeField] private float image3TargetAlpha = 0.7f;

    [Header("Fade Behavior")]
    [Tooltip("Should images start fully invisible (alpha 0) on reset?")]
    [SerializeField] private bool startInvisible = true;

    // Runtime
    private float remainingTime;
    private bool isRunning;

    void Awake()
    {
        ResetTimer();

        if (startInvisible)
        {
            SetAllImagesAlpha(0f);
        }
    }

    void Start()
    {
        if (autoStart)
        {
            StartCountdown();
        }
    }

    void Update()
    {
        if (!isRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            OnTimerFinished();
        }

        UpdateVisuals();
    }

    // ────────────────────────────────────────────────
    // Public control methods
    // ────────────────────────────────────────────────

    public void StartCountdown()
    {
        isRunning = true;
    }

    public void PauseCountdown()
    {
        isRunning = false;
    }

    public void ResetTimer()
    {
        remainingTime = totalTime;
        isRunning = false;
        UpdateVisuals();

        if (startInvisible)
        {
            SetAllImagesAlpha(0f);
        }
    }

    public void SetTotalTime(float newTime)
    {
        totalTime = Mathf.Max(0.1f, newTime);
        ResetTimer();
    }

    public void ModifyRemainingTime(float deltaSeconds)
    {
        remainingTime += deltaSeconds;
        remainingTime = Mathf.Clamp(remainingTime, 0f, totalTime);
        UpdateVisuals();
    }

    // Getters
    public float GetRemainingTime() => remainingTime;
    public float GetTotalTime() => totalTime;
    public bool IsRunning() => isRunning;
    public bool IsFinished() => remainingTime <= 0f && !isRunning;

    // ────────────────────────────────────────────────
    // Internal
    // ────────────────────────────────────────────────

    private void UpdateVisuals()
    {
        UpdateSliders();
        UpdateImageAlphas();
    }

    private void UpdateSliders()
    {
        if (slider1 == null && slider2 == null) return;

        float progress = Mathf.Clamp01(remainingTime / totalTime);
        float sliderValue = progress * (slider1?.maxValue ?? 1f - slider1?.minValue ?? 0f) + (slider1?.minValue ?? 0f);

        if (slider1 != null) slider1.value = sliderValue;
        if (slider2 != null) slider2.value = sliderValue;
    }

    private void UpdateImageAlphas()
    {
        // fadeProgress: 0 = full time left → 1 = time up
        float fadeProgress = 1f - Mathf.Clamp01(remainingTime / totalTime);

        // Each image reaches its own target alpha
        SetImageAlpha(image1, fadeProgress * image1TargetAlpha);
        SetImageAlpha(image2, fadeProgress * image2TargetAlpha);
        SetImageAlpha(image3, fadeProgress * image3TargetAlpha);
    }

    private void SetImageAlpha(Image img, float alpha)
    {
        if (img == null) return;

        Color c = img.color;
        c.a = Mathf.Clamp01(alpha);
        img.color = c;
    }

    private void SetAllImagesAlpha(float alpha)
    {
        SetImageAlpha(image1, alpha);
        SetImageAlpha(image2, alpha);
        SetImageAlpha(image3, alpha);
    }

    private void OnTimerFinished()
    {
        Debug.Log("Timer reached zero!");
        // → Add your custom logic here (sound, event, etc.)
    }

    // Editor helpers
    [ContextMenu("Start Countdown")]
    private void EditorStart() => StartCountdown();

    [ContextMenu("Pause Countdown")]
    private void EditorPause() => PauseCountdown();

    [ContextMenu("Reset to Full")]
    private void EditorReset() => ResetTimer();
}