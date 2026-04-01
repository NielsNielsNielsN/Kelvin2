using UnityEngine;
using UnityEngine.Playables;

public class CutsceneController : MonoBehaviour
{
    [Header("Timeline")]
    public PlayableDirector director;

    [Header("Cameras")]
    public GameObject gameplayCamera;   // Main Camera (child of player)
    public GameObject cutsceneCamera;   // Camera with Cinemachine Brain

    [Header("Player Look Script")]
    public MonoBehaviour playerLook;    // Your mouse look script

    void OnEnable()
    {
        director.stopped += OnCutsceneEnd;
    }

    void OnDisable()
    {
        director.stopped -= OnCutsceneEnd;
    }

    // Call this to start the cutscene
    public void PlayCutscene()
    {
        // Disable gameplay camera + look control
        gameplayCamera.SetActive(false);
        playerLook.enabled = false;

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

        // Enable gameplay camera + look control
        gameplayCamera.SetActive(true);
        playerLook.enabled = true;
    }
}
