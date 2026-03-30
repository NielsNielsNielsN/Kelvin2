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
    [Tooltip("Reference to the Multitool to read firing state")]
    [SerializeField] private Multitool multitool;

    [Header("Sway Settings")]
    [SerializeField] private float rigSwayMultiplier = 1.25f;
    [SerializeField] private float smoothTime = 0.14f;

    [Header("Weapon Kickback")]
    [Tooltip("How far back the weapon is pushed along local Z when firing")]
    [SerializeField] private float kickbackDistance = 0.04f;
    [Tooltip("How quickly the weapon kicks back (lower = snappier kick)")]
    [SerializeField] private float kickbackSmoothTime = 0.04f;
    [Tooltip("How quickly the weapon returns to rest after firing stops")]
    [SerializeField] private float kickbackReturnSmoothTime = 0.12f;

    [Header("Camera Shake")]
    [Tooltip("Maximum positional offset applied to the camera while firing")]
    [SerializeField] private float cameraShakeIntensity = 0.004f;
    [Tooltip("Speed at which the shake noise scrolls (higher = more rapid shake)")]
    [SerializeField] private float cameraShakeSpeed = 22f;

    private Vector3 originalRigLocalPos;
    private Vector3 swayVelocity;
    private Vector3 kickbackVelocity;
    private Vector3 currentKickback;
    private Vector3 originalCameraLocalPos;
    private float shakeNoiseOffset;

    void Start()
    {
        if (rigTransform != null) originalRigLocalPos = rigTransform.localPosition;
        if (mainCamera == null) mainCamera = Camera.main;
        if (inputHandler == null) inputHandler = GetComponentInParent<PlayerInputHandler>();
        if (multitool == null) multitool = GetComponentInParent<Multitool>();
        if (mainCamera != null) originalCameraLocalPos = mainCamera.transform.localPosition;

        shakeNoiseOffset = Random.Range(0f, 100f);
    }

    void LateUpdate()
    {
        if (rigTransform == null || inputHandler == null) return;

        bool firing = multitool != null && multitool.IsFiring;

        ApplyRigSway(firing);
        ApplyCameraShake(firing);
    }

    private void ApplyRigSway(bool firing)
    {
        // movement sway
        Vector3 input = new Vector3(inputHandler.MovementInput.x, 0f, inputHandler.MovementInput.y);
        Vector3 swayOffset = new Vector3(-input.x * 0.04f, 0f, -input.z * 0.08f) * rigSwayMultiplier;

        // kickback: push weapon back along local Z while firing, return when not
        Vector3 targetKickback = firing ? new Vector3(0f, 0f, -kickbackDistance) : Vector3.zero;
        float smoothT = firing ? kickbackSmoothTime : kickbackReturnSmoothTime;
        currentKickback = Vector3.SmoothDamp(currentKickback, targetKickback, ref kickbackVelocity, smoothT);

        Vector3 target = originalRigLocalPos + swayOffset + currentKickback;
        rigTransform.localPosition = Vector3.SmoothDamp(rigTransform.localPosition, target, ref swayVelocity, smoothTime);
    }

    private void ApplyCameraShake(bool firing)
    {
        if (mainCamera == null) return;

        if (firing)
        {
            shakeNoiseOffset += Time.deltaTime * cameraShakeSpeed;
            float shakeX = (Mathf.PerlinNoise(shakeNoiseOffset, 0f) - 0.5f) * 2f * cameraShakeIntensity;
            float shakeY = (Mathf.PerlinNoise(0f, shakeNoiseOffset) - 0.5f) * 2f * cameraShakeIntensity;

            // offset is additive on top of whatever FirstPersonController set this frame
            mainCamera.transform.localPosition += new Vector3(shakeX, shakeY, 0f);
        }
    }
}
