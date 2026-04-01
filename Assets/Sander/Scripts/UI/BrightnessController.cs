using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;

public class BrightnessController : MonoBehaviour
{
    [Header("Brightness Slider")]
    [Tooltip("Slider that controls in-game brightness (0 = darkest, 1 = brightest)")]
    [SerializeField] private Slider brightnessSlider;

    [Header("Post Processing")]
    [Tooltip("The HDRP Global Volume that contains a Color Adjustments override")]
    [SerializeField] private Volume globalVolume;

    [Header("Settings")]
    [Tooltip("Post-exposure EV mapped to slider minimum (e.g. -3 = very dark)")]
    [SerializeField] private float minExposure = -3f;
    [Tooltip("Post-exposure EV mapped to slider maximum (e.g. 3 = very bright)")]
    [SerializeField] private float maxExposure = 3f;
    [Tooltip("Default slider value on first launch (0 = darkest, 1 = brightest)")]
    [Range(0f, 1f)][SerializeField] private float defaultBrightness = 0.5f;

    private const string BrightnessPrefKey = "BrightnessSetting";

    private ColorAdjustments colorAdjustments;

    private void Awake()
    {
        if (globalVolume == null)
            globalVolume = FindFirstObjectByType<Volume>();

        if (globalVolume != null && !globalVolume.profile.TryGet(out colorAdjustments))
            Debug.LogWarning("[BrightnessController] No ColorAdjustments override found on the assigned Volume profile. Add one and enable Post Exposure.");
    }

    private void Start()
    {
        float saved = PlayerPrefs.GetFloat(BrightnessPrefKey, defaultBrightness);

        if (brightnessSlider != null)
        {
            brightnessSlider.value = saved;
            brightnessSlider.onValueChanged.AddListener(SetBrightness);
        }

        ApplyBrightness(saved);
    }

    public void SetBrightness(float sliderValue)
    {
        ApplyBrightness(sliderValue);
        PlayerPrefs.SetFloat(BrightnessPrefKey, sliderValue);
        PlayerPrefs.Save();
    }

    private void ApplyBrightness(float sliderValue)
    {
        if (colorAdjustments == null) return;

        float exposure = Mathf.Lerp(minExposure, maxExposure, Mathf.Clamp01(sliderValue));
        colorAdjustments.postExposure.value = exposure;
    }

    // Editor helper
    [ContextMenu("Reset Brightness to Default")]
    private void EditorReset()
    {
        if (brightnessSlider != null)
            brightnessSlider.value = defaultBrightness;
        else
            ApplyBrightness(defaultBrightness);
    }
}
