using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.UI;

public class AudioManager : MonoBehaviour
{
    [Header("Audio Mixer")]
    [Tooltip("Optional. Only needed if you still want mixer-level routing. Volume scaling is handled per-source.")]
    [SerializeField] private AudioMixer audioMixer;

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

    [Header("Volume Sliders")]
    [SerializeField] private Slider masterVolumeSlider;
    [SerializeField] private Slider musicVolumeSlider;
    [SerializeField] private Slider voiceOverVolumeSlider;

    [Header("Scene References (auto-found if left empty)")]
    [SerializeField] private SharedTimerSliders timerSliders;
    [SerializeField] private PlayerInputHandler playerInputHandler;
    [SerializeField] private Multitool multitool;

    // Runtime state flags
    private bool fiveMinPlayed  = false;
    private bool twoMinPlayed   = false;
    private bool winPlayed      = false;
    private bool losePlayed     = false;

    // Cached win / lose canvas references resolved at start
    private GameObject winCanvas;
    private GameObject loseCanvas;

    // Current slider values (0-1), defaulting to full volume
    private float masterVolume   = 1f;
    private float musicVolume    = 1f;
    private float voiceOverVolume = 1f;

    // Base volumes captured from the Inspector before any slider applies
    private float baseFootsteps;
    private float baseMissionAccomplished;
    private float baseMissionFailed;
    private float baseFiveMinLeft;
    private float baseTwoMinLeft;
    private float baseMissionBriefing;
    private float baseMiningBeam;
    private float baseTractorBeam;
    private float baseRepairBeam;
    private float baseBatteryNoise;
    private float baseMusic;
    private float baseWind;

    private void Awake()
    {
        // Capture Inspector volumes as baselines before anything modifies them
        baseFootsteps          = GetBaseVolume(footstepsSource);
        baseMissionAccomplished = GetBaseVolume(missionAccomplishedSource);
        baseMissionFailed      = GetBaseVolume(missionFailedSource);
        baseFiveMinLeft        = GetBaseVolume(fiveMinLeftSource);
        baseTwoMinLeft         = GetBaseVolume(twoMinLeftSource);
        baseMissionBriefing    = GetBaseVolume(missionBriefingSource);
        baseMiningBeam         = GetBaseVolume(miningBeamSource);
        baseTractorBeam        = GetBaseVolume(tractorBeamSource);
        baseRepairBeam         = GetBaseVolume(repairBeamSource);
        baseBatteryNoise       = GetBaseVolume(batteryNoiseSource);
        baseMusic              = GetBaseVolume(musicSource);
        baseWind               = GetBaseVolume(windSource);

        // Silence all voice-over sources immediately in case Play On Awake is enabled in the Inspector
        StopAndDisablePlayOnAwake(missionAccomplishedSource);
        StopAndDisablePlayOnAwake(missionFailedSource);
        StopAndDisablePlayOnAwake(fiveMinLeftSource);
        StopAndDisablePlayOnAwake(twoMinLeftSource);
        StopAndDisablePlayOnAwake(missionBriefingSource);

        // Auto-find scene references when not assigned
        if (timerSliders == null)
            timerSliders = Object.FindFirstObjectByType<SharedTimerSliders>();
        if (playerInputHandler == null)
            playerInputHandler = Object.FindFirstObjectByType<PlayerInputHandler>();
        if (multitool == null)
            multitool = Object.FindFirstObjectByType<Multitool>();
    }

    private float GetBaseVolume(AudioSource source)
    {
        return source != null ? source.volume : 1f;
    }

    private void StopAndDisablePlayOnAwake(AudioSource source)
    {
        if (source == null) return;
        source.playOnAwake = false;
        source.Stop();
    }

