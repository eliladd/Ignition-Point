using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Forwards pointer hover enter/exit events from choice cards or schematic modules
/// to preview live base impacts and telemetry tooltips.
/// </summary>
public sealed class ChoiceHoverRelay : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    private Action onEnterCallback;
    private Action onExitCallback;
    private bool isPointerInside;

    public void Bind(Action onEnter, Action onExit)
    {
        onEnterCallback = onEnter;
        onExitCallback = onExit;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        onEnterCallback?.Invoke();
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        onExitCallback?.Invoke();
    }

    private void OnDisable()
    {
        if (isPointerInside)
        {
            isPointerInside = false;
            onExitCallback?.Invoke();
        }
    }
}

/// <summary>
/// Combines Visual Atmosphere Pulse (#1) and Interactive Ares Base Cutaway Schematic (#4)
/// inside the Decision Panel: dynamic Martian sky &amp; sun dimming, drifting dust motes,
/// power/life-support conduits, frost &amp; emergency edge vignettes, and 5 clickable
/// NASA-tech cutaway modules with choice-hover targeting previews.
/// </summary>
public sealed class MissionBaseView : MonoBehaviour
{
    private enum ModuleId
    {
        SOLAR = 0,
        REACTOR = 1,
        HAB_TEMP = 2,
        MOXIE_O2 = 3,
        MAV_FUEL = 4
    }

    private const int DustParticleCount = 20;
    private const float DefaultMinY = 0.49f;
    private const float DefaultMaxY = 0.79f;

    private static readonly Color DefaultMarsSky = Palette.FromHex("#C88B5A");
    private static readonly Color SunClearColor = Palette.FromHex("#FFD97D");
    private static readonly Color SunStormColor = Palette.FromHex("#B52B14");
    private static readonly Color FrostCyanColor = Palette.FromHex("#5AE8FF");
    private static readonly Color EmergencyRedColor = Palette.FromHex("#E64848");
    private static readonly Color CardBaseDark = new Color(0.06f, 0.10f, 0.16f, 0.78f);
    private static readonly Color CardDimmedDark = new Color(0.04f, 0.06f, 0.10f, 0.62f);

    private MissionUI missionUI;
    private MissionAudio missionAudio;
    private TMP_FontAsset uiFont;
    private bool isBuilt;

    private RectTransform baseViewportRect;
    private Outline viewportFrameOutline;

    // Atmosphere & Sky Layers
    private Image skyBackdropImage;
    private Image horizonBandImage;
    private Image sunHaloImage;
    private Image sunCoreImage;
    private Image ridgeBackImage;
    private Image ridgeFrontImage;
    private Image terrainPadImage;

    // Dust Particles
    private RectTransform dustLayerRect;
    private readonly List<DustParticle> dustParticles = new List<DustParticle>(DustParticleCount);

    // Conduits & Energy Pulse
    private readonly List<Image> conduitSegments = new List<Image>();
    private readonly List<Image> conduitPulseNodes = new List<Image>();

    // Edge Vignettes
    private readonly List<Image> frostEdgeImages = new List<Image>();
    private readonly List<Image> emergencyEdgeImages = new List<Image>();

    // Top Compact Header Strip
    private Outline skyBadgeOutline;
    private TMP_Text skyBadgeText;
    private Image soundToggleBg;
    private Outline soundToggleOutline;
    private TMP_Text soundToggleLabel;
    private bool localMutedFallback;
    private bool lastRenderedMuted;

    // Bottom Telemetry Readout Strip
    private Outline telemetryStripOutline;
    private TMP_Text telemetryReadoutText;

    // 5 Interactive Base Module Cutaways
    private readonly ModuleSlot[] modules = new ModuleSlot[5];

    // Module-specific reactive elements
    private Image solarDustOverlay;
    private Image solarEdsBar;
    private readonly List<Image> solarEdsSparks = new List<Image>();

    private Image reactorCoreImage;
    private Image reactorCoreGlow;

    private readonly List<Image> habWindows = new List<Image>();
    private Image habDomeTint;
    private Image habHydroStrip;

    private Image moxieTankLeftGlow;
    private Image moxieTankRightGlow;
    private Image moxieIntakeGlow;

    private Image mavCryoTankGlow;
    private Image mavCommBeacon;

    // Procedural textures & sprites (disposed in OnDestroy)
    private readonly List<Texture2D> proceduralTextures = new List<Texture2D>();
    private readonly List<Sprite> proceduralSprites = new List<Sprite>();
    private Sprite radialHaloSprite;
    private Sprite sunCoreSprite;
    private Sprite domeSprite;
    private Sprite triangleSprite;
    private Sprite ridgeBackSprite;
    private Sprite ridgeFrontSprite;

    // Smoothed runtime atmosphere & state targets
    private GameState lastSyncedState;
    private bool hasAtmosphereInit;
    private bool hasStateInit;
    private Color currentSkyColor = DefaultMarsSky;
    private Color targetSkyColor = DefaultMarsSky;
    private float currentDustIntensity = 0.15f;
    private float targetDustIntensity = 0.15f;
    private float currentBlockedRatio = 0.10f;
    private float targetBlockedRatio = 0.10f;
    private float currentFrostAlpha;
    private float targetFrostAlpha;
    private bool isEmergencyActive;
    private float emergencyPhase;
    private int cachedPowerLevel = 90;

    // Hover & choice preview state
    private Choice activeChoicePreview;
    private int activeChoiceOptionIndex = -1;
    private Color activeChoiceAccent = Palette.FunkyCyan;
    private int hoveredModuleIndex = -1;
    private string defaultTelemetryMessage = "ARES BASE NOMINAL  |  Hover an option card to preview subsystem impact, or click any module to inspect NASA tech.";

    public static MissionBaseView Attach(
        MissionUI owner,
        RectTransform decisionPanelRect,
        TMP_FontAsset font,
        MissionAudio audio = null)
    {
        if (decisionPanelRect == null)
        {
            Debug.LogWarning("[MissionBaseView] Cannot attach to a null decisionPanelRect.");
            return null;
        }

        MissionBaseView existing = decisionPanelRect.GetComponentInChildren<MissionBaseView>(true);
        if (existing != null)
        {
            existing.missionUI = owner;
            if (font != null)
            {
                existing.uiFont = font;
            }
            existing.SetAudio(audio);
            existing.EnsureBuilt();
            return existing;
        }

        GameObject rootObj = new GameObject(
            "MissionBaseView",
            typeof(RectTransform),
            typeof(MissionBaseView));
        rootObj.transform.SetParent(decisionPanelRect, false);
        rootObj.transform.SetAsFirstSibling();

        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        SetAnchors(rootRt, 0f, 0f, 1f, 1f);

        MissionBaseView view = rootObj.GetComponent<MissionBaseView>();
        view.missionUI = owner;
        view.uiFont = font != null ? font : ResolveFallbackFont(decisionPanelRect);
        view.missionAudio = audio;
        view.EnsureBuilt();
        view.SetVerticalBand(DefaultMinY, DefaultMaxY);
        view.UpdateAtmosphere("#C88B5A", 0.15f);
        view.RefreshSoundButtonVisual();

        return view;
    }

    public void SetAudio(MissionAudio audio)
    {
        missionAudio = audio;
        RefreshSoundButtonVisual();
    }

    public void SetVerticalBand(float minY, float maxY)
    {
        EnsureBuilt();
        if (baseViewportRect == null)
        {
            return;
        }

        float clampedMin = Mathf.Clamp01(minY);
        float clampedMax = Mathf.Clamp(maxY, clampedMin + 0.05f, 1f);
        SetAnchors(baseViewportRect, 0.185f, clampedMin, 0.815f, clampedMax);
    }

    public void UpdateAtmosphere(string skyColorHex, float dustIntensity)
    {
        EnsureBuilt();

        Color parsed = DefaultMarsSky;
        if (!string.IsNullOrWhiteSpace(skyColorHex) && ColorUtility.TryParseHtmlString(skyColorHex.Trim(), out Color c))
        {
            parsed = c;
        }

        targetSkyColor = parsed;
        targetDustIntensity = Mathf.Clamp01(dustIntensity);

        if (!hasAtmosphereInit)
        {
            hasAtmosphereInit = true;
            currentSkyColor = targetSkyColor;
            currentDustIntensity = targetDustIntensity;
            ApplyAtmosphereColorsNow();
        }
    }

    public void SyncState(GameState state)
    {
        EnsureBuilt();
        if (state == null)
        {
            return;
        }

        lastSyncedState = state;
        cachedPowerLevel = state.power;

        if (!string.IsNullOrEmpty(state.skyColorHex))
        {
            UpdateAtmosphere(state.skyColorHex, state.dustParticleIntensity);
        }
        else
        {
            targetDustIntensity = Mathf.Clamp01(state.dustParticleIntensity);
        }

        targetBlockedRatio = Mathf.Clamp01(state.sunlightBlockedPercent / 97f);

        bool isCold = !string.IsNullOrEmpty(state.tempTag) && !state.tempGood;
        targetFrostAlpha = isCold ? 0.58f : 0f;
        isEmergencyActive = state.power <= 30 || state.crewHealth <= 30;

        if (!hasStateInit)
        {
            hasStateInit = true;
            currentBlockedRatio = targetBlockedRatio;
            currentFrostAlpha = targetFrostAlpha;
            ApplyAtmosphereColorsNow();
        }

        RefreshSkyStatusBadge(state);
        RefreshModuleVisuals(state);
        RefreshConduitBaseColors();
        RefreshDefaultTelemetryText(state);
        RefreshTelemetryDisplay();
        RefreshSoundButtonVisual();
    }

    public void HighlightForChoice(Choice choice, int optionIndex)
    {
        EnsureBuilt();
        if (choice == null)
        {
            ClearChoiceHighlight();
            return;
        }

        bool isNewChoice = !ReferenceEquals(activeChoicePreview, choice) || activeChoiceOptionIndex != optionIndex;
        activeChoicePreview = choice;
        activeChoiceOptionIndex = optionIndex;

        Color[] optionAccents = { Palette.FunkyCyan, Palette.FunkyGold, Palette.FunkyCoral };
        activeChoiceAccent = optionAccents[Mathf.Abs(optionIndex) % optionAccents.Length];

        bool[] targets = new bool[5];
        ResolveChoiceTargets(choice, targets);

        for (int i = 0; i < modules.Length; i++)
        {
            ModuleSlot slot = modules[i];
            if (slot == null)
            {
                continue;
            }
            slot.isTargeted = targets[i];
            slot.isDimmed = !targets[i];
        }

        if (isNewChoice && missionAudio != null)
        {
            missionAudio.PlayHoverTick();
        }

        RefreshTelemetryDisplay();
    }

