using UnityEngine;
using UnityEngine.InputSystem;

public class MenuUI : MonoBehaviour
{
    [Header("UI GameObjects")]
    [SerializeField] private GameObject ingameUI;
    [SerializeField] private GameObject pauseMenu;

    [Header("Input & Player")]
    [SerializeField] private PlayerInputHandler playerInputHandler;  // Drag your PlayerInputHandler here
    [SerializeField] private Multitool multitool;  // Drag your Multitool here (forces beam off)

    private bool isPaused = false;
    private InputActionMap playerMap;

    void Start()
    {
        // Auto-find if not assigned
        if (playerInputHandler == null)
            playerInputHandler = FindObjectOfType<PlayerInputHandler>();

        if (multitool == null)
            multitool = FindAnyObjectByType<Multitool>();

        // Cache the player action map
        if (playerInputHandler != null)
            playerMap = playerInputHandler.playerControls.FindActionMap("Player");  // Your action map name
    }

    void Update()
    {
        if (InputSystem.settings == null) return;  // Safety check

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;

        if (isPaused)
        {
            // Freeze time & physics
            Time.timeScale = 0f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;  // Fix physics step

            // Disable input completely
            if (playerMap != null)
                playerMap.Disable();

            // Force multitool off
            if (multitool != null)
            {
                typeof(Multitool).GetField("isActive", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)?.SetValue(multitool, false);
                multitool.StopActive();  // Immediate stop beam/effects
            }

            // Cursor & UI
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            ingameUI.SetActive(false);
            pauseMenu.SetActive(true);
        }
        else
        {
            // Unfreeze
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;

            // Re-enable input
            if (playerMap != null)
                playerMap.Enable();

            // Cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            ingameUI.SetActive(true);
            pauseMenu.SetActive(false);
        }
    }
}