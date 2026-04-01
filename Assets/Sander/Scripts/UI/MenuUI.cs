using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [Header("UI Canvases / GameObjects")]
    [SerializeField] private GameObject pauseMenu;           // Pause menu canvas
    [SerializeField] private GameObject crosshairCanvas;     // Crosshair canvas (disabled on pause)
    public GameObject startMenu;                             // Start menu root GO
    public GameObject settingsMenu;                          // Settings menu root GO
    public Button backButton;                                // Back button (in settings/pause)
    [SerializeField] private GameObject ingameUI;            // In-game HUD/UI

    [Header("Cameras")]
    [SerializeField] private Camera startCamera;             // Special camera for start menu angle
    [SerializeField] private Camera mainGameCamera;          // Regular gameplay camera

    [Header("Input & Player References")]
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Multitool multitool;
    [SerializeField] private SharedTimerSliders timerSliders;  // Reference to timer to start countdown on play
    [SerializeField] private AudioManager audioManager;         // Reference to AudioManager

    [Header("Gameplay Objects to Activate on Play")]
    [SerializeField] private GameObject[] gameplayObjectsToActivate;

    private bool visible = false;
    private bool backButtonPressed = false;
    private InputActionMap playerMap;

    private static bool skipStartMenuOnLoad = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        ShowStartMenu();

        if (backButton != null)
        {
            backButton.onClick.AddListener(() => backButtonPressed = true);
        }

        if (playerInputHandler != null && playerInputHandler.playerControls != null)
        {
            playerMap = playerInputHandler.playerControls.FindActionMap("Player");
            if (playerMap != null)
            {
                playerMap.Disable();
            }
        }

        if (multitool == null)
            multitool = Object.FindFirstObjectByType<Multitool>();

        if (timerSliders == null)
            timerSliders = Object.FindFirstObjectByType<SharedTimerSliders>();

        if (audioManager == null)
            audioManager = Object.FindFirstObjectByType<AudioManager>();

        ingameUI.SetActive(false);
        pauseMenu.SetActive(false);

        // Ensure start camera is active at scene start
        if (startCamera != null && mainGameCamera != null)
        {
            startCamera.enabled = true;
            mainGameCamera.enabled = false;
        }

        if (skipStartMenuOnLoad)
        {
            skipStartMenuOnLoad = false;
            startMenu.SetActive(false);
            OnPlayButtonPressed();
        }
    }

    void Update()
    {
        PauseMenuOn();
    }

    private void PauseMenuOn()
    {
        // Don't allow pause while the start menu is visible
        if (startMenu != null && startMenu.activeSelf)
            return;

        // Don't allow pause if game over canvas is active (game is in death sequence)
        if (IsGameOverActive())
            return;

        // Don't allow pause during a cutscene
        if (CutsceneController.IsCutscenePlaying)
            return;

        // ESC now toggles pause fully (open or close)
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            TogglePause();
        }

        // Back button now also toggles pause (same as ESC)
        if (backButtonPressed)
        {
            backButtonPressed = false;
            TogglePause();  // ← Changed: uses TogglePause instead of Resume
        }
    }

    private bool IsGameOverActive()
    {
        // Check if any game over canvas is active in the scene
        SharedTimerSliders timerSlider = Object.FindFirstObjectByType<SharedTimerSliders>();
        if (timerSlider != null)
        {
            return timerSlider.IsDead();
        }
        return false;
    }

    public void TogglePause()
    {
        if (startMenu != null && startMenu.activeSelf)
            return;

        visible = !visible;
        pauseMenu.gameObject.SetActive(visible);

        if (visible)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Freeze();
            ingameUI.SetActive(false);
            if (crosshairCanvas != null)
                crosshairCanvas.SetActive(false);
            // Pause the timer when pause menu opens
            if (timerSliders != null)
                timerSliders.PauseCountdown();
            if (audioManager != null)
                audioManager.PauseAudio();
        }
        else
        {
            CursorLockModeOn();
            ingameUI.SetActive(true);
            if (crosshairCanvas != null)
                crosshairCanvas.SetActive(true);
            // Resume the timer when pause menu closes
            if (timerSliders != null)
                timerSliders.StartCountdown();
            if (audioManager != null)
                audioManager.ResumeAudio();
        }

    }

    public void Resume()
    {
        // Force close if open (same as ESC when pause is on)
        if (visible)
        {
            TogglePause();
        }
        // Optional extra resume actions
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        startMenu.SetActive(false);
        pauseMenu.gameObject.SetActive(false);
        ingameUI.SetActive(true);
        Unfreeze();
    }

    public void Quit()
    {
        Application.Quit();
    }

    public void ShowStartMenu()
    {
        startMenu.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void ShowSettingsMenu()
    {
        startMenu.SetActive(false);
        settingsMenu.SetActive(true);
    }

    public void Restart()
    {
        Time.timeScale = 1f;
        
        // Deactivate win canvas if it's active
        ObjectiveManager objectiveManager = Object.FindFirstObjectByType<ObjectiveManager>();
        if (objectiveManager != null && objectiveManager.GetWinCanvas() != null)
        {
            objectiveManager.GetWinCanvas().SetActive(false);
        }
        
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void Retry()
    {
        Time.timeScale = 1f;
        skipStartMenuOnLoad = true;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void CursorLockModeOn()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        Unfreeze();
    }

    public void Freeze()
    {
        Time.timeScale = 0f;
        if (playerMap != null)
            playerMap.Disable();
        if (multitool != null)
            multitool.StopActive();
    }

    public void Unfreeze()
    {
        Time.timeScale = 1f;
        if (playerMap != null)
            playerMap.Enable();
    }

    public void OnPlayButtonPressed()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        startMenu.SetActive(false);
        ingameUI.SetActive(true);
        if (crosshairCanvas != null)
            crosshairCanvas.SetActive(true);

        if (gameplayObjectsToActivate != null)
        {
            foreach (GameObject go in gameplayObjectsToActivate)
            {
                if (go != null)
                    go.SetActive(true);
            }
        }

        if (playerMap != null)
            playerMap.Enable();

        if (startCamera != null && mainGameCamera != null)
        {
            startCamera.enabled = false;
            mainGameCamera.enabled = true;
        }

        // Start the timer countdown when play button is pressed
        if (timerSliders != null)
            timerSliders.StartCountdown();

        // Trigger mission briefing audio
        if (audioManager != null)
            audioManager.OnPlayPressed();
    }

    public void OnQuitButtonPressed()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void LateUpdate()
    {
        // Enforce unlocked cursor + disabled input while start menu is visible
        if (startMenu != null && startMenu.activeSelf)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;

            if (playerMap != null && playerMap.enabled)
                playerMap.Disable();

            if (ingameUI != null && ingameUI.activeSelf)
                ingameUI.SetActive(false);
        }
    }
}