    public void ClearChoiceHighlight()
    {
        activeChoicePreview = null;
        activeChoiceOptionIndex = -1;

        for (int i = 0; i < modules.Length; i++)
        {
            ModuleSlot slot = modules[i];
            if (slot == null)
            {
                continue;
            }
            slot.isTargeted = false;
            slot.isDimmed = false;
        }

        RefreshTelemetryDisplay();
    }

    private void OnEnable()
    {
        hoveredModuleIndex = -1;
        ClearChoiceHighlight();
        RefreshSoundButtonVisual();
    }

    private void Update()
    {
        if (!isBuilt)
        {
            return;
        }

        float dt = Time.unscaledDeltaTime;
        float t = Time.unscaledTime;

        if (missionAudio != null && missionAudio.IsMuted != lastRenderedMuted)
        {
            RefreshSoundButtonVisual();
        }

        UpdateAtmosphereLerp(dt);
        UpdateDustParticles(dt, t);
        UpdateConduitPulse(t);
        UpdateVignettes(dt, t);
        UpdateModuleAnimations(dt, t);
    }

    private void OnDestroy()
    {
        for (int i = 0; i < proceduralSprites.Count; i++)
        {
            if (proceduralSprites[i] != null)
            {
                Destroy(proceduralSprites[i]);
            }
        }
        proceduralSprites.Clear();

        for (int i = 0; i < proceduralTextures.Count; i++)
        {
            if (proceduralTextures[i] != null)
            {
                Destroy(proceduralTextures[i]);
            }
        }
        proceduralTextures.Clear();
    }

    private void EnsureBuilt()
    {
        if (isBuilt)
        {
            return;
        }
        isBuilt = true;

        if (uiFont == null)
        {
            uiFont = ResolveFallbackFont(transform.parent);
        }

        CreateProceduralSprites();
        BuildViewportRoot();
        BuildAtmosphereLayers();
        BuildDustParticles();
        BuildConduits();
        BuildModules();
        BuildVignettes();
        BuildHeaderStrip();
        BuildTelemetryFooter();
    }

    private static TMP_FontAsset ResolveFallbackFont(Transform searchRoot)
    {
        if (searchRoot != null)
        {
            TMP_Text anyTmp = searchRoot.GetComponentInChildren<TMP_Text>(true);
            if (anyTmp != null && anyTmp.font != null)
            {
                return anyTmp.font;
            }
        }
        return TMP_Settings.defaultFontAsset;
    }

    private void CreateProceduralSprites()
    {
        radialHaloSprite = CreateRadialGradientSprite("Halo64", 64, 0f, 2.4f);
        sunCoreSprite = CreateRadialGradientSprite("SunCore64", 64, 0.44f, 1.4f);
        domeSprite = CreateDomeSprite("Dome64x48", 64, 48);
        triangleSprite = CreateTriangleSprite("Triangle64", 64, 64);
        ridgeBackSprite = CreateRidgeSprite("RidgeBack256", 256, 64, 1.3f, 0.35f);
        ridgeFrontSprite = CreateRidgeSprite("RidgeFront256", 256, 64, 2.4f, 1.75f);
    }

