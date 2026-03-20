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
            // Trigger both upper-body arms and gun animations simultaneously.
            // Use triggers so the clips can play without changing base locomotion.
            animator.SetTrigger(PlayArmsHash);
            animator.SetTrigger(PlayGunHash);
        }
    }

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