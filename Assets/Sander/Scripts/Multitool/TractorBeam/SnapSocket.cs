using UnityEngine;

public class SnapSocket : MonoBehaviour
{
    [Tooltip("Visual radius gizmo in Scene view")]
    [SerializeField] private float detectionRadius = 0.5f;

    // Called by tractor when object snaps here
    public void SnapObject(Transform objTransform)
    {
        objTransform.SetParent(transform);
        objTransform.localPosition = Vector3.zero;
        objTransform.localRotation = Quaternion.Euler(objTransform.GetComponent<TransportObjective>().SnapRotationOffset);

        // Optional: add effects/sound
        Debug.Log(objTransform.name + " snapped to socket!");
    }

    // Scene view gizmo
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}