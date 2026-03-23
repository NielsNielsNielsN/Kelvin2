using UnityEngine;
using System.Collections;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement Speeds")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float sprintMultiplier = 2.0f;

    [Header("Jump Parameters")]
    [SerializeField] private float jumpForce = 5.0f;
    [SerializeField] private float gravityMultiplier = 2.0f;

    [Header("Look Parameters")]
    [SerializeField] private float mouseSensitivity = 0.1f;
    [SerializeField] private float upDownLookRange = 80.0f;

    [Header("References")]
    [SerializeField] private CharacterController characterController;
    [SerializeField] private Camera mainCamera;
    [SerializeField] private PlayerInputHandler playerInputHandler;

    // NEW: Animation
    [Header("Animation")]
    [SerializeField] private Animator animator;  // Drag your Animator component here (on player or child model)
    [SerializeField] private Multitool multitool; // reference to the multitool to request mode switches
    private Coroutine switchWatcherCoroutine;

    private Vector3 currentMovement;
    private float verticalRotation;

    private float CurrentSpeed => walkSpeed * (playerInputHandler.SprintTriggered ? sprintMultiplier : 1.0f);

    // Animator parameter hashes (faster than string lookup)
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int IsSprintingHash = Animator.StringToHash("IsSprinting"); // optional bool
    private static readonly int MultitoolAHash = Animator.StringToHash("MultitoolA");
    private static readonly int MultitoolBHash = Animator.StringToHash("MultitoolB");
    private static readonly int PlayArmsHash = Animator.StringToHash("PlayArms");
    private static readonly int PlayGunHash = Animator.StringToHash("PlayGun");

    private static readonly int PlayInspectHash = Animator.StringToHash("PlayInspect");

    [Header("Animation Settings")]
    [SerializeField] private float animationBlendSpeed = 10f;

    [Header("Camera Sway")]
    [Tooltip("Maximum backward camera offset when moving forward")]
    [SerializeField] private float swayBackwardAmount = 0.08f;
    [Tooltip("Maximum lateral camera offset when strafing")]
    [SerializeField] private float swayLateralAmount = 0.04f;
    [Tooltip("Smoothing time for camera movement (lower = snappier)")]
    [SerializeField] private float swaySmoothTime = 0.12f;

    private Vector3 originalCameraLocalPos;
    private Vector3 cameraSwayVelocity;
    // Rig/gun sway moved to separate `WeaponRigSway` component

    [Header("Runtime Animation Names")]
    [Tooltip("Name of the base idle state to return to after Arms_Switch finishes")]
    [SerializeField] private string baseIdleStateName = "Idle";
    [Header("Inspect / Idle Action")]
    [Tooltip("Trigger state name (base layer) for the inspect/idle action animation")]
    [SerializeField] private string inspectStateName = "PlayerInspect";
    [Tooltip("Seconds of no input before auto-playing the inspect animation")]
    [SerializeField] private float inspectIdleTimeout = 10f;
    [Tooltip("Seconds used to crossfade back to idle when cancelling inspect")]
    [SerializeField] private float inspectCancelCrossfadeDuration = 0.12f;

    private float idleTimer = 0f;
    private bool isPlayingInspect = false;
    private Coroutine inspectWatcherCoroutine;
    private float inspectCooldownRemaining = 0f;

    private bool multitoolActive = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (mainCamera != null)
        {
            originalCameraLocalPos = mainCamera.transform.localPosition;
        }
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleMultitoolToggle();
        HandleInspectInputs();
        UpdateAnimation();
    }

    void LateUpdate()
    {
        // Apply camera / rig sway after Animator updates so it isn't overwritten by animation
        UpdateCameraSway();
    }

    private void UpdateCameraSway()
    {
        if (mainCamera == null || playerInputHandler == null) return;

        // Desired sway based on movement input in local space
        Vector3 input = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        // forward/backward sway pulls camera slightly backward when moving forward
        float forward = -input.z * swayBackwardAmount;
        float lateral = -input.x * swayLateralAmount;

        Vector3 targetLocal = originalCameraLocalPos + new Vector3(lateral, 0f, forward);

        mainCamera.transform.localPosition = Vector3.SmoothDamp(mainCamera.transform.localPosition, targetLocal, ref cameraSwayVelocity, swaySmoothTime);

        // rig sway is handled by separate WeaponRigSway component
    }

    private void HandleInspectInputs()
    {
        if (animator == null || playerInputHandler == null) return;

        // manual inspect trigger (I key) — we rely on input handler or raw key depending on setup
        if (Input.GetKeyDown(KeyCode.I) && !isPlayingInspect)
        {
            TriggerInspect();
            return;
        }

        // idle timer: reset when any movement/jump/mine/sprint input (exclude camera movement)
        // We intentionally ignore RotationInput so looking around doesn't cancel the inspect animation
        bool hasActivity = playerInputHandler.MovementInput.sqrMagnitude > 0f || playerInputHandler.IsMining || playerInputHandler.JumpTriggered || playerInputHandler.SprintTriggered;
        // also treat multitool switching as activity
        if (multitool != null && multitool.IsSwitching) hasActivity = true;
        if (hasActivity)
        {
            idleTimer = 0f;
            // cancel inspect if it's playing
            if (isPlayingInspect)
            {
                // Smoothly return to base idle state instead of snapping instantly
                if (!string.IsNullOrEmpty(baseIdleStateName))
                    animator.CrossFade(baseIdleStateName, inspectCancelCrossfadeDuration, 0, 0f);
                isPlayingInspect = false;
                if (inspectWatcherCoroutine != null)
                {
                    StopCoroutine(inspectWatcherCoroutine);
                    inspectWatcherCoroutine = null;
                }
            }
        }
        else
        {
            idleTimer += Time.deltaTime;
            // decrease cooldown so auto-inspect won't immediately retrigger
            inspectCooldownRemaining = Mathf.Max(0f, inspectCooldownRemaining - Time.deltaTime);
            if (idleTimer >= inspectIdleTimeout && !isPlayingInspect && inspectCooldownRemaining <= 0f)
            {
                TriggerInspect();
            }
        }
    }

    private void TriggerInspect()
    {
        if (!HasAnimatorParameter("PlayInspect"))
        {
            Debug.LogWarning("Animator missing 'PlayInspect' trigger parameter");
            return;
        }
        // Reset trigger then set to ensure single-shot
        animator.ResetTrigger(PlayInspectHash);
        animator.SetTrigger(PlayInspectHash);
        // prevent immediate re-trigger from idle timer
        idleTimer = 0f;
        // set a cooldown so auto-inspect won't retrigger while the animation loops
        inspectCooldownRemaining = inspectIdleTimeout;
        isPlayingInspect = true;
        if (inspectWatcherCoroutine != null) StopCoroutine(inspectWatcherCoroutine);
        inspectWatcherCoroutine = StartCoroutine(WatchInspectState(inspectStateName, 0));
    }

    private System.Collections.IEnumerator WatchInspectState(string stateName, int layerIndex)
    {
        float timeout = 0.5f;
        float elapsed = 0f;
        bool entered = false;
        while (elapsed < timeout)
        {
            var s = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (s.IsName(stateName)) { entered = true; break; }
            elapsed += Time.deltaTime;
            yield return null;
        }
        if (!entered) { isPlayingInspect = false; yield break; }

        // wait until completes one cycle
        // If the clip is looping, normalizedTime will keep increasing; detect loops by checking >=1f
        while (true)
        {
            var s = animator.GetCurrentAnimatorStateInfo(layerIndex);
            if (!s.IsName(stateName)) break;
            // normalizedTime may be >1 for looping; consider finished when it reaches >=1
            if (s.normalizedTime >= 1f) break;
            yield return null;
        }
        isPlayingInspect = false;
    }

    private void HandleMultitoolToggle()
    {
        if (playerInputHandler == null || animator == null) return;

        if (playerInputHandler.ToggleModeTriggered)
        {
            Debug.Log("FirstPersonController: ToggleModeTriggered detected");
            // Request multitool mode switch (this sets Multitool.isSwitching and blocks firing)
            if (multitool != null)
            {
                multitool.RequestToggleMode();
            }

            // Trigger both switch animations (Arms in Base layer, Gun in masked Gun layer)
            // Verify parameters exist to avoid runtime errors when animator is missing them
            if (HasAnimatorParameter("PlayArms"))
            {
                animator.SetTrigger(PlayArmsHash);
            }
            else
            {
                Debug.LogWarning("Animator is missing parameter 'PlayArms'. Add a Trigger parameter named 'PlayArms' to the Animator.");
            }

            if (HasAnimatorParameter("PlayGun"))
            {
                animator.SetTrigger(PlayGunHash);
            }
            else
            {
                Debug.LogWarning("Animator is missing parameter 'PlayGun'. Add a Trigger parameter named 'PlayGun' to the Animator.");
            }

            // start watcher to end switching when animations complete
            if (multitool != null && switchWatcherCoroutine == null)
            {
                switchWatcherCoroutine = StartCoroutine(WatchSwitchAnimations("Arms_Switch", "Gun_Switch", 1));
            }
        }
    }

    private System.Collections.IEnumerator WatchSwitchAnimations(string armsStateName, string gunStateName, int gunLayerIndex)
    {
        bool armsEntered = false;
        bool gunEntered = false;
        bool armsDone = false;
        bool gunDone = false;

        float timeout = 0.5f;
        float elapsed = 0f;

        // wait for either to enter (or timeout)
        while (elapsed < timeout && !(armsEntered || gunEntered))
        {
            var s0 = animator.GetCurrentAnimatorStateInfo(0);
            if (s0.IsName(armsStateName)) armsEntered = true;
            var s1 = animator.GetCurrentAnimatorStateInfo(gunLayerIndex);
            if (s1.IsName(gunStateName)) gunEntered = true;
            elapsed += Time.deltaTime;
            yield return null;
        }

        // now wait until both are finished (or not entered)
        while (!armsDone || !gunDone)
        {
            // check arms
            var s0 = animator.GetCurrentAnimatorStateInfo(0);
            if (!armsEntered)
            {
                // never entered -> consider done
                armsDone = true;
            }
            else
            {
                if (!s0.IsName(armsStateName)) armsDone = true; // left already
                else if (s0.normalizedTime >= 1f) armsDone = true; // completed
            }

            // check gun
            var s1 = animator.GetCurrentAnimatorStateInfo(gunLayerIndex);
            if (!gunEntered)
            {
                gunDone = true;
            }
            else
            {
                if (!s1.IsName(gunStateName)) gunDone = true;
                else if (s1.normalizedTime >= 1f) gunDone = true;
            }

            yield return null;
        }

        // Notify multitool that switching finished
        multitool?.EndSwitching();

        switchWatcherCoroutine = null;
    }

    private bool HasAnimatorParameter(string name)
    {
        if (animator == null) return false;
        var pars = animator.parameters;
        for (int i = 0; i < pars.Length; i++)
        {
            if (pars[i].name == name) return true;
        }
        return false;
    }

    // Removed EnsurePlayOnce helper — rely on Animator trigger + non-looping clip settings.

    private void UpdateAnimation()
    {
        if (animator == null) return;

        if (playerInputHandler == null) return;

        float moveMagnitude = new Vector2(playerInputHandler.MovementInput.x, playerInputHandler.MovementInput.y).magnitude;
        float targetSpeed = Mathf.Clamp01(moveMagnitude);

        // Smooth the value so it doesn't snap to 0 instantly
        float current = animator.GetFloat(SpeedHash);
        float smoothed = Mathf.Lerp(current, targetSpeed, animationBlendSpeed * Time.deltaTime);

        animator.SetFloat(SpeedHash, smoothed);
    }

    private Vector3 CalculateWorldDirection()
    {
        Vector3 inputDirection = new Vector3(playerInputHandler.MovementInput.x, 0f, playerInputHandler.MovementInput.y);
        Vector3 worldDirection = transform.TransformDirection(inputDirection);
        return worldDirection.normalized;
    }

    private void HandleJumping()
    {
        if (characterController.isGrounded)
        {
            currentMovement.y = -0.5f;
            if (playerInputHandler.JumpTriggered)
            {
                currentMovement.y = jumpForce;
            }
        }
        else
        {
            currentMovement.y += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
        }
    }

    private void HandleMovement()
    {
        Vector3 worldDirection = CalculateWorldDirection();
        currentMovement.x = worldDirection.x * CurrentSpeed;
        currentMovement.z = worldDirection.z * CurrentSpeed;
        HandleJumping();
        characterController.Move(currentMovement * Time.deltaTime);
    }

    private void ApplyHorizontalRotation(float rotationAmount)
    {
        transform.Rotate(0, rotationAmount, 0);
    }

    private void ApplyVerticalRotation(float rotationAmount)
    {
        verticalRotation = Mathf.Clamp(verticalRotation - rotationAmount, -upDownLookRange, upDownLookRange);
        mainCamera.transform.localRotation = Quaternion.Euler(verticalRotation, 0, 0);
    }

    private void HandleRotation()
    {
        float mouseXRotation = playerInputHandler.RotationInput.x * mouseSensitivity;
        float mouseYRotation = playerInputHandler.RotationInput.y * mouseSensitivity;
        ApplyHorizontalRotation(mouseXRotation);
        ApplyVerticalRotation(mouseYRotation);
    }
}