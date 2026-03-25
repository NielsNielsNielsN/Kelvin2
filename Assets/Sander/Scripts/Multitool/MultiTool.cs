using UnityEngine;
using UnityEngine.UI;
using TMPro;

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

    [Header("VFX Beam Emitters")]
    [SerializeField] private GameObject miningBeamEmitterRef;
    [SerializeField] private GameObject tractorBeamEmitterRef;
    [SerializeField] private GameObject repairBeamEmitterRef;
    [Tooltip("When enabled the LineRenderer beam is invisible and VFX emitters are used as visuals")]
    [SerializeField] private bool useVFXVisuals = true;

    [Header("Mining")]
    [SerializeField] private float damagePerSecond = 10f;
    [SerializeField] private ParticleSystem impactParticlesPrefab;
    [SerializeField] private AudioSource laserSound;
    [SerializeField] private MinableRock currentTargetRock;
    [SerializeField] private UnityEngine.UI.Slider miningProgressSlider;
    [SerializeField] private float miningDecayDuration = 2f;

    [Header("Tractor")]
    [SerializeField] private float holdDistance = 3f;
    [SerializeField] private float minHoldDistance = 1f;
    [SerializeField] private float maxHoldDistance = 10f;
    [SerializeField] private float scrollSensitivity = 2f;
    [SerializeField] private float followSpeed = 10f;
    [SerializeField] private float maxVelocity = 20f;
    [SerializeField] private AudioSource tractorSound;
    [SerializeField] private UnityEngine.UI.Slider tractorDistanceSlider;

    [Header("Repair")]
    [SerializeField] private float repairPerSecond = 1f;
    [SerializeField] private ParticleSystem repairParticlesPrefab;
    [SerializeField] private AudioSource repairSound;
    [SerializeField] private UnityEngine.UI.Slider repairProgressSlider;
    [SerializeField] private float repairDecayDuration = 2f;

    [Header("Input")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Mode UI Icon (Screen Space)")]
    [SerializeField] private Image modeIconImage;
    [SerializeField] private Sprite miningIconSprite;
    [SerializeField] private Sprite tractorIconSprite;
    [SerializeField] private Sprite repairIconSprite;

    [Header("Mode Panels (World Space Canvas on Tool)")]
    [SerializeField] private GameObject[] modePanels; // 0 = Mining, 1 = Tractor, 2 = Repair

    public ToolMode currentMode = ToolMode.Mining;

    [Header("Switching")]
    [Tooltip("Delay (seconds) before mode panels switch after requesting a mode change")]
    [SerializeField] private float modePanelDelay = 0.2f;

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
    private ToolMode lastMode;
    // Runtime targets for repair mode
    private RepairableObject currentTargetRepairable; // currently aimed
    private RepairableObject trackedRepairable; // last targeted, used to decay when contact lost
    private MinableRock trackedRock; // last targeted rock when losing aim

    // Coroutine handles
    private Coroutine miningResetCoroutine;
    private Coroutine repairDecayCoroutine;
    private Coroutine rockHealCoroutine;
    private bool isSwitching = false;

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

        // Ensure all VFX emitters start inactive
        if (miningBeamEmitterRef != null) miningBeamEmitterRef.SetActive(false);
        if (tractorBeamEmitterRef != null) tractorBeamEmitterRef.SetActive(false);
        if (repairBeamEmitterRef != null) repairBeamEmitterRef.SetActive(false);

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

        lastMode = currentMode;
        UpdateModeIcon(); // Initialize panels + screen icon
    }

    // Called externally to request a mode toggle (e.g., from controller) — will respect current switching state
    public void RequestToggleMode()
    {
        if (isSwitching) return;
        StartCoroutine(HandleModeToggle());
    }

    private System.Collections.IEnumerator HandleModeToggle()
    {
        if (isSwitching) yield break;
        isSwitching = true;

        // disable firing/laser by treating tool as inactive during switch
        isActive = false;

        // wait for a short delay to align panels with animation
        yield return new WaitForSeconds(modePanelDelay);

        ToggleMode();
        Debug.Log("Switched to: " + currentMode);

        // Note: do NOT clear isSwitching here — caller (controller) will call EndSwitching() when animations finish
    }

    // Called by external code (e.g. FirstPersonController) when the switch animations have completed
    public void EndSwitching()
    {
        isSwitching = false;
        // restore active based on current input
        isActive = inputHandler != null && inputHandler.IsMining;
    }

    // Public accessor so other systems can know switching is in progress
    public bool IsSwitching => isSwitching;

    void Update()
    {
        // Respect switching state: when switching, block firing/use
        isActive = !isSwitching && input_handler_available();

        bool input_handler_available()
        {
            return inputHandler != null && inputHandler.IsMining;
        }

        // Mode switching is handled externally (FirstPersonController) via RequestToggleMode

        if (inputHandler.ScrollInput != 0f && currentMode == ToolMode.Tractor)
        {
            targetHoldDistance += inputHandler.ScrollInput * scrollSensitivity;
            targetHoldDistance = Mathf.Clamp(targetHoldDistance, minHoldDistance, maxHoldDistance);
        }

        tractorTarget.transform.localPosition = Vector3.forward * targetHoldDistance;

        if (currentMode != lastMode)
        {
            UpdateModeIcon();
            lastMode = currentMode;
        }

        MinableRock aimedRock = GetAimedMinableRock();

        // handle mining target changes
        if (aimedRock != currentTargetRock)
        {
            // stop any existing heal coroutine (we're switching targets)
            if (rockHealCoroutine != null)
            {
                StopCoroutine(rockHealCoroutine);
                rockHealCoroutine = null;
            }

            // If we lost aim entirely (aimedRock == null) start healing the previous rock
            if (aimedRock == null && currentTargetRock != null)
            {
                // set tracked rock and start smooth restore
                trackedRock = currentTargetRock;
                // remove listener to avoid mined callback during heal
                trackedRock.OnMined.RemoveListener(OnRockMined);
                // clear current target so PerformMining knows we're not targeting it
                currentTargetRock = null;
                if (rockHealCoroutine == null)
                    rockHealCoroutine = StartCoroutine(RestoreRockHealthOverTime(trackedRock, miningDecayDuration));
            }
            else
            {
                // switching directly to another rock (or first acquire)
                UnsubscribeFromCurrentRock();

                currentTargetRock = aimedRock;

                if (currentTargetRock != null && miningProgressSlider != null)
                {
                    currentTargetRock.miningProgressSlider = miningProgressSlider;
                    miningProgressSlider.value = 0f;
                    // subscribe to mined event so we can animate the slider back to zero
                    currentTargetRock.OnMined.AddListener(OnRockMined);
                }
            }
        }

        // Tractor slider update (map hold distance to slider value 0.1 -> 1)
        if (tractorDistanceSlider != null)
        {
            float norm = Mathf.InverseLerp(minHoldDistance, maxHoldDistance, targetHoldDistance);
            float sliderVal = Mathf.Lerp(0.1f, 1f, norm);
            tractorDistanceSlider.value = sliderVal;
        }

        // Repair target acquisition: when aiming at a repairable object, assign target
        RepairableObject aimedRepair = GetAimedRepairable();
        if (aimedRepair != currentTargetRepairable)
        {
            currentTargetRepairable = aimedRepair;
            if (currentTargetRepairable != null)
            {
                // start tracking this repairable (for decay when we lose aim)
                if (trackedRepairable != null && trackedRepairable != currentTargetRepairable)
                {
                    trackedRepairable.OnRepaired.RemoveListener(OnRepairComplete);
                }
                trackedRepairable = currentTargetRepairable;
                if (repairProgressSlider != null)
                    repairProgressSlider.value = trackedRepairable.GetRepairProgressNormalized();
                trackedRepairable.OnRepaired.AddListener(OnRepairComplete);
                // stop any decay when we regain contact
                if (repairDecayCoroutine != null)
                {
                    StopCoroutine(repairDecayCoroutine);
                    repairDecayCoroutine = null;
                }
            }
            else
            {
                // we lost aim entirely - start decay of the last tracked repairable
                if (trackedRepairable != null)
                {
                    StartRepairDecay();
                }
            }
        }
    }

    private RepairableObject GetAimedRepairable()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, maxRange, repairableLayer))
        {
            return hitInfo.collider.GetComponent<RepairableObject>();
        }
        return null;
    }

    private MinableRock GetAimedMinableRock()
    {
        if (Physics.Raycast(transform.position, transform.forward, out RaycastHit hitInfo, maxRange, minableLayer))
        {
            return hitInfo.collider.GetComponent<MinableRock>();
        }
        return null;
    }

    private void LateUpdate()
    {
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
        if (heldRigidbody != null)
        {
            return;
        }

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
        UpdateModeIcon();
    }

    private void UpdateModeIcon()
    {
        // Update screen-space icon (unchanged)
        if (modeIconImage != null)
        {
            modeIconImage.sprite = currentMode switch
            {
                ToolMode.Mining => miningIconSprite,
                ToolMode.Tractor => tractorIconSprite,
                ToolMode.Repair => repairIconSprite,
                _ => miningIconSprite
            };
        }

        // NEW: Update world-space panels (only one active)
        if (modePanels != null && modePanels.Length == 3)
        {
            // Deactivate all
            for (int i = 0; i < modePanels.Length; i++)
            {
                if (modePanels[i] != null)
                    modePanels[i].SetActive(false);
            }

            // Activate the correct one
            int index = currentMode switch
            {
                ToolMode.Mining => 0,
                ToolMode.Tractor => 1,
                ToolMode.Repair => 2,
                _ => 0
            };

            if (modePanels[index] != null)
                modePanels[index].SetActive(true);
        }
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

        // Activate the appropriate beam emitter based on current mode
        ActivateBeamEmitter();

        // Hide LineRenderer when useVFXVisuals is enabled, show it otherwise
        if (beamRenderer != null)
        {
            if (useVFXVisuals)
            {
                beamRenderer.startWidth = 0f;
                beamRenderer.endWidth = 0f;
            }
            else
            {
                beamRenderer.startWidth = beamWidth;
                beamRenderer.endWidth = beamWidth;
            }
        }

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

    private void ActivateBeamEmitter()
    {
        // Only activate the current mode's emitter, leave others off
        if (miningBeamEmitterRef != null)
            miningBeamEmitterRef.SetActive(currentMode == ToolMode.Mining);
        if (tractorBeamEmitterRef != null)
            tractorBeamEmitterRef.SetActive(currentMode == ToolMode.Tractor);
        if (repairBeamEmitterRef != null)
            repairBeamEmitterRef.SetActive(currentMode == ToolMode.Repair);
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
            // start healing the last tracked rock gradually instead of instant reset
            if (currentTargetRock != null && rockHealCoroutine == null)
            {
                rockHealCoroutine = StartCoroutine(RestoreRockHealthOverTime(currentTargetRock, miningDecayDuration));
            }
        }
    }

    private System.Collections.IEnumerator RestoreRockHealthOverTime(MinableRock rock, float duration)
    {
        if (rock == null) yield break;
        float max = rock.GetMaxHealth();
        // linear restore per second so speed is independent of framerate and consistent
        float restorePerSecond = (max - rock.GetCurrentHealth()) / Mathf.Max(0.0001f, miningDecayDuration);
        while (rock.GetCurrentHealth() < max)
        {
            float delta = restorePerSecond * Time.deltaTime;
            rock.ModifyHealth(delta);
            yield return null;
        }
        // ensure full reset
        rock.ModifyHealth(max - rock.GetCurrentHealth());
        // clear tracked rock
        if (trackedRock == rock)
            trackedRock = null;
        rockHealCoroutine = null;
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

            CheckSnapSocket();

            PlaySound(tractorSound);
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

    private void UnsubscribeFromCurrentRock()
    {
        if (currentTargetRock != null)
        {
            // remove event listener and clear current target; do not instantly reset health
            currentTargetRock.OnMined.RemoveListener(OnRockMined);
            currentTargetRock = null;

            if (miningResetCoroutine != null)
            {
                StopCoroutine(miningResetCoroutine);
                miningResetCoroutine = null;
            }
        }
    }

    private void OnRockMined()
    {
        // start coroutine to animate slider back to zero
        if (miningProgressSlider != null)
        {
            if (miningResetCoroutine != null) StopCoroutine(miningResetCoroutine);
            miningResetCoroutine = StartCoroutine(AnimateSliderToZero(miningProgressSlider, 2f));
        }
        // clear current target reference
        currentTargetRock = null;
    }

    private System.Collections.IEnumerator AnimateSliderToZero(UnityEngine.UI.Slider s, float duration)
    {
        if (s == null) yield break;
        float start = s.value;
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            s.value = Mathf.Lerp(start, 0f, elapsed / duration);
            yield return null;
        }
        s.value = 0f;
    }

    private void UnsubscribeFromCurrentRepairable()
    {
        if (currentTargetRepairable != null)
        {
            currentTargetRepairable.OnRepaired.RemoveListener(OnRepairComplete);
            currentTargetRepairable = null;
        }
    }

    private void OnRepairComplete()
    {
        // animate slider back to zero when repair completes
        if (repairProgressSlider != null)
        {
            StartCoroutine(AnimateSliderToZero(repairProgressSlider, 1.5f));
        }
        if (trackedRepairable != null)
        {
            trackedRepairable.OnRepaired.RemoveListener(OnRepairComplete);
            trackedRepairable = null;
        }
    }

    // Called to begin a smooth decay of the repair slider and progress when contact is lost
    private void StartRepairDecay()
    {
        if (trackedRepairable == null || repairProgressSlider == null) return;
        if (repairDecayCoroutine != null) StopCoroutine(repairDecayCoroutine);
        repairDecayCoroutine = StartCoroutine(RepairDecayCoroutine(trackedRepairable, repairProgressSlider, repairDecayDuration));
    }

    private System.Collections.IEnumerator RepairDecayCoroutine(RepairableObject obj, UnityEngine.UI.Slider s, float duration)
    {
        if (obj == null || s == null) yield break;
        // compute repair time removed per second so decay is linear and configurable
        float totalRepairTime = obj.GetMaxRepairTime();
        float currentTimeRemaining = obj.GetRepairProgressNormalized() * totalRepairTime;
        float removePerSecond = currentTimeRemaining / Mathf.Max(0.0001f, repairDecayDuration);
        while (s.value > 0f)
        {
            float deltaTime = removePerSecond * Time.deltaTime;
            obj.ModifyRepairProgress(-deltaTime);
            if (repairProgressSlider != null)
                s.value = obj.GetRepairProgressNormalized();
            yield return null;
        }
        if (s != null) s.value = 0f;
        repairDecayCoroutine = null;
    }

    private void CheckSnapSocket()
    {
        TransportObjective transportObj = heldRigidbody.GetComponent<TransportObjective>();
        if (transportObj == null || transportObj.TargetSocket == null) return;

        SnapSocket socket = transportObj.TargetSocket;
        float distance = Vector3.Distance(heldRigidbody.transform.position, socket.transform.position);

        if (distance <= transportObj.SnapDistance)
        {
            socket.SnapObject(heldRigidbody.transform);
            heldRigidbody.isKinematic = true;

            FindObjectOfType<ObjectiveManager>()?.OnTransportObjectiveCompleted(heldRigidbody.gameObject);

            heldRigidbody = null;
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
            UpdateImpactParticles(hit.point, hit.normal);
            RepairableObject repairObj = hit.collider.GetComponent<RepairableObject>();
            if (repairObj != null)
            {
                // apply repair
                repairObj.Repair(repairPerSecond * Time.deltaTime);

                // ensure slider is assigned and updated
                if (repairProgressSlider != null)
                {
                    repairProgressSlider.value = repairObj.GetRepairProgressNormalized();
                }

                // set tracked repairable so decay can continue when we lose aim
                trackedRepairable = repairObj;
            }
        }
        else
        {
            DrawBeamToRange(maxRange);
            StopEffects();
            // start a smooth decay when we lose aim
            if (trackedRepairable != null)
            {
                StartRepairDecay();
            }
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
        if (heldRigidbody != null)
        {
            TransportObjective transportObj = heldRigidbody.GetComponent<TransportObjective>();
            if (transportObj != null && transportObj.TargetSocket != null)
            {
                transportObj.TargetSocket.ShowPreview(rb.gameObject);
            }
        }
        originalUseGravity = rb.useGravity;
        originalIsKinematic = rb.isKinematic;

        rb.useGravity = false;
        rb.linearVelocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        releaseVelocity = Vector3.zero;

        Debug.Log("Picked up: " + col.name);
    }

    public void StopActive()
    {
        beamRenderer.enabled = false;
        StopEffects();

        // Deactivate all beam emitters
        if (miningBeamEmitterRef != null) miningBeamEmitterRef.SetActive(false);
        if (tractorBeamEmitterRef != null) tractorBeamEmitterRef.SetActive(false);
        if (repairBeamEmitterRef != null) repairBeamEmitterRef.SetActive(false);

        // hide preview before releasing/clearing held rigidbody
        if (heldRigidbody != null)
        {
            TransportObjective transportObj = heldRigidbody.GetComponent<TransportObjective>();
            if (transportObj != null && transportObj.TargetSocket != null)
            {
                transportObj.TargetSocket.HidePreview();
            }

            heldRigidbody.isKinematic = originalIsKinematic;
            heldRigidbody.useGravity = originalUseGravity;
            heldRigidbody.linearVelocity = releaseVelocity;
            heldRigidbody.angularVelocity = Vector3.zero;
            heldRigidbody = null;
        }

        // If we stop using the tool while mining, reset the current rock
        if (currentMode == ToolMode.Mining && currentTargetRock != null)
        {
            // start smooth heal instead of instant reset
            trackedRock = currentTargetRock;
            trackedRock.OnMined.RemoveListener(OnRockMined);
            currentTargetRock = null;
            if (rockHealCoroutine == null)
                rockHealCoroutine = StartCoroutine(RestoreRockHealthOverTime(trackedRock, miningDecayDuration));
        }

        // If we stop using the tool while repairing, start decaying the repair progress
        if (currentMode == ToolMode.Repair && currentTargetRepairable != null)
        {
            // nothing special here — decay will be handled in PerformRepair when we exit raycast
            // but ensure slider exists
            if (repairProgressSlider != null)
                repairProgressSlider.value = currentTargetRepairable.GetRepairProgressNormalized();
        }

        if (currentMode == ToolMode.Repair && trackedRepairable != null)
        {
            StartRepairDecay();
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