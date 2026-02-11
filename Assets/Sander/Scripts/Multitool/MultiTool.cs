using UnityEngine;
using UnityEngine.UI;  // Added for Image and Sprite

public enum ToolMode { Mining, Tractor, Repair }

public class Multitool : MonoBehaviour
{
    [Header("Shared Settings")]
    [SerializeField] private float maxRange = 5f;
    [SerializeField] private LayerMask minableLayer;
    [SerializeField] private LayerMask liftableLayer;
    [SerializeField] private LayerMask repairableLayer;

    [Header("Beam Visuals")]
    [SerializeField] private LineRenderer beamRenderer;
    [SerializeField] private Color miningColor = Color.red;
    [SerializeField] private Color tractorColor = Color.blue;
    [SerializeField] private Color repairColor = Color.green;
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
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private AudioSource tractorSound;

    [Header("Repair")]
    [SerializeField] private float repairPerSecond = 1f;
    [SerializeField] private ParticleSystem repairParticlesPrefab;
    [SerializeField] private AudioSource repairSound;

    [Header("Input")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Mode UI Icon")]
    [SerializeField] private Image modeIconImage;           // Drag your UI Image component here
    [SerializeField] private Sprite miningIconSprite;       // Icon for Mining mode
    [SerializeField] private Sprite tractorIconSprite;      // Icon for Tractor mode
    [SerializeField] private Sprite repairIconSprite;       // Icon for Repair mode

    public ToolMode currentMode = ToolMode.Mining;

    private bool isActive;
    private RaycastHit hit;
    private ParticleSystem activeImpactParticles;
    private Rigidbody heldRigidbody;
    private Vector3 releaseVelocity;
    private bool originalUseGravity;
    private bool originalIsKinematic;
    private GameObject tractorTarget;
    private Rigidbody tractorTargetRb;
    private float targetHoldDistance;
    private ToolMode lastMode;  // Used to detect mode changes for icon update

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

        // Initialize UI icon
        lastMode = currentMode;
        UpdateModeIcon();
    }

    void Update()
    {
        isActive = inputHandler != null && inputHandler.IsMining;

        if (inputHandler.ToggleModeTriggered)
        {
            ToggleMode();
            Debug.Log("Switched to: " + currentMode);
        }

        if (inputHandler.ScrollInput != 0f && currentMode == ToolMode.Tractor)
        {
            targetHoldDistance += inputHandler.ScrollInput * scrollSensitivity;
            targetHoldDistance = Mathf.Clamp(targetHoldDistance, minHoldDistance, maxHoldDistance);
        }

        tractorTarget.transform.localPosition = Vector3.forward * targetHoldDistance;

        // Check for mode change (in case something external changes it)
        if (currentMode != lastMode)
        {
            UpdateModeIcon();
            lastMode = currentMode;
        }

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
        switch (currentMode)
        {
            case ToolMode.Mining:
                currentMode = ToolMode.Tractor;
                break;
            case ToolMode.Tractor:
                currentMode = ToolMode.Repair;
                break;
            case ToolMode.Repair:
                currentMode = ToolMode.Mining;
                break;
        }

        UpdateModeIcon();  // Update icon immediately after toggle
    }

    private void UpdateModeIcon()
    {
        if (modeIconImage == null) return;

        modeIconImage.sprite = currentMode switch
        {
            ToolMode.Mining => miningIconSprite,
            ToolMode.Tractor => tractorIconSprite,
            ToolMode.Repair => repairIconSprite,
            _ => miningIconSprite // fallback
        };
    }

    private void PerformActive()
    {
        Color beamCol = currentMode switch
        {
            ToolMode.Mining => miningColor,
            ToolMode.Tractor => tractorColor,
            ToolMode.Repair => repairColor,
            _ => Color.white
        };

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(beamCol, 0f), new GradientColorKey(beamCol, 1f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) }
        );
        beamRenderer.colorGradient = gradient;

        beamRenderer.material.SetFloat("_Emission_Intensity", 10f);

        switch (currentMode)
        {
            case ToolMode.Mining:
                PerformMining();
                break;
            case ToolMode.Tractor:
                PerformTractor();
                break;
            case ToolMode.Repair:
                PerformRepair();
                break;
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
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, heldRigidbody.transform.position);

            Vector3 targetPos = transform.position + transform.forward * targetHoldDistance;
            Vector3 direction = targetPos - heldRigidbody.transform.position;
            Vector3 desiredVelocity = direction * followSpeed;

            if (desiredVelocity.magnitude > maxVelocity)
            {
                desiredVelocity = desiredVelocity.normalized * maxVelocity;
            }

            heldRigidbody.linearVelocity = desiredVelocity;
            releaseVelocity = heldRigidbody.linearVelocity;

            if (tractorSound != null && !tractorSound.isPlaying) tractorSound.Play();
        }
        else if (hitLiftable)
        {
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, hit.point);
            PickupObject(hit.collider);
        }
        else
        {
            DrawBeamToRange(maxRange);
        }
    }

    private void PerformRepair()
    {
        if (Physics.Raycast(transform.position, transform.forward, out hit, maxRange, repairableLayer))
        {
            beamRenderer.enabled = true;
            beamRenderer.SetPosition(0, transform.position);
            beamRenderer.SetPosition(1, hit.point);

            PlaySound(repairSound);
            UpdateImpactParticles(hit.point, hit.normal); // Reuse or change to repair-specific

            RepairableObject repairObj = hit.collider.GetComponent<RepairableObject>();
            if (repairObj != null)
            {
                repairObj.Repair(repairPerSecond * Time.deltaTime);
            }
        }
        else
        {
            DrawBeamToRange(maxRange);
            StopEffects();
        }
    }

    private void PickupObject(Collider col)
    {
        Rigidbody rb = col.attachedRigidbody;
        if (rb == null || rb.isKinematic)
        {
            Debug.LogWarning("No valid non-kinematic Rigidbody on hit object: " + col.name);
            return;
        }

        heldRigidbody = rb;
        originalUseGravity = rb.useGravity;
        originalIsKinematic = rb.isKinematic;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        releaseVelocity = Vector3.zero;

        Debug.Log("Picked up: " + col.name);
    }

    private void StopActive()
    {
        beamRenderer.enabled = false;
        StopEffects();

        if (heldRigidbody != null)
        {
            heldRigidbody.isKinematic = originalIsKinematic;
            heldRigidbody.useGravity = originalUseGravity;
            heldRigidbody.linearVelocity = releaseVelocity;
            heldRigidbody.angularVelocity = Vector3.zero;
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
        if (repairSound != null && repairSound.isPlaying) repairSound.Stop();
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