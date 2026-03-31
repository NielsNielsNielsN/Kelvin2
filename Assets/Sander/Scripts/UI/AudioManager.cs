using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

/// <summary>
/// Central AudioManager that owns all game audio sources and reacts to game events.
/// Attach this to a persistent GameObject in the scene and wire up every field in the Inspector.
/// </summary>
public class AudioManager : MonoBehaviour
{
    // ?????????????????????????????????????????????????????????????
    //  Audio Mixer
    // ?????????????????????????????????????????????????????????????
    [Header("Audio Mixer")]
    [Tooltip("The main AudioMixer asset. Must expose parameters: MasterVolume, MusicVolume, VoiceOverVolume")]
    [SerializeField] private AudioMixer audioMixer;

    // ?????????????????????????????????????????????????????????????
    //  AudioSources
    // ?????????????????????????????????????????????????????????????
    [Header("Footsteps")]
    [SerializeField] private AudioSource footstepsSource;
    [Tooltip("Playback speed when walking (default 1)")]
    [SerializeField] private float walkPitch = 1f;
    [Tooltip("Playback speed when sprinting")]
    [SerializeField] private float sprintPitch = 1.4f;

    [Header("Voice-Over / Cues")]
    [SerializeField] private AudioSource missionAccomplishedSource;
    [SerializeField] private AudioSource missionFailedSource;
    [SerializeField] private AudioSource fiveMinLeftSource;
    [SerializeField] private AudioSource twoMinLeftSource;
    [SerializeField] private AudioSource missionBriefingSource;
    [Tooltip("Delay in seconds after pressing Play before the briefing starts")]
    [SerializeField] private float missionBriefingDelay = 3f;

    [Header("Beam Sounds")]
    [SerializeField] private AudioSource miningBeamSource;
    [SerializeField] private AudioSource tractorBeamSource;
    [SerializeField] private AudioSource repairBeamSource;

    [Header("Ambient / Continuous")]
    [SerializeField] private AudioSource batteryNoiseSource;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource windSource;

    // ?????????????????????????????????????????????????????????????
    //  Volume Sliders (assign in Inspector or via Settings canvas)
    // ?????????????????????????????????????????????????????????????
    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider voiceOverVolumeSlider;

    // ?????????????????????????????????????????????????????????????
    //  Game-state references (auto-found if not assigned)
    // ?????????????????????????????????????????????????????????????
    [Header("Scene References (auto-found if left empty)")]
    [SerializeField] private SharedTimerSliders timerSliders;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Multitool multitool;

    // Exposed mixer parameter names
    private const string MixerMaster    = "MasterVolume";
    private const string MixerMusic     = "MusicVolume";
    private const string MixerVoiceOver = "VoiceOverVolume";

    // Runtime state flags
    private bool fiveMinPlayed  = false;
    private bool twoMinPlayed   = false;
    private bool winPlayed      = false;
    private bool losePlayed     = false;

    // Cached win / lose canvas references resolved at start
    private GameObject winCanvas;
    private GameObject loseCanvas;

    // ?????????????????????????????????????????????????????????????
    //  Unity lifecycle
    // ?????????????????????????????????????????????????????????????

    private void Awake()
    {
        // Auto-find scene references when not assigned
        if (timerSliders == null)
            timerSliders = FindObjectOfType<SharedTimerSliders>();
        if (playerInputHandler == null)
            playerInputHandler = FindObjectOfType<PlayerInputHandler>();
        if (multitool == null)
            multitool = FindObjectOfType<Multitool>();
    }

    private void Start()
    {
        // Resolve win / lose canvases
        ObjectiveManager objManager = FindObjectOfType<ObjectiveManager>();
        if (objManager != null)
            winCanvas = objManager.GetWinCanvas();

        if (timerSliders != null)
            loseCanvas = timerSliders.GetGameOverCanvas();

        // Hook up volume sliders
        if (masterVolumeSlider != null)
        {
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
            masterVolumeSlider.value = masterVolumeSlider.value; // trigger initial update
        }
        if (musicVolumeSlider != null)
        {
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
            musicVolumeSlider.value = musicVolumeSlider.value;
        }
        if (voiceOverVolumeSlider != null)
        {
            voiceOverVolumeSlider.onValueChanged.AddListener(SetVoiceOverVolume);
            voiceOverVolumeSlider.value = voiceOverVolumeSlider.value;
        }

        // Looping ambient sounds start immediately
        StartLoopingSource(musicSource);
        StartLoopingSource(windSource);
        StartLoopingSource(batteryNoiseSource);
    }

