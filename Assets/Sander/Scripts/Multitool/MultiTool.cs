using UnityEngine;

public enum ToolMode { Mining, Tractor }

public class Multitool : MonoBehaviour
{
    [Header("Shared Settings")]
    [SerializeField] private float maxRange = 5f;
    [SerializeField] private LayerMask minableLayer;
    [SerializeField] private LayerMask liftableLayer;

    [Header("Beam Visuals")]
    [SerializeField] private LineRenderer beamRenderer;
    [SerializeField] private Color miningColor = Color.red;
    [SerializeField] private Color tractorColor = Color.blue;
    [SerializeField] private float beamWidth = 0.05f;

    [Header("Mining")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private ParticleSystem impactParticlesPrefab;
    [SerializeField] private AudioSource laserSound;

    [Header("Tractor")]
    [SerializeField] private float holdDistance = 3f;
    [SerializeField] private float minHoldDistance = 1f;
    [SerializeField] private float maxHoldDistance = 10f;
    [SerializeField] private float scrollSensitivity = 2f;
    [SerializeField] private float tractorSpring = 50000f;  
    [SerializeField] private float tractorDamperMult = 0.7f;
    [SerializeField] private AudioSource tractorSound;
    [SerializeField] private float springForce = 1000f;
    [SerializeField] private float damperForce = 500f;
    [SerializeField] private float heldDrag = 10f;

    [Header("Input")]
    [SerializeField] private PlayerInputHandler inputHandler;

    private ToolMode currentMode = ToolMode.Mining;
    private bool isActive;
    private RaycastHit hit;
    private ParticleSystem activeImpactParticles;
    private Rigidbody heldRigidbody;
    private SpringJoint tractorJoint;
    private GameObject tractorTarget;  // New: Kinematic target for joint
    private Rigidbody tractorTargetRb;
    private float targetHoldDistance;

    void Start()
    {
        Gradient baseGradient = new Gradient();
        baseGradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        beamRenderer.colorGradient = baseGradient;

        if (beamRenderer == null) beamRenderer = GetComponent<LineRenderer>();
        beamRenderer.startWidth = beamWidth;
        beamRenderer.endWidth = beamWidth;
        beamRenderer.positionCount = 2;
        beamRenderer.enabled = false;

        if (inputHandler == null) inputHandler = GetComponentInParent<PlayerInputHandler>();

        tractorTarget = new GameObject("TractorTarget");
        tractorTarget.transform.SetParent(transform);
        tractorTarget.transform.localPosition = Vector3.forward * holdDistance;
        tractorTarget.transform.localRotation = Quaternion.identity;
        tractorTargetRb = tractorTarget.AddComponent<Rigidbody>();
        tractorTargetRb.isKinematic = true;
        tractorTargetRb.useGravity = false;
        tractorTargetRb.linearDamping = 0f;
        tractorTargetRb.angularDamping = 0f;

        targetHoldDistance = holdDistance;
    }

    void Update()
    {
        isActive = inputHandler != null && inputHandler.IsMining;

        if (inputHandler.ToggleModeTriggered)
        {
            ToggleMode();
            Debug.Log("Switched to: " + currentMode);  // Temp: Confirm toggle
        }

        if (inputHandler.ScrollInput != 0f)
        {
            targetHoldDistance += inputHandler.ScrollInput * scrollSensitivity;
            targetHoldDistance = Mathf.Clamp(targetHoldDistance, minHoldDistance, maxHoldDistance);
        }

        // Update target position every frame
        tractorTarget.transform.localPosition = Vector3.forward * targetHoldDistance;

        if (isActive)
        {
            PerformActive();
        }
        else
        {
            StopActive();
        }
    }

    private void ToggleMode()
    {
        currentMode = currentMode == ToolMode.Mining ? ToolMode.Tractor : ToolMode.Mining;
    }

    private void PerformActive()
    {
        Color beamCol = currentMode == ToolMode.Mining ? miningColor : tractorColor;

        // Set vertex tint (now used by shader graph)
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(beamCol, 0f), new GradientColorKey(beamCol, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        beamRenderer.colorGradient = gradient;

        // Optional: Boost emission via material property if you exposed it
        beamRenderer.material.SetFloat("_Emission_Intensity", 10f);  // Match your property name

        if (currentMode == ToolMode.Mining)
        {
            PerformMining();
        }
        else
        {
            PerformTractor();
        }
    }

    private void PerformMining()
    {
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxRange, minableLayer))
        {
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, hit.point);

            PlaySound(laserSound);

            UpdateImpactParticles(hit.point, hit.normal);

            MinableRock rock = hit.collider.GetComponent<MinableRock>();
            if (rock != null)
            {
                rock.TakeDamage(damagePerSecond * Time.deltaTime);
            }
        }
        else
        {
            DrawBeamToRange(maxRange);
            StopEffects();
        }
    }

    private void PerformTractor()
    {
        bool hitLiftable = Physics.Raycast(transform.position, transform.forward, out hit, maxRange, liftableLayer);

        if (heldRigidbody != null)
        {
            // Existing: beam to held object
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, heldRigidbody.transform.position);

            UpdateTractorJoint();

            if (tractorSound != null && !tractorSound.isPlaying) tractorSound.Play();
        }
        else if (hitLiftable)
        {
            // Pickup and draw to hit point (new: draw before pickup for smooth attach)
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, hit.point);

            PickupObject(hit.collider);
        }
        else
        {
            // No hit/hold: always draw full range
            DrawBeamToRange(maxRange);
        }

        // Debug: Visualize ray in editor
        Debug.DrawRay(transform.position, transform.forward * maxRange, Color.cyan, 0.1f);
    }


    private void PickupObject(Collider col)
    {
        Rigidbody rb = col.attachedRigidbody;  // Better: gets RB even if on parent
        if (rb == null || rb.isKinematic)
        {
            Debug.LogWarning("No valid non-kinematic Rigidbody on hit object: " + col.name);
            return;
        }

        heldRigidbody = rb;
        heldRigidbody.linearDamping = heldDrag;  // Apply drag

        tractorJoint = heldRigidbody.gameObject.AddComponent<SpringJoint>();
        tractorJoint.autoConfigureConnectedAnchor = false;
        tractorJoint.connectedBody = null;  // World space target
        tractorJoint.spring = springForce;
        tractorJoint.damper = damperForce;
        tractorJoint.minDistance = 0f;
        tractorJoint.maxDistance = 0.1f;  // Small tolerance to reduce jitter

        Debug.Log("Picked up: " + col.name + " | RB mass: " + rb.mass);
    }

    private void UpdateTractorJoint()
    {
        if (tractorJoint != null && heldRigidbody != null)
        {
            Vector3 targetPos = transform.position + transform.forward * targetHoldDistance;
            tractorJoint.connectedAnchor = Vector3.zero;  // Not needed if world-anchored
            tractorJoint.anchor = heldRigidbody.transform.InverseTransformPoint(targetPos);  // Pull to target
        }
    }

    private void StopActive()
    {
        beamRenderer.enabled = false;
        StopEffects();

        if (heldRigidbody != null && tractorJoint != null)
        {
            Destroy(tractorJoint);
            heldRigidbody = null;
            tractorJoint = null;
        }

        if (heldRigidbody != null)
        {
            if (tractorJoint != null) Destroy(tractorJoint);
            heldRigidbody.linearDamping = 0f;  // Reset to original (assume 0; store original if needed)
            heldRigidbody = null;
        }
    }

    private void DrawBeamToRange(float range)
    {
        beamRenderer.enabled = true;
        beamRenderer.SetPosition(0, transform.position);
        beamRenderer.SetPosition(1, transform.position + transform.forward * range);
    }

    private void PlaySound(AudioSource sound)
    {
        if (sound != null && !sound.isPlaying) sound.Play();
    }

    private void UpdateImpactParticles(Vector3 pos, Vector3 normal)
    {
        if (impactParticlesPrefab != null)
        {
            if (activeImpactParticles == null)
            {
                activeImpactParticles = Instantiate(impactParticlesPrefab, pos, Quaternion.LookRotation(normal));
            }
            else
            {
                activeImpactParticles.transform.position = pos;
                activeImpactParticles.transform.rotation = Quaternion.LookRotation(normal);
            }
            if (!activeImpactParticles.isPlaying) activeImpactParticles.Play();
        }
    }

    private void StopEffects()
    {
        if (laserSound != null && laserSound.isPlaying) laserSound.Stop();
        if (tractorSound != null && tractorSound.isPlaying) tractorSound.Stop();
        if (activeImpactParticles != null)
        {
            activeImpactParticles.Stop();
            Destroy(activeImpactParticles.gameObject, 2f);
            activeImpactParticles = null;
        }
    }

    void OnDestroy()
    {
        if (tractorTarget != null) Destroy(tractorTarget);
    }
}