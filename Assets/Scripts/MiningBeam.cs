using UnityEngine;
using UnityEngine.VFX;
public class MiningBeam : MonoBehaviour
{
    public VisualEffect impactVFX;
    public VisualEffect beamVFX;
    public float maxDistance = 50f;

    private Transform lockedTarget;
    private Vector3 lockedHitNormal;

    public void LockTarget(Transform target, Vector3 hitNormal)
    {
        lockedTarget = target;
        lockedHitNormal = hitNormal;
    }

    public void ClearTarget()
    {
        lockedTarget = null;
    }

    void Update()
    {
        if (lockedTarget != null)
        {
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
            impactVFX.transform.rotation = Quaternion.LookRotation(hit.normal);
            impactVFX.SendEvent("Active");
        }
        else
        {
            Vector3 worldEnd = origin + direction * maxDistance;
            Vector3 localEnd = beamVFX.transform.InverseTransformPoint(worldEnd);
            beamVFX.SetVector3("endPointLocal", localEnd);

            impactVFX.SendEvent("NotActive");
        }
    }
}









