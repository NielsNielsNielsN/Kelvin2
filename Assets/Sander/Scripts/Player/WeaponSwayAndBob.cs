using UnityEngine;

[DisallowMultipleComponent]
public class WeaponSwayAndBob : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Transform of the rig (arms+gun) to sway and bob")]
    public Transform rigTransform;
    [Tooltip("Player input handler (reads movement and rotation)")]
    public PlayerInputHandler inputHandler;
    [Tooltip("Optional character controller used to determine grounded state")]
    public CharacterController characterController;

    [Header("Sway")]
    public float step = 0.01f;
    public float maxStepDistance = 0.06f;
    private Vector3 swayPos;

    [Header("Sway Rotation")]
    public float rotationStep = 4f;
    public float maxRotationStep = 5f;
    private Vector3 swayEulerRot;

    [Header("Smoothing")]
    public float smooth = 10f;
    public float smoothRot = 12f;

    [Header("Bobbing")]
    public float speedCurve = 0f;
    private float CurveSin => Mathf.Sin(speedCurve);
    private float CurveCos => Mathf.Cos(speedCurve);

    public Vector3 travelLimit = default(Vector3);
    public Vector3 bobLimit = default(Vector3);
    private Vector3 bobPosition;

    [Tooltip("How strongly movement affects bob speed")]
    public float bobExaggeration = 1f;

    [Header("Bob Rotation")]
    public Vector3 multiplier = new Vector3(1f, 1f, 1f);
    private Vector3 bobEulerRotation;

    // runtime
    private Vector2 walkInput;
    private Vector2 lookInput;
    private Vector3 originalLocalPos;
    private Quaternion originalLocalRot;
    [Header("Runtime Options")]
    [Tooltip("If true, create an unanimated pivot above the rig and apply sway to it to avoid animation overwrite")]
    public bool createSwayPivot = true;

    private Transform swayPivot;
    private Vector3 originalPivotLocalPos;
    private Quaternion initialRigToCameraRot = Quaternion.identity;
    [Tooltip("Optional reference to main camera; if null Camera.main will be used")]
    public Camera mainCamera;
    [Header("Orientation Correction")]
    [Tooltip("Euler offset applied to final rig orientation (use to correct 90deg mismatches)")]
    public Vector3 orientationEulerOffset = Vector3.zero;

    void Reset()
    {
        // sensible defaults
        travelLimit = Vector3.one * 0.025f;
        bobLimit = Vector3.one * 0.01f;
        step = 0.01f;
        maxStepDistance = 0.06f;
        rotationStep = 4f;
        maxRotationStep = 5f;
        smooth = 10f;
        smoothRot = 12f;
        bobExaggeration = 1f;
    }

    void Start()
    {
        if (inputHandler == null) inputHandler = GetComponentInParent<PlayerInputHandler>();
        if (characterController == null) characterController = GetComponentInParent<CharacterController>();

        // If rigTransform not assigned, try to find a likely child on this object
        if (rigTransform == null)
        {
            // look for likely names
            var candidates = GetComponentsInChildren<Transform>(true);
            foreach (var t in candidates)
            {
                string n = t.name.ToLower();
                if (n.Contains("rig") || n.Contains("arm") || n.Contains("weapon") || n.Contains("gun"))
                {
                    rigTransform = t;
                    break;
                }
            }
        }

        if (rigTransform != null)
        {
            // If requested, create a pivot in world space at the rig's location and reparent the rig under it.
            if (createSwayPivot && rigTransform.parent != null)
            {
                var originalParent = rigTransform.parent;
                swayPivot = new GameObject(rigTransform.name + "_SwayPivot").transform;
                // parent pivot under the original parent
                swayPivot.SetParent(originalParent, true);
                // place pivot at rig's world transform
                swayPivot.position = rigTransform.position;
                swayPivot.rotation = rigTransform.rotation;
                swayPivot.localScale = rigTransform.localScale;

                // reparent rig under pivot, keeping world position
                rigTransform.SetParent(swayPivot, true);

                // now record originals relative to the pivot
                originalLocalPos = rigTransform.localPosition;
                originalLocalRot = rigTransform.localRotation;
                originalPivotLocalPos = swayPivot.localPosition;

                // compute initial offset from camera to rig so pivot can follow camera orientation
                if (mainCamera == null) mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    initialRigToCameraRot = Quaternion.Inverse(mainCamera.transform.rotation) * rigTransform.rotation;
                }
            }
            else
            {
                // no pivot, record original rig local transform relative to its parent
                originalLocalPos = rigTransform.localPosition;
                originalLocalRot = rigTransform.localRotation;
            }
        }
    }

    // Run in LateUpdate so animation updates have already been applied
    void LateUpdate()
    {
        if (inputHandler == null) return;

        GetInput();

        Sway();
        SwayRotation();
        BobOffset();
        BobRotation();

        CompositePositionRotation();
    }

    void GetInput()
    {
        // movementInput: x = strafe, y = forward
        walkInput = inputHandler.MovementInput;
        // rotationInput: x = mouseX, y = mouseY
        lookInput = inputHandler.RotationInput;
        // normalize movement for consistent behaviour
        if (walkInput.sqrMagnitude > 1f) walkInput.Normalize();
    }

    void Sway()
    {
        Vector3 invertLook = new Vector3(lookInput.x, lookInput.y, 0f) * -step;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxStepDistance, maxStepDistance);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxStepDistance, maxStepDistance);

        // apply as local X (lateral) and Y (vertical) offsets
        swayPos = new Vector3(invertLook.x, invertLook.y, 0f);
    }

    void SwayRotation()
    {
        Vector2 invertLook = new Vector2(lookInput.x, lookInput.y) * -rotationStep;
        invertLook.x = Mathf.Clamp(invertLook.x, -maxRotationStep, maxRotationStep);
        invertLook.y = Mathf.Clamp(invertLook.y, -maxRotationStep, maxRotationStep);
        // pitch from mouse Y, yaw from mouse X
        swayEulerRot = new Vector3(invertLook.y, invertLook.x, invertLook.x);
    }

    void CompositePositionRotation()
    {
        if (rigTransform == null) return;

        Vector3 targetPos = swayPos + bobPosition; // applied relative to pivot

        if (swayPivot != null)
        {
            Vector3 worldTarget = swayPivot.TransformPoint(originalPivotLocalPos + targetPos);
            Vector3 localTarget = swayPivot.InverseTransformPoint(worldTarget);
            swayPivot.localPosition = Vector3.Lerp(swayPivot.localPosition, originalPivotLocalPos + targetPos, Time.deltaTime * smooth);
        }
        else
        {
            // fallback: apply directly to rig transform
            Vector3 finalTarget = originalLocalPos + targetPos;
            rigTransform.localPosition = Vector3.Lerp(rigTransform.localPosition, finalTarget, Time.deltaTime * smooth);
        }

        Quaternion swayQuat = Quaternion.Euler(swayEulerRot);
        Quaternion bobQuat = Quaternion.Euler(bobEulerRotation);
        Quaternion targetRot = swayQuat * bobQuat * originalLocalRot;

        if (swayPivot != null)
        {
            // apply rotation relative to pivot: combine sway and bob rotations, but keep original rig orientation as base
            // Keep pivot oriented to camera + initial offset so the rig follows camera rotation
            Quaternion camRot = mainCamera != null ? mainCamera.transform.rotation : Camera.main.transform.rotation;
            Quaternion desiredRigWorldRot = camRot * initialRigToCameraRot * Quaternion.Euler(orientationEulerOffset);
            Quaternion targetPivotRot = Quaternion.Euler(swayEulerRot) * Quaternion.Euler(bobEulerRotation) * desiredRigWorldRot;
            swayPivot.rotation = Quaternion.Slerp(swayPivot.rotation, targetPivotRot, Time.deltaTime * smoothRot);
        }
        else
        {
            rigTransform.localRotation = Quaternion.Slerp(rigTransform.localRotation, targetRot, Time.deltaTime * smoothRot);
        }
    }

    void BobOffset()
    {
        bool grounded = true;
        if (characterController != null) grounded = characterController.isGrounded;

        // speedCurve increases based on movement magnitude when grounded
        float movementMagnitude = new Vector2(inputHandler.MovementInput.x, inputHandler.MovementInput.y).magnitude;
        speedCurve += Time.deltaTime * (grounded ? (movementMagnitude * bobExaggeration) : 1f) + 0.01f;

        bobPosition.x = (CurveCos * bobLimit.x * (grounded ? 1f : 0f)) - (walkInput.x * travelLimit.x);
        bobPosition.y = (CurveSin * bobLimit.y) - (walkInput.y * travelLimit.y);
        bobPosition.z = -(walkInput.y * travelLimit.z);
    }

    void BobRotation()
    {
        bool moving = walkInput != Vector2.zero;
        bobEulerRotation.x = (moving ? multiplier.x * (Mathf.Sin(2f * speedCurve)) : multiplier.x * (Mathf.Sin(2f * speedCurve) / 2f));
        bobEulerRotation.y = (moving ? multiplier.y * CurveCos : 0f);
        bobEulerRotation.z = (moving ? multiplier.z * CurveCos * walkInput.x : 0f);
    }
}
