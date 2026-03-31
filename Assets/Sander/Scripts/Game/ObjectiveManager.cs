using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class ObjectiveManager : MonoBehaviour
{
    [Header("=== OBJECTIVES ===")]
    [SerializeField] private List<MinableRock> mineObjectives = new List<MinableRock>();
    [SerializeField] private List<RepairableObject> repairObjectives = new List<RepairableObject>();
    [SerializeField] private List<GameObject> transportObjectives = new List<GameObject>();

    [Header("Win Condition")]
    [SerializeField] private UnityEvent onAllObjectivesCompleted;

    [Header("Win Sequence")]
    [SerializeField] private Image fadeToBlackImage;       // Full-screen black image for fade effect
    [SerializeField] private float winFadeDuration = 3f;   // How long to fade to black
    [SerializeField] private GameObject winCanvas;         // "You Win" canvas to display
    [SerializeField] private GameObject crosshairCanvas;   // Crosshair to fade with screen

    private int totalObjectives;
    private int completedCount;
    private HashSet<TransportObjective> completedTransports = new HashSet<TransportObjective>();
    private bool allObjectivesCompleted = false;
    private bool hasWon = false;

    private void Start()
    {
        totalObjectives = mineObjectives.Count + repairObjectives.Count + transportObjectives.Count;
        completedCount = 0;
        allObjectivesCompleted = false;
        hasWon = false;

        // Initialize fade to black image
        if (fadeToBlackImage != null)
        {
            Color c = fadeToBlackImage.color;
            c.a = 0f;
            fadeToBlackImage.color = c;
        }

        if (winCanvas != null)
            winCanvas.SetActive(false);

        // Mining & repair (unchanged)
        foreach (var rock in mineObjectives)
            if (rock) rock.OnMined.AddListener(OnObjectiveComplete);

        foreach (var rep in repairObjectives)
            if (rep) rep.OnRepaired.AddListener(OnObjectiveComplete);

        // No need to subscribe to transport here anymore — we handle snap completion differently
    }

    public void OnTransportObjectiveCompleted(GameObject transportedObject)
    {
        if (transportObjectives.Contains(transportedObject))
        {
            transportObjectives.Remove(transportedObject); // optional: clean up list
            completedCount++;
            CheckWin();
            ObjectiveImageTransportHook hook = transportedObject.GetComponent<ObjectiveImageTransportHook>();
            if (hook != null) hook.NotifyCompleted();
        }
    }

    public void OnObjectiveComplete()
    {
        completedCount++;
        CheckWin();
    }

    public void OnTransportSnapped(TransportObjective transportObj)
    {
        if (completedTransports.Contains(transportObj))
        {
            completedTransports.Remove(transportObj);
            completedCount++;
            CheckWin();
        }
    }

    private void CheckWin()
    {
        if (completedCount >= totalObjectives && !allObjectivesCompleted)
        {
            allObjectivesCompleted = true;
        }
    }

    // Getter to check if all objectives are complete
    public bool AreAllObjectivesCompleted() => allObjectivesCompleted;

    // Getter for win canvas
    public GameObject GetWinCanvas() => winCanvas;

    // Method called when win objective is triggered
    public void TriggerWin()
    {
        if (allObjectivesCompleted && !hasWon)
        {
            hasWon = true;
            StartCoroutine(FadeToWin());
        }
    }

    private IEnumerator FadeToWin()
    {
        if (fadeToBlackImage == null) yield break;

        // Deactivate crosshair immediately when win is triggered
        if (crosshairCanvas != null)
            crosshairCanvas.SetActive(false);

        float elapsed = 0f;
        Color startColor = fadeToBlackImage.color;
        Color targetColor = startColor;
        targetColor.a = 1f;

        while (elapsed < winFadeDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / winFadeDuration;
            fadeToBlackImage.color = Color.Lerp(startColor, targetColor, t);
            yield return null;
        }

        fadeToBlackImage.color = targetColor;

        if (winCanvas != null)
        {
            winCanvas.SetActive(true);
            // Unlock cursor so player can interact with win canvas buttons
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        // Invoke the win event
        onAllObjectivesCompleted.Invoke();
    }

    // Optional: progress getter
    public float GetProgress() => totalObjectives > 0 ? (float)completedCount / totalObjectives : 0f;
}