using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class CutsceneController : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector director;

    [Header("Cameras")]
    public GameObject gameplayCamera;   // Main Camera (child of player)
    public GameObject cutsceneCamera;   // Camera with Cinemachine Brain

    [Header("UI")]
    [SerializeField] private GameObject crosshairCanvas;

    [Header("Player References")]
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Multitool multitool;

    // Checked by MenuUI to block pause input during a cutscene
    public static bool IsCutscenePlaying { get; private set; } = false;

    private InputActionMap playerMap;

    private void Awake()
    {
        if (playerInputHandler == null)
            playerInputHandler = Object.FindFirstObjectByType<PlayerInputHandler>();

        if (multitool == null)
            multitool = Object.FindFirstObjectByType<Multitool>();

        if (playerInputHandler != null && playerInputHandler.playerControls != null)
            playerMap = playerInputHandler.playerControls.FindActionMap("Player");
    }

    private void OnEnable()
    {
        director.stopped += OnCutsceneEnd;
    }

    private void OnDisable()
    {
        director.stopped -= OnCutsceneEnd;
    }

    // Call this to start the cutscene
    public void PlayCutscene()
    {
        IsCutscenePlaying = true;

        // Disable player input
        if (playerMap != null)
            playerMap.Disable();

        // Stop any active tool
        if (multitool != null)
            multitool.StopActive();

        // Disable crosshair
        if (crosshairCanvas != null)
            crosshairCanvas.SetActive(false);

        // Disable gameplay camera
        gameplayCamera.SetActive(false);

        // Enable cutscene camera
        cutsceneCamera.SetActive(true);

        // Play the Timeline
        director.Play();
    }

    // Automatically called when the Timeline finishes
    private void OnCutsceneEnd(PlayableDirector d)
    {
        // Disable cutscene camera
        cutsceneCamera.SetActive(false);

        // Enable gameplay camera
        gameplayCamera.SetActive(true);

        // Restore player input
        if (playerMap != null)
            playerMap.Enable();

        // Re-enable crosshair
        if (crosshairCanvas != null)
            crosshairCanvas.SetActive(true);

        IsCutscenePlaying = false;
    }
}