    private void Start()
    {
        // Resolve win / lose canvases
        ObjectiveManager objManager = Object.FindFirstObjectByType<ObjectiveManager>();
        if (objManager != null)
            winCanvas = objManager.GetWinCanvas();

        if (timerSliders != null)
            loseCanvas = timerSliders.GetGameOverCanvas();

        // Hook up volume sliders — read their current Inspector value as the starting volume
        if (masterVolumeSlider != null)
        {
            masterVolume = masterVolumeSlider.value;
            masterVolumeSlider.onValueChanged.AddListener(SetMasterVolume);
        }
        if (musicVolumeSlider != null)
        {
            musicVolume = musicVolumeSlider.value;
            musicVolumeSlider.onValueChanged.AddListener(SetMusicVolume);
        }
        if (voiceOverVolumeSlider != null)
        {
            voiceOverVolume = voiceOverVolumeSlider.value;
            voiceOverVolumeSlider.onValueChanged.AddListener(SetVoiceOverVolume);
        }

        // Apply initial slider values to all sources
        ApplyAllVolumes();

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

    public void OnPlayPressed()
    {
        StartCoroutine(PlayBriefingAfterDelay());
    }

    public void ResetAudioState()
    {
        fiveMinPlayed = false;
        twoMinPlayed  = false;
        winPlayed     = false;
        losePlayed    = false;
    }

    public void PauseAudio()
    {
        PauseSource(footstepsSource);
        PauseSource(miningBeamSource);
        PauseSource(tractorBeamSource);
        PauseSource(repairBeamSource);
        PauseSource(batteryNoiseSource);
        PauseSource(musicSource);
        PauseSource(windSource);
        PauseSource(missionAccomplishedSource);
        PauseSource(missionFailedSource);
        PauseSource(fiveMinLeftSource);
        PauseSource(twoMinLeftSource);
        PauseSource(missionBriefingSource);
    }

    public void ResumeAudio()
    {
        UnPauseSource(footstepsSource);
        UnPauseSource(miningBeamSource);
        UnPauseSource(tractorBeamSource);
        UnPauseSource(repairBeamSource);
        UnPauseSource(batteryNoiseSource);
        UnPauseSource(musicSource);
        UnPauseSource(windSource);
        UnPauseSource(missionAccomplishedSource);
        UnPauseSource(missionFailedSource);
        UnPauseSource(fiveMinLeftSource);
        UnPauseSource(twoMinLeftSource);
        UnPauseSource(missionBriefingSource);
    }

    private void PauseSource(AudioSource source)
    {
        if (source != null && source.isPlaying)
            source.Pause();
    }

    private void UnPauseSource(AudioSource source)
    {
        if (source != null)
            source.UnPause();
    }

    public void SetMasterVolume(float sliderValue)
    {
        masterVolume = sliderValue;
        ApplyAllVolumes();
    }

    public void SetMusicVolume(float sliderValue)
    {
        musicVolume = sliderValue;
        ApplyMusicVolumes();
    }

    public void SetVoiceOverVolume(float sliderValue)
    {
        voiceOverVolume = sliderValue;
        ApplyVoiceOverVolumes();
    }

    private void ApplyAllVolumes()
    {
        ApplySfxVolumes();
        ApplyMusicVolumes();
        ApplyVoiceOverVolumes();
    }

    private void ApplySfxVolumes()
    {
        SetSourceVolume(footstepsSource,  baseFootsteps,  masterVolume);
        SetSourceVolume(miningBeamSource, baseMiningBeam, masterVolume);
        SetSourceVolume(tractorBeamSource, baseTractorBeam, masterVolume);
        SetSourceVolume(repairBeamSource, baseRepairBeam, masterVolume);
        SetSourceVolume(batteryNoiseSource, baseBatteryNoise, masterVolume);
        SetSourceVolume(windSource, baseWind, masterVolume);
    }

    private void ApplyMusicVolumes()
    {
        SetSourceVolume(musicSource, baseMusic, musicVolume * masterVolume);
    }

    private void ApplyVoiceOverVolumes()
    {
        float combined = voiceOverVolume * masterVolume;
        SetSourceVolume(missionAccomplishedSource, baseMissionAccomplished, combined);
        SetSourceVolume(missionFailedSource,       baseMissionFailed,       combined);
        SetSourceVolume(fiveMinLeftSource,         baseFiveMinLeft,         combined);
        SetSourceVolume(twoMinLeftSource,          baseTwoMinLeft,          combined);
        SetSourceVolume(missionBriefingSource,     baseMissionBriefing,     combined);
    }

    private void SetSourceVolume(AudioSource source, float baseVolume, float multiplier)
    {
        if (source == null) return;
        source.volume = baseVolume * Mathf.Clamp01(multiplier);
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
        yield return new WaitForSecondsRealtime(missionBriefingDelay);
        PlayOneShot(missionBriefingSource);
    }
}
