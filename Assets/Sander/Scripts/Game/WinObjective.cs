using UnityEngine;
using UnityEngine.InputSystem;

public class WinObjective : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private ObjectiveManager objectiveManager;

    [Header("Input")]
    [SerializeField] private KeyCode activationKey = KeyCode.E;

    [Header("Interaction Feedback")]
    [SerializeField] private float interactionRange = 5f;
    [SerializeField] private GameObject activationPrompt;  // UI element showing "Press E to win" (optional)

    private Camera mainCamera;
    private bool isPlayerInRange = false;

    private void Start()
    {
        mainCamera = Camera.main;

        if (objectiveManager == null)
            objectiveManager = Object.FindFirstObjectByType<ObjectiveManager>();

        if (activationPrompt != null)
            activationPrompt.SetActive(false);
    }

    private void Update()
    {
        // Only check input if all objectives are completed
        if (!objectiveManager.AreAllObjectivesCompleted())
        {
            isPlayerInRange = false;
            if (activationPrompt != null)
                activationPrompt.SetActive(false);
            return;
        }

        // Check if player is looking at this object
        isPlayerInRange = IsPlayerLooking();

        if (activationPrompt != null)
            activationPrompt.SetActive(isPlayerInRange);

        // Check for input when player is looking and all objectives are done
        if (isPlayerInRange && Input.GetKeyDown(activationKey))
        {
            TriggerWin();
        }
    }

    private bool IsPlayerLooking()
    {
        if (mainCamera == null)
            return false;

        // Raycast from camera forward
        if (Physics.Raycast(mainCamera.transform.position, mainCamera.transform.forward, out RaycastHit hit, interactionRange))
        {
            // Check if we hit this object or its children
            return hit.collider.gameObject == gameObject || hit.collider.transform.IsChildOf(transform);
        }

        return false;
    }

    private void TriggerWin()
    {
        objectiveManager.TriggerWin();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
    }
}