    private Sprite CreateRadialGradientSprite(string spriteName, int size, float innerSolidRadius, float falloffPower)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = spriteName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        float center = (size - 1) * 0.5f;
        float invRadius = 1f / Mathf.Max(1f, center);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) * invRadius;
                float dy = (y - center) * invRadius;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float alpha;
                if (dist >= 1f)
                {
                    alpha = 0f;
                }
                else if (dist <= innerSolidRadius)
                {
                    alpha = 1f;
                }
                else
                {
                    float norm = Mathf.InverseLerp(innerSolidRadius, 1f, dist);
                    alpha = Mathf.Pow(1f - norm, falloffPower);
                }
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
        sp.name = spriteName;
        sp.hideFlags = HideFlags.HideAndDontSave;
        proceduralTextures.Add(tex);
        proceduralSprites.Add(sp);
        return sp;
    }

    private Sprite CreateDomeSprite(string spriteName, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = spriteName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int y = 0; y < height; y++)
        {
            float ny = (y + 0.5f) / height;
            for (int x = 0; x < width; x++)
            {
                float nx = ((x + 0.5f) / width - 0.5f) * 2f;
                float r2 = nx * nx + ny * ny;
                float edge = Mathf.Sqrt(r2);
                float alpha = Mathf.Clamp01((1.0f - edge) * width * 0.35f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 100f);
        sp.name = spriteName;
        sp.hideFlags = HideFlags.HideAndDontSave;
        proceduralTextures.Add(tex);
        proceduralSprites.Add(sp);
        return sp;
    }

    private Sprite CreateTriangleSprite(string spriteName, int width, int height)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = spriteName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int y = 0; y < height; y++)
        {
            float ny = (y + 0.5f) / height;
            float halfSpan = (1f - ny) * 0.48f;
            for (int x = 0; x < width; x++)
            {
                float nx = Mathf.Abs((x + 0.5f) / width - 0.5f);
                float alpha = Mathf.Clamp01((halfSpan - nx) * width * 0.5f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 100f);
        sp.name = spriteName;
        sp.hideFlags = HideFlags.HideAndDontSave;
        proceduralTextures.Add(tex);
        proceduralSprites.Add(sp);
        return sp;
    }

    private Sprite CreateRidgeSprite(string spriteName, int width, int height, float freqSeed, float phaseSeed)
    {
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false)
        {
            name = spriteName,
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };

        for (int x = 0; x < width; x++)
        {
            float u = (float)x / (width - 1);
            float ridgeH = 0.42f
                + 0.22f * Mathf.Sin(u * Mathf.PI * (2.3f * freqSeed) + phaseSeed)
                + 0.14f * Mathf.Cos(u * Mathf.PI * (5.1f * freqSeed) + phaseSeed * 1.7f)
                + 0.08f * Mathf.Sin(u * Mathf.PI * 11.4f + phaseSeed * 0.5f);
            // Stepped mesa plateaus characteristic of Mars crater rims
            ridgeH = Mathf.Clamp(Mathf.Lerp(ridgeH, Mathf.Round(ridgeH * 9f) / 9f, 0.35f), 0.16f, 0.94f);

            for (int y = 0; y < height; y++)
            {
                float v = (y + 0.5f) / height;
                float alpha = Mathf.Clamp01((ridgeH - v) * height * 0.55f);
                tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tex.Apply();

        Sprite sp = Sprite.Create(tex, new Rect(0, 0, width, height), new Vector2(0.5f, 0f), 100f);
        sp.name = spriteName;
        sp.hideFlags = HideFlags.HideAndDontSave;
        proceduralTextures.Add(tex);
        proceduralSprites.Add(sp);
        return sp;
    }

    private void BuildViewportRoot()
    {
        GameObject vpObj = new GameObject(
            "BaseViewport",
            typeof(RectTransform),
            typeof(Image),
            typeof(Outline),
            typeof(RectMask2D));
        vpObj.transform.SetParent(transform, false);

        baseViewportRect = vpObj.GetComponent<RectTransform>();
        SetAnchors(baseViewportRect, 0.185f, DefaultMinY, 0.815f, DefaultMaxY);

        Image vpBg = vpObj.GetComponent<Image>();
        vpBg.color = new Color(0f, 0f, 0f, 0f);
        vpBg.raycastTarget = false;

        viewportFrameOutline = vpObj.GetComponent<Outline>();
        viewportFrameOutline.effectColor = new Color(Palette.PanelBorder.r, Palette.PanelBorder.g, Palette.PanelBorder.b, 0.92f);
        viewportFrameOutline.effectDistance = new Vector2(1.8f, -1.8f);
    }

    private void BuildAtmosphereLayers()
    {
        GameObject skyLayer = CreateChildRect("AtmosphereLayer", baseViewportRect, 0f, 0f, 1f, 1f);

        skyBackdropImage = CreateDecorativeImage("SkyBackdrop", skyLayer.transform, 0f, 0f, 1f, 1f, Palette.BackgroundDeep);
        horizonBandImage = CreateDecorativeImage("HorizonBand", skyLayer.transform, 0f, 0.24f, 1f, 0.64f, new Color(0.75f, 0.42f, 0.22f, 0.35f));

        // Sun Halo & Core positioned in upper-right Martian sky
        sunHaloImage = CreateDecorativeImage("SunHalo", skyLayer.transform, 0.78f, 0.72f, 0.78f, 0.72f, SunClearColor);
        sunHaloImage.sprite = radialHaloSprite;
        sunHaloImage.rectTransform.sizeDelta = new Vector2(180f, 180f);

        sunCoreImage = CreateDecorativeImage("SunCore", skyLayer.transform, 0.78f, 0.72f, 0.78f, 0.72f, SunClearColor);
        sunCoreImage.sprite = sunCoreSprite;
        sunCoreImage.rectTransform.sizeDelta = new Vector2(54f, 54f);

        // Layered Martian mountain / crater ridges along lower sky
        ridgeBackImage = CreateDecorativeImage("MartianRidgeBack", skyLayer.transform, 0f, 0.28f, 1f, 0.50f, new Color(0.36f, 0.19f, 0.13f, 0.85f));
        ridgeBackImage.sprite = ridgeBackSprite;

        ridgeFrontImage = CreateDecorativeImage("MartianRidgeFront", skyLayer.transform, 0f, 0.23f, 1f, 0.42f, new Color(0.25f, 0.13f, 0.09f, 0.94f));
        ridgeFrontImage.sprite = ridgeFrontSprite;

        // Martian ground / base pad at Y: 0..0.34
        terrainPadImage = CreateDecorativeImage("MartianTerrainPad", skyLayer.transform, 0f, 0f, 1f, 0.34f, new Color(0.22f, 0.12f, 0.09f, 0.96f));
        CreateDecorativeImage("BaseFoundationApron", skyLayer.transform, 0f, 0f, 1f, 0.20f, new Color(0.11f, 0.13f, 0.17f, 0.92f));

        // Keep the telemetry box transparent so the rotating NASA folder photos/videos show through behind the modules.
        skyLayer.SetActive(false);
    }

    private void BuildDustParticles()
    {
        GameObject dustObj = CreateChildRect("DustParticlesLayer", baseViewportRect, 0f, 0f, 1f, 1f);
        dustLayerRect = dustObj.GetComponent<RectTransform>();

        dustParticles.Clear();
        for (int i = 0; i < DustParticleCount; i++)
        {
            bool isStreak = i % 3 != 0;
            float width = isStreak ? UnityEngine.Random.Range(32f, 88f) : UnityEngine.Random.Range(10f, 22f);
            float height = isStreak ? UnityEngine.Random.Range(2.2f, 5.5f) : UnityEngine.Random.Range(8f, 16f);

            GameObject pObj = new GameObject($"Dust_{i}", typeof(RectTransform), typeof(Image));
            pObj.transform.SetParent(dustLayerRect, false);

            RectTransform rt = pObj.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(width, height);

            float startX = Mathf.Lerp(-860f, 860f, (i + 0.5f) / DustParticleCount);
            float baseNormY = UnityEngine.Random.Range(0.12f, 0.92f);
            rt.anchoredPosition = new Vector2(startX, (baseNormY - 0.5f) * 300f);

            Image img = pObj.GetComponent<Image>();
            img.sprite = radialHaloSprite;
            img.raycastTarget = false;
            img.color = new Color(0.95f, 0.62f, 0.36f, 0.12f);

            dustParticles.Add(new DustParticle
            {
                rect = rt,
                image = img,
                localX = startX,
                baseNormY = baseNormY,
                halfWidth = width * 0.5f,
                baseSpeed = UnityEngine.Random.Range(25f, 160f),
                wobbleFreq = UnityEngine.Random.Range(0.8f, 2.4f),
                wobbleAmp = UnityEngine.Random.Range(3f, 12f),
                phase = UnityEngine.Random.Range(0f, Mathf.PI * 2f),
                alphaWeight = isStreak ? UnityEngine.Random.Range(0.65f, 1f) : UnityEngine.Random.Range(0.45f, 0.8f)
            });
        }
    }

    private void BuildConduits()
    {
        GameObject conduitLayer = CreateChildRect("ConduitsLayer", baseViewportRect, 0f, 0f, 1f, 1f);
        conduitSegments.Clear();
        conduitPulseNodes.Clear();

        // Main power bus connecting Solar -> Reactor -> Hab -> MOXIE -> MAV
        CreateConduitSegment(conduitLayer.transform, 0.115f, 0.42f, 0.305f, 0.435f);
        CreateConduitSegment(conduitLayer.transform, 0.305f, 0.42f, 0.500f, 0.435f);
        CreateConduitSegment(conduitLayer.transform, 0.500f, 0.42f, 0.695f, 0.435f);
        CreateConduitSegment(conduitLayer.transform, 0.695f, 0.42f, 0.885f, 0.435f);

        // Lower auxiliary life-support / propellant feed conduit
        CreateConduitSegment(conduitLayer.transform, 0.500f, 0.33f, 0.695f, 0.342f);
        CreateConduitSegment(conduitLayer.transform, 0.695f, 0.33f, 0.885f, 0.342f);

        // Angled power drop from Solar/Reactor bridge into Hab Dome
        CreateConduitAngledSegment(conduitLayer.transform, new Vector2(0.115f, 0.428f), new Vector2(0.305f, 0.336f), 3f);
        CreateConduitAngledSegment(conduitLayer.transform, new Vector2(0.305f, 0.336f), new Vector2(0.500f, 0.428f), 3f);

        // Junction glow nodes between modules
        float[] nodeXs = { 0.21f, 0.40f, 0.60f, 0.79f };
        for (int i = 0; i < nodeXs.Length; i++)
        {
            Image node = CreateDecorativeImage("ConduitNode_" + i, conduitLayer.transform, nodeXs[i], 0.428f, nodeXs[i], 0.428f, Palette.FunkyCyan);
            node.sprite = radialHaloSprite;
            node.rectTransform.sizeDelta = new Vector2(22f, 22f);
            conduitPulseNodes.Add(node);
        }
    }

    private void CreateConduitSegment(Transform parent, float minX, float minY, float maxX, float maxY)
    {
        Image seg = CreateDecorativeImage("ConduitBus", parent, minX, minY, maxX, maxY, Palette.FunkyCyan);
        conduitSegments.Add(seg);
    }

    private void CreateConduitAngledSegment(Transform parent, Vector2 anchorStart, Vector2 anchorEnd, float thicknessPx)
    {
        Vector2 mid = (anchorStart + anchorEnd) * 0.5f;
        GameObject segObj = new GameObject("ConduitAngleBus", typeof(RectTransform), typeof(Image));
        segObj.transform.SetParent(parent, false);
        RectTransform rt = segObj.GetComponent<RectTransform>();
        rt.anchorMin = mid;
        rt.anchorMax = mid;
        rt.pivot = new Vector2(0.5f, 0.5f);

        // Approximate reference-space vector so diagonal bus links cleanly
        Vector2 deltaPx = new Vector2((anchorEnd.x - anchorStart.x) * 1700f, (anchorEnd.y - anchorStart.y) * 320f);
        rt.sizeDelta = new Vector2(deltaPx.magnitude, thicknessPx);
        rt.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(deltaPx.y, deltaPx.x) * Mathf.Rad2Deg);

        Image img = segObj.GetComponent<Image>();
        img.color = Palette.FunkyCyan;
        img.raycastTarget = false;
        conduitSegments.Add(img);
    }

    private void BuildModules()
    {
        GameObject modLayer = CreateChildRect("ModulesLayer", baseViewportRect, 0f, 0f, 1f, 1f);

        modules[0] = CreateModuleCard(modLayer.transform, ModuleId.SOLAR, "SOLAR ARRAYS", 0.03f, 0.20f, Palette.FunkyGold);
        BuildSolarSilhouette(modules[0].artRoot);

        modules[1] = CreateModuleCard(modLayer.transform, ModuleId.REACTOR, "KILOPOWER", 0.22f, 0.39f, Palette.FunkyMint);
        BuildReactorSilhouette(modules[1].artRoot);

        modules[2] = CreateModuleCard(modLayer.transform, ModuleId.HAB_TEMP, "HAB & CROPS", 0.41f, 0.59f, Palette.FunkyCyan);
        BuildHabSilhouette(modules[2].artRoot);

        modules[3] = CreateModuleCard(modLayer.transform, ModuleId.MOXIE_O2, "MOXIE O2 & AIR", 0.61f, 0.78f, Palette.ScienceCyan);
        BuildMoxieSilhouette(modules[3].artRoot);

        modules[4] = CreateModuleCard(modLayer.transform, ModuleId.MAV_FUEL, "RETURN ROCKET", 0.80f, 0.97f, Palette.MarsAccent);
        BuildRocketSilhouette(modules[4].artRoot);
    }

    private ModuleSlot CreateModuleCard(
        Transform parent,
        ModuleId id,
        string title,
        float minX,
        float maxX,
        Color defaultAccent)
    {
        GameObject cardObj = new GameObject($"Module_{id}", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button), typeof(ChoiceHoverRelay));
        cardObj.transform.SetParent(parent, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        SetAnchors(cardRt, minX, 0.20f, maxX, 0.81f);

        Image bg = cardObj.GetComponent<Image>();
        bg.color = CardBaseDark;
        bg.raycastTarget = true;

        Outline outline = cardObj.GetComponent<Outline>();
        outline.effectColor = Palette.PanelBorder;
        outline.effectDistance = new Vector2(1.7f, -1.7f);

        Button btn = cardObj.GetComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = bg;

        // Title tag at top of module card
        CreateLabel(
            "TitleTag",
            cardRt,
            0.04f, 0.81f, 0.96f, 0.96f,
            title,
            12f,
            FontStyles.Bold,
            Palette.TextPrimary,
            TextAlignmentOptions.Center);

        // Cutaway silhouette canvas in middle
        GameObject artObj = CreateChildRect("CutawayArt", cardRt, 0.06f, 0.25f, 0.94f, 0.79f);
        RectTransform artRt = artObj.GetComponent<RectTransform>();

        // Bottom status badge pill
        Image badgeBg = CreateDecorativeImage("StatusBadge", cardRt, 0.06f, 0.04f, 0.94f, 0.22f, Palette.PanelElevated);
        Outline badgeOutline = badgeBg.gameObject.AddComponent<Outline>();
        badgeOutline.effectColor = defaultAccent;
        badgeOutline.effectDistance = new Vector2(1.0f, -1.0f);

        TMP_Text badgeTmp = CreateLabel(
            "StatusText",
            badgeBg.rectTransform,
            0.04f, 0.04f, 0.96f, 0.96f,
            "NOMINAL",
            10.5f,
            FontStyles.Bold,
            Palette.TextPrimary,
            TextAlignmentOptions.Center);
        badgeTmp.overflowMode = TextOverflowModes.Truncate;

        // Pulsing targeting reticle brackets (visible when HighlightForChoice targets this module)
        List<Image> reticleCorners = BuildTargetReticle(cardRt);

        ModuleSlot slot = new ModuleSlot
        {
            rootRect = cardRt,
            cardBg = bg,
            cardOutline = outline,
            artRoot = artRt,
            badgeBg = badgeBg,
            badgeOutline = badgeOutline,
            badgeText = badgeTmp,
            reticleParts = reticleCorners,
            currentScale = 1f
        };

        int index = (int)id;
        btn.onClick.AddListener(() => OnModuleClicked(index));
        ChoiceHoverRelay relay = cardObj.GetComponent<ChoiceHoverRelay>();
        relay.Bind(() => OnModuleHoverEnter(index), () => OnModuleHoverExit(index));

        return slot;
    }

    private List<Image> BuildTargetReticle(RectTransform parent)
    {
        List<Image> parts = new List<Image>(8);
        // 4 corner L-brackets
        parts.Add(CreateDecorativeImage("ReticleTL_H", parent, 0.01f, 0.94f, 0.14f, 0.985f, Palette.FunkyCyan));
        parts.Add(CreateDecorativeImage("ReticleTL_V", parent, 0.01f, 0.84f, 0.03f, 0.985f, Palette.FunkyCyan));

        parts.Add(CreateDecorativeImage("ReticleTR_H", parent, 0.86f, 0.94f, 0.99f, 0.985f, Palette.FunkyCyan));
        parts.Add(CreateDecorativeImage("ReticleTR_V", parent, 0.97f, 0.84f, 0.99f, 0.985f, Palette.FunkyCyan));

        parts.Add(CreateDecorativeImage("ReticleBL_H", parent, 0.01f, 0.015f, 0.14f, 0.06f, Palette.FunkyCyan));
        parts.Add(CreateDecorativeImage("ReticleBL_V", parent, 0.01f, 0.015f, 0.03f, 0.16f, Palette.FunkyCyan));

        parts.Add(CreateDecorativeImage("ReticleBR_H", parent, 0.86f, 0.015f, 0.99f, 0.06f, Palette.FunkyCyan));
        parts.Add(CreateDecorativeImage("ReticleBR_V", parent, 0.97f, 0.015f, 0.99f, 0.16f, Palette.FunkyCyan));

        for (int i = 0; i < parts.Count; i++)
        {
            parts[i].gameObject.SetActive(false);
        }
        return parts;
    }

    private void BuildSolarSilhouette(RectTransform artRoot)
    {
        // Central mast & inverter base
        CreateDecorativeImage("BasePad", artRoot, 0.36f, 0.04f, 0.64f, 0.18f, Palette.PanelBorder);
        CreateDecorativeImage("Mast", artRoot, 0.47f, 0.16f, 0.53f, 0.82f, Palette.TextSecondary);

        // Twin multi-panel solar wing frames
        Color wingFrame = new Color(0.18f, 0.25f, 0.35f, 1f);
        Color cellBlue = Palette.FromHex("#1A5599");
        CreateDecorativeImage("LeftWingFrame", artRoot, 0.04f, 0.34f, 0.45f, 0.78f, wingFrame);
        CreateDecorativeImage("RightWingFrame", artRoot, 0.55f, 0.34f, 0.96f, 0.78f, wingFrame);

        // Sub-panel grid cells on each wing
        for (int c = 0; c < 3; c++)
        {
            float lx0 = 0.065f + c * 0.128f;
            CreateDecorativeImage($"LCell_{c}", artRoot, lx0, 0.38f, lx0 + 0.105f, 0.74f, cellBlue);
            float rx0 = 0.575f + c * 0.128f;
            CreateDecorativeImage($"RCell_{c}", artRoot, rx0, 0.38f, rx0 + 0.105f, 0.74f, cellBlue);
        }

        // Red dust coat overlay scaling with sunlightBlockedPercent
        solarDustOverlay = CreateDecorativeImage("DustCoatOverlay", artRoot, 0.04f, 0.34f, 0.96f, 0.78f, new Color(0.68f, 0.27f, 0.14f, 0.10f));

        // Glowing cyan EDS wire bar & sparks
        solarEdsBar = CreateDecorativeImage("EdsWireBar", artRoot, 0.04f, 0.54f, 0.96f, 0.58f, Palette.FunkyCyan);
        solarEdsSparks.Clear();
        float[] sparkXs = { 0.14f, 0.31f, 0.69f, 0.86f };
        for (int i = 0; i < sparkXs.Length; i++)
        {
            Image spk = CreateDecorativeImage($"EdsSpark_{i}", artRoot, sparkXs[i], 0.56f, sparkXs[i], 0.56f, Palette.FunkyCyan);
            spk.sprite = radialHaloSprite;
            spk.rectTransform.sizeDelta = new Vector2(18f, 18f);
            solarEdsSparks.Add(spk);
        }
        SetEdsVisualActive(false);
    }

    private void BuildReactorSilhouette(RectTransform artRoot)
    {
        // Protective regolith trench berms flanking the fission pit
        Color bermColor = new Color(0.45f, 0.23f, 0.15f, 0.95f);
        Image leftBerm = CreateDecorativeImage("LeftBerm", artRoot, 0.04f, 0.04f, 0.38f, 0.36f, bermColor);
        leftBerm.sprite = triangleSprite;
        Image rightBerm = CreateDecorativeImage("RightBerm", artRoot, 0.62f, 0.04f, 0.96f, 0.36f, bermColor);
        rightBerm.sprite = triangleSprite;

        // Trench floor & containment collar
        CreateDecorativeImage("TrenchBase", artRoot, 0.28f, 0.04f, 0.72f, 0.18f, Palette.PanelBorder);

        // Glowing reactor aura & core cylinder
        reactorCoreGlow = CreateDecorativeImage("CoreGlow", artRoot, 0.50f, 0.36f, 0.50f, 0.36f, Palette.FunkyGold);
        reactorCoreGlow.sprite = radialHaloSprite;
        reactorCoreGlow.rectTransform.sizeDelta = new Vector2(68f, 68f);

        CreateDecorativeImage("CoreCasing", artRoot, 0.39f, 0.16f, 0.61f, 0.56f, new Color(0.20f, 0.26f, 0.34f, 1f));
        reactorCoreImage = CreateDecorativeImage("CoreFuelGlow", artRoot, 0.43f, 0.20f, 0.57f, 0.52f, Palette.TextSecondary);

        // Stirling column & umbrella radiator disc at top
        CreateDecorativeImage("StirlingNeck", artRoot, 0.45f, 0.56f, 0.55f, 0.76f, Palette.TextSecondary);
        Image umbrellaDisc = CreateDecorativeImage("UmbrellaRadiator", artRoot, 0.12f, 0.72f, 0.88f, 0.92f, new Color(0.68f, 0.76f, 0.85f, 0.95f));
        umbrellaDisc.sprite = domeSprite;
        CreateDecorativeImage("RadiatorRim", artRoot, 0.12f, 0.70f, 0.88f, 0.75f, Palette.PowerGold);
    }

    private void BuildHabSilhouette(RectTransform artRoot)
    {
        // Airlock foundation base
        CreateDecorativeImage("HabFoundation", artRoot, 0.10f, 0.04f, 0.90f, 0.24f, Palette.PanelBorder);

        // Pressurized outer dome shell + subtle inner tint
        Image domeShell = CreateDecorativeImage("HabDomeShell", artRoot, 0.12f, 0.22f, 0.88f, 0.92f, new Color(0.70f, 0.78f, 0.88f, 0.92f));
        domeShell.sprite = domeSprite;

        habDomeTint = CreateDecorativeImage("HabDomeInterior", artRoot, 0.18f, 0.24f, 0.82f, 0.84f, new Color(0.14f, 0.22f, 0.32f, 0.95f));
        habDomeTint.sprite = domeSprite;

        // 2x3 grid of habitat windows (warm gold or icy blue)
        habWindows.Clear();
        float[] winX = { 0.28f, 0.45f, 0.62f };
        float[] winY = { 0.38f, 0.56f };
        for (int row = 0; row < winY.Length; row++)
        {
            for (int col = 0; col < winX.Length; col++)
            {
                Image win = CreateDecorativeImage(
                    $"HabWindow_{row}_{col}",
                    artRoot,
                    winX[col],
                    winY[row],
                    winX[col] + 0.10f,
                    winY[row] + 0.11f,
                    Palette.FunkyGold);
                habWindows.Add(win);
            }
        }

        // Hydroponics crop bay strip along lower foundation
        CreateDecorativeImage("HydroTrayFrame", artRoot, 0.18f, 0.08f, 0.82f, 0.22f, Palette.BarTrack);
        habHydroStrip = CreateDecorativeImage("HydroGlowStrip", artRoot, 0.20f, 0.10f, 0.80f, 0.20f, Palette.FunkyMint);
    }

    private void BuildMoxieSilhouette(RectTransform artRoot)
    {
        // Ground manifold platform
        CreateDecorativeImage("MoxiePad", artRoot, 0.08f, 0.04f, 0.92f, 0.16f, Palette.PanelBorder);

        // Left atmospheric intake tower with filter louver fins
        CreateDecorativeImage("IntakeTower", artRoot, 0.12f, 0.16f, 0.34f, 0.88f, new Color(0.32f, 0.42f, 0.54f, 1f));
        moxieIntakeGlow = CreateDecorativeImage("IntakeCap", artRoot, 0.10f, 0.84f, 0.36f, 0.92f, Palette.ScienceCyan);
        for (int i = 0; i < 4; i++)
        {
            float ly = 0.28f + i * 0.13f;
            CreateDecorativeImage($"Louver_{i}", artRoot, 0.14f, ly, 0.32f, ly + 0.05f, Palette.BarTrack);
        }

        // Connecting cross-manifold pipe
        CreateDecorativeImage("ManifoldPipe", artRoot, 0.34f, 0.44f, 0.86f, 0.52f, Palette.TextSecondary);

        // Twin cylindrical O2 & air scrubber pressure tanks
        Color tankShell = new Color(0.55f, 0.66f, 0.78f, 0.96f);
        Image capL = CreateDecorativeImage("TankLCap", artRoot, 0.42f, 0.72f, 0.62f, 0.84f, tankShell);
        capL.sprite = domeSprite;
        CreateDecorativeImage("TankLBody", artRoot, 0.42f, 0.16f, 0.62f, 0.72f, tankShell);
        moxieTankLeftGlow = CreateDecorativeImage("TankLGauge", artRoot, 0.46f, 0.22f, 0.58f, 0.68f, Palette.FunkyCyan);

        Image capR = CreateDecorativeImage("TankRCap", artRoot, 0.68f, 0.72f, 0.88f, 0.84f, tankShell);
        capR.sprite = domeSprite;
        CreateDecorativeImage("TankRBody", artRoot, 0.68f, 0.16f, 0.88f, 0.72f, tankShell);
        moxieTankRightGlow = CreateDecorativeImage("TankRGauge", artRoot, 0.72f, 0.22f, 0.84f, 0.68f, Palette.FunkyMint);
    }

    private void BuildRocketSilhouette(RectTransform artRoot)
    {
        // Launch pad & gantry tower
        CreateDecorativeImage("LaunchPad", artRoot, 0.12f, 0.04f, 0.88f, 0.14f, Palette.PanelBorder);
        CreateDecorativeImage("GantryMast", artRoot, 0.18f, 0.14f, 0.25f, 0.72f, Palette.TextSecondary);
        CreateDecorativeImage("GantryArm", artRoot, 0.25f, 0.58f, 0.40f, 0.63f, Palette.TextSecondary);

        // Stabilizer fins
        Image finL = CreateDecorativeImage("FinL", artRoot, 0.32f, 0.14f, 0.44f, 0.34f, Palette.MarsAccent);
        finL.sprite = triangleSprite;
        Image finR = CreateDecorativeImage("FinR", artRoot, 0.56f, 0.14f, 0.68f, 0.34f, Palette.MarsAccent);
        finR.sprite = triangleSprite;

        // Upright fuselage + pointed nosecone
        CreateDecorativeImage("Fuselage", artRoot, 0.40f, 0.14f, 0.60f, 0.70f, Palette.TextPrimary);
        Image nosecone = CreateDecorativeImage("Nosecone", artRoot, 0.40f, 0.70f, 0.60f, 0.94f, Palette.MarsAccent);
        nosecone.sprite = triangleSprite;

        // Cryo fuel tank glow bay inside fuselage
        CreateDecorativeImage("CryoFrame", artRoot, 0.42f, 0.24f, 0.58f, 0.56f, Palette.BarTrack);
        mavCryoTankGlow = CreateDecorativeImage("CryoTankGlow", artRoot, 0.44f, 0.26f, 0.56f, 0.54f, Palette.ScienceCyan);

        // Small orbital comm dish on right pad
        CreateDecorativeImage("DishMast", artRoot, 0.77f, 0.14f, 0.81f, 0.34f, Palette.TextSecondary);
        Image dishCup = CreateDecorativeImage("DishCup", artRoot, 0.71f, 0.31f, 0.89f, 0.45f, Palette.TextPrimary);
        dishCup.sprite = domeSprite;
        dishCup.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        dishCup.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 145f);
        mavCommBeacon = CreateDecorativeImage("CommBeacon", artRoot, 0.82f, 0.43f, 0.82f, 0.43f, Palette.FunkyCyan);
        mavCommBeacon.sprite = radialHaloSprite;
        mavCommBeacon.rectTransform.sizeDelta = new Vector2(14f, 14f);
    }

    private void BuildVignettes()
    {
        GameObject vigLayer = CreateChildRect("VignettesLayer", baseViewportRect, 0f, 0f, 1f, 1f);

        frostEdgeImages.Clear();
        BuildEdgeFrame("Frost", vigLayer.transform, FrostCyanColor, 0.045f, 0.015f, frostEdgeImages);

        emergencyEdgeImages.Clear();
        BuildEdgeFrame("Emergency", vigLayer.transform, EmergencyRedColor, 0.055f, 0.018f, emergencyEdgeImages);

        SetEdgeGroupAlpha(frostEdgeImages, 0f);
        SetEdgeGroupAlpha(emergencyEdgeImages, 0f);
    }

    private static void BuildEdgeFrame(
        string prefix,
        Transform parent,
        Color baseColor,
        float thicknessY,
        float thicknessX,
        List<Image> output)
    {
        Color c = new Color(baseColor.r, baseColor.g, baseColor.b, 0f);
        output.Add(CreateDecorativeImage($"{prefix}_Top", parent, 0f, 1f - thicknessY, 1f, 1f, c));
        output.Add(CreateDecorativeImage($"{prefix}_Bottom", parent, 0f, 0f, 1f, thicknessY, c));
        output.Add(CreateDecorativeImage($"{prefix}_Left", parent, 0f, thicknessY, thicknessX, 1f - thicknessY, c));
        output.Add(CreateDecorativeImage($"{prefix}_Right", parent, 1f - thicknessX, thicknessY, 1f, 1f - thicknessY, c));
    }

    private void BuildHeaderStrip()
    {
        GameObject headerObj = CreateChildRect("HeaderStrip", baseViewportRect, 0f, 0.85f, 1f, 0.98f);
        Image headerBg = CreateDecorativeImage("HeaderBackdrop", headerObj.transform, 0.015f, 0.04f, 0.985f, 0.96f, new Color(0.05f, 0.08f, 0.13f, 0.84f));
        Outline hdrOutline = headerBg.gameObject.AddComponent<Outline>();
        hdrOutline.effectColor = new Color(Palette.PanelBorder.r, Palette.PanelBorder.g, Palette.PanelBorder.b, 0.7f);
        hdrOutline.effectDistance = new Vector2(1.0f, -1.0f);

        CreateLabel(
            "HeaderTitle",
            headerObj.transform,
            0.03f, 0.08f, 0.29f, 0.92f,
            "ARES BASE LIVE TELEMETRY",
            13f,
            FontStyles.Bold,
            Palette.FunkyGold,
            TextAlignmentOptions.MidlineLeft);

        Image skyBadgeBg = CreateDecorativeImage("OpticalSkyBadge", headerObj.transform, 0.30f, 0.14f, 0.56f, 0.86f, Palette.PanelElevated);
        skyBadgeOutline = skyBadgeBg.gameObject.AddComponent<Outline>();
        skyBadgeOutline.effectColor = Palette.FunkyCyan;
        skyBadgeOutline.effectDistance = new Vector2(1.1f, -1.1f);

        skyBadgeText = CreateLabel(
            "SkyBadgeText",
            skyBadgeBg.rectTransform,
            0.03f, 0.04f, 0.97f, 0.96f,
            "SKY: 10% DUST HAZE (CLEAR)",
            11.5f,
            FontStyles.Bold,
            Palette.FunkyCyan,
            TextAlignmentOptions.Center);

        CreateLabel(
            "InspectHint",
            headerObj.transform,
            0.57f, 0.08f, 0.845f, 0.92f,
            "Click any module to inspect NASA tech",
            11.5f,
            FontStyles.Normal,
            Palette.TextSecondary,
            TextAlignmentOptions.MidlineRight);

        // Right: Clickable [SOUND: ON] / [SOUND: OFF] toggle Button
        GameObject sndObj = new GameObject("SoundToggleButton", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        sndObj.transform.SetParent(headerObj.transform, false);
        SetAnchors(sndObj.GetComponent<RectTransform>(), 0.858f, 0.12f, 0.975f, 0.88f);

        soundToggleBg = sndObj.GetComponent<Image>();
        soundToggleBg.color = Palette.NasaBoxSurface;
        soundToggleBg.raycastTarget = true;

        soundToggleOutline = sndObj.GetComponent<Outline>();
        soundToggleOutline.effectColor = Palette.FunkyCyan;
        soundToggleOutline.effectDistance = new Vector2(1.3f, -1.3f);

        Button sndBtn = sndObj.GetComponent<Button>();
        sndBtn.transition = Selectable.Transition.None;
        sndBtn.targetGraphic = soundToggleBg;
        sndBtn.onClick.AddListener(OnSoundToggleClicked);

        soundToggleLabel = CreateLabel(
            "Label",
            sndObj.transform,
            0.04f, 0.04f, 0.96f, 0.96f,
            "[SOUND: ON]",
            11.5f,
            FontStyles.Bold,
            Palette.FunkyCyan,
            TextAlignmentOptions.Center);
    }

    private void BuildTelemetryFooter()
    {
        GameObject footerObj = CreateChildRect("TelemetryFooterStrip", baseViewportRect, 0.03f, 0.02f, 0.97f, 0.17f);

        Image telemetryBg = footerObj.AddComponent<Image>();
        telemetryBg.color = new Color(0.05f, 0.09f, 0.14f, 0.92f);
        telemetryBg.raycastTarget = false;

        telemetryStripOutline = footerObj.AddComponent<Outline>();
        telemetryStripOutline.effectColor = Palette.PanelBorder;
        telemetryStripOutline.effectDistance = new Vector2(1.4f, -1.4f);

        telemetryReadoutText = CreateLabel(
            "TelemetryReadout",
            footerObj.transform,
            0.015f, 0.06f, 0.985f, 0.94f,
            defaultTelemetryMessage,
            13f,
            FontStyles.Normal,
            Palette.TextPrimary,
            TextAlignmentOptions.MidlineLeft);
        telemetryReadoutText.overflowMode = TextOverflowModes.Truncate;
    }

    private void UpdateAtmosphereLerp(float dt)
    {
        float step = Mathf.Clamp01(dt * 4.5f);
        currentSkyColor = Color.Lerp(currentSkyColor, targetSkyColor, step);
        currentDustIntensity = Mathf.Lerp(currentDustIntensity, targetDustIntensity, step);
        currentBlockedRatio = Mathf.Lerp(currentBlockedRatio, targetBlockedRatio, step);
        ApplyAtmosphereColorsNow();
    }

    private void ApplyAtmosphereColorsNow()
    {
        // Tinted sky & Martian terrain shift smoothly with skyColorHex
        if (skyBackdropImage != null)
        {
            skyBackdropImage.color = Color.Lerp(Palette.BackgroundDeep, currentSkyColor, 0.78f);
        }

        if (horizonBandImage != null)
        {
            Color horizon = Color.Lerp(currentSkyColor, Palette.MarsAccent, 0.35f);
            horizon.a = Mathf.Lerp(0.32f, 0.65f, currentDustIntensity);
            horizonBandImage.color = horizon;
        }

        if (ridgeBackImage != null)
        {
            Color back = Color.Lerp(new Color(0.20f, 0.10f, 0.08f, 1f), currentSkyColor * 0.52f, 0.55f);
            back.a = 0.88f;
            ridgeBackImage.color = back;
        }

        if (ridgeFrontImage != null)
        {
            Color front = Color.Lerp(new Color(0.13f, 0.07f, 0.06f, 1f), currentSkyColor * 0.38f, 0.45f);
            front.a = 0.95f;
            ridgeFrontImage.color = front;
        }

        if (terrainPadImage != null)
        {
            Color terrain = Color.Lerp(new Color(0.14f, 0.08f, 0.06f, 1f), currentSkyColor * 0.34f, 0.42f);
            terrain.a = 0.97f;
            terrainPadImage.color = terrain;
        }

        // Sun shrinks & fades from butterscotch gold (0.95 alpha) to deep blood-orange (0.06 alpha)
        Color sunCol = Color.Lerp(SunClearColor, SunStormColor, currentBlockedRatio);
        sunCol = Color.Lerp(sunCol, currentSkyColor, 0.16f);

        if (sunCoreImage != null)
        {
            float coreAlpha = Mathf.Lerp(0.95f, 0.06f, currentBlockedRatio);
            sunCoreImage.color = new Color(sunCol.r, sunCol.g, sunCol.b, coreAlpha);
            float coreScale = Mathf.Lerp(1.0f, 0.42f, currentBlockedRatio);
            sunCoreImage.rectTransform.localScale = new Vector3(coreScale, coreScale, 1f);
        }

        if (sunHaloImage != null)
        {
            float haloAlpha = Mathf.Lerp(0.55f, 0.03f, currentBlockedRatio);
            sunHaloImage.color = new Color(sunCol.r, sunCol.g, sunCol.b, haloAlpha);
            float haloScale = Mathf.Lerp(1.15f, 0.48f, currentBlockedRatio);
            sunHaloImage.rectTransform.localScale = new Vector3(haloScale, haloScale, 1f);
        }

        // Solar dust coat overlay thickens as sunlight gets blocked
        if (solarDustOverlay != null)
        {
            Color dustCoat = Color.Lerp(Palette.MarsAccent, currentSkyColor, 0.40f);
            dustCoat.a = Mathf.Lerp(0.06f, 0.76f, currentBlockedRatio);
            solarDustOverlay.color = dustCoat;
        }
    }

    private void UpdateDustParticles(float dt, float time)
    {
        if (dustLayerRect == null || dustParticles.Count == 0)
        {
            return;
        }

        Rect rect = dustLayerRect.rect;
        float width = rect.width > 10f ? rect.width : 1700f;
        float height = rect.height > 10f ? rect.height : 320f;
        float halfW = width * 0.5f;

        float stormStrength = Mathf.Clamp01(Mathf.Max(currentDustIntensity, currentBlockedRatio * 0.92f));
        float speedMultiplier = Mathf.Lerp(0.55f, 1.35f, stormStrength);
        float baseAlpha = Mathf.Lerp(0.06f, 0.60f, stormStrength);
        Color dustTint = Color.Lerp(Palette.MarsAccent, currentSkyColor, 0.50f);

        for (int i = 0; i < dustParticles.Count; i++)
        {
            DustParticle p = dustParticles[i];
            if (p.rect == null || p.image == null)
            {
                continue;
            }

            p.localX += p.baseSpeed * speedMultiplier * dt;
            if (p.localX > halfW + p.halfWidth + 16f)
            {
                p.localX = -halfW - p.halfWidth - 16f;
            }

            float y = (p.baseNormY - 0.5f) * height + Mathf.Sin(time * p.wobbleFreq + p.phase) * p.wobbleAmp;
            p.rect.anchoredPosition = new Vector2(p.localX, y);

            float alpha = Mathf.Clamp(baseAlpha * p.alphaWeight, 0.04f, 0.62f);
            p.image.color = new Color(dustTint.r, dustTint.g, dustTint.b, alpha);
        }
    }

    private void UpdateConduitPulse(float time)
    {
        Color baseColor = GetConduitPowerColor(cachedPowerLevel);
        float speed = cachedPowerLevel <= 30 ? 5.2f : 2.6f;
        float pulse = 0.62f + 0.38f * Mathf.Sin(time * speed);

        Color lineColor = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0.38f, 0.90f, pulse));
        for (int i = 0; i < conduitSegments.Count; i++)
        {
            if (conduitSegments[i] != null)
            {
                conduitSegments[i].color = lineColor;
            }
        }

        for (int i = 0; i < conduitPulseNodes.Count; i++)
        {
            Image node = conduitPulseNodes[i];
            if (node == null)
            {
                continue;
            }
            float wave = 0.5f + 0.5f * Mathf.Sin(time * speed - i * 1.15f);
            node.color = new Color(baseColor.r, baseColor.g, baseColor.b, Mathf.Lerp(0.35f, 0.95f, wave));
            float s = Mathf.Lerp(0.85f, 1.30f, wave);
            node.rectTransform.localScale = new Vector3(s, s, 1f);
        }

        // Subtle pulse on active EDS sparks & MAV comm beacon
        if (solarEdsBar != null && solarEdsBar.gameObject.activeSelf)
        {
            float edsPulse = 0.55f + 0.45f * Mathf.Sin(time * 7.5f);
            solarEdsBar.color = new Color(Palette.FunkyCyan.r, Palette.FunkyCyan.g, Palette.FunkyCyan.b, edsPulse);
            for (int i = 0; i < solarEdsSparks.Count; i++)
            {
                if (solarEdsSparks[i] != null)
                {
                    float spk = 0.4f + 0.6f * Mathf.Abs(Mathf.Sin(time * 9f + i * 1.7f));
                    solarEdsSparks[i].color = new Color(Palette.FunkyCyan.r, Palette.FunkyCyan.g, Palette.FunkyCyan.b, spk);
                }
            }
        }

        if (mavCommBeacon != null)
        {
            float b = 0.35f + 0.65f * Mathf.Abs(Mathf.Sin(time * 2.2f));
            mavCommBeacon.color = new Color(Palette.FunkyCyan.r, Palette.FunkyCyan.g, Palette.FunkyCyan.b, b);
        }
    }

    private void UpdateVignettes(float dt, float time)
    {
        currentFrostAlpha = Mathf.Lerp(currentFrostAlpha, targetFrostAlpha, Mathf.Clamp01(dt * 4f));
        SetEdgeGroupAlpha(frostEdgeImages, currentFrostAlpha);

        if (isEmergencyActive)
        {
            emergencyPhase += dt * 4.2f;
            float pulse = 0.5f + 0.5f * Mathf.Sin(emergencyPhase);
            float pulseAlpha = 0.14f + 0.24f * pulse;
            SetEdgeGroupAlpha(emergencyEdgeImages, pulseAlpha);

            if (viewportFrameOutline != null)
            {
                viewportFrameOutline.effectColor = Color.Lerp(Palette.PanelBorder, Palette.DangerRed, 0.45f + 0.55f * pulse);
            }
        }
        else
        {
            emergencyPhase = 0f;
            float currentEmergency = emergencyEdgeImages.Count > 0 && emergencyEdgeImages[0] != null
                ? emergencyEdgeImages[0].color.a
                : 0f;
            float faded = Mathf.Lerp(currentEmergency, 0f, Mathf.Clamp01(dt * 6f));
            SetEdgeGroupAlpha(emergencyEdgeImages, faded);

            if (viewportFrameOutline != null)
            {
                Color targetBorder = activeChoicePreview != null
                    ? Color.Lerp(Palette.PanelBorder, activeChoiceAccent, 0.55f)
                    : new Color(Palette.PanelBorder.r, Palette.PanelBorder.g, Palette.PanelBorder.b, 0.92f);
                viewportFrameOutline.effectColor = Color.Lerp(viewportFrameOutline.effectColor, targetBorder, Mathf.Clamp01(dt * 8f));
            }
        }
    }

    private void UpdateModuleAnimations(float dt, float time)
    {
        float pulse01 = 0.5f + 0.5f * Mathf.Sin(time * 6.5f);

        for (int i = 0; i < modules.Length; i++)
        {
            ModuleSlot slot = modules[i];
            if (slot == null || slot.rootRect == null)
            {
                continue;
            }

            bool isDirectlyHovered = hoveredModuleIndex == i;
            bool showReticle = slot.isTargeted;

            float desiredScale = slot.isTargeted
                ? 1.06f
                : isDirectlyHovered
                    ? 1.045f
                    : slot.isDimmed ? 0.975f : 1f;

            slot.currentScale = Mathf.Lerp(slot.currentScale, desiredScale, Mathf.Clamp01(dt * 14f));
            slot.rootRect.localScale = new Vector3(slot.currentScale, slot.currentScale, 1f);

            if (slot.cardBg != null)
            {
                Color targetBg = slot.isTargeted
                    ? Color.Lerp(CardBaseDark, activeChoiceAccent, 0.22f)
                    : isDirectlyHovered
                        ? Color.Lerp(CardBaseDark, Palette.FunkyCyan, 0.18f)
                        : slot.isDimmed
                            ? CardDimmedDark
                            : CardBaseDark;
                slot.cardBg.color = targetBg;
            }

            if (slot.cardOutline != null)
            {
                Color targetOutline = slot.isTargeted
                    ? Color.Lerp(activeChoiceAccent, Palette.TextPrimary, pulse01 * 0.5f)
                    : isDirectlyHovered
                        ? Palette.FunkyCyan
                        : slot.isDimmed
                            ? new Color(Palette.PanelBorder.r, Palette.PanelBorder.g, Palette.PanelBorder.b, 0.35f)
                            : slot.activeBorderColor;
                slot.cardOutline.effectColor = targetOutline;
            }

            if (slot.reticleParts != null)
            {
                Color reticleColor = Color.Lerp(activeChoiceAccent, Palette.TextPrimary, pulse01 * 0.45f);
                reticleColor.a = Mathf.Lerp(0.55f, 1f, pulse01);
                for (int r = 0; r < slot.reticleParts.Count; r++)
                {
                    Image part = slot.reticleParts[r];
                    if (part == null)
                    {
                        continue;
                    }
                    if (part.gameObject.activeSelf != showReticle)
                    {
                        part.gameObject.SetActive(showReticle);
                    }
                    if (showReticle)
                    {
                        part.color = reticleColor;
                    }
                }
            }
        }
    }

    private void RefreshSkyStatusBadge(GameState state)
    {
        if (skyBadgeText == null || skyBadgeOutline == null)
        {
            return;
        }

        int blocked = Mathf.Clamp(state.sunlightBlockedPercent, 0, 100);
        string label;
        Color badgeColor;
        if (blocked >= 90)
        {
            label = $"SKY: {blocked}% NOON BLACKOUT";
            badgeColor = Palette.FunkyCoral;
        }
        else if (blocked >= 55)
        {
            label = $"SKY: {blocked}% HEAVY DUST";
            badgeColor = Palette.MarsAccent;
        }
        else if (blocked >= 25)
        {
            label = $"SKY: {blocked}% GROWING STORM";
            badgeColor = Palette.FunkyGold;
        }
        else
        {
            label = $"SKY: {blocked}% DUST HAZE";
            badgeColor = Palette.FunkyCyan;
        }

        skyBadgeText.text = label;
        skyBadgeText.color = badgeColor;
        skyBadgeOutline.effectColor = badgeColor;
    }

    private void RefreshModuleVisuals(GameState state)
    {
        // 1. SOLAR ARRAYS
        bool edsActive = !string.IsNullOrEmpty(state.airTag)
            && state.airTag.IndexOf("EDS", StringComparison.OrdinalIgnoreCase) >= 0;
        SetEdsVisualActive(edsActive);

        int sunThru = Mathf.Clamp(100 - state.sunlightBlockedPercent, 0, 100);
        if (edsActive)
        {
            SetModuleBadge(ModuleId.SOLAR, $"EDS ACTIVE • {sunThru}% SUN", Palette.FunkyCyan, true);
        }
        else if (state.sunlightBlockedPercent >= 85)
        {
            SetModuleBadge(ModuleId.SOLAR, $"BLACKOUT • {sunThru}% SUN", Palette.FunkyCoral, false);
        }
        else if (state.sunlightBlockedPercent >= 45)
        {
            SetModuleBadge(ModuleId.SOLAR, $"DUSTY • {sunThru}% SUN", Palette.MarsAccent, false);
        }
        else
        {
            SetModuleBadge(ModuleId.SOLAR, $"{sunThru}% SUNLIGHT", Palette.FunkyGold, true);
        }

        // 2. KILOPOWER REACTOR
        bool reactorActive = !string.IsNullOrEmpty(state.reactorTag);
        if (reactorCoreImage != null && reactorCoreGlow != null)
        {
            if (!reactorActive)
            {
                reactorCoreImage.color = new Color(0.28f, 0.35f, 0.44f, 0.72f);
                reactorCoreGlow.color = new Color(Palette.FunkyCyan.r, Palette.FunkyCyan.g, Palette.FunkyCyan.b, 0.08f);
                SetModuleBadge(ModuleId.REACTOR, "STANDBY (OFFLINE)", Palette.TextSecondary, true);
            }
            else if (!state.reactorGood || state.reactorTag.IndexOf("REDLINE", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                reactorCoreImage.color = Palette.FunkyCoral;
                reactorCoreGlow.color = new Color(Palette.FunkyCoral.r, Palette.FunkyCoral.g, Palette.FunkyCoral.b, 0.68f);
                SetModuleBadge(ModuleId.REACTOR, StripCategoryPrefix(state.reactorTag, "REACTOR:"), Palette.FunkyCoral, false);
            }
            else
            {
                reactorCoreImage.color = Palette.FunkyGold;
                reactorCoreGlow.color = new Color(Palette.FunkyMint.r, Palette.FunkyMint.g, Palette.FunkyMint.b, 0.62f);
                SetModuleBadge(ModuleId.REACTOR, StripCategoryPrefix(state.reactorTag, "REACTOR:"), Palette.FunkyMint, true);
            }
        }

        // 3. CREW HAB & CROPS
        bool isCold = !string.IsNullOrEmpty(state.tempTag) && !state.tempGood;
        Color winColor = isCold ? FrostCyanColor : Palette.FunkyGold;
        for (int i = 0; i < habWindows.Count; i++)
        {
            if (habWindows[i] != null)
            {
                habWindows[i].color = winColor;
            }
        }
        if (habDomeTint != null)
        {
            habDomeTint.color = isCold
                ? new Color(0.08f, 0.22f, 0.32f, 0.95f)
                : new Color(0.16f, 0.22f, 0.30f, 0.95f);
        }
        if (habHydroStrip != null)
        {
            if (string.IsNullOrEmpty(state.foodTag))
            {
                habHydroStrip.color = new Color(Palette.FunkyMint.r, Palette.FunkyMint.g, Palette.FunkyMint.b, 0.45f);
            }
            else
            {
                habHydroStrip.color = state.foodGood ? Palette.FunkyMint : Palette.MarsAccent;
            }
        }

        string habBadge;
        Color habColor;
        bool habGood = state.tempGood && state.foodGood;
        if (!string.IsNullOrEmpty(state.tempTag) && !string.IsNullOrEmpty(state.foodTag))
        {
            string tShort = isCold ? "54°F COLD" : "68°F WARM";
            string fShort = state.foodGood ? "CROPS ON" : "CROPS OFF";
            habBadge = $"{tShort} • {fShort}";
            habColor = habGood ? Palette.FunkyMint : Palette.FunkyCoral;
        }
        else if (!string.IsNullOrEmpty(state.tempTag))
        {
            habBadge = StripCategoryPrefix(state.tempTag, "TEMP:");
            habColor = state.tempGood ? Palette.FunkyGold : FrostCyanColor;
        }
        else if (!string.IsNullOrEmpty(state.foodTag))
        {
            habBadge = StripCategoryPrefix(state.foodTag, "FOOD:");
            habColor = state.foodGood ? Palette.FunkyMint : Palette.MarsAccent;
        }
        else
        {
            habBadge = "68°F • CROPS READY";
            habColor = Palette.FunkyCyan;
        }
        SetModuleBadge(ModuleId.HAB_TEMP, habBadge, habColor, habGood);

        // 4. MOXIE O2 & AIR
        bool o2Set = !string.IsNullOrEmpty(state.oxygenTag);
        bool airSet = !string.IsNullOrEmpty(state.airTag);
        bool o2Bad = (o2Set && !state.oxygenGood) || (airSet && !state.airGood);

        Color o2Glow = o2Bad ? Palette.FunkyCoral : (o2Set ? Palette.FunkyMint : Palette.FunkyCyan);
        Color airGlow = (airSet && !state.airGood) ? Palette.MarsAccent : Palette.FunkyMint;
        if (moxieTankLeftGlow != null) moxieTankLeftGlow.color = o2Glow;
        if (moxieTankRightGlow != null) moxieTankRightGlow.color = airGlow;
        if (moxieIntakeGlow != null) moxieIntakeGlow.color = o2Bad ? Palette.FunkyCoral : Palette.ScienceCyan;

        string moxieBadge = o2Set
            ? StripCategoryPrefix(state.oxygenTag, "OXYGEN:")
            : airSet
                ? StripCategoryPrefix(state.airTag, "AIR:")
                : "O2 / HEPA NOMINAL";
        SetModuleBadge(ModuleId.MOXIE_O2, moxieBadge, o2Bad ? Palette.FunkyCoral : Palette.FunkyCyan, !o2Bad);

        // 5. RETURN ROCKET (MAV_FUEL)
        bool fuelSet = !string.IsNullOrEmpty(state.fuelTag);
        bool fuelBad = fuelSet && !state.fuelGood;
        if (mavCryoTankGlow != null)
        {
            mavCryoTankGlow.color = fuelBad
                ? Palette.FunkyCoral
                : fuelSet ? Palette.FunkyMint : Palette.ScienceCyan;
        }
        string mavBadge = fuelSet
            ? StripCategoryPrefix(state.fuelTag, "FUEL:")
            : "CRYO CHILLERS ON";
        SetModuleBadge(ModuleId.MAV_FUEL, mavBadge, fuelBad ? Palette.FunkyCoral : Palette.ScienceCyan, !fuelBad);
    }

    private void SetEdsVisualActive(bool active)
    {
        if (solarEdsBar != null)
        {
            solarEdsBar.gameObject.SetActive(active);
        }
        for (int i = 0; i < solarEdsSparks.Count; i++)
        {
            if (solarEdsSparks[i] != null)
            {
                solarEdsSparks[i].gameObject.SetActive(active);
            }
        }
    }

    private void SetModuleBadge(ModuleId id, string text, Color accent, bool isGood)
    {
        ModuleSlot slot = modules[(int)id];
        if (slot == null)
        {
            return;
        }

        slot.activeBorderColor = isGood
            ? new Color(accent.r, accent.g, accent.b, 0.72f)
            : Palette.FunkyCoral;

        if (slot.badgeText != null)
        {
            slot.badgeText.text = text;
            slot.badgeText.color = isGood ? Palette.TextPrimary : Palette.FunkyGold;
        }
        if (slot.badgeOutline != null)
        {
            slot.badgeOutline.effectColor = accent;
        }
        if (slot.badgeBg != null)
        {
            slot.badgeBg.color = isGood
                ? new Color(0.08f, 0.16f, 0.24f, 0.95f)
                : new Color(0.28f, 0.10f, 0.13f, 0.95f);
        }
    }

    private void RefreshConduitBaseColors()
    {
        Color c = GetConduitPowerColor(cachedPowerLevel);
        for (int i = 0; i < conduitSegments.Count; i++)
        {
            if (conduitSegments[i] != null)
            {
                conduitSegments[i].color = c;
            }
        }
    }

    private static Color GetConduitPowerColor(int power)
    {
        if (power <= 30)
        {
            return Palette.FunkyCoral;
        }
        if (power <= 40)
        {
            return Palette.MarsAccent;
        }
        return Palette.FunkyCyan;
    }

    private void RefreshDefaultTelemetryText(GameState state)
    {
        if (state == null)
        {
            return;
        }

        int sunThru = Mathf.Clamp(100 - state.sunlightBlockedPercent, 0, 100);
        string statusWord = (state.power <= 30 || state.crewHealth <= 30)
            ? "<color=#FF5E7E><b>EMERGENCY ALERT</b></color>"
            : "<color=#06D6A0><b>BASE TELEMETRY NOMINAL</b></color>";

        defaultTelemetryMessage =
            $"SOL {state.sol}  •  {statusWord}  |  Grid: <b>{state.power}%</b>  •  Crew: <b>{state.crewHealth}%</b>  •  Sunlight: <b>{sunThru}%</b>  |  Hover an option to preview targets, or click any module for NASA facts.";
    }

    private void RefreshTelemetryDisplay()
    {
        if (telemetryReadoutText == null || telemetryStripOutline == null)
        {
            return;
        }

        if (activeChoicePreview != null)
        {
            telemetryReadoutText.text = BuildChoicePreviewText(activeChoicePreview, activeChoiceOptionIndex);
            telemetryStripOutline.effectColor = activeChoiceAccent;
            return;
        }

        if (hoveredModuleIndex >= 0 && hoveredModuleIndex < modules.Length)
        {
            telemetryReadoutText.text = BuildModuleTooltipText((ModuleId)hoveredModuleIndex);
            telemetryStripOutline.effectColor = Palette.FunkyCyan;
            return;
        }

        telemetryReadoutText.text = defaultTelemetryMessage;
        telemetryStripOutline.effectColor = isEmergencyActive ? Palette.DangerRed : Palette.PanelBorder;
    }

    private string BuildChoicePreviewText(Choice choice, int optionIndex)
    {
        bool[] targets = new bool[5];
        ResolveChoiceTargets(choice, targets);

        List<string> targetNames = new List<string>(3);
        for (int i = 0; i < targets.Length; i++)
        {
            if (!targets[i])
            {
                continue;
            }
            targetNames.Add(GetFriendlyTargetName((ModuleId)i));
        }

        string targetList = targetNames.Count > 0 ? string.Join(" + ", targetNames) : "BASE SYSTEMS";
        int pDelta = lastSyncedState != null ? lastSyncedState.GetEffectivePowerDelta(choice) : choice.powerDelta;
        int hDelta = lastSyncedState != null ? lastSyncedState.GetEffectiveHealthDelta(choice) : choice.EffectiveCrewHealthDelta;

        string pStr = pDelta > 0
            ? $"<color=#FFD166>+{pDelta}% Elec</color>"
            : pDelta == 0
                ? "<color=#00F5D4>0% Elec</color>"
                : $"<color=#E87A3E>{pDelta}% Elec</color>";
        string hStr = hDelta > 0
            ? $"<color=#06D6A0>+{hDelta}% Health</color>"
            : hDelta == 0
                ? "<color=#A8B8CC>0% Health</color>"
                : $"<color=#FF5E7E>{hDelta}% Health</color>";

        string tagPart = !string.IsNullOrEmpty(choice.subsystemTag)
            ? $"   <color=#A8B8CC>[{choice.subsystemTag}]</color>"
            : string.Empty;

        string accentHex = "#" + ColorUtility.ToHtmlStringRGB(activeChoiceAccent);
        return $"<color={accentHex}><b>OPTION {optionIndex + 1} PREVIEW -> Targets {targetList}</b></color>   |   {pStr}  •  {hStr}{tagPart}";
    }

    private string BuildModuleTooltipText(ModuleId id)
    {
        int blocked = lastSyncedState != null ? lastSyncedState.sunlightBlockedPercent : 10;
        switch (id)
        {
            case ModuleId.SOLAR:
            {
                bool eds = lastSyncedState != null
                    && !string.IsNullOrEmpty(lastSyncedState.airTag)
                    && lastSyncedState.airTag.IndexOf("EDS", StringComparison.OrdinalIgnoreCase) >= 0;
                string factName = eds ? "2018 Static-Cling Dust & EDS" : "2018 Opportunity Rover Solar Panels";
                return $"<color=#FFD166><b>SOLAR ARRAYS:</b></color> {100 - blocked}% sunlight reaching wings ({blocked}% dust blocked). <color=#00F5D4>Click to inspect: {factName}.</color>";
            }
            case ModuleId.REACTOR:
            {
                string stateTag = lastSyncedState != null && !string.IsNullOrEmpty(lastSyncedState.reactorTag)
                    ? lastSyncedState.reactorTag
                    : "Standby — sunlight-independent nuclear fission backup";
                return $"<color=#06D6A0><b>KILOPOWER REACTOR:</b></color> {stateTag}. <color=#00F5D4>Click to inspect: NASA Fission Surface Power.</color>";
            }
            case ModuleId.HAB_TEMP:
            {
                bool hasFood = lastSyncedState != null && !string.IsNullOrEmpty(lastSyncedState.foodTag);
                string factName = hasFood ? "NASA Mars Hydroponic Greenhouses" : "2018 Heat-Blanket Effect & Warm Nights";
                string tempTag = lastSyncedState != null && !string.IsNullOrEmpty(lastSyncedState.tempTag)
                    ? lastSyncedState.tempTag
                    : "68°F Heated";
                return $"<color=#00F5D4><b>CREW HAB &amp; GREENHOUSE:</b></color> {tempTag}. <color=#FFD166>Click to inspect: {factName}.</color>";
            }
            case ModuleId.MOXIE_O2:
            {
                string o2Info = lastSyncedState != null && !string.IsNullOrEmpty(lastSyncedState.oxygenTag)
                    ? lastSyncedState.oxygenTag
                    : "Splitting 95% CO2 Mars air into breathable O2";
                return $"<color=#2CC5E8><b>MOXIE O2 &amp; AIR LOOP:</b></color> {o2Info}. <color=#00F5D4>Click to inspect: NASA MOXIE Oxygen Generator.</color>";
            }
            case ModuleId.MAV_FUEL:
            default:
            {
                string fuelInfo = lastSyncedState != null && !string.IsNullOrEmpty(lastSyncedState.fuelTag)
                    ? lastSyncedState.fuelTag
                    : "Chilling liquid methane/LOX below -297°F";
                return $"<color=#E87A3E><b>RETURN ROCKET (MAV):</b></color> {fuelInfo}. <color=#00F5D4>Click to inspect: NASA Cryogenic Fuel Cooling.</color>";
            }
        }
    }

    private void ResolveChoiceTargets(Choice choice, bool[] targets)
    {
        if (choice == null || targets == null || targets.Length < 5)
        {
            return;
        }

        string cat = choice.subsystemCategory != null
            ? choice.subsystemCategory.Trim().ToUpperInvariant()
            : string.Empty;

        switch (cat)
        {
            case "FUEL":
                targets[(int)ModuleId.MAV_FUEL] = true;
                break;
            case "TEMP":
            case "TEMPERATURE":
            case "FOOD":
            case "GREENHOUSE":
                targets[(int)ModuleId.HAB_TEMP] = true;
                break;
            case "REACTOR":
                targets[(int)ModuleId.REACTOR] = true;
                break;
            case "OXYGEN":
            case "O2":
                targets[(int)ModuleId.MOXIE_O2] = true;
                break;
            case "AIR":
            case "DUST":
                targets[(int)ModuleId.MOXIE_O2] = true;
                if ((!string.IsNullOrEmpty(choice.subsystemTag) && choice.subsystemTag.IndexOf("EDS", StringComparison.OrdinalIgnoreCase) >= 0)
                    || (!string.IsNullOrEmpty(choice.EffectiveLabel)
                        && (choice.EffectiveLabel.IndexOf("EDS", StringComparison.OrdinalIgnoreCase) >= 0
                            || choice.EffectiveLabel.IndexOf("SOLAR", StringComparison.OrdinalIgnoreCase) >= 0
                            || choice.EffectiveLabel.IndexOf("PANEL", StringComparison.OrdinalIgnoreCase) >= 0)))
                {
                    targets[(int)ModuleId.SOLAR] = true;
                }
                break;
        }

        int pDelta = lastSyncedState != null ? lastSyncedState.GetEffectivePowerDelta(choice) : choice.powerDelta;
        int hDelta = lastSyncedState != null ? lastSyncedState.GetEffectiveHealthDelta(choice) : choice.EffectiveCrewHealthDelta;

        if (pDelta > 0 && !targets[(int)ModuleId.REACTOR])
        {
            targets[(int)ModuleId.SOLAR] = true;
        }
        else if (pDelta <= -12 && !targets[(int)ModuleId.SOLAR] && !targets[(int)ModuleId.REACTOR])
        {
            bool reactorOn = lastSyncedState != null && !string.IsNullOrEmpty(lastSyncedState.reactorTag);
            targets[(int)(reactorOn ? ModuleId.REACTOR : ModuleId.SOLAR)] = true;
        }

        if (hDelta != 0 && !targets[(int)ModuleId.HAB_TEMP] && !targets[(int)ModuleId.MOXIE_O2])
        {
            targets[(int)ModuleId.HAB_TEMP] = true;
        }

        bool any = false;
        for (int i = 0; i < targets.Length; i++)
        {
            if (targets[i])
            {
                any = true;
                break;
            }
        }
        if (!any)
        {
            targets[(int)ModuleId.HAB_TEMP] = true;
        }
    }

    private static string GetFriendlyTargetName(ModuleId id)
    {
        switch (id)
        {
            case ModuleId.SOLAR:
                return "SOLAR POWER GRID";
            case ModuleId.REACTOR:
                return "KILOPOWER REACTOR";
            case ModuleId.HAB_TEMP:
                return "HAB & GREENHOUSE";
            case ModuleId.MOXIE_O2:
                return "MOXIE O2 & AIR";
            case ModuleId.MAV_FUEL:
                return "RETURN ROCKET";
            default:
                return "BASE";
        }
    }

    private void OnModuleHoverEnter(int index)
    {
        hoveredModuleIndex = index;
        if (missionAudio != null && activeChoicePreview == null)
        {
            missionAudio.PlayHoverTick();
        }
        RefreshTelemetryDisplay();
    }

    private void OnModuleHoverExit(int index)
    {
        if (hoveredModuleIndex == index)
        {
            hoveredModuleIndex = -1;
        }
        RefreshTelemetryDisplay();
    }

    private void OnModuleClicked(int index)
    {
        if (missionAudio != null)
        {
            missionAudio.PlayModuleInspect();
        }

        string factKey = ResolveModuleFactKey((ModuleId)index);
        if (missionUI != null && !string.IsNullOrEmpty(factKey))
        {
            missionUI.ShowFactPopupByKey(factKey);
        }
    }

    private string ResolveModuleFactKey(ModuleId id)
    {
        switch (id)
        {
            case ModuleId.SOLAR:
                bool eds = lastSyncedState != null
                    && !string.IsNullOrEmpty(lastSyncedState.airTag)
                    && lastSyncedState.airTag.IndexOf("EDS", StringComparison.OrdinalIgnoreCase) >= 0;
                return eds ? "2018_static_dust" : "2018_opportunity_panels";
            case ModuleId.REACTOR:
                return "2018_fission_power";
            case ModuleId.HAB_TEMP:
                bool hasFood = lastSyncedState != null && !string.IsNullOrEmpty(lastSyncedState.foodTag);
                return hasFood ? "2018_greenhouse_crops" : "2018_warm_nights";
            case ModuleId.MOXIE_O2:
                return "2018_moxie_oxygen";
            case ModuleId.MAV_FUEL:
            default:
                return "2018_fuel_cooling";
        }
    }

    private void OnSoundToggleClicked()
    {
        if (missionAudio != null)
        {
            missionAudio.ToggleMute();
            if (!missionAudio.IsMuted)
            {
                missionAudio.PlayHoverTick();
            }
        }
        else
        {
            localMutedFallback = !localMutedFallback;
        }

        RefreshSoundButtonVisual();
    }

    private void RefreshSoundButtonVisual()
    {
        bool muted = missionAudio != null ? missionAudio.IsMuted : localMutedFallback;
        lastRenderedMuted = muted;

        if (soundToggleLabel != null)
        {
            soundToggleLabel.text = muted ? "[SOUND: OFF]" : "[SOUND: ON]";
            soundToggleLabel.color = muted ? Palette.TextSecondary : Palette.FunkyCyan;
        }
        if (soundToggleOutline != null)
        {
            soundToggleOutline.effectColor = muted ? Palette.MarsAccent : Palette.FunkyCyan;
        }
        if (soundToggleBg != null)
        {
            soundToggleBg.color = muted ? Palette.PanelSurface : Palette.NasaBoxSurface;
        }
    }

    private static string StripCategoryPrefix(string tag, string prefix)
    {
        if (string.IsNullOrEmpty(tag))
        {
            return string.Empty;
        }
        if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return tag.Substring(prefix.Length).Trim();
        }
        return tag.Trim();
    }

    private static void SetEdgeGroupAlpha(List<Image> images, float alpha)
    {
        for (int i = 0; i < images.Count; i++)
        {
            Image img = images[i];
            if (img == null)
            {
                continue;
            }
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }

    private static GameObject CreateChildRect(string name, Transform parent, float minX, float minY, float maxX, float maxY)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform));
        obj.transform.SetParent(parent, false);
        SetAnchors(obj.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
        return obj;
    }

    private static Image CreateDecorativeImage(
        string name,
        Transform parent,
        float minX,
        float minY,
        float maxX,
        float maxY,
        Color color)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(parent, false);
        SetAnchors(obj.GetComponent<RectTransform>(), minX, minY, maxX, maxY);
        Image img = obj.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    private TMP_Text CreateLabel(
        string name,
        Transform parent,
        float minX,
        float minY,
        float maxX,
        float maxY,
        string text,
        float fontSize,
        FontStyles style,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        SetAnchors(obj.GetComponent<RectTransform>(), minX, minY, maxX, maxY);

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null)
        {
            tmp.font = uiFont;
        }
        tmp.text = text;
        tmp.enableAutoSizing = false;
        tmp.fontSize = fontSize;
        tmp.fontStyle = style;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Truncate;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        if (rt == null)
        {
            return;
        }
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private sealed class DustParticle
    {
        public RectTransform rect;
        public Image image;
        public float localX;
        public float baseNormY;
        public float halfWidth;
        public float baseSpeed;
        public float wobbleFreq;
        public float wobbleAmp;
        public float phase;
        public float alphaWeight;
    }

    private sealed class ModuleSlot
    {
        public RectTransform rootRect;
        public Image cardBg;
        public Outline cardOutline;
        public RectTransform artRoot;
        public Image badgeBg;
        public Outline badgeOutline;
        public TMP_Text badgeText;
        public List<Image> reticleParts;
        public Color activeBorderColor = Palette.PanelBorder;
        public float currentScale = 1f;
        public bool isTargeted;
        public bool isDimmed;
    }
}
