using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placed automatically by ObjectiveImageTracker on each transport objective GameObject.
/// Notifies the tracker when this transport has been completed so its Image material can be swapped.
/// </summary>
public class ObjectiveImageTransportHook : MonoBehaviour
{
    private Image trackedImage;
    private Material completedMaterial;
    private ObjectiveImageTracker tracker;
    private bool completed = false;

    public void Register(Image image, Material material, ObjectiveImageTracker objectiveImageTracker)
    {
        trackedImage = image;
        completedMaterial = material;
        tracker = objectiveImageTracker;
    }

    // Called by ObjectiveManager when this transport GameObject is snapped/delivered
    public void NotifyCompleted()
    {
        if (completed) return;
        completed = true;
        tracker?.OnTransportCompleted(trackedImage, completedMaterial);
    }
}
