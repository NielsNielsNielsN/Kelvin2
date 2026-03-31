using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class SharedTimerSliders : MonoBehaviour
{
    [Header("Timer Settings")]
    [Tooltip("Total time in seconds")]
    [SerializeField] private float totalTime = 30f;

    [Tooltip("Start counting down automatically on scene load? (Disable this - let MenuUI call StartCountdown when play button is pressed)")]
    [SerializeField] private bool autoStart = false;

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
    [SerializeField] private Image fadeToBlackImage;
    [SerializeField] private float fadeDuration = 3f;
    [SerializeField] private GameObject gameOverCanvas;
    [SerializeField] private GameObject crosshairCanvas;

    [Header("Player Freeze References")]
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Multitool multitool;
    [SerializeField] private MenuUI menuUI;

    private float remainingTime;
    private bool isRunning;
    private bool isDead = false;
    private bool gameStarted = false;
    private InputActionMap playerMap;

    void Awake()
    {
        ResetTimer();

        if (startInvisible)
        {
            SetAllImagesAlpha(0f);
        }

        if (fadeToBlackImage != null)
        {
            Color c = fadeToBlackImage.color;
            c.a = 0f;
            fadeToBlackImage.color = c;
            fadeToBlackImage.gameObject.SetActive(true);
        }

        if (gameOverCanvas != null)
            gameOverCanvas.SetActive(false);

        if (playerInputHandler != null && playerInputHandler.playerControls != null)
        {
            playerMap = playerInputHandler.playerControls.FindActionMap("Player");
        }

        if (menuUI == null)
            menuUI = Object.FindFirstObjectByType<MenuUI>();
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
        if (isDead || !isRunning || !gameStarted) return;

        remainingTime -= Time.deltaTime;

        if (remainingTime <= 0f)
        {
            remainingTime = 0f;
            isRunning = false;
            TriggerDeathSequence();
            return;
        }

        UpdateVisuals();
    }

    public void StartCountdown()
    {
        gameStarted = true;
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
        gameStarted = false;

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

        UnfreezePlayer();
    }

    public void SetTotalTime(float newTime)
    {
        totalTime = Mathf.Max(0.1f, newTime);
        ResetTimer();
    }

    public void ModifyRemainingTime(float deltaSeconds)
    {
        if (!gameStarted) return;
        remainingTime += deltaSeconds;
        remainingTime = Mathf.Clamp(remainingTime, 0f, totalTime);
        UpdateVisuals();
    }

    // Getters
    public float GetRemainingTime() => remainingTime;
    public float GetTotalTime() => totalTime;
    public bool IsRunning() => isRunning;
    public bool IsFinished() => remainingTime <= 0f && !isRunning;
    public bool IsDead() => isDead;
    public GameObject GetGameOverCanvas() => gameOverCanvas;

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

        FreezePlayer();

        StartCoroutine(FadeToBlack());
    }

    private void FreezePlayer()
    {
        if (playerMap != null)
            playerMap.Disable();

        if (multitool != null)
            multitool.StopActive();
    }

    private void UnfreezePlayer()
    {
        if (playerMap != null)
            playerMap.Enable();
    }

    private IEnumerator FadeToBlack()
    {
        if (fadeToBlackImage == null) yield break;

        if (crosshairCanvas != null)
            crosshairCanvas.SetActive(false);

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

        fadeToBlackImage.color = targetColor;

        if (gameOverCanvas != null)
        {
            gameOverCanvas.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }

    // Editor helpers
    [ContextMenu("Start Countdown")]
    private void EditorStart() => StartCountdown();

    [ContextMenu("Pause Countdown")]
    private void EditorPause() => PauseCountdown();

    [ContextMenu("Reset to Full")]
    private void EditorReset() => ResetTimer();
}