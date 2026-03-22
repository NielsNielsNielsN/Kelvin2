using UnityEngine;
using UnityEngine.VFX;
public class MiningBeam : MonoBehaviour
{
    public bool coreLifetimeOn = true;   // lifetime when hitting
    public bool coreLifetimeOff = false;  // lifetime when not hitting
    public VisualEffect impactVFX;
    public VisualEffect beamVFX;
    public float maxDistance = 50f;

    void Update()
    {
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

            impactVFX.SetBool("CoreLifetimeMultiplier", coreLifetimeOn);
        }
        else
        {
            Vector3 worldEnd = origin + direction * maxDistance;
            Vector3 localEnd = beamVFX.transform.InverseTransformPoint(worldEnd);
            beamVFX.SetVector3("endPointLocal", localEnd);

            // Disable the impact effect when nothing is hit
            impactVFX.SendEvent("NotActive");

            impactVFX.SetBool("CoreLifetimeMultiplier", coreLifetimeOff);
        }
    }
}