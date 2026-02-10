using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputHandler : MonoBehaviour
{
    [Header("Input Action Asset")]
    [SerializeField] private InputActionAsset playerControls;

    [Header("Action Map Name Reference")]
    [SerializeField] private string actionMapName = "Player";

    [Header("Action Name References")]
    [SerializeField] private string movement = "Movement";
    [SerializeField] private string rotation = "Rotation";
    [SerializeField] private string jump = "Jump";
    [SerializeField] private string sprint = "Sprint";
    [SerializeField] private string mine = "Mine";
    [SerializeField] private string toggleMode = "ToggleMode";  // New: Right Mouse
    [SerializeField] private string scrollDistance = "ScrollDistance";  // New: Mouse Wheel

    private InputAction movementAction;
    private InputAction rotationAction;
    private InputAction jumpAction;
    private InputAction sprintAction;
    private InputAction mineAction;
    private InputAction toggleModeAction;
    private InputAction scrollDistanceAction;

    public Vector2 MovementInput { get; private set; }
    public Vector2 RotationInput { get; private set; }
    public bool JumpTriggered { get; private set; }
    public bool SprintTriggered { get; private set; }
    public bool IsMining { get; private set; }
    public bool ToggleModeTriggered { get; private set; }  // New
    public float ScrollInput { get; private set; }  // New: Wheel delta

    private void Awake()
    {
        InputActionMap mapReference = playerControls.FindActionMap(actionMapName);

        movementAction = mapReference.FindAction(movement);
        rotationAction = mapReference.FindAction(rotation);
        jumpAction = mapReference.FindAction(jump);
        sprintAction = mapReference.FindAction(sprint);
        mineAction = mapReference.FindAction(mine);
        toggleModeAction = mapReference.FindAction(toggleMode);
        scrollDistanceAction = mapReference.FindAction(scrollDistance);

        SubscribeActionValuesToInputEvents();
    }

    private void SubscribeActionValuesToInputEvents()
    {
        movementAction.performed += inputInfo => MovementInput = inputInfo.ReadValue<Vector2>();
        movementAction.canceled += inputInfo => MovementInput = Vector2.zero;

        rotationAction.performed += inputInfo => RotationInput = inputInfo.ReadValue<Vector2>();
        rotationAction.canceled += inputInfo => RotationInput = Vector2.zero;

        jumpAction.performed += inputInfo => JumpTriggered = true;
        jumpAction.canceled += inputInfo => JumpTriggered = false;

        sprintAction.performed += inputInfo => SprintTriggered = true;
        sprintAction.canceled += inputInfo => SprintTriggered = false;

        mineAction.started += _ => IsMining = true;
        mineAction.canceled += _ => IsMining = false;

        toggleModeAction.performed += _ => ToggleModeTriggered = true;

        scrollDistanceAction.performed += inputInfo => ScrollInput = inputInfo.ReadValue<float>();
        scrollDistanceAction.canceled += inputInfo => ScrollInput = 0f;
    }

    private void OnEnable()
    {
        playerControls.FindActionMap(actionMapName).Enable();
    }

    private void OnDisable()
    {
        playerControls.FindActionMap(actionMapName).Disable();
    }

    private void Update()
    {
        ToggleModeTriggered = false;  // Reset per frame
        ScrollInput = 0f;  // Reset per frame after use
    }
}