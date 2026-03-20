using UnityEngine;

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

    [Header("Animation Settings")]
    [SerializeField] private float animationBlendSpeed = 10f;

    [Header("Runtime Animation Names")]
    [Tooltip("Name of the base idle state to return to after Arms_Switch finishes")]
    [SerializeField] private string baseIdleStateName = "Idle";

    private bool multitoolActive = false;

    void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    void Update()
    {
        HandleMovement();
        HandleRotation();
        HandleMultitoolToggle();
        UpdateAnimation();
    }

    private void HandleMultitoolToggle()
    {
        if (playerInputHandler == null || animator == null) return;

        if (playerInputHandler.ToggleModeTriggered)
        {
            Debug.Log("FirstPersonController: ToggleModeTriggered detected");
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
        }
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