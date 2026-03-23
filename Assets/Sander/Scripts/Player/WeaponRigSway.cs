using UnityEngine;

[DisallowMultipleComponent]
public class WeaponRigSway : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform of the rig (arms+gun) to sway")]
    [SerializeField] private Transform rigTransform;
    [Tooltip("Reference to the main camera for relative offset calcs")]
    [SerializeField] private Camera mainCamera;
    [Tooltip("Reference to the PlayerInputHandler to read movement input")]
    [SerializeField] private PlayerInputHandler inputHandler;

    [Header("Sway Settings")]
    [SerializeField] private float rigSwayMultiplier = 1.25f;
    [SerializeField] private float smoothTime = 0.14f;

    private Vector3 originalRigLocalPos;
    private Vector3 velocity;

    void Start()
    {
        if (rigTransform != null) originalRigLocalPos = rigTransform.localPosition;
        if (mainCamera == null) mainCamera = Camera.main;
        if (inputHandler == null) inputHandler = GetComponentInParent<PlayerInputHandler>();
    }

    void LateUpdate()
    {
        if (rigTransform == null || inputHandler == null) return;

        // movement input from handler (x = strafe, y = forward)
        Vector3 input = new Vector3(inputHandler.MovementInput.x, 0f, inputHandler.MovementInput.y);
        // base sway amounts match previous camera sway defaults
        Vector3 offset = new Vector3(-input.x * 0.04f, 0f, -input.z * 0.08f);
        Vector3 target = originalRigLocalPos + offset * rigSwayMultiplier;

        rigTransform.localPosition = Vector3.SmoothDamp(rigTransform.localPosition, target, ref velocity, smoothTime);
    }
}
