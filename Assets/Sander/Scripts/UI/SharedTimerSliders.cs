using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

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

    [Header("Frost / Freeze Images (fade in as timer runs out)")]
    [SerializeField] private Image image1;
    [Range(0f, 1f)][SerializeField] private float image1TargetAlpha = 1f;
    [SerializeField] private Image image2;
    [Range(0f, 1f)][SerializeField] private float image2TargetAlpha = 0.85f;
    [SerializeField] private Image image3;
    [Range(0f, 1f)][SerializeField] private float image3TargetAlpha = 0.7f;

    [Header("Fade Behavior")]
    [Tooltip("Should images start fully invisible (alpha 0) on reset?")]
    [SerializeField] private bool startInvisible = true;

    [Header("Death Sequence")]
    [SerializeField] private Image fadeToBlackImage;       // Full-screen black image (alpha 0 at start)
    [SerializeField] private float fadeDuration = 3f;      // How long to fade to black
    [SerializeField] private GameObject gameOverCanvas;    // "You Died" / Game Over screen

    [Header("Player Freeze References")]
    [SerializeField] private PlayerInputHandler playerInputHandler;  // Drag PlayerInputHandler here
    [SerializeField] private Multitool multitool;                    // Drag Multitool here

    // Runtime
    private float remainingTime;
    private bool isRunning;
    private bool isDead = false;
    private InputActionMap playerMap;

    void Awake()
    {
        ResetTimer();

        // Frost images start invisible if wanted
        if (startInvisible)
        {
            SetAllImagesAlpha(0f);
        }

        // Fade-to-black starts invisible
        if (fadeToBlackImage != null)
        {
            Color c = fadeToBlackImage.color;
            c.a = 0f;
            fadeToBlackImage.color = c;
            fadeToBlackImage.gameObject.SetActive(true);
        }

        // Game over canvas starts hidden
        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);

        // Cache player input map
        if (playerInputHandler != null && playerInputHandler.playerControls != null)
        {
            playerMap = playerInputHandler.playerControls.FindActionMap("Player"); // change "Player" if your map has different name
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
        if (isDead || !isRunning) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            TriggerDeathSequence();
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
        isDead = false;
        UpdateVisuals();

        if (startInvisible)
        {
            SetAllImagesAlpha(0f);
        }

        if (fadeToBlackImage != null)
        {
            Color c = fadeToBlackImage.color;
            c.a = 0f;
            fadeToBlackImage.color = c;
        }

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);
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
        float fadeProgress = 1f - Mathf.Clamp01(remainingTime / totalTime);

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

    private void TriggerDeathSequence()
    {
        isDead = true;

        // Freeze player input & actions
        FreezePlayer();

        // Start fade to black + frost already fading in via UpdateImageAlphas
        StartCoroutine(FadeToBlackAndShowGameOver());
    }

    private void FreezePlayer()
    {
        // Disable player input map
        if (playerMap != null)
        {
            playerMap.Disable();
        }

        // Force-stop multitool
        if (multitool != null)
        {
            multitool.StopActive();
        }

        // Optional: complete freeze (uncomment if you want NO animations/particles during fade)
        // Time.timeScale = 0f;
    }

    private IEnumerator FadeToBlackAndShowGameOver()
    {
        float elapsed = 0f;
        Color startColor = fadeToBlackImage.color;
        Color targetColor = startColor;
        targetColor.a = 1f;

        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / fadeDuration;
            fadeToBlackImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        // Fully black
        fadeToBlackImage.color = targetColor;

        // Show Game Over canvas
        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
        }
    }

    private void OnTimerFinished()
    {
        // This is now handled by TriggerDeathSequence()
        // You can leave it empty or add extra sound/effects here if wanted
    }

    // Editor helpers
    [ContextMenu("Start Countdown")]
    private void EditorStart() => StartCountdown();

    [ContextMenu("Pause Countdown")]
    private void EditorPause() => PauseCountdown();

    [ContextMenu("Reset to Full")]
    private void EditorReset() => ResetTimer();
}