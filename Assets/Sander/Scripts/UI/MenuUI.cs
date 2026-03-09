using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class MenuUI : MonoBehaviour
{
    [Header("UI Canvases / GameObjects")]
    [SerializeField] private GameObject pauseMenu;           // Pause menu canvas
    public GameObject startMenu;                           // Start menu root GO
    public Button backButton;                              // Back button (in settings/pause)
    [SerializeField] private GameObject ingameUI;

    [Header("Input & Player References")]
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Multitool multitool;

    private bool visible = false;
    private bool backButtonPressed = false;
    private InputActionMap playerMap;

    void Start()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        ShowStartMenu();

        if (backButton != null)
        {
            backButton.onClick.AddListener(() => backButtonPressed = true);
        }

        // Find player input map and disable it until Play
        if (playerInputHandler != null && playerInputHandler.playerControls != null)
        {
            playerMap = playerInputHandler.playerControls.FindActionMap("Player");
            if (playerMap != null)
            {
                playerMap.Disable();
            }
        }

        // Auto-find multitool if not assigned
        if (multitool == null)
            multitool = FindObjectOfType<Multitool>();
    }

    void Update()
    {
        PauseMenuOn();
    }

    private void PauseMenuOn()
    {
        // Use old Input for pause toggle (ESC key)
        if (Input.GetKeyDown(KeyCode.Escape) || backButtonPressed)
        {
            backButtonPressed = false;
            visible = !visible;

            pauseMenu.gameObject.SetActive(visible);

            if (visible)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                Freeze();
                ingameUI.SetActive(false);  // Hide gameplay HUD if needed
            }
            else
            {
                CursorLockModeOn();
            }
        }
    }

    public void Resume()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        startMenu.SetActive(false);
        pauseMenu.gameObject.SetActive(false);
        ingameUI.SetActive(true);  // Show gameplay HUD
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

    public void Restart()
    {
        Time.timeScale = 1f;
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

        if (playerMap != null)
            playerMap.Enable();
    }
}