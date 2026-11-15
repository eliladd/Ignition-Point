using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Procedural interactive companion characters:
/// - Captain Nova (Mission Commander) docked on the LEFT side of the screen.
/// - Pip (Science Rover) docked on the RIGHT side of the screen.
/// Each character has an independent animated avatar, voice chirp, and speech bubble.
/// </summary>
public sealed class MissionGuide : MonoBehaviour
{
    public enum Speaker
    {
        Pip,
        Nova
    }

    public enum Mood
    {
        Normal,
        Happy,
        Thinking,
        Warning
    }

    // Dependencies
    private MissionUI missionUI;
    private TMP_FontAsset uiFont;
    private MissionAudio missionAudio;

    // Hierarchy RectTransforms
    private RectTransform rootRect;
    private RectTransform leftDockRect;   // Captain Nova (Left)
    private RectTransform rightDockRect;  // Pip Rover (Right)

    // Captain Nova (Left) UI & Visuals
    private RectTransform novaRootRect;
    private RectTransform novaHeadRect;
    private RectTransform novaBodyRect;
    private Image novaVisorImage;
    private Image novaCommLight;
    private Image novaBubbleBg;
    private Outline novaBubbleOutline;
    private TMP_Text novaBadgeText;
    private TMP_Text novaDialogueText;
    private string fullNovaText = string.Empty;
    private string currentNovaSolText = string.Empty;
    private Coroutine novaTypeCoroutine;
    private bool isNovaTyping;

    // Pip Rover (Right) UI & Visuals
    private RectTransform pipRootRect;
    private RectTransform pipHeadRect;
    private RectTransform pipBodyRect;
    private RectTransform pipEyesContainerRect;
    private RectTransform pipLeftEyeRect;
    private RectTransform pipRightEyeRect;
    private Image pipLeftEyeImage;
    private Image pipRightEyeImage;
    private Image pipAntennaLight;
    private readonly List<RectTransform> pipWheels = new List<RectTransform>();
    private Image pipBubbleBg;
    private Outline pipBubbleOutline;
    private TMP_Text pipBadgeText;
    private TMP_Text pipDialogueText;
    private string fullPipText = string.Empty;
    private string currentPipSolText = string.Empty;
    private Coroutine pipTypeCoroutine;
    private bool isPipTyping;

    // Procedural assets cleanup
    private readonly List<Texture2D> proceduralTextures = new List<Texture2D>();
    private readonly List<Sprite> proceduralSprites = new List<Sprite>();
    private Sprite roundedRectSprite;
    private Sprite circleSprite;
    private Sprite triangleSprite;

    // State
    private bool isHoverOverridden;

    // Animation Timers
    private float idleTimer;
    private float blinkTimer;
    private float nextBlinkInterval = 3.5f;
    private float hopOffsetPip;
    private float hopOffsetNova;
    private float speakBouncePip;
    private float speakBounceNova;

    // UI Colors
    private static readonly Color ColorSpaceNavy = new Color(0.043f, 0.075f, 0.169f, 0.90f);
    private static readonly Color ColorCyanAccent = new Color(0.00f, 0.96f, 0.83f, 1f);
    private static readonly Color ColorGoldAccent = new Color(1.00f, 0.72f, 0.01f, 1f);
    private static readonly Color ColorRoverOrange = new Color(0.957f, 0.635f, 0.380f, 1f);
    private static readonly Color ColorAstronautWhite = new Color(0.96f, 0.97f, 0.99f, 1f);
    private static readonly Color ColorSuitOrange = new Color(0.91f, 0.38f, 0.18f, 1f);
    private static readonly Color ColorDarkPlates = new Color(0.12f, 0.16f, 0.24f, 1f);

    public static MissionGuide Attach(
        MissionUI owner,
        Transform canvasRoot,
        TMP_FontAsset font,
        MissionAudio audio)
    {
        if (canvasRoot == null)
        {
            Debug.LogWarning("[MissionGuide] Cannot attach to a null canvasRoot.");
            return null;
        }

        MissionGuide existing = canvasRoot.GetComponentInChildren<MissionGuide>(true);
        if (existing != null)
        {
            existing.missionUI = owner;
            existing.uiFont = font;
            existing.missionAudio = audio;
            existing.EnsureBuilt();
            return existing;
        }

        GameObject guideObj = new GameObject("MissionGuide", typeof(RectTransform), typeof(MissionGuide));
        guideObj.transform.SetParent(canvasRoot, false);
        guideObj.transform.SetAsLastSibling();

        MissionGuide guide = guideObj.GetComponent<MissionGuide>();
        guide.missionUI = owner;
        guide.uiFont = font;
        guide.missionAudio = audio;
        guide.EnsureBuilt();
        return guide;
    }

    private void EnsureBuilt()
    {
        if (rootRect != null)
        {
            return;
        }

        rootRect = GetComponent<RectTransform>();
        SetAnchors(rootRect, 0f, 0f, 1f, 1f);

        CreateProceduralSprites();
        BuildLeftNovaDock();
        BuildRightPipDock();
    }

