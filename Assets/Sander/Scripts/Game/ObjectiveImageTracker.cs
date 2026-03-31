using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Pairs each objective with a world-space canvas Image.
/// When the objective is completed its Image's material is swapped to the completed material.
/// Add this component to the same GameObject as ObjectiveManager, then populate the three lists.
/// </summary>
public class ObjectiveImageTracker : MonoBehaviour
{
    [System.Serializable]
    public class MineObjectiveImage
    {
        [Tooltip("The MinableRock objective to track")]
        public MinableRock objective;
        [Tooltip("The world-space canvas Image linked to this objective")]
        public Image image;
        [Tooltip("Material to apply to the Image when this objective is completed")]
        public Material completedMaterial;
    }

    [System.Serializable]
    public class RepairObjectiveImage
    {
        [Tooltip("The RepairableObject objective to track")]
        public RepairableObject objective;
        [Tooltip("The world-space canvas Image linked to this objective")]
        public Image image;
        [Tooltip("Material to apply to the Image when this objective is completed")]
        public Material completedMaterial;
    }

    [System.Serializable]
    public class TransportObjectiveImage
    {
        [Tooltip("The GameObject with a TransportObjective component to track")]
        public GameObject objective;
        [Tooltip("The world-space canvas Image linked to this objective")]
        public Image image;
        [Tooltip("Material to apply to the Image when this objective is completed")]
        public Material completedMaterial;
    }

    [Header("Mine Objectives")]
    [SerializeField] private List<MineObjectiveImage> mineEntries = new List<MineObjectiveImage>();

    [Header("Repair Objectives")]
    [SerializeField] private List<RepairObjectiveImage> repairEntries = new List<RepairObjectiveImage>();

    [Header("Transport Objectives")]
    [SerializeField] private List<TransportObjectiveImage> transportEntries = new List<TransportObjectiveImage>();

    private void Start()
    {
        foreach (MineObjectiveImage entry in mineEntries)
        {
            if (entry.objective == null || entry.image == null) continue;
            MineObjectiveImage captured = entry;
            captured.objective.OnMined.AddListener(() => OnObjectiveCompleted(captured.image, captured.completedMaterial));
        }

        foreach (RepairObjectiveImage entry in repairEntries)
        {
            if (entry.objective == null || entry.image == null) continue;
            RepairObjectiveImage captured = entry;
            captured.objective.OnRepaired.AddListener(() => OnObjectiveCompleted(captured.image, captured.completedMaterial));
        }

        foreach (TransportObjectiveImage entry in transportEntries)
        {
            if (entry.objective == null || entry.image == null) continue;
            if (entry.objective.GetComponent<TransportObjective>() == null) continue;
            TransportObjectiveImage captured = entry;
            ObjectiveImageTransportHook hook = entry.objective.GetComponent<ObjectiveImageTransportHook>();
            if (hook == null) hook = entry.objective.AddComponent<ObjectiveImageTransportHook>();
            hook.Register(captured.image, captured.completedMaterial, this);
        }
    }

    private void OnObjectiveCompleted(Image image, Material completedMaterial)
    {
        if (image == null || completedMaterial == null) return;
        image.material = completedMaterial;
    }
}
