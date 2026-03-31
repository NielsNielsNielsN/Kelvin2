using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Placed automatically by ObjectiveImageTracker on each transport objective GameObject.
/// Swaps the tracked Image's material when this transport has been completed.
/// </summary>
public class ObjectiveImageTransportHook : MonoBehaviour
{
    private Image trackedImage;
    private Material completedMaterial;
    private bool completed = false;

    public void Register(Image image, Material material, ObjectiveImageTracker objectiveImageTracker)
    {
        trackedImage = image;
        completedMaterial = material;
    }

    // Called by ObjectiveManager when this transport GameObject is snapped/delivered
    public void NotifyCompleted()
    {
        if (completed) return;
        completed = true;
        if (trackedImage != null && completedMaterial != null)
            trackedImage.material = completedMaterial;
    }
}