    private void CreateProceduralSprites()
    {
        // 1. Crisp Rounded Rectangle Sprite (32x32, 9-sliced)
        int rSize = 32;
        Texture2D rTex = new Texture2D(rSize, rSize, TextureFormat.RGBA32, false)
        {
            name = "GuideRoundedRectTex",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        float radius = 8f;
        for (int y = 0; y < rSize; y++)
        {
            for (int x = 0; x < rSize; x++)
            {
                float cx = (x < radius) ? radius : (x >= rSize - radius) ? (rSize - 1 - radius) : x;
                float cy = (y < radius) ? radius : (y >= rSize - radius) ? (rSize - 1 - radius) : y;
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                float alpha = Mathf.Clamp01(radius + 0.5f - dist);
                rTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        rTex.Apply();
        roundedRectSprite = Sprite.Create(rTex, new Rect(0, 0, rSize, rSize), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, new Vector4(radius, radius, radius, radius));
        roundedRectSprite.hideFlags = HideFlags.HideAndDontSave;
        proceduralTextures.Add(rTex);
        proceduralSprites.Add(roundedRectSprite);

        // 2. Crisp Circle Sprite (64x64)
        int cSize = 64;
        Texture2D cTex = new Texture2D(cSize, cSize, TextureFormat.RGBA32, false)
        {
            name = "GuideCircleTex",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        float cCenter = (cSize - 1) * 0.5f;
        for (int y = 0; y < cSize; y++)
        {
            for (int x = 0; x < cSize; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cCenter, cCenter));
                float alpha = Mathf.Clamp01(cCenter + 0.5f - dist);
                cTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        cTex.Apply();
        circleSprite = Sprite.Create(cTex, new Rect(0, 0, cSize, cSize), new Vector2(0.5f, 0.5f), 100f);
        circleSprite.hideFlags = HideFlags.HideAndDontSave;
        proceduralTextures.Add(cTex);
        proceduralSprites.Add(circleSprite);

        // 3. Triangle Pointer Sprite (32x32)
        int tSize = 32;
        Texture2D tTex = new Texture2D(tSize, tSize, TextureFormat.RGBA32, false)
        {
            name = "GuideTriangleTex",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear,
            hideFlags = HideFlags.HideAndDontSave
        };
        for (int y = 0; y < tSize; y++)
        {
            float halfW = (tSize - y) * 0.5f;
            for (int x = 0; x < tSize; x++)
            {
                float dx = Mathf.Abs(x - (tSize * 0.5f));
                float alpha = Mathf.Clamp01(halfW + 0.5f - dx);
                tTex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
            }
        }
        tTex.Apply();
        triangleSprite = Sprite.Create(tTex, new Rect(0, 0, tSize, tSize), new Vector2(0.5f, 0.5f), 100f);
        triangleSprite.hideFlags = HideFlags.HideAndDontSave;
        proceduralTextures.Add(tTex);
        proceduralSprites.Add(triangleSprite);
    }

    private void BuildLeftNovaDock()
    {
        // Left Side Dock: X = 0.012 to 0.175, Y = 0.240 to 0.800
        GameObject dockObj = new GameObject("LeftNovaDock", typeof(RectTransform));
        dockObj.transform.SetParent(rootRect, false);
        leftDockRect = dockObj.GetComponent<RectTransform>();
        SetAnchors(leftDockRect, 0.012f, 0.240f, 0.175f, 0.800f);

        // Nova's Speech Bubble (Y = 0.37 to 1.00)
        GameObject bubbleObj = new GameObject("NovaSpeechBubble", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        bubbleObj.transform.SetParent(leftDockRect, false);
        RectTransform bubbleRt = bubbleObj.GetComponent<RectTransform>();
        SetAnchors(bubbleRt, 0f, 0.37f, 1f, 1f);

        novaBubbleBg = bubbleObj.GetComponent<Image>();
        novaBubbleBg.sprite = roundedRectSprite;
        novaBubbleBg.type = Image.Type.Sliced;
        novaBubbleBg.color = ColorSpaceNavy;
        novaBubbleBg.raycastTarget = true;

        novaBubbleOutline = bubbleObj.GetComponent<Outline>();
        novaBubbleOutline.effectColor = ColorGoldAccent;
        novaBubbleOutline.effectDistance = new Vector2(2f, -2f);

        Button bubbleBtn = bubbleObj.GetComponent<Button>();
        bubbleBtn.transition = Selectable.Transition.None;
        bubbleBtn.onClick.AddListener(() =>
        {
            if (isNovaTyping)
            {
                SkipNovaTypewriter();
            }
            else
            {
                missionAudio?.PlayCharacterBlip(false);
                TriggerNovaBounce();
            }
        });

        novaBadgeText = CreateLabel("NovaBadge", bubbleRt, 0.06f, 0.82f, 0.94f, 0.96f, "[NOVA] Commander", 13.5f, FontStyles.Bold, ColorGoldAccent, TextAlignmentOptions.MidlineLeft);
        novaDialogueText = CreateLabel("NovaDialogue", bubbleRt, 0.06f, 0.12f, 0.94f, 0.80f, "Welcome aboard, Cadet!", 15.5f, FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);
        novaDialogueText.textWrappingMode = TextWrappingModes.Normal;
        novaDialogueText.lineSpacing = 1.15f;
        CreateLabel("NovaTapHint", bubbleRt, 0.06f, 0.02f, 0.94f, 0.11f, "Tap Nova for Hint", 10.5f, FontStyles.Italic, new Color(1f, 0.85f, 0.45f, 0.78f), TextAlignmentOptions.MidlineRight);

        // Bubble Tail pointing down to Nova
        GameObject tailObj = new GameObject("NovaBubbleTail", typeof(RectTransform), typeof(Image));
        tailObj.transform.SetParent(leftDockRect, false);
        SetAnchors(tailObj.GetComponent<RectTransform>(), 0.42f, 0.335f, 0.58f, 0.37f);
        Image tailImg = tailObj.GetComponent<Image>();
        tailImg.sprite = triangleSprite;
        tailImg.color = ColorSpaceNavy;
        tailImg.raycastTarget = false;
        tailObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        // Stage Pedestal
        GameObject groundObj = new GameObject("NovaPedestal", typeof(RectTransform), typeof(Image), typeof(Outline));
        groundObj.transform.SetParent(leftDockRect, false);
        SetAnchors(groundObj.GetComponent<RectTransform>(), 0.10f, 0.00f, 0.90f, 0.03f);
        Image groundImg = groundObj.GetComponent<Image>();
        groundImg.sprite = roundedRectSprite;
        groundImg.type = Image.Type.Sliced;
        groundImg.color = ColorDarkPlates;
        Outline groundOut = groundObj.GetComponent<Outline>();
        groundOut.effectColor = ColorGoldAccent;
        groundOut.effectDistance = new Vector2(1f, -1f);

        // Captain Nova Character Avatar (Y = 0.03 to 0.335)
        BuildCaptainNova(leftDockRect);
    }

    private void BuildRightPipDock()
    {
        // Right Side Dock: X = 0.825 to 0.988, Y = 0.240 to 0.800
        GameObject dockObj = new GameObject("RightPipDock", typeof(RectTransform));
        dockObj.transform.SetParent(rootRect, false);
        rightDockRect = dockObj.GetComponent<RectTransform>();
        SetAnchors(rightDockRect, 0.825f, 0.240f, 0.988f, 0.800f);

        // Pip's Speech Bubble (Y = 0.37 to 1.00)
        GameObject bubbleObj = new GameObject("PipSpeechBubble", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        bubbleObj.transform.SetParent(rightDockRect, false);
        RectTransform bubbleRt = bubbleObj.GetComponent<RectTransform>();
        SetAnchors(bubbleRt, 0f, 0.37f, 1f, 1f);

        pipBubbleBg = bubbleObj.GetComponent<Image>();
        pipBubbleBg.sprite = roundedRectSprite;
        pipBubbleBg.type = Image.Type.Sliced;
        pipBubbleBg.color = ColorSpaceNavy;
        pipBubbleBg.raycastTarget = true;

        pipBubbleOutline = bubbleObj.GetComponent<Outline>();
        pipBubbleOutline.effectColor = ColorCyanAccent;
        pipBubbleOutline.effectDistance = new Vector2(2f, -2f);

        Button bubbleBtn = bubbleObj.GetComponent<Button>();
        bubbleBtn.transition = Selectable.Transition.None;
        bubbleBtn.onClick.AddListener(() =>
        {
            if (isPipTyping)
            {
                SkipPipTypewriter();
            }
            else
            {
                missionAudio?.PlayCharacterBlip(true);
                TriggerPipBounce();
            }
        });

        pipBadgeText = CreateLabel("PipBadge", bubbleRt, 0.06f, 0.82f, 0.94f, 0.96f, "[PIP] Science Rover", 13.5f, FontStyles.Bold, ColorCyanAccent, TextAlignmentOptions.MidlineLeft);
        pipDialogueText = CreateLabel("PipDialogue", bubbleRt, 0.06f, 0.12f, 0.94f, 0.80f, "BEEP BOOP! Ready to explore Mars!", 15.5f, FontStyles.Normal, Color.white, TextAlignmentOptions.TopLeft);
        pipDialogueText.textWrappingMode = TextWrappingModes.Normal;
        pipDialogueText.lineSpacing = 1.15f;
        CreateLabel("PipTapHint", bubbleRt, 0.06f, 0.02f, 0.94f, 0.11f, "Tap Pip for Science", 10.5f, FontStyles.Italic, new Color(0.55f, 0.95f, 0.92f, 0.78f), TextAlignmentOptions.MidlineRight);

        // Bubble Tail pointing down to Pip
        GameObject tailObj = new GameObject("PipBubbleTail", typeof(RectTransform), typeof(Image));
        tailObj.transform.SetParent(rightDockRect, false);
        SetAnchors(tailObj.GetComponent<RectTransform>(), 0.42f, 0.335f, 0.58f, 0.37f);
        Image tailImg = tailObj.GetComponent<Image>();
        tailImg.sprite = triangleSprite;
        tailImg.color = ColorSpaceNavy;
        tailImg.raycastTarget = false;
        tailObj.transform.localRotation = Quaternion.Euler(0f, 0f, 180f);

        // Stage Pedestal
        GameObject groundObj = new GameObject("PipPedestal", typeof(RectTransform), typeof(Image), typeof(Outline));
        groundObj.transform.SetParent(rightDockRect, false);
        SetAnchors(groundObj.GetComponent<RectTransform>(), 0.10f, 0.00f, 0.90f, 0.03f);
        Image groundImg = groundObj.GetComponent<Image>();
        groundImg.sprite = roundedRectSprite;
        groundImg.type = Image.Type.Sliced;
        groundImg.color = ColorDarkPlates;
        Outline groundOut = groundObj.GetComponent<Outline>();
        groundOut.effectColor = ColorCyanAccent;
        groundOut.effectDistance = new Vector2(1f, -1f);

        // Pip Rover Character Avatar (Y = 0.03 to 0.335)
        BuildPipRover(rightDockRect);
    }

    private void BuildCaptainNova(RectTransform parent)
    {
        GameObject novaObj = new GameObject("CaptainNovaAvatar", typeof(RectTransform), typeof(Button));
        novaObj.transform.SetParent(parent, false);
        novaRootRect = novaObj.GetComponent<RectTransform>();
        SetAnchors(novaRootRect, 0.18f, 0.03f, 0.82f, 0.335f);

        Button novaBtn = novaObj.GetComponent<Button>();
        novaBtn.transition = Selectable.Transition.None;
        novaBtn.onClick.AddListener(() =>
        {
            missionAudio?.PlayCharacterBlip(false);
            TriggerNovaBounce();
            if (isNovaTyping)
            {
                SkipNovaTypewriter();
            }
            else if (!string.IsNullOrEmpty(currentNovaSolText))
            {
                StartNovaTypewriter(currentNovaSolText);
            }
        });

        // Astronaut Body / Suit Torso
        GameObject bodyObj = new GameObject("SuitTorso", typeof(RectTransform), typeof(Image), typeof(Outline));
        bodyObj.transform.SetParent(novaRootRect, false);
        novaBodyRect = bodyObj.GetComponent<RectTransform>();
        SetAnchors(novaBodyRect, 0.15f, 0.18f, 0.85f, 0.58f);
        Image bodyImg = bodyObj.GetComponent<Image>();
        bodyImg.sprite = roundedRectSprite;
        bodyImg.type = Image.Type.Sliced;
        bodyImg.color = ColorAstronautWhite;
        Outline bodyOut = bodyObj.GetComponent<Outline>();
        bodyOut.effectColor = ColorSuitOrange;
        bodyOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Shoulder Pads (Orange EVA trim)
        GameObject shoulderObj = new GameObject("ShoulderTrim", typeof(RectTransform), typeof(Image));
        shoulderObj.transform.SetParent(novaBodyRect, false);
        SetAnchors(shoulderObj.GetComponent<RectTransform>(), 0f, 0.65f, 1f, 1f);
        Image shoulderImg = shoulderObj.GetComponent<Image>();
        shoulderImg.sprite = roundedRectSprite;
        shoulderImg.type = Image.Type.Sliced;
        shoulderImg.color = ColorSuitOrange;

        // Life Support Chest Pack with Status LEDs
        GameObject chestObj = new GameObject("ChestPack", typeof(RectTransform), typeof(Image));
        chestObj.transform.SetParent(novaBodyRect, false);
        SetAnchors(chestObj.GetComponent<RectTransform>(), 0.22f, 0.15f, 0.78f, 0.58f);
        Image chestImg = chestObj.GetComponent<Image>();
        chestImg.sprite = roundedRectSprite;
        chestImg.type = Image.Type.Sliced;
        chestImg.color = ColorDarkPlates;

        // Chest Status LED
        GameObject ledObj = new GameObject("ChestLED", typeof(RectTransform), typeof(Image));
        ledObj.transform.SetParent(chestObj.transform, false);
        SetAnchors(ledObj.GetComponent<RectTransform>(), 0.15f, 0.35f, 0.40f, 0.65f);
        Image ledImg = ledObj.GetComponent<Image>();
        ledImg.sprite = circleSprite;
        ledImg.color = ColorCyanAccent;

        // Mission Commander Star Badge
        GameObject starObj = new GameObject("CommanderBadge", typeof(RectTransform), typeof(Image));
        starObj.transform.SetParent(chestObj.transform, false);
        SetAnchors(starObj.GetComponent<RectTransform>(), 0.55f, 0.30f, 0.85f, 0.70f);
        Image starImg = starObj.GetComponent<Image>();
        starImg.sprite = circleSprite;
        starImg.color = ColorGoldAccent;

        // Helmet Dome
        GameObject headObj = new GameObject("AstronautHelmet", typeof(RectTransform), typeof(Image), typeof(Outline));
        headObj.transform.SetParent(novaRootRect, false);
        novaHeadRect = headObj.GetComponent<RectTransform>();
        SetAnchors(novaHeadRect, 0.10f, 0.50f, 0.90f, 0.98f);
        Image headImg = headObj.GetComponent<Image>();
        headImg.sprite = circleSprite;
        headImg.color = ColorAstronautWhite;
        Outline headOut = headObj.GetComponent<Outline>();
        headOut.effectColor = new Color(0.2f, 0.3f, 0.4f, 0.8f);
        headOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Gold Solar Visor
        GameObject visorObj = new GameObject("SolarVisor", typeof(RectTransform), typeof(Image));
        visorObj.transform.SetParent(novaHeadRect, false);
        SetAnchors(visorObj.GetComponent<RectTransform>(), 0.12f, 0.14f, 0.88f, 0.82f);
        novaVisorImage = visorObj.GetComponent<Image>();
        novaVisorImage.sprite = roundedRectSprite;
        novaVisorImage.type = Image.Type.Sliced;
        novaVisorImage.color = new Color(0.92f, 0.68f, 0.28f, 0.98f);

        // Visor Reflection Highlight Arc
        GameObject gleamObj = new GameObject("VisorGleam", typeof(RectTransform), typeof(Image));
        gleamObj.transform.SetParent(visorObj.transform, false);
        SetAnchors(gleamObj.GetComponent<RectTransform>(), 0.18f, 0.58f, 0.52f, 0.88f);
        Image gleamImg = gleamObj.GetComponent<Image>();
        gleamImg.sprite = roundedRectSprite;
        gleamImg.type = Image.Type.Sliced;
        gleamImg.color = new Color(1f, 1f, 1f, 0.45f);

        // Helmet Comm Antenna with active beacon light
        GameObject antObj = new GameObject("CommMic", typeof(RectTransform), typeof(Image));
        antObj.transform.SetParent(novaHeadRect, false);
        SetAnchors(antObj.GetComponent<RectTransform>(), 0.86f, 0.35f, 0.98f, 0.55f);
        novaCommLight = antObj.GetComponent<Image>();
        novaCommLight.sprite = circleSprite;
        novaCommLight.color = ColorCyanAccent;

        // Nameplate Tag
        CreateLabel("NovaTag", novaRootRect, 0f, 0.01f, 1f, 0.16f, "CAPT. NOVA\n<size=9><color=#FFD166>Commander</color></size>", 10.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
    }

    private void BuildPipRover(RectTransform parent)
    {
        GameObject pipObj = new GameObject("PipRoverAvatar", typeof(RectTransform), typeof(Button));
        pipObj.transform.SetParent(parent, false);
        pipRootRect = pipObj.GetComponent<RectTransform>();
        SetAnchors(pipRootRect, 0.18f, 0.03f, 0.82f, 0.335f);

        Button pipBtn = pipObj.GetComponent<Button>();
        pipBtn.transition = Selectable.Transition.None;
        pipBtn.onClick.AddListener(() =>
        {
            missionAudio?.PlayCharacterBlip(true);
            TriggerPipBounce();
            if (isPipTyping)
            {
                SkipPipTypewriter();
            }
            else if (!string.IsNullOrEmpty(currentPipSolText))
            {
                StartPipTypewriter(currentPipSolText);
            }
        });

        // Rover Chassis Body (Gold/Orange solar rover)
        GameObject bodyObj = new GameObject("RoverChassis", typeof(RectTransform), typeof(Image), typeof(Outline));
        bodyObj.transform.SetParent(pipRootRect, false);
        pipBodyRect = bodyObj.GetComponent<RectTransform>();
        SetAnchors(pipBodyRect, 0.05f, 0.22f, 0.95f, 0.56f);
        Image bodyImg = bodyObj.GetComponent<Image>();
        bodyImg.sprite = roundedRectSprite;
        bodyImg.type = Image.Type.Sliced;
        bodyImg.color = ColorRoverOrange;
        Outline bodyOut = bodyObj.GetComponent<Outline>();
        bodyOut.effectColor = ColorCyanAccent;
        bodyOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Rear Solar Deck (deep blue solar cell pattern)
        GameObject solarObj = new GameObject("SolarDeck", typeof(RectTransform), typeof(Image));
        solarObj.transform.SetParent(pipBodyRect, false);
        SetAnchors(solarObj.GetComponent<RectTransform>(), 0.55f, 0.30f, 0.94f, 0.88f);
        Image solarImg = solarObj.GetComponent<Image>();
        solarImg.sprite = roundedRectSprite;
        solarImg.type = Image.Type.Sliced;
        solarImg.color = new Color(0.11f, 0.21f, 0.34f, 1f);

        // 3 Rugged Wheels
        float wheelW = 0.28f;
        float[] wheelX = { 0.06f, 0.36f, 0.66f };
        for (int i = 0; i < 3; i++)
        {
            GameObject wheelObj = new GameObject($"Wheel_{i + 1}", typeof(RectTransform), typeof(Image));
            wheelObj.transform.SetParent(pipRootRect, false);
            RectTransform wheelRt = wheelObj.GetComponent<RectTransform>();
            SetAnchors(wheelRt, wheelX[i], 0.12f, wheelX[i] + wheelW, 0.32f);
            Image wheelImg = wheelObj.GetComponent<Image>();
            wheelImg.sprite = circleSprite;
            wheelImg.color = ColorDarkPlates;
            pipWheels.Add(wheelRt);

            // Hubcap
            GameObject hubObj = new GameObject("Hubcap", typeof(RectTransform), typeof(Image));
            hubObj.transform.SetParent(wheelObj.transform, false);
            SetAnchors(hubObj.GetComponent<RectTransform>(), 0.30f, 0.30f, 0.70f, 0.70f);
            Image hubImg = hubObj.GetComponent<Image>();
            hubImg.sprite = circleSprite;
            hubImg.color = ColorCyanAccent;
        }

        // Neck Mast
        GameObject neckObj = new GameObject("NeckMast", typeof(RectTransform), typeof(Image));
        neckObj.transform.SetParent(pipRootRect, false);
        SetAnchors(neckObj.GetComponent<RectTransform>(), 0.20f, 0.54f, 0.32f, 0.68f);
        Image neckImg = neckObj.GetComponent<Image>();
        neckImg.sprite = roundedRectSprite;
        neckImg.type = Image.Type.Sliced;
        neckImg.color = ColorDarkPlates;

        // Rover Head Visor Screen
        GameObject headObj = new GameObject("RoverHeadScreen", typeof(RectTransform), typeof(Image), typeof(Outline));
        headObj.transform.SetParent(pipRootRect, false);
        pipHeadRect = headObj.GetComponent<RectTransform>();
        SetAnchors(pipHeadRect, 0.08f, 0.66f, 0.65f, 0.94f);
        Image headImg = headObj.GetComponent<Image>();
        headImg.sprite = roundedRectSprite;
        headImg.type = Image.Type.Sliced;
        headImg.color = ColorDarkPlates;
        Outline headOut = headObj.GetComponent<Outline>();
        headOut.effectColor = ColorCyanAccent;
        headOut.effectDistance = new Vector2(1.5f, -1.5f);

        // Glowing Cyan LED Eyes Container
        GameObject eyesObj = new GameObject("EyesContainer", typeof(RectTransform));
        eyesObj.transform.SetParent(pipHeadRect, false);
        pipEyesContainerRect = eyesObj.GetComponent<RectTransform>();
        SetAnchors(pipEyesContainerRect, 0.12f, 0.20f, 0.88f, 0.80f);

        // Left Eye
        GameObject leftEyeObj = new GameObject("LeftEye", typeof(RectTransform), typeof(Image));
        leftEyeObj.transform.SetParent(pipEyesContainerRect, false);
        pipLeftEyeRect = leftEyeObj.GetComponent<RectTransform>();
        SetAnchors(pipLeftEyeRect, 0.08f, 0.15f, 0.44f, 0.85f);
        pipLeftEyeImage = leftEyeObj.GetComponent<Image>();
        pipLeftEyeImage.sprite = circleSprite;
        pipLeftEyeImage.color = ColorCyanAccent;

        // Left Eye Shine
        GameObject leftShineObj = new GameObject("Shine", typeof(RectTransform), typeof(Image));
        leftShineObj.transform.SetParent(leftEyeObj.transform, false);
        SetAnchors(leftShineObj.GetComponent<RectTransform>(), 0.25f, 0.55f, 0.55f, 0.85f);
        Image leftShineImg = leftShineObj.GetComponent<Image>();
        leftShineImg.sprite = circleSprite;
        leftShineImg.color = Color.white;

        // Right Eye
        GameObject rightEyeObj = new GameObject("RightEye", typeof(RectTransform), typeof(Image));
        rightEyeObj.transform.SetParent(pipEyesContainerRect, false);
        pipRightEyeRect = rightEyeObj.GetComponent<RectTransform>();
        SetAnchors(pipRightEyeRect, 0.56f, 0.15f, 0.92f, 0.85f);
        pipRightEyeImage = rightEyeObj.GetComponent<Image>();
        pipRightEyeImage.sprite = circleSprite;
        pipRightEyeImage.color = ColorCyanAccent;

        // Right Eye Shine
        GameObject rightShineObj = new GameObject("Shine", typeof(RectTransform), typeof(Image));
        rightShineObj.transform.SetParent(rightEyeObj.transform, false);
        SetAnchors(rightShineObj.GetComponent<RectTransform>(), 0.25f, 0.55f, 0.55f, 0.85f);
        Image rightShineImg = rightShineObj.GetComponent<Image>();
        rightShineImg.sprite = circleSprite;
        rightShineImg.color = Color.white;

        // Antenna Mast with Pulsing Beacon LED
        GameObject antMastObj = new GameObject("AntennaMast", typeof(RectTransform), typeof(Image));
        antMastObj.transform.SetParent(pipHeadRect, false);
        SetAnchors(antMastObj.GetComponent<RectTransform>(), 0.45f, 0.96f, 0.55f, 1.25f);
        Image antMastImg = antMastObj.GetComponent<Image>();
        antMastImg.color = ColorDarkPlates;

        GameObject beaconObj = new GameObject("BeaconLED", typeof(RectTransform), typeof(Image));
        beaconObj.transform.SetParent(antMastObj.transform, false);
        SetAnchors(beaconObj.GetComponent<RectTransform>(), -0.6f, 0.80f, 1.6f, 1.40f);
        pipAntennaLight = beaconObj.GetComponent<Image>();
        pipAntennaLight.sprite = circleSprite;
        pipAntennaLight.color = ColorCyanAccent;

        // Nameplate Tag
        CreateLabel("PipTag", pipRootRect, 0f, 0.01f, 1f, 0.16f, "PIP ROVER\n<size=9><color=#00F5D4>Science Rover</color></size>", 10.5f, FontStyles.Bold, Color.white, TextAlignmentOptions.Center);
    }

    private void Update()
    {
        idleTimer += Time.unscaledDeltaTime;

        float novaBob = Mathf.Sin(idleTimer * 2.2f) * 3f;
        float pipBob = Mathf.Sin(idleTimer * 2.8f) * 2f;

        hopOffsetPip = Mathf.MoveTowards(hopOffsetPip, 0f, Time.unscaledDeltaTime * 45f);
        hopOffsetNova = Mathf.MoveTowards(hopOffsetNova, 0f, Time.unscaledDeltaTime * 45f);

        speakBouncePip = isPipTyping
            ? Mathf.Sin(idleTimer * 16f) * 3.5f
            : Mathf.MoveTowards(speakBouncePip, 0f, Time.unscaledDeltaTime * 30f);

        speakBounceNova = isNovaTyping
            ? Mathf.Sin(idleTimer * 14f) * 3.0f
            : Mathf.MoveTowards(speakBounceNova, 0f, Time.unscaledDeltaTime * 30f);

        if (novaRootRect != null)
        {
            novaRootRect.anchoredPosition = new Vector2(0f, novaBob + hopOffsetNova + speakBounceNova);
        }

        if (pipRootRect != null)
        {
            pipRootRect.anchoredPosition = new Vector2(0f, pipBob + hopOffsetPip + speakBouncePip);
        }

        if (pipAntennaLight != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(idleTimer * 6f);
            pipAntennaLight.color = Color.Lerp(new Color(0f, 0.5f, 0.5f, 0.6f), ColorCyanAccent, pulse);
        }

        if (novaCommLight != null)
        {
            float pulse = 0.5f + 0.5f * Mathf.Sin(idleTimer * 4f);
            novaCommLight.color = Color.Lerp(new Color(0.8f, 0.6f, 0.1f, 0.6f), ColorGoldAccent, pulse);
        }

        blinkTimer += Time.unscaledDeltaTime;
        if (blinkTimer >= nextBlinkInterval)
        {
            blinkTimer = 0f;
            nextBlinkInterval = UnityEngine.Random.Range(2.8f, 5.5f);
            StartCoroutine(AnimatePipBlink());
        }
    }

    private IEnumerator AnimatePipBlink()
    {
        if (pipEyesContainerRect != null)
        {
            pipEyesContainerRect.localScale = new Vector3(1f, 0.1f, 1f);
        }
        yield return new WaitForSecondsRealtime(0.12f);
        if (pipEyesContainerRect != null)
        {
            pipEyesContainerRect.localScale = Vector3.one;
        }
    }

    public void TriggerPipBounce()
    {
        hopOffsetPip = 12f;
    }

    public void TriggerNovaBounce()
    {
        hopOffsetNova = 12f;
    }

    public void SelectSpeaker(Speaker speaker, bool playVoiceChirp)
    {
        if (speaker == Speaker.Pip)
        {
            if (playVoiceChirp) missionAudio?.PlayCharacterBlip(true);
            TriggerPipBounce();
            if (!string.IsNullOrEmpty(currentPipSolText))
            {
                StartPipTypewriter(currentPipSolText);
            }
        }
        else
        {
            if (playVoiceChirp) missionAudio?.PlayCharacterBlip(false);
            TriggerNovaBounce();
            if (!string.IsNullOrEmpty(currentNovaSolText))
            {
                StartNovaTypewriter(currentNovaSolText);
            }
        }
    }

    public void ShowForDecision(GameState state, StoryNode node)
    {
        EnsureBuilt();
        isHoverOverridden = false;

        currentPipSolText = node != null && !string.IsNullOrEmpty(node.guideTip)
            ? node.guideTip
            : "Keep an eye on our battery reserve! Dust is settling over Mars.";

        currentNovaSolText = node != null && !string.IsNullOrEmpty(node.novaHint)
            ? node.novaHint
            : "Review both life support and power. A balanced base survives the storm!";

        SetVisible(true);

        if (pipBadgeText != null) pipBadgeText.text = "[PIP] Science Rover";
        if (novaBadgeText != null) novaBadgeText.text = "[NOVA] Commander";

        TriggerPipBounce();
        TriggerNovaBounce();
        StartPipTypewriter(currentPipSolText);
        StartNovaTypewriter(currentNovaSolText);
    }

    public void OnChoiceHover(Choice choice)
    {
        if (choice == null || string.IsNullOrEmpty(choice.guideReaction))
        {
            return;
        }

        isHoverOverridden = true;
        if (choice.subsystemGood)
        {
            TriggerPipBounce();
            if (pipBadgeText != null) pipBadgeText.text = "[PIP] Science Preview";
            StartPipTypewriter(choice.guideReaction);
        }
        else
        {
            TriggerNovaBounce();
            if (novaBadgeText != null) novaBadgeText.text = "[NOVA] Tactical Check";
            StartNovaTypewriter(choice.guideReaction);
        }
    }

    public void OnChoiceUnhover()
    {
        if (!isHoverOverridden)
        {
            return;
        }
        isHoverOverridden = false;

        if (pipBadgeText != null) pipBadgeText.text = "[PIP] Science Rover";
        if (novaBadgeText != null) novaBadgeText.text = "[NOVA] Commander";

        if (!string.IsNullOrEmpty(currentPipSolText))
        {
            StartPipTypewriter(currentPipSolText);
        }
        if (!string.IsNullOrEmpty(currentNovaSolText))
        {
            StartNovaTypewriter(currentNovaSolText);
        }
    }

    public void ShowForOutcome(Choice choice)
    {
        EnsureBuilt();
        if (choice == null)
        {
            return;
        }

        SetVisible(true);

        currentPipSolText = !string.IsNullOrEmpty(choice.guideReaction)
            ? choice.guideReaction
            : "Look at how our base telemetry changed! Every choice teaches us real NASA science.";

        currentNovaSolText = $"Good call on {choice.EffectiveLabel}. Let's check our power and crew before the next Sol!";

        if (pipBadgeText != null) pipBadgeText.text = "[PIP] Science Rover";
        if (novaBadgeText != null) novaBadgeText.text = "[NOVA] Commander";

        TriggerPipBounce();
        StartPipTypewriter(currentPipSolText);
        StartNovaTypewriter(currentNovaSolText);
    }

    public void ShowForIntro(IntroScreen screen)
    {
        EnsureBuilt();
        if (screen == null)
        {
            return;
        }

        SetVisible(true);

        currentPipSolText = !string.IsNullOrEmpty(screen.guideLine)
            ? screen.guideLine
            : "Welcome Cadet! I'm Pip, your science rover. Let's learn how Mars dust storms work!";

        currentNovaSolText = "Pay close attention to these NASA mission briefings. Real data helps us protect Ares Base!";

        if (pipBadgeText != null) pipBadgeText.text = "[PIP] Science Rover";
        if (novaBadgeText != null) novaBadgeText.text = "[NOVA] Commander";

        TriggerPipBounce();
        StartPipTypewriter(currentPipSolText);
        StartNovaTypewriter(currentNovaSolText);
    }

    public void ShowForEnding(StoryEnding ending)
    {
        EnsureBuilt();
        if (ending == null)
        {
            return;
        }

        SetVisible(true);

        currentPipSolText = !string.IsNullOrEmpty(ending.guideLine)
            ? ending.guideLine
            : "You did it Cadet! Thank you for protecting Ares Base and learning real NASA science with us!";

        currentNovaSolText = "Outstanding leadership, Commander! You guided your crew through a planet-wide Martian dust storm.";

        if (pipBadgeText != null) pipBadgeText.text = "[PIP] Science Rover";
        if (novaBadgeText != null) novaBadgeText.text = "[NOVA] Commander";

        TriggerNovaBounce();
        TriggerPipBounce();
        StartPipTypewriter(currentPipSolText);
        StartNovaTypewriter(currentNovaSolText);
    }

    public void ShowForTitle()
    {
        EnsureBuilt();
        SetVisible(true);

        currentPipSolText = "BEEP BOOP! Ready to command a real Mars base? Tap Start Briefing to begin our mission!";
        currentNovaSolText = "Welcome aboard, Cadet! Ready your engineering skills. Mars is full of surprises!";

        if (pipBadgeText != null) pipBadgeText.text = "[PIP] Science Rover";
        if (novaBadgeText != null) novaBadgeText.text = "[NOVA] Commander";

        TriggerPipBounce();
        StartPipTypewriter(currentPipSolText);
        StartNovaTypewriter(currentNovaSolText);
    }

    public void SetVisible(bool visible)
    {
        EnsureBuilt();
        gameObject.SetActive(visible);
        if (leftDockRect != null) leftDockRect.gameObject.SetActive(visible);
        if (rightDockRect != null) rightDockRect.gameObject.SetActive(visible);
    }

    public void SetOverlayActive(bool active)
    {
        if (leftDockRect != null) leftDockRect.gameObject.SetActive(!active);
        if (rightDockRect != null) rightDockRect.gameObject.SetActive(!active);
    }

    private void StartPipTypewriter(string fullText)
    {
        if (pipTypeCoroutine != null)
        {
            StopCoroutine(pipTypeCoroutine);
        }
        fullPipText = fullText ?? string.Empty;
        pipTypeCoroutine = StartCoroutine(TypewriterRoutine(
            fullPipText,
            pipDialogueText,
            true,
            () => { isPipTyping = false; pipTypeCoroutine = null; },
            val => isPipTyping = val));
    }

    private void StartNovaTypewriter(string fullText)
    {
        if (novaTypeCoroutine != null)
        {
            StopCoroutine(novaTypeCoroutine);
        }
        fullNovaText = fullText ?? string.Empty;
        novaTypeCoroutine = StartCoroutine(TypewriterRoutine(
            fullNovaText,
            novaDialogueText,
            false,
            () => { isNovaTyping = false; novaTypeCoroutine = null; },
            val => isNovaTyping = val));
    }

    private IEnumerator TypewriterRoutine(
        string targetText,
        TMP_Text label,
        bool isPip,
        Action onComplete,
        Action<bool> setTyping)
    {
        setTyping(true);
        if (label != null)
        {
            label.text = string.Empty;
        }

        int len = targetText.Length;
        int blipCounter = 0;

        for (int i = 0; i < len; i++)
        {
            if (label != null)
            {
                label.text = targetText.Substring(0, i + 1);
            }

            char c = targetText[i];
            if (c != ' ' && ++blipCounter % 5 == 0)
            {
                missionAudio?.PlayCharacterBlip(isPip);
            }

            float delay = (c == '.' || c == '!' || c == '?') ? 0.14f : 0.020f;
            yield return new WaitForSecondsRealtime(delay);
        }

        if (label != null)
        {
            label.text = targetText;
        }
        onComplete?.Invoke();
    }

    public void SkipPipTypewriter()
    {
        if (!isPipTyping) return;
        if (pipTypeCoroutine != null)
        {
            StopCoroutine(pipTypeCoroutine);
            pipTypeCoroutine = null;
        }
        if (pipDialogueText != null) pipDialogueText.text = fullPipText;
        isPipTyping = false;
    }

    public void SkipNovaTypewriter()
    {
        if (!isNovaTyping) return;
        if (novaTypeCoroutine != null)
        {
            StopCoroutine(novaTypeCoroutine);
            novaTypeCoroutine = null;
        }
        if (novaDialogueText != null) novaDialogueText.text = fullNovaText;
        isNovaTyping = false;
    }

    public void SkipTypewriter()
    {
        SkipPipTypewriter();
        SkipNovaTypewriter();
    }

    private TMP_Text CreateLabel(
        string name,
        Transform parent,
        float minX, float minY, float maxX, float maxY,
        string text,
        float fontSize,
        FontStyles fontStyle,
        Color color,
        TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        RectTransform rt = obj.GetComponent<RectTransform>();
        SetAnchors(rt, minX, minY, maxX, maxY);

        TextMeshProUGUI tmp = obj.GetComponent<TextMeshProUGUI>();
        if (uiFont != null)
        {
            tmp.font = uiFont;
        }
        tmp.enableAutoSizing = false;
        tmp.fontSize = fontSize;
        tmp.fontStyle = fontStyle;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.text = text;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void SetAnchors(RectTransform rt, float minX, float minY, float maxX, float maxY)
    {
        if (rt == null) return;
        rt.anchorMin = new Vector2(minX, minY);
        rt.anchorMax = new Vector2(maxX, maxY);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    private void OnDestroy()
    {
        for (int i = 0; i < proceduralSprites.Count; i++)
        {
            if (proceduralSprites[i] != null)
            {
                DestroyImmediate(proceduralSprites[i]);
            }
        }
        proceduralSprites.Clear();

        for (int i = 0; i < proceduralTextures.Count; i++)
        {
            if (proceduralTextures[i] != null)
            {
                DestroyImmediate(proceduralTextures[i]);
            }
        }
        proceduralTextures.Clear();
    }
}
