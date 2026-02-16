using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ObjectiveManager : MonoBehaviour
{
    [Header("=== OBJECTIVES ===")]
    [SerializeField] private List<MinableRock> mineObjectives = new List<MinableRock>();
    [SerializeField] private List<RepairableObject> repairObjectives = new List<RepairableObject>();
    [SerializeField] private List<GameObject> transportObjectives = new List<GameObject>();

    [Header("Win Condition")]
    [SerializeField] private UnityEvent onAllObjectivesCompleted;

    private int totalObjectives;
    private int completedCount;
    private HashSet<TransportObjective> completedTransports = new HashSet<TransportObjective>();  // Track which transports are done

    private void Start()
    {
        totalObjectives = mineObjectives.Count + repairObjectives.Count + transportObjectives.Count;
        completedCount = 0;

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
            Debug.Log("Transport objective completed: " + transportedObject.name);
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
        if (completedCount >= totalObjectives)
        {
            Debug.Log("★ ALL OBJECTIVES COMPLETED! YOU WIN! ★");
            onAllObjectivesCompleted.Invoke();
        }
    }

    // Optional: progress getter
    public float GetProgress() => totalObjectives > 0 ? (float)completedCount / totalObjectives : 0f;
}