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
    [SerializeField] private float tractorSpring = 50000f;  // Increased for strong pull
    [SerializeField] private float tractorDamperMult = 0.7f;
    [SerializeField] private AudioSource tractorSound;

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
        if (beamRenderer == null) beamRenderer = GetComponent<LineRenderer>();
        beamRenderer.startWidth = beamWidth;
        beamRenderer.endWidth = beamWidth;
        beamRenderer.positionCount = 2;
        beamRenderer.enabled = false;

        if (inputHandler == null) inputHandler = GetComponentInParent<PlayerInputHandler>();

        // New: Create kinematic target child
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

        // Fixed: Use gradient for reliable color update (HDRP-friendly)
        Gradient grad = new Gradient();
        grad.SetKeys(
            new GradientColorKey[] { new GradientColorKey(beamCol, 0f), new GradientColorKey(beamCol, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        beamRenderer.colorGradient = grad;

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
        if (heldRigidbody != null)
        {
            // Beam to held object center
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, heldRigidbody.worldCenterOfMass);

            UpdateTractorJoint();
            PlaySound(tractorSound);
        }
        else
        {
            // Raycast to pickup
            if (Physics.Raycast(transform.position, transform.forward, out hit, maxRange, liftableLayer))
            {
                PickupObject(hit.collider);
            }
            // Beam to target position
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, tractorTarget.transform.position);
        }
    }

    private void PickupObject(Collider col)
    {
        Rigidbody rb = col.GetComponent<Rigidbody>();
        if (rb == null || rb == heldRigidbody) return;

        heldRigidbody = rb;
        tractorJoint = heldRigidbody.gameObject.AddComponent<SpringJoint>();
        tractorJoint.connectedBody = tractorTargetRb;
        tractorJoint.connectedAnchor = Vector3.zero;  // Target center
        tractorJoint.anchor = Vector3.zero;  // Held center
        tractorJoint.spring = tractorSpring;
        tractorJoint.damper = tractorSpring * tractorDamperMult;
        tractorJoint.minDistance = 0f;
        tractorJoint.maxDistance = 1f;  // Tight hold
        tractorJoint.massScale = 1f / rb.mass;  // Consistent force regardless of mass
    }

    private void UpdateTractorJoint()
    {
        // Joint auto-pulls to moving target � no extra needed!
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