    private void Update()
    {
        HandleFootsteps();
        HandleBeamAudio();
        HandleTimerCues();
        HandleScreenCues();
    }

    // ?????????????????????????????????????????????????????????????
    //  Public API — called from MenuUI
    // ?????????????????????????????????????????????????????????????

    /// <summary>Called by MenuUI.OnPlayButtonPressed to start the briefing countdown.</summary>
    public void OnPlayPressed()
    {
        StartCoroutine(PlayBriefingAfterDelay());
    }

    /// <summary>Resets all one-shot flags so audio cues fire again after a restart/retry.</summary>
    public void ResetAudioState()
    {
        fiveMinPlayed = false;
        twoMinPlayed  = false;
        winPlayed     = false;
        losePlayed    = false;
    }

    // ?????????????????????????????????????????????????????????????
    //  Volume control (mixer-based, logarithmic conversion)
    // ?????????????????????????????????????????????????????????????

    public void SetMasterVolume(float sliderValue)
    {
        SetMixerVolume(MixerMaster, sliderValue);
    }

    public void SetMusicVolume(float sliderValue)
    {
        SetMixerVolume(MixerMusic, sliderValue);
    }

    public void SetVoiceOverVolume(float sliderValue)
    {
        SetMixerVolume(MixerVoiceOver, sliderValue);
    }

    // ?????????????????????????????????????????????????????????????
    //  Private helpers
    // ?????????????????????????????????????????????????????????????

    private void SetMixerVolume(string parameter, float sliderValue)
    {
        if (audioMixer == null) return;
        // Sliders should be configured with a range of 0.0001 to 1.
        // Convert to decibels: 0.0001 ? -80 dB, 1 ? 0 dB
        float dB = Mathf.Log10(Mathf.Max(sliderValue, 0.0001f)) * 20f;
        audioMixer.SetFloat(parameter, dB);
    }

    private void StartLoopingSource(AudioSource source)
    {
        if (source == null) return;
        source.loop = true;
        if (!source.isPlaying)
            source.Play();
    }

    private void HandleFootsteps()
    {
        if (footstepsSource == null || playerInputHandler == null) return;

        bool isMoving = playerInputHandler.MovementInput.sqrMagnitude > 0.01f;
        bool isSprinting = playerInputHandler.SprintTriggered;

        if (isMoving)
        {
            footstepsSource.pitch = isSprinting ? sprintPitch : walkPitch;
            if (!footstepsSource.isPlaying)
                footstepsSource.Play();
        }
        else
        {
            if (footstepsSource.isPlaying)
                footstepsSource.Stop();
        }
    }

    private void HandleBeamAudio()
    {
        if (multitool == null) return;

        bool isFiring = multitool.IsFiring;
        ToolMode mode = multitool.currentMode;

        // Mining beam
        ToggleLoopingSource(miningBeamSource, isFiring && mode == ToolMode.Mining);
        // Tractor beam
        ToggleLoopingSource(tractorBeamSource, isFiring && mode == ToolMode.Tractor);
        // Repair beam
        ToggleLoopingSource(repairBeamSource, isFiring && mode == ToolMode.Repair);
    }

    private void ToggleLoopingSource(AudioSource source, bool shouldPlay)
    {
        if (source == null) return;
        if (shouldPlay)
        {
            source.loop = true;
            if (!source.isPlaying) source.Play();
        }
        else
        {
            if (source.isPlaying) source.Stop();
        }
    }

    private void HandleTimerCues()
    {
        if (timerSliders == null) return;
        if (!timerSliders.IsRunning()) return;

        float remaining = timerSliders.GetRemainingTime();

        if (!fiveMinPlayed && remaining <= 300f)
        {
            fiveMinPlayed = true;
            PlayOneShot(fiveMinLeftSource);
        }

        if (!twoMinPlayed && remaining <= 120f)
        {
            twoMinPlayed = true;
            PlayOneShot(twoMinLeftSource);
        }
    }

    private void HandleScreenCues()
    {
        // Win screen
        if (!winPlayed && winCanvas != null && winCanvas.activeInHierarchy)
        {
            winPlayed = true;
            PlayOneShot(missionAccomplishedSource);
        }

        // Lose / game-over screen
        if (!losePlayed && loseCanvas != null && loseCanvas.activeInHierarchy)
        {
            losePlayed = true;
            PlayOneShot(missionFailedSource);
        }
    }

    private void PlayOneShot(AudioSource source)
    {
        if (source == null) return;
        source.Stop();
        source.Play();
    }

    private IEnumerator PlayBriefingAfterDelay()
    {
        yield return new WaitForSeconds(missionBriefingDelay);
        PlayOneShot(missionBriefingSource);
    }
}
