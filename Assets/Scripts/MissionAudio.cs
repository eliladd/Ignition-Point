using System;
using UnityEngine;

/// <summary>
/// Procedural WebAudio-style atmospheric and telemetry sound synthesizer for Ares Sols: 2038.
/// Generates all ambient loops and UI telemetry sound effects at runtime without external audio assets.
/// </summary>
public sealed class MissionAudio : MonoBehaviour
{
    private const int SampleRate = 44100;

    private const float WindDurationSeconds = 4f;
    private const float WindCrossfadeSeconds = 0.15f;
    private const float MinWindVolume = 0.03f;
    private const float MaxWindVolume = 0.24f;
    private const float MinWindPitch = 0.78f;
    private const float MaxWindPitch = 1.22f;

    private const float ReactorDurationSeconds = 2f;
    private const float ReactorActiveVolume = 0.085f;

    private const float HoverCooldownSeconds = 0.045f;
    private const float WarningCooldownSeconds = 8f;
    private const int DangerThreshold = 25;
    private const float EnvironmentLerpSpeed = 3.5f;

    private AudioSource windSource;
    private AudioSource reactorSource;
    private AudioSource sfxSource;

    private AudioClip windClip;
    private AudioClip reactorClip;
    private AudioClip hoverTickClip;
    private AudioClip choiceGoodClip;
    private AudioClip choiceBadClip;
    private AudioClip inciteGoodClip;
    private AudioClip inciteBadClip;
    private AudioClip moduleInspectClip;
    private AudioClip warningPulseClip;
    private AudioClip pipBlipClip;
    private AudioClip novaBlipClip;

    private bool isInitialized;
    private float targetDustIntensity = 0.15f;
    private float targetSunlightBlocked = 0.10f;
    private bool reactorActive;
    private float targetWindVolume = 0.06f;
    private float targetWindPitch = 0.84f;

    private float lastHoverTime = -10f;
    private float lastWarningTime = -20f;
    private bool wasInDanger;

    /// <summary>
    /// Gets whether all mission audio output is currently muted.
    /// </summary>
    public bool IsMuted { get; private set; }

    /// <summary>
    /// Invoked whenever <see cref="IsMuted"/> changes via <see cref="ToggleMute"/>.
    /// </summary>
    public event Action<bool> MuteChanged;

    /// <summary>
    /// Finds an existing <see cref="MissionAudio"/> instance in the scene or creates one,
    /// ensures an active <see cref="AudioListener"/> exists, builds procedural clips, and returns it.
    /// </summary>
    /// <param name="parent">Optional parent transform for a newly created GameObject.</param>
    /// <returns>The active <see cref="MissionAudio"/> instance.</returns>
    public static MissionAudio EnsureInstance(Transform parent = null)
    {
        MissionAudio instance = FindAnyObjectByType<MissionAudio>();
        if (instance == null)
        {
            GameObject host = new GameObject("MissionAudio");
            if (parent != null)
            {
                host.transform.SetParent(parent, false);
            }

            EnsureAudioListener(host);
            instance = host.AddComponent<MissionAudio>();
        }
        else
        {
            EnsureAudioListener(instance.gameObject);
        }

        instance.EnsureInitialized();
        return instance;
    }

    private void Awake()
    {
        EnsureInitialized();
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }

        float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
        float blend = 1f - Mathf.Exp(-EnvironmentLerpSpeed * dt);

        if (windSource != null)
        {
            float desiredWindVolume = IsMuted ? 0f : targetWindVolume;
            windSource.volume = Mathf.Lerp(windSource.volume, desiredWindVolume, blend);
            windSource.pitch = Mathf.Lerp(windSource.pitch, targetWindPitch, blend);
        }

