using UnityEngine;
using UnityEngine.VFX;
public class MiningBeam : MonoBehaviour
{
    public VisualEffect impactVFX;
    public VisualEffect beamVFX;
    public float maxDistance = 50f;

    // When set, the beam endpoint is locked to this transform instead of raycasting
    private Transform lockedTarget;
    private Vector3 lockedHitNormal;

    // Called by Multitool when an object is picked up with the tractor beam
    public void LockTarget(Transform target, Vector3 hitNormal)
    {
        lockedTarget = target;
        lockedHitNormal = hitNormal;
    }

    // Called by Multitool when the tractor beam releases the object
    public void ClearTarget()
    {
        lockedTarget = null;
    }

    void Update()
    {
        if (lockedTarget != null)
        {
            // Point beam at the held object's current position
            Vector3 localEnd = beamVFX.transform.InverseTransformPoint(lockedTarget.position);
            beamVFX.SetVector3("endPointLocal", localEnd);

            impactVFX.transform.position = lockedTarget.position;
            impactVFX.transform.rotation = Quaternion.LookRotation(lockedHitNormal);
            impactVFX.SendEvent("Active");
            return;
        }

        Vector3 origin = transform.position;
        Vector3 direction = transform.right;

        if (Physics.Raycast(origin, direction, out RaycastHit hit, maxDistance))
        {
            Vector3 localEnd = beamVFX.transform.InverseTransformPoint(hit.point);
            beamVFX.SetVector3("endPointLocal", localEnd);

            // Move the impact VFX to the hit point
            impactVFX.transform.position = hit.point;

            // Optional: orient it to face the surface normal
            impactVFX.transform.rotation = Quaternion.LookRotation(hit.normal);

            // Optional: enable the effect
            impactVFX.SendEvent("Active");
        }
        else
        {
            Vector3 worldEnd = origin + direction * maxDistance;
            Vector3 localEnd = beamVFX.transform.InverseTransformPoint(worldEnd);
            beamVFX.SetVector3("endPointLocal", localEnd);

            // Disable the impact effect when nothing is hit
            impactVFX.SendEvent("NotActive");
        }
    }
}