        if (reactorSource != null)
        {
            float desiredReactorVolume = (reactorActive && !IsMuted) ? ReactorActiveVolume : 0f;
            reactorSource.volume = Mathf.Lerp(reactorSource.volume, desiredReactorVolume, blend);
        }
    }

    private void OnDestroy()
    {
        DestroyClip(ref windClip);
        DestroyClip(ref reactorClip);
        DestroyClip(ref hoverTickClip);
        DestroyClip(ref choiceGoodClip);
        DestroyClip(ref choiceBadClip);
        DestroyClip(ref inciteGoodClip);
        DestroyClip(ref inciteBadClip);
        DestroyClip(ref moduleInspectClip);
        DestroyClip(ref warningPulseClip);
    }

    /// <summary>
    /// Toggles mute state across all procedural audio sources, notifies listeners,
    /// and plays a soft confirmation tick when unmuting.
    /// </summary>
    /// <returns>The updated <see cref="IsMuted"/> state.</returns>
    public bool ToggleMute()
    {
        EnsureInitialized();
        IsMuted = !IsMuted;
        ApplyMuteState();
        MuteChanged?.Invoke(IsMuted);

        if (!IsMuted && sfxSource != null && hoverTickClip != null)
        {
            sfxSource.PlayOneShot(hoverTickClip, 0.28f);
        }

        return IsMuted;
    }

    /// <summary>
    /// Plays a 35 ms high-tech telemetry hover tick, rate-limited by a short cooldown.
    /// </summary>
    public void PlayHoverTick()
    {
        if (IsMuted)
        {
            return;
        }

        EnsureInitialized();
        float now = Time.unscaledTime;
        if (now - lastHoverTime < HoverCooldownSeconds)
        {
            return;
        }

        lastHoverTime = now;
        if (sfxSource != null && hoverTickClip != null)
        {
            sfxSource.PlayOneShot(hoverTickClip, 0.25f);
        }
    }

    /// <summary>
    /// Plays a 110 ms two-tone confirmation chime for player decisions.
    /// </summary>
    /// <param name="isGood">True for an ascending confirmation chime; false for a lowered tone.</param>
    public void PlayChoiceClick(bool isGood = true)
    {
        PlayOneShotClip(isGood ? choiceGoodClip : choiceBadClip, 0.55f);
    }

    /// <summary>
    /// Plays a 180 ms subsystem incitement synth cue (3-note triad when positive, 2-tone alert when strained).
    /// </summary>
    /// <param name="isGood">True when the subsystem status is nominal/positive; false when strained.</param>
    public void PlaySubsystemIncite(bool isGood)
    {
        PlayOneShotClip(isGood ? inciteGoodClip : inciteBadClip, 0.55f);
    }

    /// <summary>
    /// Plays a short, friendly character voice chirp for Pip or Captain Nova.
    /// </summary>
    public void PlayCharacterBlip(bool isPip)
    {
        if (IsMuted)
        {
            return;
        }

        PlayOneShotClip(isPip ? pipBlipClip : novaBlipClip, isPip ? 0.35f : 0.40f);
    }

    /// <summary>
    /// Plays a 120 ms radar scan ping for inspecting base modules or NASA telemetry items.
    /// </summary>
    public void PlayModuleInspect()
    {
        PlayOneShotClip(moduleInspectClip, 0.50f);
    }

    /// <summary>
    /// Plays a 160 ms soft caution pulse for low power or low crew health alerts.
    /// </summary>
    public void PlayWarningPulse()
    {
        if (IsMuted)
        {
            return;
        }

        lastWarningTime = Time.unscaledTime;
        PlayOneShotClip(warningPulseClip, 0.45f);
    }

    /// <summary>
    /// Synchronizes wind intensity, Kilopower reactor hum, and low-resource warning cues with current mission telemetry.
    /// </summary>
    /// <param name="state">Current mission telemetry state, or null during non-gameplay transitions.</param>
    /// <param name="isDecisionActive">True when the Sol decision screen is active; false on briefing or outcome screens.</param>
    public void SyncEnvironment(GameState state, bool isDecisionActive)
    {
        EnsureInitialized();

        if (state != null)
        {
            targetDustIntensity = Mathf.Clamp01(state.dustParticleIntensity);
            targetSunlightBlocked = Mathf.Clamp01(state.sunlightBlockedPercent / 100f);
        }
        else
        {
            targetDustIntensity = 0.15f;
            targetSunlightBlocked = 0.10f;
        }

        reactorActive = isDecisionActive
            && state != null
            && !string.IsNullOrEmpty(state.reactorTag);

        float stormFactor = Mathf.Clamp01(targetDustIntensity * 0.7f + targetSunlightBlocked * 0.3f);
        float rawWindVolume = isDecisionActive
            ? Mathf.Lerp(0.06f, MaxWindVolume, stormFactor)
            : Mathf.Lerp(MinWindVolume, 0.13f, stormFactor);
        targetWindVolume = Mathf.Clamp(rawWindVolume, MinWindVolume, MaxWindVolume);

        float rawWindPitch = Mathf.Lerp(0.80f, MaxWindPitch, stormFactor);
        if (!isDecisionActive)
        {
            rawWindPitch -= 0.04f;
        }
        targetWindPitch = Mathf.Clamp(rawWindPitch, MinWindPitch, MaxWindPitch);

        if (state != null)
        {
            bool inDanger = state.power <= DangerThreshold || state.crewHealth <= DangerThreshold;
            bool enteredDanger = inDanger && !wasInDanger;
            bool cooldownElapsed = (Time.unscaledTime - lastWarningTime) > WarningCooldownSeconds;

            if (inDanger && (enteredDanger || cooldownElapsed))
            {
                PlayWarningPulse();
            }

            wasInDanger = inDanger;
        }
        else
        {
            wasInDanger = false;
        }
    }

    private void EnsureInitialized()
    {
        if (isInitialized)
        {
            return;
        }

        isInitialized = true;
        EnsureAudioListener(gameObject);

        windClip = GenerateMartianWindClip();
        reactorClip = GenerateReactorHumClip();
        hoverTickClip = GenerateChirpClip("Sfx_HoverTick", 0.035f, 1400f, 1900f, 5.5f, 0f, 0.002f, 0.55f);
        choiceGoodClip = GenerateSequenceClip("Sfx_ChoiceGood", 0.110f, new[] { 660f, 880f }, 0.14f);
        choiceBadClip = GenerateSequenceClip("Sfx_ChoiceBad", 0.110f, new[] { 520f, 440f }, 0.12f);
        inciteGoodClip = GenerateSequenceClip("Sfx_InciteGood", 0.180f, new[] { 523f, 659f, 784f }, 0.16f);
        inciteBadClip = GenerateSequenceClip("Sfx_InciteStrained", 0.180f, new[] { 440f, 349f }, 0.14f);
        moduleInspectClip = GenerateChirpClip("Sfx_ModuleInspect", 0.120f, 980f, 1320f, 4.2f, 0.12f, 0.004f, 0.65f);
        warningPulseClip = GenerateChirpClip("Sfx_WarningPulse", 0.160f, 380f, 380f, 3.2f, 0.18f, 0.012f, 0.60f);
        pipBlipClip = GenerateChirpClip("Sfx_PipBlip", 0.075f, 1200f, 1750f, 6.0f, 0.10f, 0.003f, 0.50f);
        novaBlipClip = GenerateChirpClip("Sfx_NovaBlip", 0.090f, 520f, 680f, 5.0f, 0.15f, 0.004f, 0.55f);

        windSource = CreateAudioSource("WindSource", windClip, true, IsMuted ? 0f : targetWindVolume);
        reactorSource = CreateAudioSource("ReactorSource", reactorClip, true, 0f);
        sfxSource = CreateAudioSource("SfxSource", null, false, 1f);

        windSource.Play();
        reactorSource.Play();
    }

    private static void EnsureAudioListener(GameObject fallbackObject)
    {
        if (FindAnyObjectByType<AudioListener>() != null)
        {
            return;
        }

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.gameObject.AddComponent<AudioListener>();
        }
        else if (fallbackObject != null)
        {
            fallbackObject.AddComponent<AudioListener>();
        }
    }

    private AudioSource CreateAudioSource(string childName, AudioClip clip, bool loop, float initialVolume)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(transform, false);
        AudioSource source = child.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.loop = loop;
        source.spatialBlend = 0f;
        source.dopplerLevel = 0f;
        source.volume = initialVolume;
        source.mute = IsMuted;
        source.clip = clip;
        return source;
    }

    private void ApplyMuteState()
    {
        if (windSource != null)
        {
            windSource.mute = IsMuted;
        }
        if (reactorSource != null)
        {
            reactorSource.mute = IsMuted;
        }
        if (sfxSource != null)
        {
            sfxSource.mute = IsMuted;
        }
    }

    private void PlayOneShotClip(AudioClip clip, float volume)
    {
        if (IsMuted)
        {
            return;
        }

        EnsureInitialized();
        if (sfxSource != null && clip != null)
        {
            sfxSource.PlayOneShot(clip, volume);
        }
    }

    private static AudioClip GenerateMartianWindClip()
    {
        int totalSamples = SampleRate * (int)WindDurationSeconds;
        int fadeSamples = Mathf.RoundToInt(SampleRate * WindCrossfadeSeconds);
        int warmupSamples = SampleRate / 2;

        float[] raw = new float[totalSamples + fadeSamples];
        System.Random rng = new System.Random(2038);

        float b0 = 0f, b1 = 0f, b2 = 0f, b3 = 0f, b4 = 0f, b5 = 0f, b6 = 0f;
        float brown = 0f;
        float lp1 = 0f, lp2 = 0f;
        float hpPrevX = 0f, hpPrevY = 0f;

        for (int i = -warmupSamples; i < totalSamples + fadeSamples; i++)
        {
            float white = (float)(rng.NextDouble() * 2.0 - 1.0);

            b0 = 0.99886f * b0 + white * 0.0555179f;
            b1 = 0.99332f * b1 + white * 0.0750759f;
            b2 = 0.96900f * b2 + white * 0.1538520f;
            b3 = 0.86650f * b3 + white * 0.3104856f;
            b4 = 0.55000f * b4 + white * 0.5329522f;
            b5 = -0.7616f * b5 - white * 0.0168980f;
            float pink = (b0 + b1 + b2 + b3 + b4 + b5 + b6 + white * 0.5362f) * 0.11f;
            b6 = white * 0.115926f;

            brown = (brown + 0.02f * white) / 1.02f;
            float blended = brown * 1.9f + pink * 0.45f;

            float t = (float)i / SampleRate;
            float gust = 0.68f
                + 0.20f * Mathf.Sin(2f * Mathf.PI * 0.25f * t)
                + 0.12f * Mathf.Sin(2f * Mathf.PI * 0.50f * t + 0.8f);

            float cutoffHz = Mathf.Lerp(210f, 520f, Mathf.Clamp01((gust - 0.36f) / 0.64f));
            float alpha = 1f - Mathf.Exp(-2f * Mathf.PI * cutoffHz / SampleRate);

            lp1 += alpha * (blended - lp1);
            lp2 += alpha * (lp1 - lp2);

            float highPassed = lp2 - hpPrevX + 0.996f * hpPrevY;
            hpPrevX = lp2;
            hpPrevY = highPassed;

            if (i >= 0)
            {
                raw[i] = highPassed * gust;
            }
        }

        NormalizeBuffer(raw, 0.78f);

        float[] loopSamples = new float[totalSamples];
        for (int i = 0; i < totalSamples; i++)
        {
            if (i < fadeSamples)
            {
                float u = (float)i / fadeSamples;
                float headGain = Mathf.Sin(u * Mathf.PI * 0.5f);
                float tailGain = Mathf.Cos(u * Mathf.PI * 0.5f);
                loopSamples[i] = raw[i] * headGain + raw[totalSamples + i] * tailGain;
            }
            else
            {
                loopSamples[i] = raw[i];
            }
        }

        AudioClip clip = AudioClip.Create("Ambience_MartianWind", totalSamples, 1, SampleRate, false);
        clip.SetData(loopSamples, 0);
        return clip;
    }

    private static AudioClip GenerateReactorHumClip()
    {
        int totalSamples = SampleRate * (int)ReactorDurationSeconds;
        float[] samples = new float[totalSamples];

        for (int i = 0; i < totalSamples; i++)
        {
            double t = (double)i / SampleRate;
            double fundamental = Math.Sin(2.0 * Math.PI * 110.0 * t);
            double overtone2 = Math.Sin(2.0 * Math.PI * 220.0 * t);
            double overtone3 = Math.Sin(2.0 * Math.PI * 330.0 * t);
            double modulation = 0.93 + 0.07 * Math.Sin(2.0 * Math.PI * 1.0 * t);
            double combined = (fundamental * 0.64 + overtone2 * 0.24 + overtone3 * 0.12) * modulation;
            samples[i] = (float)(combined * 0.85);
        }

        AudioClip clip = AudioClip.Create("Ambience_ReactorHum", totalSamples, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip GenerateChirpClip(
        string clipName,
        float durationSeconds,
        float startHz,
        float endHz,
        float decayRate,
        float overtoneMix,
        float attackSeconds,
        float amplitude)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
        float[] samples = new float[sampleCount];
        double phase = 0.0;

        for (int i = 0; i < sampleCount; i++)
        {
            float u = sampleCount > 1 ? (float)i / (sampleCount - 1) : 0f;
            float t = (float)i / SampleRate;
            float freq = Mathf.Lerp(startHz, endHz, u);
            phase += 2.0 * Math.PI * freq / SampleRate;

            float wave = (float)Math.Sin(phase);
            if (overtoneMix > 0f)
            {
                wave += overtoneMix * (float)Math.Sin(2.0 * phase);
            }

            float attackEnv = attackSeconds > 0f ? Mathf.Clamp01(t / attackSeconds) : 1f;
            float decayEnv = Mathf.Exp(-decayRate * u);
            float tailEnv = Mathf.Clamp01((1f - u) / 0.12f);

            samples[i] = wave * attackEnv * decayEnv * tailEnv * amplitude;
        }

        NormalizeBuffer(samples, 0.85f);
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static AudioClip GenerateSequenceClip(
        string clipName,
        float durationSeconds,
        float[] noteFrequencies,
        float overtoneMix)
    {
        int sampleCount = Mathf.Max(1, Mathf.RoundToInt(SampleRate * durationSeconds));
        float[] samples = new float[sampleCount];
        int noteCount = noteFrequencies.Length;

        for (int n = 0; n < noteCount; n++)
        {
            int startSample = Mathf.RoundToInt((float)n / noteCount * sampleCount);
            int stepSamples = Mathf.Max(1, Mathf.RoundToInt((float)sampleCount / noteCount));
            int noteLength = Mathf.Min(sampleCount - startSample, Mathf.RoundToInt(stepSamples * 1.35f));
            float freq = noteFrequencies[n];

            for (int j = 0; j < noteLength; j++)
            {
                float u = noteLength > 1 ? (float)j / (noteLength - 1) : 0f;
                float t = (float)j / SampleRate;
                double phase = 2.0 * Math.PI * freq * t;

                float wave = (float)Math.Sin(phase);
                if (overtoneMix > 0f)
                {
                    wave += overtoneMix * (float)Math.Sin(2.0 * phase);
                }

                float attackEnv = Mathf.Clamp01(t / 0.003f);
                float decayEnv = Mathf.Exp(-4.5f * u);
                float tailEnv = Mathf.Clamp01((1f - u) / 0.15f);

                samples[startSample + j] += wave * attackEnv * decayEnv * tailEnv * 0.55f;
            }
        }

        NormalizeBuffer(samples, 0.85f);
        AudioClip clip = AudioClip.Create(clipName, sampleCount, 1, SampleRate, false);
        clip.SetData(samples, 0);
        return clip;
    }

    private static void NormalizeBuffer(float[] samples, float maxTarget)
    {
        float peak = 0f;
        for (int i = 0; i < samples.Length; i++)
        {
            float abs = Mathf.Abs(samples[i]);
            if (abs > peak)
            {
                peak = abs;
            }
        }

        if (peak > maxTarget && peak > 0f)
        {
            float scale = maxTarget / peak;
            for (int i = 0; i < samples.Length; i++)
            {
                samples[i] *= scale;
            }
        }
    }

    private static void DestroyClip(ref AudioClip clip)
    {
        if (clip == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(clip);
        }
        else
        {
            DestroyImmediate(clip);
        }

        clip = null;
    }
}
