using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the Ares Base Mission Control uGUI:
/// - Click-to-enlarge NASA photographs with interactive zoom overlay & content-fitted captions
/// - Content-fitted text boxes across every panel (including individually sized Option 1, 2, 3 cards)
/// - Background-visible translucent cards with 4-card skeletal Top HUD and incited subsystem pills
/// - Word/sentence-first NASA Fact Popup with image explanations below every photo and clickable NASA URLs
/// - Sol Outcome, Ending, and Singular End-of-Mission Comparison spotlight
/// </summary>
public class MissionUI : MonoBehaviour
{
    private const float RefCanvasWidth = 1920f;
    private const float RefCanvasHeight = 1080f;

    [Header("Root Toggleable Panels")]
    [SerializeField] private GameObject titlePanel = null;
    [SerializeField] private GameObject decisionPanel = null;
    [SerializeField] private GameObject outcomePanel = null;

    [Header("Title Panel Elements")]
    [SerializeField] private TMP_Text titleHeaderText = null;
    [SerializeField] private TMP_Text titlePremiseText = null;
    [SerializeField] private Button startButton = null;

    [Header("Decision Panel - Top HUD")]
    [SerializeField] private TMP_Text solCounterText = null;
    [SerializeField] private TMP_Text dustTauText = null;
    [SerializeField] private Image powerBarFill = null;
    [SerializeField] private TMP_Text powerValueText = null;
    [SerializeField] private Image integrityBarFill = null;
    [SerializeField] private TMP_Text integrityValueText = null;
    [SerializeField] private Image scienceBarFill = null;
    [SerializeField] private TMP_Text scienceValueText = null;

    [Header("Decision Panel - Dilemma & Choices")]
    [SerializeField] private TMP_Text nodeTitleText = null;
    [SerializeField] private TMP_Text dilemmaPromptText = null;
    [SerializeField] private RectTransform choicesContainer = null;
    [SerializeField] private Button choiceButtonTemplate = null;

    [Header("Outcome Panel Elements")]
    [SerializeField] private TMP_Text outcomeHeaderText = null;
    [SerializeField] private TMP_Text outcomeBodyText = null;
    [SerializeField] private TMP_Text outcomeTelemetryText = null;
    [SerializeField] private GameObject nasaScienceBox = null;
    [SerializeField] private TMP_Text nasaScienceTitleText = null;
    [SerializeField] private TMP_Text nasaNoteText = null;
    [SerializeField] private Button nextButton = null;
    [SerializeField] private TMP_Text nextButtonLabel = null;

    private GameManager gameManager;
    private bool isInitialized;
    private readonly List<GameObject> spawnedChoiceButtons = new List<GameObject>();
    private readonly Dictionary<string, Texture2D> loadedTextures = new Dictionary<string, Texture2D>(StringComparer.OrdinalIgnoreCase);

    // Runtime scene helpers
    private RectTransform titleCardRect;
    private RectTransform missionBadgeRect;
    private TMP_Text badgeLabelText;
    private TMP_Text startButtonLabelText;

    private RectTransform dilemmaCardRect;
    private RectTransform outcomeCardRect;
    private RectTransform outcomeBodyBoxRect;
    private RectTransform nasaScienceBoxRect;

    private Image consoleBackgroundImage;
    private Image titlePanelBackdrop;
    private Image decisionPanelBackdrop;
    private Image outcomePanelBackdrop;
    private TMP_FontAsset uiFont;

    // Top HUD 4-box references & dynamically sized pill containers
    private RectTransform topHudRect;
    private RectTransform solBoxRect;
    private RectTransform powerBoxRect;
    private RectTransform healthBoxRect;
    private RectTransform dustBoxRect;
    private RectTransform scienceBoxRect;
    private RectTransform powerPillRowRect;
    private RectTransform healthPillRowRect;
    private TMP_Text powerTitleText;
    private TMP_Text healthTitleText;
    private TMP_Text scienceTitleText;
    private readonly List<Image> solPips = new List<Image>();

    private SubsystemPillWidget fuelPill;
    private SubsystemPillWidget tempPill;
    private SubsystemPillWidget reactorPill;

    private SubsystemPillWidget oxygenPill;
    private SubsystemPillWidget foodPill;
    private SubsystemPillWidget airPill;

    private Button stormArchiveButton;
    private TMP_Text stormArchiveLabel;

    // NASA Fact & Word-Explanation Popup
    private GameObject factPopupOverlay;
    private RectTransform factCardRect;
    private TMP_Text factTagText;
    private TMP_Text factPopupTermText;
    private TMP_Text factPopupBodyText;
    private RectTransform factBodyBoxRect;
    private PhotoCaptionWidget factPhotoWidget;
    private Button factCloseButton;
    private TMP_Text factCloseButtonLabel;
    private string activeFactKey;
    private string currentFactUrl;

    // Interactive Click-to-Enlarge Photo Overlay
    private GameObject photoZoomOverlay;
    private RectTransform photoZoomCardRect;
    private RectTransform photoZoomFrameRect;
    private RectTransform photoZoomBadgeRect;
    private RawImage photoZoomRawImage;
    private AspectRatioFitter photoZoomFitter;
    private RectTransform photoZoomCaptionBoxRect;
    private TMP_Text photoZoomCaptionText;
    private Coroutine photoZoomRoutine;

    // Outcome / Ending Unique Image + Caption Block
    private PhotoCaptionWidget outcomePhotoWidget;
    private string currentOutcomeUrl;

    // Singular End-of-Mission Comparison Spotlight
    private RectTransform comparisonContainer;
    private TMP_Text compBadgeText;
    private TMP_Text compSubsystemBadgeText;
    private PhotoCaptionWidget compPhotoWidget;
    private RectTransform compBodyBoxRect;
    private TMP_Text compBodyText;
    private RectTransform compFactBoxRect;
    private TMP_Text compFactText;
    private Button compPrevButton;
    private TMP_Text compPrevButtonText;
    private Button compNextButton;
    private TMP_Text compNextButtonText;
    private string currentComparisonUrl;
    private readonly List<PlayerChoiceRecord> activeComparisonRecords = new List<PlayerChoiceRecord>();
    private int currentComparisonIndex;

    // Procedural audio synthesizer & interactive Ares Base atmospheric cutaway
    private MissionAudio missionAudio;
    private MissionBaseView baseView;
    private MissionGuide missionGuide;
    private MissionBackground missionBackground;

    public void Initialize(GameManager manager)
    {
        gameManager = manager;
        if (isInitialized)
        {
            return;
        }
        isInitialized = true;

        uiFont = titleHeaderText != null ? titleHeaderText.font : solCounterText?.font;

        CacheSceneHelpers();
        ConfigureCompactSkeletalLayout();
        BuildDynamicHudWidgets();
        BuildFactPopupOverlay();
        BuildOutcomePhotoBlock();
        BuildSingularComparisonView();
        BuildPhotoZoomOverlay();

        missionAudio = MissionAudio.EnsureInstance(transform);
        RectTransform decisionRect = decisionPanel != null ? decisionPanel.GetComponent<RectTransform>() : null;
        baseView = MissionBaseView.Attach(this, decisionRect, uiFont, missionAudio);

        Transform canvasRoot = titlePanel != null && titlePanel.transform.parent != null
            ? titlePanel.transform.parent
            : transform;
        missionBackground = MissionBackground.Attach(canvasRoot);
        missionGuide = MissionGuide.Attach(this, canvasRoot, uiFont, missionAudio);

        AttachFactLinkHandler(titlePremiseText);
        AttachFactLinkHandler(dilemmaPromptText);
        AttachFactLinkHandler(outcomeBodyText);
        AttachUrlLinkHandler(nasaNoteText, () => currentOutcomeUrl);

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(HandleStartOrBriefingClicked);
            AddFunkyEffect(startButton.gameObject, Palette.MarsAccent, Palette.FunkyGold);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNextClicked);
            AddFunkyEffect(nextButton.gameObject, Palette.MarsAccent, Palette.FunkyCyan);
        }

        if (choiceButtonTemplate != null)
        {
            choiceButtonTemplate.gameObject.SetActive(false);
        }
    }

    private void CacheSceneHelpers()
    {
        if (titlePanel != null)
        {
            titlePanelBackdrop = titlePanel.GetComponent<Image>();
            Transform titleCardTf = titlePanel.transform.Find("TitleCard");
            if (titleCardTf != null)
            {
                titleCardRect = titleCardTf.GetComponent<RectTransform>();
                Transform badgeTf = titleCardTf.Find("MissionBadge");
                if (badgeTf != null)
                {
                    missionBadgeRect = badgeTf.GetComponent<RectTransform>();
                    Transform lblTf = badgeTf.Find("BadgeLabel");
                    if (lblTf != null)
                    {
                        badgeLabelText = lblTf.GetComponent<TMP_Text>();
                    }
                }
            }
        }

        if (startButton != null)
        {
            startButtonLabelText = startButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (decisionPanel != null)
        {
            decisionPanelBackdrop = decisionPanel.GetComponent<Image>();
            Transform topHudTf = decisionPanel.transform.Find("TopHUD");
            if (topHudTf != null)
            {
                topHudRect = topHudTf.GetComponent<RectTransform>();
                solBoxRect = topHudTf.Find("SolCounterBox")?.GetComponent<RectTransform>();
                powerBoxRect = topHudTf.Find("PowerBarGroup")?.GetComponent<RectTransform>();
                healthBoxRect = topHudTf.Find("IntegrityBarGroup")?.GetComponent<RectTransform>();
                dustBoxRect = topHudTf.Find("DustTauBox")?.GetComponent<RectTransform>();
                Transform scienceTf = topHudTf.Find("ScienceBarGroup")
                    ?? (dustBoxRect != null ? dustBoxRect.Find("ScienceBarGroup") : null);
                scienceBoxRect = scienceTf?.GetComponent<RectTransform>();
                if (scienceBoxRect != null && dustBoxRect != null && scienceBoxRect.parent != dustBoxRect)
                {
                    scienceBoxRect.SetParent(dustBoxRect, false);
                }
            }
            dilemmaCardRect = decisionPanel.transform.Find("DilemmaCard")?.GetComponent<RectTransform>();
        }

        if (outcomePanel != null)
        {
            outcomePanelBackdrop = outcomePanel.GetComponent<Image>();
            outcomeCardRect = outcomePanel.transform.Find("OutcomeCard")?.GetComponent<RectTransform>();
        }

        if (nasaScienceBox != null)
        {
            nasaScienceBoxRect = nasaScienceBox.GetComponent<RectTransform>();
        }

        Transform canvasParent = titlePanel != null ? titlePanel.transform.parent : null;
        if (canvasParent != null)
        {
            Transform consoleBg = canvasParent.Find("ConsoleBackground");
            if (consoleBg != null)
            {
                consoleBackgroundImage = consoleBg.GetComponent<Image>();
            }
        }

        powerTitleText = SetMeterLabel(powerBarFill, "ELECTRICITY");
        healthTitleText = SetMeterLabel(integrityBarFill, "CREW HEALTH");
        scienceTitleText = SetMeterLabel(scienceBarFill, "SUNLIGHT BLOCKED");
    }

    private static TMP_Text SetMeterLabel(Image barFill, string newLabel)
    {
        if (barFill == null || barFill.transform.parent == null || barFill.transform.parent.parent == null)
        {
            return null;
        }

        Transform titleTf = barFill.transform.parent.parent.Find("Title");
        if (titleTf != null)
        {
            TMP_Text label = titleTf.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = newLabel;
                return label;
            }
        }
        return null;
    }

    private void ConfigureCompactSkeletalLayout()
    {
        if (titleCardRect != null)
        {
            SetAnchors(titleCardRect, 0.17f, 0.16f, 0.83f, 0.84f);
            StyleHudCard(titleCardRect.gameObject, new Color(0.09f, 0.13f, 0.18f, 0.92f), Palette.FunkyBlue, 2.2f);
        }

        if (badgeLabelText != null)
        {
            badgeLabelText.enableAutoSizing = false;
            badgeLabelText.fontSize = 15f;
            badgeLabelText.fontStyle = FontStyles.Bold;
            badgeLabelText.textWrappingMode = TextWrappingModes.NoWrap;
            badgeLabelText.alignment = TextAlignmentOptions.Center;
        }

        if (titleHeaderText != null)
        {
            titleHeaderText.enableAutoSizing = false;
            titleHeaderText.fontSize = 30f;
            titleHeaderText.lineSpacing = -2f;
            titleHeaderText.textWrappingMode = TextWrappingModes.Normal;
            titleHeaderText.alignment = TextAlignmentOptions.Center;
        }

        if (titlePremiseText != null)
        {
            titlePremiseText.enableAutoSizing = false;
            titlePremiseText.fontSize = 20f;
            titlePremiseText.lineSpacing = -2f;
            titlePremiseText.paragraphSpacing = 5f;
            titlePremiseText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (startButtonLabelText != null)
        {
            startButtonLabelText.enableAutoSizing = false;
            startButtonLabelText.fontSize = 18f;
            startButtonLabelText.fontStyle = FontStyles.Bold;
            startButtonLabelText.textWrappingMode = TextWrappingModes.NoWrap;
            startButtonLabelText.alignment = TextAlignmentOptions.Center;
        }

        if (topHudRect != null)
        {
            SetAnchors(topHudRect, 0.05f, 0.80f, 0.95f, 0.97f);
            Image topHudBg = topHudRect.GetComponent<Image>();
            if (topHudBg != null)
            {
                topHudBg.color = new Color(0f, 0f, 0f, 0f);
            }
            Outline topOutline = topHudRect.GetComponent<Outline>();
            if (topOutline != null)
            {
                topOutline.enabled = false;
            }
        }

        if (solBoxRect != null)
        {
            StyleHudCard(solBoxRect.gameObject, new Color(0.09f, 0.14f, 0.21f, 0.92f), Palette.FunkyGold);
            if (solCounterText != null)
            {
                solCounterText.enableAutoSizing = false;
                solCounterText.fontSize = 18f;
                solCounterText.lineSpacing = -3f;
                solCounterText.textWrappingMode = TextWrappingModes.Normal;
                solCounterText.alignment = TextAlignmentOptions.Center;
            }
        }

        if (powerBoxRect != null)
        {
            StyleHudCard(powerBoxRect.gameObject, new Color(0.09f, 0.14f, 0.21f, 0.92f), Palette.FunkyGold);
            ConfigureBarCardTopHalf(powerBoxRect);
        }

        if (healthBoxRect != null)
        {
            StyleHudCard(healthBoxRect.gameObject, new Color(0.09f, 0.14f, 0.21f, 0.92f), Palette.FunkyMint);
            ConfigureBarCardTopHalf(healthBoxRect);
        }

        if (dustBoxRect != null)
        {
            StyleHudCard(dustBoxRect.gameObject, new Color(0.09f, 0.14f, 0.21f, 0.92f), Palette.FunkyCyan);
            if (dustTauText != null)
            {
                dustTauText.enableAutoSizing = false;
                dustTauText.fontSize = 15.5f;
                dustTauText.textWrappingMode = TextWrappingModes.Normal;
                dustTauText.alignment = TextAlignmentOptions.Center;
            }
        }

        if (scienceBoxRect != null)
        {
            Image sciBg = scienceBoxRect.GetComponent<Image>();
            if (sciBg != null)
            {
                sciBg.color = new Color(0f, 0f, 0f, 0f);
            }
            ConfigureCompactScienceBar(scienceBoxRect);
        }

        if (dilemmaCardRect != null)
        {
            SetAnchors(dilemmaCardRect, 0.09f, 0.26f, 0.91f, 0.48f);
            StyleHudCard(dilemmaCardRect.gameObject, new Color(0.09f, 0.13f, 0.19f, 0.90f), Palette.FunkyBlue, 2.2f);
        }

        if (nodeTitleText != null)
        {
            nodeTitleText.enableAutoSizing = false;
            nodeTitleText.fontSize = 23f;
            nodeTitleText.lineSpacing = -2f;
            nodeTitleText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (dilemmaPromptText != null)
        {
            dilemmaPromptText.enableAutoSizing = false;
            dilemmaPromptText.fontSize = 19.5f;
            dilemmaPromptText.lineSpacing = -2f;
            dilemmaPromptText.paragraphSpacing = 4f;
            dilemmaPromptText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (choicesContainer != null)
        {
            SetAnchors(choicesContainer, 0.04f, 0.035f, 0.96f, 0.24f);

            VerticalLayoutGroup oldVlg = choicesContainer.GetComponent<VerticalLayoutGroup>();
            if (oldVlg != null)
            {
                DestroyImmediate(oldVlg);
            }

            HorizontalLayoutGroup hlg = choicesContainer.GetComponent<HorizontalLayoutGroup>();
            if (hlg == null)
            {
                hlg = choicesContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
            }
            hlg.padding = new RectOffset(4, 4, 2, 2);
            hlg.spacing = 16f;
            hlg.childAlignment = TextAnchor.UpperCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;
        }

        if (outcomeCardRect != null)
        {
            SetAnchors(outcomeCardRect, 0.11f, 0.11f, 0.89f, 0.89f);
            StyleHudCard(outcomeCardRect.gameObject, new Color(0.09f, 0.13f, 0.19f, 0.92f), Palette.FunkyBlue, 2.2f);
        }

        if (outcomeHeaderText != null)
        {
            outcomeHeaderText.enableAutoSizing = false;
            outcomeHeaderText.fontSize = 26f;
            outcomeHeaderText.lineSpacing = -2f;
            outcomeHeaderText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (outcomeTelemetryText != null)
        {
            outcomeTelemetryText.enableAutoSizing = false;
            outcomeTelemetryText.fontSize = 16f;
            outcomeTelemetryText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (outcomeBodyText != null)
        {
            outcomeBodyText.enableAutoSizing = false;
            outcomeBodyText.fontSize = 18.5f;
            outcomeBodyText.lineSpacing = -2f;
            outcomeBodyText.textWrappingMode = TextWrappingModes.Normal;
            outcomeBodyText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (nasaScienceTitleText != null)
        {
            nasaScienceTitleText.enableAutoSizing = false;
            nasaScienceTitleText.fontSize = 14f;
            nasaScienceTitleText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (nasaNoteText != null)
        {
            nasaNoteText.enableAutoSizing = false;
            nasaNoteText.fontSize = 15.5f;
            nasaNoteText.lineSpacing = -2f;
            nasaNoteText.textWrappingMode = TextWrappingModes.Normal;
            nasaNoteText.alignment = TextAlignmentOptions.TopLeft;
        }

        if (nextButtonLabel != null)
        {
            nextButtonLabel.enableAutoSizing = false;
            nextButtonLabel.fontSize = 17.5f;
            nextButtonLabel.fontStyle = FontStyles.Bold;
            nextButtonLabel.textWrappingMode = TextWrappingModes.NoWrap;
            nextButtonLabel.alignment = TextAlignmentOptions.Center;
        }
    }

    private static void ConfigureBarCardTopHalf(Transform barGroup)
    {
        Transform titleTf = barGroup.Find("Title");
        if (titleTf != null)
        {
            SetAnchors(titleTf.GetComponent<RectTransform>(), 0.05f, 0.62f, 0.68f, 0.94f);
            TMP_Text t = titleTf.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.enableAutoSizing = false;
                t.fontSize = 15f;
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        Transform valueTf = barGroup.Find("ValueText");
        if (valueTf != null)
        {
            SetAnchors(valueTf.GetComponent<RectTransform>(), 0.68f, 0.62f, 0.95f, 0.94f);
            TMP_Text v = valueTf.GetComponent<TMP_Text>();
            if (v != null)
            {
                v.enableAutoSizing = false;
                v.fontSize = 16.5f;
                v.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        Transform trackTf = barGroup.Find("BarTrack");
        if (trackTf != null)
        {
            SetAnchors(trackTf.GetComponent<RectTransform>(), 0.05f, 0.44f, 0.95f, 0.61f);
        }
    }

    private static void ConfigureCompactScienceBar(Transform scienceBox)
    {
        Transform titleTf = scienceBox.Find("Title");
        if (titleTf != null)
        {
            SetAnchors(titleTf.GetComponent<RectTransform>(), 0.02f, 0.48f, 0.72f, 0.96f);
            TMP_Text t = titleTf.GetComponent<TMP_Text>();
            if (t != null)
            {
                t.enableAutoSizing = false;
                t.fontSize = 11.5f;
                t.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        Transform valueTf = scienceBox.Find("ValueText");
        if (valueTf != null)
        {
            SetAnchors(valueTf.GetComponent<RectTransform>(), 0.72f, 0.48f, 0.98f, 0.96f);
            TMP_Text v = valueTf.GetComponent<TMP_Text>();
            if (v != null)
            {
                v.enableAutoSizing = false;
                v.fontSize = 12f;
                v.textWrappingMode = TextWrappingModes.NoWrap;
            }
        }

        Transform trackTf = scienceBox.Find("BarTrack");
        if (trackTf != null)
        {
            SetAnchors(trackTf.GetComponent<RectTransform>(), 0.02f, 0.08f, 0.98f, 0.44f);
        }
    }

    private void BuildDynamicHudWidgets()
    {
        if (topHudRect == null)
        {
            return;
        }

        if (solBoxRect != null)
        {
            GameObject pipsRow = new GameObject("SolPipsRow", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            pipsRow.transform.SetParent(solBoxRect, false);
            SetAnchors(pipsRow.GetComponent<RectTransform>(), 0.06f, 0.08f, 0.94f, 0.23f);
            HorizontalLayoutGroup hlg = pipsRow.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            solPips.Clear();
            for (int i = 0; i < GameState.MaxSols; i++)
            {
                GameObject pipObj = new GameObject($"SolPip_{i + 1}", typeof(RectTransform), typeof(Image));
                pipObj.transform.SetParent(pipsRow.transform, false);
                Image pipImg = pipObj.GetComponent<Image>();
                pipImg.color = Palette.PanelBorder;
                pipImg.raycastTarget = false;
                solPips.Add(pipImg);
            }
        }

        if (powerBoxRect != null)
        {
            powerPillRowRect = CreateCompactPillRow(powerBoxRect, "PowerIncitedPills");
            fuelPill = CreateSubsystemPill(powerPillRowRect, "FuelPill");
            tempPill = CreateSubsystemPill(powerPillRowRect, "TempPill");
            reactorPill = CreateSubsystemPill(powerPillRowRect, "ReactorPill");
        }

        if (healthBoxRect != null)
        {
            healthPillRowRect = CreateCompactPillRow(healthBoxRect, "HealthIncitedPills");
            oxygenPill = CreateSubsystemPill(healthPillRowRect, "OxygenPill");
            foodPill = CreateSubsystemPill(healthPillRowRect, "FoodPill");
            airPill = CreateSubsystemPill(healthPillRowRect, "AirPill");
        }

        if (dustBoxRect != null)
        {
            GameObject archiveBtnObj = new GameObject("StormArchiveButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
            archiveBtnObj.transform.SetParent(dustBoxRect, false);
            SetAnchors(archiveBtnObj.GetComponent<RectTransform>(), 0.16f, 0.07f, 0.84f, 0.31f);

            Image btnBg = archiveBtnObj.GetComponent<Image>();
            btnBg.color = Palette.NasaBoxSurface;
            Outline btnOutline = archiveBtnObj.GetComponent<Outline>();
            btnOutline.effectColor = Palette.FunkyCyan;
            btnOutline.effectDistance = new Vector2(1.4f, -1.4f);

            stormArchiveButton = archiveBtnObj.GetComponent<Button>();
            stormArchiveButton.targetGraphic = btnBg;
            stormArchiveButton.onClick.AddListener(() => ShowFactPopupByKey("2018_noon_blackout"));
            AddFunkyEffect(archiveBtnObj, Palette.NasaBoxSurface, Palette.FunkyCyan);

            GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblObj.transform.SetParent(archiveBtnObj.transform, false);
            SetAnchors(lblObj.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
            TextMeshProUGUI lbl = lblObj.GetComponent<TextMeshProUGUI>();
            lbl.font = uiFont;
            lbl.text = "2018 PHOTOS";
            lbl.enableAutoSizing = false;
            lbl.fontSize = 12f;
            lbl.fontStyle = FontStyles.Bold;
            lbl.textWrappingMode = TextWrappingModes.NoWrap;
            lbl.color = Palette.FunkyCyan;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.raycastTarget = false;
            stormArchiveLabel = lbl;
        }
    }

    private static RectTransform CreateCompactPillRow(Transform parent, string name)
    {
        GameObject rowObj = new GameObject(name, typeof(RectTransform));
        rowObj.transform.SetParent(parent, false);
        RectTransform rt = rowObj.GetComponent<RectTransform>();
        SetAnchors(rt, 0.04f, 0.08f, 0.96f, 0.38f);
        return rt;
    }

    private SubsystemPillWidget CreateSubsystemPill(Transform parent, string name)
    {
        GameObject pillObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Outline));
        pillObj.transform.SetParent(parent, false);
        RectTransform pillRt = pillObj.GetComponent<RectTransform>();
        SetAnchors(pillRt, 0f, 0f, 0.3f, 1f);

        Image bg = pillObj.GetComponent<Image>();
        bg.color = Palette.PanelElevated;
        bg.raycastTarget = true;

        Outline outline = pillObj.GetComponent<Outline>();
        outline.effectColor = Palette.FunkyCyan;
        outline.effectDistance = new Vector2(1.4f, -1.4f);

        FunkyCardEffect effect = AddFunkyEffect(pillObj, Palette.PanelElevated, Palette.FunkyCyan, 1.05f);

        GameObject txtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(pillObj.transform, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        SetAnchors(txtRt, 0.04f, 0.06f, 0.96f, 0.94f);

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.font = uiFont;
        tmp.enableAutoSizing = false;
        tmp.fontSize = 12f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.overflowMode = TextOverflowModes.Overflow;
        tmp.raycastTarget = false;

        pillObj.SetActive(false);

        return new SubsystemPillWidget
        {
            root = pillObj,
            rootRect = pillRt,
            background = bg,
            outline = outline,
            label = tmp,
            effect = effect,
            fittedWidth = 80f
        };
    }

    /// <summary>
    /// Creates a clickable NASA photograph frame with an aspect-fitted RawImage, a compact zoom hint badge,
    /// and a content-fitted explanation caption panel below it.
    /// </summary>
    private PhotoCaptionWidget CreatePhotoWithCaptionBlock(
        Transform parent,
        string prefix,
        Vector2 minAnchor,
        Vector2 maxAnchor,
        float captionSplit = 0.28f)
    {
        GameObject rootObj = new GameObject($"{prefix}PhotoBlock", typeof(RectTransform));
        rootObj.transform.SetParent(parent, false);
        RectTransform rootRt = rootObj.GetComponent<RectTransform>();
        SetAnchors(rootRt, minAnchor.x, minAnchor.y, maxAnchor.x, maxAnchor.y);

        // Top clickable image frame
        GameObject frameObj = new GameObject("ImageFrame", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        frameObj.transform.SetParent(rootRt, false);
        RectTransform frameRt = frameObj.GetComponent<RectTransform>();
        SetAnchors(frameRt, 0f, captionSplit + 0.025f, 1f, 1f);
        StyleHudCard(frameObj, Palette.BarTrack, Palette.FunkyBlue, 1.8f);
        Image frameBg = frameObj.GetComponent<Image>();
        frameBg.raycastTarget = true;

        Button zoomBtn = frameObj.GetComponent<Button>();
        zoomBtn.targetGraphic = frameBg;
        AddFunkyEffect(frameObj, Palette.BarTrack, Palette.FunkyCyan, 1.035f);

        GameObject photoObj = new GameObject("NasaPhoto", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        photoObj.transform.SetParent(frameObj.transform, false);
        RectTransform photoRt = photoObj.GetComponent<RectTransform>();
        photoRt.anchorMin = Vector2.zero;
        photoRt.anchorMax = Vector2.one;
        photoRt.offsetMin = new Vector2(6f, 6f);
        photoRt.offsetMax = new Vector2(-6f, -6f);

        RawImage rawImg = photoObj.GetComponent<RawImage>();
        rawImg.color = Color.white;
        rawImg.raycastTarget = false;
        AspectRatioFitter fitter = photoObj.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;
        fitter.aspectRatio = 1.33f;

        // Compact text-fitted "CLICK TO ENLARGE" corner badge
        GameObject hintObj = new GameObject("ZoomHintBadge", typeof(RectTransform), typeof(Image), typeof(Outline));
        hintObj.transform.SetParent(frameObj.transform, false);
        RectTransform hintRt = hintObj.GetComponent<RectTransform>();
        hintRt.anchorMin = new Vector2(1f, 1f);
        hintRt.anchorMax = new Vector2(1f, 1f);
        hintRt.pivot = new Vector2(1f, 1f);
        hintRt.anchoredPosition = new Vector2(-6f, -6f);
        StyleHudCard(hintObj, new Color(0.04f, 0.09f, 0.14f, 0.90f), Palette.FunkyCyan, 1.0f);
        hintObj.GetComponent<Image>().raycastTarget = false;

        GameObject hintTxtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        hintTxtObj.transform.SetParent(hintObj.transform, false);
        RectTransform hintTxtRt = hintTxtObj.GetComponent<RectTransform>();
        hintTxtRt.anchorMin = Vector2.zero;
        hintTxtRt.anchorMax = Vector2.one;
        hintTxtRt.offsetMin = new Vector2(7f, 3f);
        hintTxtRt.offsetMax = new Vector2(-7f, -3f);
        TextMeshProUGUI hintTxt = hintTxtObj.GetComponent<TextMeshProUGUI>();
        hintTxt.font = uiFont;
        hintTxt.text = "CLICK TO ENLARGE";
        hintTxt.enableAutoSizing = false;
        hintTxt.fontSize = 11f;
        hintTxt.fontStyle = FontStyles.Bold;
        hintTxt.textWrappingMode = TextWrappingModes.NoWrap;
        hintTxt.color = Palette.FunkyCyan;
        hintTxt.alignment = TextAlignmentOptions.Center;
        hintTxt.raycastTarget = false;
        Vector2 hintPref = hintTxt.GetPreferredValues(hintTxt.text);
        hintRt.sizeDelta = new Vector2(Mathf.Ceil(hintPref.x + 14f), Mathf.Ceil(hintPref.y + 7f));

        // Bottom Image Caption Box explaining what the image shows
        GameObject capObj = new GameObject("ImageCaptionBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        capObj.transform.SetParent(rootRt, false);
        RectTransform capRt = capObj.GetComponent<RectTransform>();
        SetAnchors(capRt, 0f, 0f, 1f, captionSplit);
        StyleHudCard(capObj, Palette.NasaBoxSurface, Palette.FunkyCyan, 1.2f);
        capObj.GetComponent<Image>().raycastTarget = false;

        GameObject capTxtObj = new GameObject("CaptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        capTxtObj.transform.SetParent(capObj.transform, false);
        RectTransform capTxtRt = capTxtObj.GetComponent<RectTransform>();
        capTxtRt.anchorMin = Vector2.zero;
        capTxtRt.anchorMax = Vector2.one;
        capTxtRt.offsetMin = new Vector2(10f, 7f);
        capTxtRt.offsetMax = new Vector2(-10f, -7f);
        TextMeshProUGUI capTxt = capTxtObj.GetComponent<TextMeshProUGUI>();
        capTxt.font = uiFont;
        capTxt.enableAutoSizing = false;
        capTxt.fontSize = 13.5f;
        capTxt.lineSpacing = -2f;
        capTxt.color = Palette.TextPrimary;
        capTxt.textWrappingMode = TextWrappingModes.Normal;
        capTxt.alignment = TextAlignmentOptions.TopLeft;
        capTxt.raycastTarget = false;

        PhotoCaptionWidget widget = new PhotoCaptionWidget
        {
            root = rootObj,
            rootRect = rootRt,
            frameRect = frameRt,
            captionBoxRect = capRt,
            zoomButton = zoomBtn,
            rawImage = rawImg,
            fitter = fitter,
            captionText = capTxt
        };
        zoomBtn.onClick.AddListener(() => ShowPhotoZoom(widget));
        return widget;
    }

    private void BuildPhotoZoomOverlay()
    {
        Transform canvasRoot = titlePanel != null && titlePanel.transform.parent != null
            ? titlePanel.transform.parent
            : decisionPanel != null ? decisionPanel.transform : transform;

        photoZoomOverlay = new GameObject("NasaPhotoZoomOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        photoZoomOverlay.transform.SetParent(canvasRoot, false);
        SetAnchors(photoZoomOverlay.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

        Image overlayDim = photoZoomOverlay.GetComponent<Image>();
        overlayDim.color = new Color(0.01f, 0.03f, 0.06f, 0.88f);

        Button dismissBgBtn = photoZoomOverlay.GetComponent<Button>();
        dismissBgBtn.transition = Selectable.Transition.None;
        dismissBgBtn.onClick.AddListener(HidePhotoZoom);

        GameObject cardObj = new GameObject("PhotoZoomCard", typeof(RectTransform), typeof(Image), typeof(Outline), typeof(Button));
        cardObj.transform.SetParent(photoZoomOverlay.transform, false);
        photoZoomCardRect = cardObj.GetComponent<RectTransform>();
        photoZoomCardRect.anchorMin = new Vector2(0.5f, 0.5f);
        photoZoomCardRect.anchorMax = new Vector2(0.5f, 0.5f);
        photoZoomCardRect.pivot = new Vector2(0.5f, 0.5f);
        photoZoomCardRect.anchoredPosition = Vector2.zero;
        StyleHudCard(cardObj, new Color(0.07f, 0.11f, 0.17f, 0.98f), Palette.FunkyCyan, 2.5f);
        Button cardClickBtn = cardObj.GetComponent<Button>();
        cardClickBtn.transition = Selectable.Transition.None;
        cardClickBtn.onClick.AddListener(HidePhotoZoom);

        // Large image frame
        GameObject frameObj = new GameObject("ZoomImageFrame", typeof(RectTransform), typeof(Image), typeof(Outline));
        frameObj.transform.SetParent(photoZoomCardRect, false);
        photoZoomFrameRect = frameObj.GetComponent<RectTransform>();
        photoZoomFrameRect.anchorMin = new Vector2(0.5f, 1f);
        photoZoomFrameRect.anchorMax = new Vector2(0.5f, 1f);
        photoZoomFrameRect.pivot = new Vector2(0.5f, 1f);
        StyleHudCard(frameObj, Palette.BarTrack, Palette.FunkyBlue, 1.8f);
        frameObj.GetComponent<Image>().raycastTarget = false;

        GameObject photoObj = new GameObject("ZoomNasaPhoto", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        photoObj.transform.SetParent(photoZoomFrameRect, false);
        RectTransform photoRt = photoObj.GetComponent<RectTransform>();
        photoRt.anchorMin = Vector2.zero;
        photoRt.anchorMax = Vector2.one;
        photoRt.offsetMin = new Vector2(8f, 8f);
        photoRt.offsetMax = new Vector2(-8f, -8f);

        photoZoomRawImage = photoObj.GetComponent<RawImage>();
        photoZoomRawImage.color = Color.white;
        photoZoomRawImage.raycastTarget = false;
        photoZoomFitter = photoObj.GetComponent<AspectRatioFitter>();
        photoZoomFitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent;

        // Top-right close hint badge sized to its text
        GameObject badgeObj = new GameObject("ZoomCloseHint", typeof(RectTransform), typeof(Image), typeof(Outline));
        badgeObj.transform.SetParent(photoZoomFrameRect, false);
        photoZoomBadgeRect = badgeObj.GetComponent<RectTransform>();
        photoZoomBadgeRect.anchorMin = new Vector2(1f, 1f);
        photoZoomBadgeRect.anchorMax = new Vector2(1f, 1f);
        photoZoomBadgeRect.pivot = new Vector2(1f, 1f);
        photoZoomBadgeRect.anchoredPosition = new Vector2(-10f, -10f);
        StyleHudCard(badgeObj, new Color(0.05f, 0.09f, 0.14f, 0.92f), Palette.FunkyGold, 1.2f);
        badgeObj.GetComponent<Image>().raycastTarget = false;

        GameObject badgeTxtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeTxtObj.transform.SetParent(photoZoomBadgeRect, false);
        RectTransform badgeTxtRt = badgeTxtObj.GetComponent<RectTransform>();
        badgeTxtRt.anchorMin = Vector2.zero;
        badgeTxtRt.anchorMax = Vector2.one;
        badgeTxtRt.offsetMin = new Vector2(10f, 4f);
        badgeTxtRt.offsetMax = new Vector2(-10f, -4f);
        TextMeshProUGUI badgeTxt = badgeTxtObj.GetComponent<TextMeshProUGUI>();
        badgeTxt.font = uiFont;
        badgeTxt.text = "CLICK IMAGE TO SHRINK";
        badgeTxt.enableAutoSizing = false;
        badgeTxt.fontSize = 13f;
        badgeTxt.fontStyle = FontStyles.Bold;
        badgeTxt.textWrappingMode = TextWrappingModes.NoWrap;
        badgeTxt.color = Palette.FunkyGold;
        badgeTxt.alignment = TextAlignmentOptions.Center;
        badgeTxt.raycastTarget = false;
        Vector2 bPref = badgeTxt.GetPreferredValues(badgeTxt.text);
        photoZoomBadgeRect.sizeDelta = new Vector2(Mathf.Ceil(bPref.x + 20f), Mathf.Ceil(bPref.y + 9f));

        // Bottom content-fitted caption box
        GameObject capBoxObj = new GameObject("ZoomCaptionBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        capBoxObj.transform.SetParent(photoZoomCardRect, false);
        photoZoomCaptionBoxRect = capBoxObj.GetComponent<RectTransform>();
        photoZoomCaptionBoxRect.anchorMin = new Vector2(0.5f, 0f);
        photoZoomCaptionBoxRect.anchorMax = new Vector2(0.5f, 0f);
        photoZoomCaptionBoxRect.pivot = new Vector2(0.5f, 0f);
        StyleHudCard(capBoxObj, Palette.NasaBoxSurface, Palette.FunkyCyan, 1.4f);
        capBoxObj.GetComponent<Image>().raycastTarget = false;

        GameObject capTxtObj = new GameObject("CaptionText", typeof(RectTransform), typeof(TextMeshProUGUI));
        capTxtObj.transform.SetParent(photoZoomCaptionBoxRect, false);
        RectTransform capTxtRt = capTxtObj.GetComponent<RectTransform>();
        capTxtRt.anchorMin = Vector2.zero;
        capTxtRt.anchorMax = Vector2.one;
        capTxtRt.offsetMin = new Vector2(14f, 10f);
        capTxtRt.offsetMax = new Vector2(-14f, -10f);
        photoZoomCaptionText = capTxtObj.GetComponent<TextMeshProUGUI>();
        photoZoomCaptionText.font = uiFont;
        photoZoomCaptionText.enableAutoSizing = false;
        photoZoomCaptionText.fontSize = 17f;
        photoZoomCaptionText.lineSpacing = -2f;
        photoZoomCaptionText.color = Palette.TextPrimary;
        photoZoomCaptionText.textWrappingMode = TextWrappingModes.Normal;
        photoZoomCaptionText.alignment = TextAlignmentOptions.TopLeft;
        photoZoomCaptionText.raycastTarget = false;

        photoZoomOverlay.SetActive(false);
    }

    public void ShowPhotoZoom(PhotoCaptionWidget sourceWidget)
    {
        if (photoZoomOverlay == null || sourceWidget == null || sourceWidget.rawImage == null || sourceWidget.rawImage.texture == null)
        {
            return;
        }

        Texture tex = sourceWidget.rawImage.texture;
        float aspect = (float)tex.width / Mathf.Max(1, tex.height);

        const float maxImgW = 1360f;
        const float maxImgH = 720f;
        float frameW = maxImgW;
        float frameH = frameW / aspect;
        if (frameH > maxImgH)
        {
            frameH = maxImgH;
            frameW = frameH * aspect;
        }
        frameW = Mathf.Clamp(frameW, 340f, maxImgW);
        frameH = Mathf.Clamp(frameH, 260f, maxImgH);

        photoZoomOverlay.transform.SetAsLastSibling();
        photoZoomOverlay.SetActive(true);
        missionGuide?.SetOverlayActive(true);

        photoZoomRawImage.texture = tex;
        photoZoomFitter.aspectRatio = aspect;

        string capRich = !string.IsNullOrEmpty(sourceWidget.rawCaption)
            ? $"<color=#FFD166><b>ABOUT THIS IMAGE:</b></color> {sourceWidget.rawCaption}"
            : sourceWidget.captionText.text;

        float capMaxW = Mathf.Clamp(Mathf.Max(frameW, 640f), 520f, 1340f);
        photoZoomCaptionText.enableAutoSizing = false;
        photoZoomCaptionText.fontSize = 17f;
        Vector2 capPref = MeasureText(photoZoomCaptionText, capRich, capMaxW - 28f);
        float capBoxW = Mathf.Clamp(Mathf.Ceil(capPref.x + 32f), 260f, capMaxW);
        float capBoxH = Mathf.Clamp(Mathf.Ceil(capPref.y + 22f), 46f, 240f);

        const float pad = 16f;
        const float gap = 12f;
        float cardW = Mathf.Ceil(Mathf.Max(frameW, capBoxW) + pad * 2f);
        float cardH = Mathf.Ceil(pad + frameH + gap + capBoxH + pad);

        photoZoomCardRect.sizeDelta = new Vector2(cardW, cardH);

        photoZoomFrameRect.anchoredPosition = new Vector2(0f, -pad);
        photoZoomFrameRect.sizeDelta = new Vector2(frameW, frameH);

        photoZoomCaptionBoxRect.anchoredPosition = new Vector2(0f, pad);
        photoZoomCaptionBoxRect.sizeDelta = new Vector2(capBoxW, capBoxH);

        if (photoZoomRoutine != null)
        {
            StopCoroutine(photoZoomRoutine);
        }
        photoZoomRoutine = StartCoroutine(AnimatePhotoZoomCoroutine());
    }

    private IEnumerator AnimatePhotoZoomCoroutine()
    {
        photoZoomCardRect.localScale = new Vector3(0.55f, 0.55f, 1f);
        float elapsed = 0f;
        const float duration = 0.15f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
            float s = Mathf.Lerp(0.55f, 1f, t);
            photoZoomCardRect.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        photoZoomCardRect.localScale = Vector3.one;
        photoZoomRoutine = null;
    }

    public void HidePhotoZoom()
    {
        if (photoZoomRoutine != null)
        {
            StopCoroutine(photoZoomRoutine);
            photoZoomRoutine = null;
        }
        if (photoZoomOverlay != null)
        {
            photoZoomOverlay.SetActive(false);
            missionGuide?.SetOverlayActive(false);
        }
    }

    private void BuildFactPopupOverlay()
    {
        Transform canvasRoot = titlePanel != null && titlePanel.transform.parent != null
            ? titlePanel.transform.parent
            : decisionPanel != null ? decisionPanel.transform : transform;

        factPopupOverlay = new GameObject("NasaFactPopupOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        factPopupOverlay.transform.SetParent(canvasRoot, false);
        SetAnchors(factPopupOverlay.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

        Image overlayDim = factPopupOverlay.GetComponent<Image>();
        overlayDim.color = new Color(0.02f, 0.04f, 0.08f, 0.78f);

        Button dismissBgBtn = factPopupOverlay.GetComponent<Button>();
        dismissBgBtn.transition = Selectable.Transition.None;
        dismissBgBtn.onClick.AddListener(HideFactPopup);

        GameObject cardObj = new GameObject("NasaFactCard", typeof(RectTransform), typeof(Image), typeof(Outline));
        cardObj.transform.SetParent(factPopupOverlay.transform, false);
        factCardRect = cardObj.GetComponent<RectTransform>();
        SetAnchors(factCardRect, 0.13f, 0.13f, 0.87f, 0.87f);
        StyleHudCard(cardObj, new Color(0.09f, 0.13f, 0.19f, 0.96f), Palette.FunkyCyan, 2.4f);
        cardObj.GetComponent<Image>().raycastTarget = true;

        GameObject tagObj = new GameObject("ArchiveBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagObj.transform.SetParent(factCardRect, false);
        SetAnchors(tagObj.GetComponent<RectTransform>(), 0.04f, 0.89f, 0.96f, 0.96f);
        factTagText = tagObj.GetComponent<TextMeshProUGUI>();
        factTagText.font = uiFont;
        factTagText.text = "WORD GUIDE  &  2018 NASA PHOTO ARCHIVE";
        factTagText.enableAutoSizing = false;
        factTagText.fontSize = 15f;
        factTagText.fontStyle = FontStyles.Bold;
        factTagText.color = Palette.FunkyGold;
        factTagText.alignment = TextAlignmentOptions.MidlineLeft;
        factTagText.raycastTarget = false;

        GameObject termObj = new GameObject("FactHeadline", typeof(RectTransform), typeof(TextMeshProUGUI));
        termObj.transform.SetParent(factCardRect, false);
        SetAnchors(termObj.GetComponent<RectTransform>(), 0.04f, 0.77f, 0.96f, 0.88f);
        factPopupTermText = termObj.GetComponent<TextMeshProUGUI>();
        factPopupTermText.font = uiFont;
        factPopupTermText.enableAutoSizing = false;
        factPopupTermText.fontSize = 24f;
        factPopupTermText.fontStyle = FontStyles.Bold;
        factPopupTermText.color = Palette.FunkyCyan;
        factPopupTermText.textWrappingMode = TextWrappingModes.Normal;
        factPopupTermText.alignment = TextAlignmentOptions.MidlineLeft;
        factPopupTermText.raycastTarget = false;

        // Left: Clickable Photo + fitted Image Caption below it
        factPhotoWidget = CreatePhotoWithCaptionBlock(
            factCardRect,
            "Fact",
            new Vector2(0.04f, 0.15f),
            new Vector2(0.42f, 0.75f),
            0.30f);

        // Right: Styled background text box wrapping FactBody
        GameObject bodyBoxObj = new GameObject("FactBodyBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        bodyBoxObj.transform.SetParent(factCardRect, false);
        factBodyBoxRect = bodyBoxObj.GetComponent<RectTransform>();
        SetAnchors(factBodyBoxRect, 0.45f, 0.15f, 0.96f, 0.75f);
        StyleHudCard(bodyBoxObj, Palette.PanelElevated, Palette.FunkyBlue, 1.4f);
        bodyBoxObj.GetComponent<Image>().raycastTarget = false;

        GameObject defObj = new GameObject("FactBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        defObj.transform.SetParent(factBodyBoxRect, false);
        RectTransform defRt = defObj.GetComponent<RectTransform>();
        defRt.anchorMin = Vector2.zero;
        defRt.anchorMax = Vector2.one;
        defRt.offsetMin = new Vector2(14f, 10f);
        defRt.offsetMax = new Vector2(-14f, -10f);

        factPopupBodyText = defObj.GetComponent<TextMeshProUGUI>();
        factPopupBodyText.font = uiFont;
        factPopupBodyText.enableAutoSizing = false;
        factPopupBodyText.fontSize = 18f;
        factPopupBodyText.lineSpacing = -2f;
        factPopupBodyText.paragraphSpacing = 4f;
        factPopupBodyText.color = Palette.TextPrimary;
        factPopupBodyText.textWrappingMode = TextWrappingModes.Normal;
        factPopupBodyText.alignment = TextAlignmentOptions.TopLeft;
        AttachUrlLinkHandler(factPopupBodyText, () => currentFactUrl);

        // Compact Back button sized tightly to its text
        GameObject closeBtnObj = new GameObject("CloseFactButton", typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        closeBtnObj.transform.SetParent(factCardRect, false);
        SetAnchors(closeBtnObj.GetComponent<RectTransform>(), 0.39f, 0.04f, 0.61f, 0.12f);
        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = Palette.MarsAccent;
        closeBtnObj.GetComponent<Outline>().effectColor = Palette.FunkyGold;
        factCloseButton = closeBtnObj.GetComponent<Button>();
        factCloseButton.targetGraphic = closeImg;
        factCloseButton.onClick.AddListener(HideFactPopup);
        AddFunkyEffect(closeBtnObj, Palette.MarsAccent, Palette.FunkyGold);

        GameObject closeTxtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeBtnObj.transform, false);
        SetAnchors(closeTxtObj.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);
        TextMeshProUGUI closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTxt.font = uiFont;
        closeTxt.text = "CLOSE";
        closeTxt.enableAutoSizing = false;
        closeTxt.fontSize = 17f;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.textWrappingMode = TextWrappingModes.NoWrap;
        closeTxt.color = Palette.TextPrimary;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.raycastTarget = false;
        factCloseButtonLabel = closeTxt;

        factPopupOverlay.SetActive(false);
    }

    private void BuildOutcomePhotoBlock()
    {
        if (outcomePanel == null)
        {
            return;
        }

        Transform parentTarget = outcomeCardRect != null ? outcomeCardRect : outcomePanel.transform;

        // Visible container box behind outcomeBodyText that resizes cleanly around Outcome text
        if (outcomeBodyText != null)
        {
            GameObject bodyBoxObj = new GameObject("OutcomeBodyBox", typeof(RectTransform), typeof(Image), typeof(Outline));
            bodyBoxObj.transform.SetParent(parentTarget, false);
            bodyBoxObj.transform.SetSiblingIndex(outcomeBodyText.transform.GetSiblingIndex());
            outcomeBodyBoxRect = bodyBoxObj.GetComponent<RectTransform>();
            StyleHudCard(bodyBoxObj, Palette.PanelElevated, Palette.FunkyBlue, 1.4f);
            bodyBoxObj.GetComponent<Image>().raycastTarget = false;
        }

        outcomePhotoWidget = CreatePhotoWithCaptionBlock(
            parentTarget,
            "Outcome",
            new Vector2(0.04f, 0.16f),
            new Vector2(0.38f, 0.76f),
            0.30f);
        outcomePhotoWidget.root.SetActive(false);
    }

    private void BuildSingularComparisonView()
    {
        if (outcomePanel == null)
        {
            return;
        }

        Transform parentTarget = outcomeCardRect != null ? outcomeCardRect : outcomePanel.transform;

        GameObject containerObj = new GameObject("SingularComparisonRoot", typeof(RectTransform), typeof(Image), typeof(Outline));
        containerObj.transform.SetParent(parentTarget, false);
        comparisonContainer = containerObj.GetComponent<RectTransform>();
        SetAnchors(comparisonContainer, 0.03f, 0.16f, 0.97f, 0.76f);
        StyleHudCard(containerObj, Palette.PanelElevated, Palette.FunkyCyan, 2.0f);

        GameObject badgeObj = new GameObject("ResultIndexBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeObj.transform.SetParent(comparisonContainer, false);
        SetAnchors(badgeObj.GetComponent<RectTransform>(), 0.03f, 0.87f, 0.60f, 0.96f);
        compBadgeText = badgeObj.GetComponent<TextMeshProUGUI>();
        compBadgeText.font = uiFont;
        compBadgeText.enableAutoSizing = false;
        compBadgeText.fontSize = 17.5f;
        compBadgeText.fontStyle = FontStyles.Bold;
        compBadgeText.color = Palette.FunkyGold;
        compBadgeText.alignment = TextAlignmentOptions.MidlineLeft;
        compBadgeText.raycastTarget = false;

        GameObject subBadgeObj = new GameObject("SubsystemImpactBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        subBadgeObj.transform.SetParent(comparisonContainer, false);
        SetAnchors(subBadgeObj.GetComponent<RectTransform>(), 0.60f, 0.87f, 0.97f, 0.96f);
        compSubsystemBadgeText = subBadgeObj.GetComponent<TextMeshProUGUI>();
        compSubsystemBadgeText.font = uiFont;
        compSubsystemBadgeText.enableAutoSizing = false;
        compSubsystemBadgeText.fontSize = 15.5f;
        compSubsystemBadgeText.fontStyle = FontStyles.Bold;
        compSubsystemBadgeText.color = Palette.FunkyMint;
        compSubsystemBadgeText.alignment = TextAlignmentOptions.MidlineRight;
        compSubsystemBadgeText.raycastTarget = false;

        // Left: Clickable Unique Option NASA Photo + fitted Image Caption
        compPhotoWidget = CreatePhotoWithCaptionBlock(
            comparisonContainer,
            "Comp",
            new Vector2(0.03f, 0.04f),
            new Vector2(0.39f, 0.84f),
            0.30f);

        // Right Top: Fitted background box + CompBodyText
        GameObject bodyBoxObj = new GameObject("CompBodyBox", typeof(RectTransform), typeof(Image), typeof(Outline));
        bodyBoxObj.transform.SetParent(comparisonContainer, false);
        compBodyBoxRect = bodyBoxObj.GetComponent<RectTransform>();
        SetAnchors(compBodyBoxRect, 0.415f, 0.34f, 0.975f, 0.85f);
        StyleHudCard(bodyBoxObj, new Color(0.07f, 0.11f, 0.17f, 0.95f), Palette.FunkyBlue, 1.4f);
        bodyBoxObj.GetComponent<Image>().raycastTarget = false;

        GameObject bodyObj = new GameObject("CompBodyText", typeof(RectTransform), typeof(TextMeshProUGUI));
        bodyObj.transform.SetParent(compBodyBoxRect, false);
        RectTransform bodyRt = bodyObj.GetComponent<RectTransform>();
        bodyRt.anchorMin = Vector2.zero;
        bodyRt.anchorMax = Vector2.one;
        bodyRt.offsetMin = new Vector2(14f, 10f);
        bodyRt.offsetMax = new Vector2(-14f, -10f);
        compBodyText = bodyObj.GetComponent<TextMeshProUGUI>();
        compBodyText.font = uiFont;
        compBodyText.enableAutoSizing = false;
        compBodyText.fontSize = 17f;
        compBodyText.lineSpacing = -2f;
        compBodyText.color = Palette.TextPrimary;
        compBodyText.textWrappingMode = TextWrappingModes.Normal;
        compBodyText.alignment = TextAlignmentOptions.TopLeft;
        AttachUrlLinkHandler(compBodyText, () => currentComparisonUrl);

        // Right Bottom: Core NASA Science Fact Callout
        GameObject factBoxObj = new GameObject("CompFactCallout", typeof(RectTransform), typeof(Image), typeof(Outline));
        factBoxObj.transform.SetParent(comparisonContainer, false);
        compFactBoxRect = factBoxObj.GetComponent<RectTransform>();
        SetAnchors(compFactBoxRect, 0.415f, 0.04f, 0.975f, 0.31f);
        StyleHudCard(factBoxObj, Palette.NasaBoxSurface, Palette.FunkyGold, 1.5f);
        factBoxObj.GetComponent<Image>().raycastTarget = false;

        GameObject factTxtObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
        factTxtObj.transform.SetParent(factBoxObj.transform, false);
        RectTransform factTxtRt = factTxtObj.GetComponent<RectTransform>();
        factTxtRt.anchorMin = Vector2.zero;
        factTxtRt.anchorMax = Vector2.one;
        factTxtRt.offsetMin = new Vector2(12f, 8f);
        factTxtRt.offsetMax = new Vector2(-12f, -8f);
        compFactText = factTxtObj.GetComponent<TextMeshProUGUI>();
        compFactText.font = uiFont;
        compFactText.enableAutoSizing = false;
        compFactText.fontSize = 15f;
        compFactText.lineSpacing = -2f;
        compFactText.color = Palette.TextPrimary;
        compFactText.textWrappingMode = TextWrappingModes.Normal;
        compFactText.alignment = TextAlignmentOptions.TopLeft;
        compFactText.raycastTarget = false;

        compPrevButton = CreateComparisonNavButton(
            parentTarget,
            "CompPrevButton",
            "< PREV",
            new Vector2(0.07f, 0.04f),
            new Vector2(0.28f, 0.13f),
            Palette.PanelElevated,
            Palette.FunkyCyan,
            () => StepComparison(-1));
        compPrevButtonText = compPrevButton.GetComponentInChildren<TMP_Text>(true);

        compNextButton = CreateComparisonNavButton(
            parentTarget,
            "CompNextButton",
            "NEXT >",
            new Vector2(0.72f, 0.04f),
            new Vector2(0.93f, 0.13f),
            Palette.PanelElevated,
            Palette.FunkyGold,
            () => StepComparison(1));
        compNextButtonText = compNextButton.GetComponentInChildren<TMP_Text>(true);

        containerObj.SetActive(false);
        compPrevButton.gameObject.SetActive(false);
        compNextButton.gameObject.SetActive(false);
    }

    private Button CreateComparisonNavButton(
        Transform parent,
        string name,
        string caption,
        Vector2 min,
        Vector2 max,
        Color bgColor,
        Color accentColor,
        UnityEngine.Events.UnityAction onClick)
    {
        GameObject btnObj = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button), typeof(Outline));
        btnObj.transform.SetParent(parent, false);
        SetAnchors(btnObj.GetComponent<RectTransform>(), min.x, min.y, max.x, max.y);

        Image bg = btnObj.GetComponent<Image>();
        bg.color = bgColor;
        Outline outline = btnObj.GetComponent<Outline>();
        outline.effectColor = accentColor;
        outline.effectDistance = new Vector2(1.8f, -1.8f);

        Button btn = btnObj.GetComponent<Button>();
        btn.targetGraphic = bg;
        btn.onClick.AddListener(onClick);
        AddFunkyEffect(btnObj, bgColor, accentColor);

        GameObject txtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(btnObj.transform, false);
        SetAnchors(txtObj.GetComponent<RectTransform>(), 0f, 0f, 1f, 1f);

        TextMeshProUGUI tmp = txtObj.GetComponent<TextMeshProUGUI>();
        tmp.font = uiFont;
        tmp.text = caption;
        tmp.enableAutoSizing = false;
        tmp.fontSize = 16.5f;
        tmp.fontStyle = FontStyles.Bold;
        tmp.textWrappingMode = TextWrappingModes.NoWrap;
        tmp.color = Palette.TextPrimary;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;

        return btn;
    }

    private void AttachFactLinkHandler(TMP_Text textComponent)
    {
        if (textComponent == null)
        {
            return;
        }
        textComponent.raycastTarget = true;
        TmpLinkClickHandler handler = textComponent.GetComponent<TmpLinkClickHandler>();
        if (handler == null)
        {
            handler = textComponent.gameObject.AddComponent<TmpLinkClickHandler>();
        }
        handler.Bind(textComponent, ShowFactPopupByKey);
    }

    private static void AttachUrlLinkHandler(TMP_Text textComponent, Func<string> urlProvider)
    {
        if (textComponent == null || urlProvider == null)
        {
            return;
        }
        textComponent.raycastTarget = true;
        TmpLinkClickHandler handler = textComponent.GetComponent<TmpLinkClickHandler>();
        if (handler == null)
        {
            handler = textComponent.gameObject.AddComponent<TmpLinkClickHandler>();
        }
        handler.Bind(textComponent, linkId =>
        {
            if (string.Equals(linkId, "OPEN_URL", StringComparison.OrdinalIgnoreCase))
            {
                string url = urlProvider.Invoke();
                if (!string.IsNullOrEmpty(url))
                {
                    Application.OpenURL(url);
                }
            }
        });
    }

    public void ApplyAtmosphere(string skyColorHex, float dustIntensity)
    {
        Color skyColor = Palette.BackgroundDeep;
        if (!string.IsNullOrEmpty(skyColorHex) && ColorUtility.TryParseHtmlString(skyColorHex, out Color parsed))
        {
            skyColor = parsed;
        }

        if (Camera.main != null)
        {
            Camera.main.backgroundColor = skyColor;
        }

        float blendStrength = Mathf.Lerp(0.35f, 0.72f, Mathf.Clamp01(dustIntensity));
        Color backdropColor = Color.Lerp(Palette.BackgroundDeep, skyColor, blendStrength);

        if (consoleBackgroundImage != null)
        {
            bool hasCustomSprite = consoleBackgroundImage.sprite != null;
            consoleBackgroundImage.color = hasCustomSprite
                ? Color.Lerp(Color.white, skyColor, 0.25f)
                : new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0.35f);
        }

        Color glassOverlay = new Color(backdropColor.r, backdropColor.g, backdropColor.b, 0.08f);
        if (titlePanelBackdrop != null)
        {
            titlePanelBackdrop.color = glassOverlay;
        }
        if (decisionPanelBackdrop != null)
        {
            decisionPanelBackdrop.color = glassOverlay;
        }
        if (outcomePanelBackdrop != null)
        {
            outcomePanelBackdrop.color = glassOverlay;
        }

        baseView?.UpdateAtmosphere(skyColorHex, dustIntensity);
        missionBackground?.UpdateTint(skyColor, dustIntensity);
    }

    public void ShowTitlePanel(string customTitle = null)
    {
        HideFactPopup();
        SetActivePanel(titlePanel);
        missionBackground?.ShowForPhaseOrSol(0);

        if (badgeLabelText != null)
        {
            badgeLabelText.text = "ARES SOLS: 2038  |  NASA 2018 MARS DUST STORM";
        }

        if (titleHeaderText != null)
        {
            titleHeaderText.text = !string.IsNullOrEmpty(customTitle)
                ? customTitle
                : "Ares Sols: 2038";
        }

        if (titlePremiseText != null)
        {
            titlePremiseText.text =
                "A giant dust storm is sweeping across Mars!\n" +
                "Join Captain Nova and Pip the Science Rover at Ares Base.\n" +
                "Can you protect your crew, save power, and survive all 10 Sols?\n" +
                "Tap Start Briefing to launch your mission!";
            titlePremiseText.alignment = TextAlignmentOptions.Center;
        }

        if (startButtonLabelText != null)
        {
            startButtonLabelText.text = "START BRIEFING";
        }

        FitTitlePanelToText();
        missionGuide?.ShowForTitle();
    }

    public void ShowIntroScreen(
        string gameTitle,
        IntroScreen screen,
        int screenIndex,
        int totalScreens,
        List<GlossaryEntry> glossary)
    {
        HideFactPopup();
        SetActivePanel(titlePanel);
        missionBackground?.ShowForPhaseOrSol(0);

        if (screen == null)
        {
            return;
        }

        if (badgeLabelText != null)
        {
            string titleTag = !string.IsNullOrEmpty(gameTitle) ? gameTitle.ToUpperInvariant() : "ARES SOLS: 2038";
            badgeLabelText.text = $"{titleTag}  —  BRIEFING {screenIndex + 1} OF {totalScreens}";
        }

        if (titleHeaderText != null)
        {
            titleHeaderText.text = screen.headline;
        }

        if (titlePremiseText != null)
        {
            string sourceLine = string.Empty;
            if (!string.IsNullOrEmpty(screen.nasaSource))
            {
                string introUrl = !string.IsNullOrEmpty(screen.sourceUrl)
                    ? screen.sourceUrl
                    : "https://science.nasa.gov/mission/mars-exploration-rovers/";
                sourceLine = $"\n<size=17><color=#00F5D4>NASA SOURCE:</color> <link=\"URL:{introUrl}\"><u><color=#FFD166>{screen.nasaSource}</color></u></link></size>";
            }

            titlePremiseText.alignment = TextAlignmentOptions.Left;
            titlePremiseText.text = $"{screen.bodyText}{sourceLine}";
        }

        if (startButtonLabelText != null)
        {
            bool isLast = screenIndex >= totalScreens - 1;
            startButtonLabelText.text = isLast ? "LAUNCH SOL 1" : "NEXT BRIEFING >";
        }

        FitTitlePanelToText();
        missionGuide?.ShowForIntro(screen);
    }

    private void FitTitlePanelToText()
    {
        if (titleCardRect == null)
        {
            return;
        }

        Vector2 badgePref = badgeLabelText != null
            ? MeasureText(badgeLabelText, badgeLabelText.text, 0f)
            : new Vector2(360f, 24f);

        Vector2 headerUnwrapped = titleHeaderText != null
            ? MeasureText(titleHeaderText, titleHeaderText.text, 0f)
            : new Vector2(520f, 38f);

        Vector2 premiseUnwrapped = titlePremiseText != null
            ? MeasureText(titlePremiseText, titlePremiseText.text, 0f)
            : new Vector2(720f, 120f);

        Vector2 btnPref = startButtonLabelText != null
            ? MeasureText(startButtonLabelText, startButtonLabelText.text, 0f)
            : new Vector2(200f, 24f);

        float candidateWrapW = Mathf.Clamp(
            Mathf.Max(badgePref.x + 28f, Mathf.Max(headerUnwrapped.x, Mathf.Min(premiseUnwrapped.x + 16f, 1080f))),
            540f,
            1140f);

        Vector2 headerPref = titleHeaderText != null
            ? MeasureText(titleHeaderText, titleHeaderText.text, candidateWrapW)
            : headerUnwrapped;
        Vector2 premisePref = titlePremiseText != null
            ? MeasureText(titlePremiseText, titlePremiseText.text, candidateWrapW)
            : premiseUnwrapped;

        // If tall briefing would exceed vertical comfort range, widen wrap width slightly first
        float estH = 20f + (badgePref.y + 16f) + 12f + headerPref.y + 12f + premisePref.y + 18f + (btnPref.y + 22f) + 22f;
        if (estH > 840f && candidateWrapW < 1360f)
        {
            candidateWrapW = 1360f;
            headerPref = titleHeaderText != null ? MeasureText(titleHeaderText, titleHeaderText.text, candidateWrapW) : headerUnwrapped;
            premisePref = titlePremiseText != null ? MeasureText(titlePremiseText, titlePremiseText.text, candidateWrapW) : premiseUnwrapped;
        }

        float contentW = Mathf.Clamp(Mathf.Max(badgePref.x + 36f, Mathf.Max(headerPref.x, premisePref.x)), 460f, 1360f);
        float cardW = Mathf.Ceil(contentW + 76f);

        float badgeH = Mathf.Ceil(badgePref.y + 14f);
        float headerH = Mathf.Ceil(headerPref.y + 6f);
        float premiseH = Mathf.Ceil(premisePref.y + 10f);
        float btnH = Mathf.Clamp(Mathf.Ceil(btnPref.y + 20f), 44f, 64f);

        float cardH = Mathf.Clamp(Mathf.Ceil(22f + badgeH + 14f + headerH + 10f + premiseH + 20f + btnH + 24f), 280f, 880f);
        SetCenteredAnchors(titleCardRect, RefCanvasWidth, RefCanvasHeight, cardW, cardH);

        float topCursor = 1f - 22f / cardH;
        if (missionBadgeRect != null && badgeLabelText != null)
        {
            float bw = Mathf.Clamp(badgePref.x + 38f, 220f, cardW - 60f) / cardW;
            float bh = badgeH / cardH;
            SetAnchors(missionBadgeRect, 0.5f - bw * 0.5f, topCursor - bh, 0.5f + bw * 0.5f, topCursor);
            SetAnchors(badgeLabelText.rectTransform, 0.03f, 0.05f, 0.97f, 0.95f);
            topCursor -= bh + 14f / cardH;
        }

        if (titleHeaderText != null)
        {
            float hh = headerH / cardH;
            float hw = Mathf.Clamp(headerPref.x + 28f, 320f, cardW - 56f) / cardW;
            SetAnchors(titleHeaderText.rectTransform, 0.5f - hw * 0.5f, topCursor - hh, 0.5f + hw * 0.5f, topCursor);
            topCursor -= hh + 10f / cardH;
        }

        float btnBottomY = 24f / cardH;
        if (startButton != null && startButtonLabelText != null)
        {
            FitButtonToText(startButton, startButtonLabelText, 0.5f, btnBottomY, cardW, cardH, 52f, 20f, 160f, 44f, 18f);
        }

        if (titlePremiseText != null)
        {
            float premiseBottom = btnBottomY + (btnH + 20f) / cardH;
            float pxInset = 38f / cardW;
            SetAnchors(titlePremiseText.rectTransform, pxInset, premiseBottom, 1f - pxInset, topCursor);
        }
    }

    public void ShowDecisionPanel(
        GameState state,
        StoryNode node,
        List<Choice> availableChoices,
        List<GlossaryEntry> glossary = null)
    {
        HideFactPopup();
        SetActivePanel(decisionPanel);
        UpdateHud(state);

        if (nodeTitleText != null)
        {
            string dateTag = !string.IsNullOrEmpty(node?.realDate)
                ? $"  <size=19><color=#00F5D4>[{node.realDate}]</color></size>"
                : string.Empty;
            string tipTag = "  <size=14><color=#FFD166>(Tap blue words to explore NASA facts!)</color></size>";
            nodeTitleText.text = node != null ? $"{node.title}{dateTag}{tipTag}" : "ARES BASE DECISION";
        }

        if (dilemmaPromptText != null && node != null)
        {
            string consequenceBlock = !string.IsNullOrEmpty(state?.activeConsequenceText)
                ? $"\n<size=18><color=#FFD166><b>BECAUSE OF YOUR LAST CHOICE:</b> {state.activeConsequenceText}</color></size>"
                : string.Empty;

            string banner = string.Empty;
            if (!string.IsNullOrEmpty(node.nasaFactBanner))
            {
                string lessonUrl = !string.IsNullOrEmpty(node.sourceUrl) ? node.sourceUrl : node.choiceA?.sourceUrl;
                if (string.IsNullOrEmpty(lessonUrl)) lessonUrl = node.choiceB?.sourceUrl;
                if (string.IsNullOrEmpty(lessonUrl)) lessonUrl = node.choiceC?.sourceUrl;
                if (string.IsNullOrEmpty(lessonUrl)) lessonUrl = "https://science.nasa.gov/resource/the-martian-dust-storm-of-june-2018/";
                banner = $"\n<size=17><color=#00F5D4>{node.nasaFactBanner}</color>  <link=\"URL:{lessonUrl}\"><u><color=#FFD166>[NASA Source]</color></u></link></size>";
            }

            dilemmaPromptText.text = $"{node.EffectiveStoryText}{consequenceBlock}{banner}";
        }

        baseView?.ClearChoiceHighlight();
        float choiceMaxY = RebuildHorizontalChoiceCards(state, availableChoices);
        FitDilemmaCardToText(choiceMaxY);
        missionBackground?.ShowForPhaseOrSol(state != null ? state.sol : 1);
        missionGuide?.ShowForDecision(state, node);
    }

    private float FitDilemmaCardToText(float choiceMaxY = 0.24f)
    {
        float minY = Mathf.Clamp(choiceMaxY + 0.02f, 0.16f, 0.52f);
        if (dilemmaCardRect == null)
        {
            baseView?.SetVerticalBand(Mathf.Clamp(minY + 0.012f, 0.45f, 0.58f), 0.79f);
            return minY;
        }

        const float maxTopLimitY = 0.54f;
        float maxAllowedH = Mathf.Max(130f, (maxTopLimitY - minY) * RefCanvasHeight);

        if (dilemmaPromptText != null)
        {
            dilemmaPromptText.fontSize = 19.5f;
        }

        Vector2 titleUn = nodeTitleText != null
            ? MeasureText(nodeTitleText, nodeTitleText.text, 0f)
            : new Vector2(600f, 30f);
        Vector2 bodyUn = dilemmaPromptText != null
            ? MeasureText(dilemmaPromptText, dilemmaPromptText.text, 0f)
            : new Vector2(900f, 180f);

        float wrapW = Mathf.Clamp(Mathf.Max(titleUn.x, Mathf.Min(bodyUn.x + 14f, 1130f)), 740f, 1140f);
        Vector2 titlePref = nodeTitleText != null ? MeasureText(nodeTitleText, nodeTitleText.text, wrapW) : titleUn;
        Vector2 bodyPref = dilemmaPromptText != null ? MeasureText(dilemmaPromptText, dilemmaPromptText.text, wrapW) : bodyUn;

        float totalH = Mathf.Ceil(titlePref.y + bodyPref.y + 42f);
        if (totalH > maxAllowedH && wrapW < 1140f)
        {
            wrapW = 1140f;
            titlePref = nodeTitleText != null ? MeasureText(nodeTitleText, nodeTitleText.text, wrapW) : titleUn;
            bodyPref = dilemmaPromptText != null ? MeasureText(dilemmaPromptText, dilemmaPromptText.text, wrapW) : bodyUn;
            totalH = Mathf.Ceil(titlePref.y + bodyPref.y + 42f);
        }

        if (totalH > maxAllowedH && dilemmaPromptText != null)
        {
            dilemmaPromptText.fontSize = 17.5f;
            bodyPref = MeasureText(dilemmaPromptText, dilemmaPromptText.text, wrapW);
            totalH = Mathf.Ceil(titlePref.y + bodyPref.y + 42f);
        }

        float cardW = Mathf.Clamp(Mathf.Max(titlePref.x, bodyPref.x) + 56f, 740f, 1200f);
        float cardH = Mathf.Clamp(totalH, Mathf.Min(165f, maxAllowedH), maxAllowedH);

        float halfNormW = (cardW / RefCanvasWidth) * 0.5f;
        float maxY = Mathf.Min(maxTopLimitY, minY + cardH / RefCanvasHeight);
        float leftX = Mathf.Max(0.185f, 0.50f - halfNormW);
        float rightX = Mathf.Min(0.815f, leftX + (cardW / RefCanvasWidth));
        SetAnchors(dilemmaCardRect, leftX, minY, rightX, maxY);

        baseView?.SetVerticalBand(Mathf.Clamp(maxY + 0.012f, 0.45f, 0.58f), 0.79f);

        float padY = 14f / cardH;
        float padX = 28f / cardW;
        float titleH = Mathf.Clamp((titlePref.y + 4f) / cardH, 0.08f, 0.35f);

        if (nodeTitleText != null)
        {
            SetAnchors(nodeTitleText.rectTransform, padX, 1f - padY - titleH, 1f - padX, 1f - padY);
        }
        if (dilemmaPromptText != null)
        {
            SetAnchors(dilemmaPromptText.rectTransform, padX, padY, 1f - padX, 1f - padY - titleH - 8f / cardH);
        }

        return minY;
    }

    public void ShowOutcomePanel(
        GameState state,
        Choice chosenChoice,
        StoryNode sourceNode,
        bool isFinalSolOutcome)
    {
        HideFactPopup();
        SetActivePanel(outcomePanel);
        ToggleComparisonMode(false);
        UpdateHud(state);
        missionBackground?.ShowForPhaseOrSol(state != null ? state.sol : 1);

        if (outcomeHeaderText != null)
        {
            outcomeHeaderText.color = Palette.TextPrimary;
            string choiceName = chosenChoice != null ? chosenChoice.EffectiveLabel : "Action Taken";
            outcomeHeaderText.text = $"SOL {state.sol} RESULT — {choiceName}";
        }

        if (outcomeTelemetryText != null)
        {
            int powerDelta = state != null ? state.lastChoicePowerDelta : (chosenChoice?.powerDelta ?? 0);
            int healthDelta = state != null ? state.lastChoiceHealthDelta : (chosenChoice?.EffectiveCrewHealthDelta ?? 0);
            string powerBadge = FormatPowerActionText(powerDelta);
            string healthBadge = GameState.FormatSignedDelta(healthDelta);
            string subTag = chosenChoice != null && !string.IsNullOrEmpty(chosenChoice.subsystemTag)
                ? $"   |   {chosenChoice.subsystemTag}"
                : string.Empty;

            outcomeTelemetryText.text =
                $"ELECTRICITY: {state.power}% ({powerBadge})   |   CREW HEALTH: {state.crewHealth}% ({healthBadge}){subTag}";
        }

        if (outcomeBodyText != null)
        {
            outcomeBodyText.text = state != null && !string.IsNullOrEmpty(state.lastChoiceOutcomeText)
                ? state.lastChoiceOutcomeText
                : (chosenChoice != null ? chosenChoice.EffectiveOutcome : string.Empty);
        }

        bool hasPhoto = ApplyOutcomePhoto(
            chosenChoice?.imageFile,
            chosenChoice?.imageCaption,
            chosenChoice?.sourceUrl);

        if (nasaScienceBox != null)
        {
            nasaScienceBox.SetActive(true);
        }
        if (nasaScienceTitleText != null)
        {
            nasaScienceTitleText.text = "REAL NASA SCIENCE!";
        }
        if (nasaNoteText != null)
        {
            string realNote = chosenChoice != null && !string.IsNullOrEmpty(chosenChoice.nasaRealDecision)
                ? chosenChoice.nasaRealDecision
                : (sourceNode?.nasaFactBanner ?? string.Empty);
            string srcLine = BuildClickableSourceLine(chosenChoice?.nasaSource, chosenChoice?.nasaId, chosenChoice?.sourceUrl);
            nasaNoteText.text = $"{realNote}{srcLine}";
        }

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = isFinalSolOutcome
                ? "FINAL OUTCOME >"
                : $"SOL {Mathf.Min(state.sol + 1, GameState.MaxSols)} >";
        }

        FitOutcomeOrEndingPanelToText(hasPhoto);
        missionGuide?.ShowForOutcome(chosenChoice);
    }

    public void ShowEndingPanel(GameState state, StoryEnding ending)
    {
        HideFactPopup();
        SetActivePanel(outcomePanel);
        ToggleComparisonMode(false);
        UpdateHud(state);
        missionBackground?.ShowForPhaseOrSol(11);

        if (outcomeHeaderText != null)
        {
            outcomeHeaderText.color = ResolveEndingHeaderColor(ending?.endingId);
            outcomeHeaderText.text = ending != null ? ending.headline : $"Sol {GameState.MaxSols}: Mission Complete";
        }

        if (outcomeTelemetryText != null)
        {
            string dateText = !string.IsNullOrEmpty(ending?.realDate) ? ending.realDate : "July 23, 2038";
            outcomeTelemetryText.text =
                $"{dateText.ToUpperInvariant()}   |   FINAL ELECTRICITY: {state.power}%   |   FINAL CREW HEALTH: {state.crewHealth}%";
        }

        if (outcomeBodyText != null)
        {
            outcomeBodyText.text = ending != null ? ending.storyText : string.Empty;
        }

        bool hasPhoto = ApplyOutcomePhoto(
            ending?.imageFile,
            ending?.imageCaption,
            ending?.sourceUrl);

        if (nasaScienceBox != null)
        {
            nasaScienceBox.SetActive(true);
        }
        if (nasaScienceTitleText != null)
        {
            nasaScienceTitleText.text = $"SOL {GameState.MaxSols} OUTCOME CRITERIA";
        }
        if (nasaNoteText != null)
        {
            string crit = ending != null ? ending.conditionDescription : string.Empty;
            string srcLine = BuildClickableSourceLine(ending?.nasaSource, ending?.nasaId, ending?.sourceUrl);
            nasaNoteText.text = $"{crit}{srcLine}";
        }

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = "COMPARE RESULTS >";
        }

        FitOutcomeOrEndingPanelToText(hasPhoto);
        missionGuide?.ShowForEnding(ending);
    }

    private void FitOutcomeOrEndingPanelToText(bool hasPhoto)
    {
        if (outcomeCardRect == null)
        {
            return;
        }

        Vector2 hdrPref = outcomeHeaderText != null
            ? MeasureText(outcomeHeaderText, outcomeHeaderText.text, 0f)
            : new Vector2(600f, 36f);
        Vector2 telPref = outcomeTelemetryText != null
            ? MeasureText(outcomeTelemetryText, outcomeTelemetryText.text, 0f)
            : new Vector2(720f, 24f);

        float rightColumnW = hasPhoto ? 600f : 1080f;
        Vector2 bodyPref = outcomeBodyText != null
            ? MeasureText(outcomeBodyText, outcomeBodyText.text, rightColumnW - 28f)
            : Vector2.zero;
        Vector2 sciTitlePref = nasaScienceTitleText != null
            ? MeasureText(nasaScienceTitleText, nasaScienceTitleText.text, rightColumnW - 28f)
            : Vector2.zero;
        Vector2 sciNotePref = nasaNoteText != null
            ? MeasureText(nasaNoteText, nasaNoteText.text, rightColumnW - 28f)
            : Vector2.zero;

        float bodyBoxW = Mathf.Clamp(Mathf.Ceil(bodyPref.x + 30f), 360f, rightColumnW);
        float bodyBoxH = Mathf.Clamp(Mathf.Ceil(bodyPref.y + 24f), 90f, 340f);

        float sciHeaderH = Mathf.Ceil(sciTitlePref.y + 6f);
        float sciBodyH = Mathf.Ceil(sciNotePref.y + 6f);
        float sciBoxW = Mathf.Clamp(Mathf.Ceil(Mathf.Max(sciTitlePref.x, sciNotePref.x) + 30f), 360f, rightColumnW);
        float sciBoxH = Mathf.Clamp(Mathf.Ceil(10f + sciHeaderH + 4f + sciBodyH + 12f), 90f, 320f);

        float usedRightW = Mathf.Max(bodyBoxW, sciBoxW);
        float rightTotalH = bodyBoxH + 14f + sciBoxH;

        float leftPhotoW = hasPhoto ? 420f : 0f;
        float contentSpanW = hasPhoto ? (leftPhotoW + 24f + usedRightW) : usedRightW;

        Vector2 hdrWrapped = outcomeHeaderText != null
            ? MeasureText(outcomeHeaderText, outcomeHeaderText.text, Mathf.Max(contentSpanW, 740f))
            : hdrPref;
        Vector2 telWrapped = outcomeTelemetryText != null
            ? MeasureText(outcomeTelemetryText, outcomeTelemetryText.text, Mathf.Max(contentSpanW, 740f))
            : telPref;

        float hdrH = Mathf.Ceil(hdrWrapped.y + 4f);
        float telH = Mathf.Ceil(telWrapped.y + 4f);
        float minBtnW = nextButtonLabel != null ? Mathf.Ceil(MeasureText(nextButtonLabel, nextButtonLabel.text, 0f).x + 56f) : 220f;
        float btnH = nextButtonLabel != null ? Mathf.Clamp(Mathf.Ceil(MeasureText(nextButtonLabel, nextButtonLabel.text, 0f).y + 20f), 44f, 64f) : 52f;

        float cardW = Mathf.Clamp(Mathf.Ceil(Mathf.Max(contentSpanW, Mathf.Max(hdrWrapped.x, Mathf.Max(telWrapped.x, minBtnW))) + 64f), 760f, 1180f);
        float middleH = hasPhoto ? Mathf.Max(rightTotalH, 380f) : rightTotalH;
        float cardH = Mathf.Clamp(Mathf.Ceil(20f + hdrH + 6f + telH + 14f + middleH + 16f + btnH + 20f), 420f, 920f);

        SetCenteredAnchors(outcomeCardRect, RefCanvasWidth, RefCanvasHeight, cardW, cardH);

        float topCursor = 1f - 18f / cardH;
        float sidePad = 32f / cardW;

        if (outcomeHeaderText != null)
        {
            float hh = hdrH / cardH;
            SetAnchors(outcomeHeaderText.rectTransform, sidePad, topCursor - hh, 1f - sidePad, topCursor);
            topCursor -= hh + 6f / cardH;
        }

        if (outcomeTelemetryText != null)
        {
            float th = telH / cardH;
            SetAnchors(outcomeTelemetryText.rectTransform, sidePad, topCursor - th, 1f - sidePad, topCursor);
            topCursor -= th + 14f / cardH;
        }

        float bottomBtnTop = (20f + btnH + 14f) / cardH;
        float availMiddleH = (topCursor - bottomBtnTop) * cardH;

        float rightStartX = hasPhoto ? (32f + leftPhotoW + 24f) / cardW : sidePad;
        if (hasPhoto && outcomePhotoWidget != null)
        {
            float photoMaxX = (32f + leftPhotoW) / cardW;
            SetAnchors(outcomePhotoWidget.rootRect, sidePad, bottomBtnTop, photoMaxX, topCursor);
            FitBoxToCaption(outcomePhotoWidget, leftPhotoW, availMiddleH);
        }

        // Right-hand boxes resized cleanly to their text widths & stacked heights
        if (outcomeBodyText != null)
        {
            float bHNorm = bodyBoxH / cardH;
            float bWNorm = bodyBoxW / cardW;
            float bMaxX = Mathf.Min(1f - sidePad, rightStartX + bWNorm);
            float bMinY = topCursor - bHNorm;

            if (outcomeBodyBoxRect != null)
            {
                outcomeBodyBoxRect.gameObject.SetActive(true);
                SetAnchors(outcomeBodyBoxRect, rightStartX, bMinY, bMaxX, topCursor);
            }
            SetAnchors(outcomeBodyText.rectTransform, rightStartX + 14f / cardW, bMinY + 10f / cardH, bMaxX - 14f / cardW, topCursor - 10f / cardH);

            float sciTop = bMinY - 14f / cardH;
            if (nasaScienceBoxRect != null)
            {
                float sHNorm = Mathf.Min(sciBoxH / cardH, sciTop - bottomBtnTop);
                float sWNorm = sciBoxW / cardW;
                float sMaxX = Mathf.Min(1f - sidePad, rightStartX + sWNorm);
                float sMinY = sciTop - sHNorm;
                SetAnchors(nasaScienceBoxRect, rightStartX, sMinY, sMaxX, sciTop);

                float innerH = Mathf.Max(60f, sHNorm * cardH);
                float titleSplit = Mathf.Clamp((sciHeaderH + 8f) / innerH, 0.16f, 0.42f);
                if (nasaScienceTitleText != null)
                {
                    SetAnchors(nasaScienceTitleText.rectTransform, 0.03f, 1f - titleSplit, 0.97f, 0.95f);
                }
                if (nasaNoteText != null)
                {
                    SetAnchors(nasaNoteText.rectTransform, 0.03f, 0.06f, 0.97f, 1f - titleSplit - 0.02f);
                }
            }
        }

        if (nextButton != null && nextButtonLabel != null)
        {
            FitButtonToText(nextButton, nextButtonLabel, 0.5f, 20f / cardH, cardW, cardH, 52f, 20f, 170f, 44f, 17.5f);
        }
    }

    private bool ApplyOutcomePhoto(string imageFile, string caption, string sourceUrl)
    {
        currentOutcomeUrl = sourceUrl;
        if (outcomePhotoWidget == null)
        {
            return false;
        }

        Texture2D tex = LoadNasaTexture(imageFile);
        if (tex == null)
        {
            outcomePhotoWidget.root.SetActive(false);
            return false;
        }

        outcomePhotoWidget.root.SetActive(true);
        outcomePhotoWidget.rawImage.texture = tex;
        outcomePhotoWidget.fitter.aspectRatio = (float)tex.width / Mathf.Max(1, tex.height);
        outcomePhotoWidget.rawCaption = !string.IsNullOrEmpty(caption) ? caption : "Official NASA archive photograph.";
        outcomePhotoWidget.captionText.text = !string.IsNullOrEmpty(caption)
            ? $"<color=#FFD166><b>ABOUT THIS IMAGE:</b></color> {caption}"
            : "<color=#FFD166><b>NASA ARCHIVE PHOTO</b></color>";
        return true;
    }

    public void ShowComparisonPanel(GameState state, RealLifeComparison comparison)
    {
        HideFactPopup();
        SetActivePanel(outcomePanel);
        ToggleComparisonMode(true);

        if (outcomeHeaderText != null)
        {
            outcomeHeaderText.color = Palette.FunkyCyan;
            outcomeHeaderText.text = "Compare Results: Each Choice vs. Real NASA";
        }

        if (outcomeTelemetryText != null)
        {
            outcomeTelemetryText.text =
                $"ARES SOLS: 2038 FINAL REPORT   |   ELECTRICITY: {state.power}%   |   CREW HEALTH: {state.crewHealth}%";
        }

        activeComparisonRecords.Clear();
        if (state != null && state.choiceHistory != null && state.choiceHistory.Count > 0)
        {
            activeComparisonRecords.AddRange(state.choiceHistory);
        }
        else if (comparison != null && comparison.cards != null)
        {
            for (int i = 0; i < comparison.cards.Count; i++)
            {
                ComparisonCard card = comparison.cards[i];
                if (card == null)
                {
                    continue;
                }
                string fallbackFactId = i == 0 ? "2018_noon_blackout" : i == 1 ? "2018_fission_power" : "2018_moxie_oxygen";
                GlossaryEntry g = gameManager?.FindFactByKey(fallbackFactId);
                activeComparisonRecords.Add(new PlayerChoiceRecord
                {
                    sol = i + 1,
                    realDate = "2018 -> 2038 Archive",
                    buttonLabel = card.topic,
                    nasaComparisonTitle = card.topic,
                    nasaRealDecision = card.whatHappenedInRealLife,
                    nasaFactEmphasis = card.howItConnectsToGame,
                    nasaFactId = fallbackFactId,
                    nasaId = g?.nasaId,
                    imageFile = g?.imageFile,
                    imageCaption = g?.imageCaption,
                    nasaSource = g?.nasaSource,
                    sourceUrl = g?.sourceUrl
                });
            }
        }

        currentComparisonIndex = 0;
        RenderSingularComparisonCard();

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = "PLAY AGAIN";
        }
        FitComparisonActionButtons();
    }

    private void StepComparison(int delta)
    {
        if (activeComparisonRecords.Count == 0)
        {
            return;
        }

        int nextIdx = currentComparisonIndex + delta;
        if (nextIdx < 0)
        {
            nextIdx = activeComparisonRecords.Count - 1;
        }
        else if (nextIdx >= activeComparisonRecords.Count)
        {
            nextIdx = 0;
        }

        currentComparisonIndex = nextIdx;
        RenderSingularComparisonCard();
        FitComparisonActionButtons();
    }

    private void RenderSingularComparisonCard()
    {
        if (activeComparisonRecords.Count == 0 || comparisonContainer == null)
        {
            return;
        }

        PlayerChoiceRecord rec = activeComparisonRecords[currentComparisonIndex];
        int total = activeComparisonRecords.Count;

        if (compBadgeText != null)
        {
            compBadgeText.text = $"RESULT {currentComparisonIndex + 1} OF {total}   •   SOL {rec.sol} ({rec.realDate})";
        }

        if (compSubsystemBadgeText != null)
        {
            if (!string.IsNullOrEmpty(rec.subsystemTag))
            {
                string colorHex = rec.subsystemGood ? "#06D6A0" : "#FF5E7E";
                compSubsystemBadgeText.text = $"<color={colorHex}>{rec.subsystemTag}</color>";
            }
            else
            {
                compSubsystemBadgeText.text = string.Empty;
            }
        }

        GlossaryEntry fallbackEntry = gameManager != null && !string.IsNullOrEmpty(rec.nasaFactId)
            ? gameManager.FindFactByKey(rec.nasaFactId)
            : gameManager?.FindFactByKey("2018_noon_blackout");

        string imgFile = !string.IsNullOrEmpty(rec.imageFile) ? rec.imageFile : fallbackEntry?.imageFile;
        string imgCap = !string.IsNullOrEmpty(rec.imageCaption) ? rec.imageCaption : fallbackEntry?.imageCaption;
        string imgId = !string.IsNullOrEmpty(rec.nasaId) ? rec.nasaId : fallbackEntry?.nasaId;
        string imgSource = !string.IsNullOrEmpty(rec.nasaSource) ? rec.nasaSource : fallbackEntry?.nasaSource;
        currentComparisonUrl = !string.IsNullOrEmpty(rec.sourceUrl) ? rec.sourceUrl : fallbackEntry?.sourceUrl;

        Texture2D tex = LoadNasaTexture(imgFile);
        bool hasImage = tex != null && compPhotoWidget != null;
        if (compPhotoWidget != null)
        {
            compPhotoWidget.root.SetActive(hasImage);
            if (hasImage)
            {
                compPhotoWidget.rawImage.texture = tex;
                compPhotoWidget.fitter.aspectRatio = (float)tex.width / Mathf.Max(1, tex.height);
                compPhotoWidget.rawCaption = !string.IsNullOrEmpty(imgCap) ? imgCap : $"Official NASA photograph ({imgId}).";
                compPhotoWidget.captionText.text = !string.IsNullOrEmpty(imgCap)
                    ? $"<color=#FFD166><b>ABOUT THIS IMAGE:</b></color> {imgCap}"
                    : $"<color=#FFD166><b>NASA ARCHIVE ({imgId})</b></color>";
            }
        }

        if (compBodyText != null)
        {
            string pBadge = FormatPowerActionText(rec.netPowerDelta);
            string hBadge = rec.netHealthDelta >= 0 ? $"+{rec.netHealthDelta}% Health" : $"{rec.netHealthDelta}% Health";
            string title = !string.IsNullOrEmpty(rec.nasaComparisonTitle) ? rec.nasaComparisonTitle : rec.buttonLabel;
            string srcLine = BuildClickableSourceLine(imgSource, imgId, currentComparisonUrl);

            compBodyText.text =
                $"<size=20><b><color=#FFD166>{title}</color></b></size>\n" +
                $"<size=15><color=#00F5D4>YOUR SOL {rec.sol} CHOICE:</color> <b>{rec.buttonLabel}</b>  " +
                $"({pBadge} | {hBadge} -> {rec.powerAfter}% Elec, {rec.healthAfter}% Health)</size>\n\n" +
                $"<size=16><b><color=#06D6A0>REAL NASA PAST & FUTURE CONNECTION:</color></b></size>\n" +
                $"{rec.nasaRealDecision}{srcLine}";
        }

        if (compFactText != null)
        {
            string factLine = !string.IsNullOrEmpty(rec.nasaFactEmphasis)
                ? rec.nasaFactEmphasis
                : (fallbackEntry != null ? fallbackEntry.simpleDefinition : string.Empty);
            compFactText.text = $"<color=#FFD166><b>KEY NASA FACT:</b></color> {factLine}";
        }

        if (compPrevButton != null)
        {
            compPrevButton.interactable = total > 1;
        }
        if (compNextButton != null)
        {
            compNextButton.interactable = total > 1;
            if (compNextButtonText != null)
            {
                int nextNum = ((currentComparisonIndex + 1) % total) + 1;
                compNextButtonText.text = $"NEXT ({nextNum}/{total}) >";
            }
        }

        FitComparisonLayoutToText(hasImage);
    }

    private void FitComparisonLayoutToText(bool hasImage)
    {
        if (outcomeCardRect == null || comparisonContainer == null)
        {
            return;
        }

        Vector2 hdrPref = outcomeHeaderText != null ? MeasureText(outcomeHeaderText, outcomeHeaderText.text, 0f) : new Vector2(540f, 34f);
        Vector2 telPref = outcomeTelemetryText != null ? MeasureText(outcomeTelemetryText, outcomeTelemetryText.text, 0f) : new Vector2(680f, 24f);

        float rightMaxW = hasImage ? 760f : 1180f;
        Vector2 bodyPref = compBodyText != null ? MeasureText(compBodyText, compBodyText.text, rightMaxW - 28f) : Vector2.zero;
        Vector2 factPref = compFactText != null ? MeasureText(compFactText, compFactText.text, rightMaxW - 24f) : Vector2.zero;

        float bodyBoxW = Mathf.Clamp(Mathf.Ceil(bodyPref.x + 30f), 400f, rightMaxW);
        float bodyBoxH = Mathf.Clamp(Mathf.Ceil(bodyPref.y + 22f), 110f, 330f);

        float factBoxW = Mathf.Clamp(Mathf.Ceil(factPref.x + 26f), 400f, rightMaxW);
        float factBoxH = Mathf.Clamp(Mathf.Ceil(factPref.y + 18f), 64f, 210f);

        float usedRightW = Mathf.Max(bodyBoxW, factBoxW);
        float rightStackH = bodyBoxH + 14f + factBoxH;

        float leftPhotoW = hasImage ? 480f : 0f;
        float containerInnerW = hasImage ? (leftPhotoW + 22f + usedRightW) : usedRightW;
        float containerW = Mathf.Clamp(Mathf.Ceil(containerInnerW + 52f), 820f, 1460f);
        float topBadgeH = 34f;
        float containerH = Mathf.Clamp(Mathf.Ceil(topBadgeH + 16f + Mathf.Max(rightStackH, hasImage ? 380f : 220f) + 22f), 340f, 640f);

        float cardW = Mathf.Clamp(Mathf.Ceil(Mathf.Max(containerW + 56f, Mathf.Max(hdrPref.x, telPref.x) + 64f)), 900f, 1540f);
        float cardH = Mathf.Clamp(Mathf.Ceil(20f + hdrPref.y + 6f + telPref.y + 14f + containerH + 16f + 52f + 20f), 540f, 920f);

        SetCenteredAnchors(outcomeCardRect, RefCanvasWidth, RefCanvasHeight, cardW, cardH);

        float topCursor = 1f - 18f / cardH;
        float sidePad = 30f / cardW;

        if (outcomeHeaderText != null)
        {
            float hh = Mathf.Ceil(hdrPref.y + 4f) / cardH;
            SetAnchors(outcomeHeaderText.rectTransform, sidePad, topCursor - hh, 1f - sidePad, topCursor);
            topCursor -= hh + 6f / cardH;
        }

        if (outcomeTelemetryText != null)
        {
            float th = Mathf.Ceil(telPref.y + 4f) / cardH;
            SetAnchors(outcomeTelemetryText.rectTransform, sidePad, topCursor - th, 1f - sidePad, topCursor);
            topCursor -= th + 12f / cardH;
        }

        float compMinY = (20f + 52f + 14f) / cardH;
        SetAnchors(comparisonContainer, sidePad, compMinY, 1f - sidePad, topCursor);

        float actualCompW = (1f - 2f * sidePad) * cardW;
        float actualCompH = (topCursor - compMinY) * cardH;

        float badgeSplitY = 1f - 38f / actualCompH;
        if (compBadgeText != null)
        {
            SetAnchors(compBadgeText.rectTransform, 0.025f, badgeSplitY, 0.62f, 0.97f);
        }
        if (compSubsystemBadgeText != null)
        {
            SetAnchors(compSubsystemBadgeText.rectTransform, 0.62f, badgeSplitY, 0.975f, 0.97f);
        }

        float innerTop = badgeSplitY - 10f / actualCompH;
        float innerBottom = 18f / actualCompH;
        float innerAvailH = (innerTop - innerBottom) * actualCompH;

        float rightStartX = hasImage ? (24f + leftPhotoW + 22f) / actualCompW : 0.025f;
        if (hasImage && compPhotoWidget != null)
        {
            float photoMaxX = (24f + leftPhotoW) / actualCompW;
            SetAnchors(compPhotoWidget.rootRect, 24f / actualCompW, innerBottom, photoMaxX, innerTop);
            FitBoxToCaption(compPhotoWidget, leftPhotoW, innerAvailH);
        }

        if (compBodyBoxRect != null)
        {
            float bWNorm = bodyBoxW / actualCompW;
            float bHNorm = bodyBoxH / actualCompH;
            float bMaxX = Mathf.Min(0.975f, rightStartX + bWNorm);
            float bMinY = innerTop - bHNorm;
            SetAnchors(compBodyBoxRect, rightStartX, bMinY, bMaxX, innerTop);

            if (compFactBoxRect != null)
            {
                float fTop = bMinY - 12f / actualCompH;
                float fWNorm = factBoxW / actualCompW;
                float fHNorm = Mathf.Min(factBoxH / actualCompH, fTop - innerBottom);
                float fMaxX = Mathf.Min(0.975f, rightStartX + fWNorm);
                SetAnchors(compFactBoxRect, rightStartX, fTop - fHNorm, fMaxX, fTop);
            }
        }
    }

    private void FitComparisonActionButtons()
    {
        if (outcomeCardRect == null)
        {
            return;
        }
        float cardW = (outcomeCardRect.anchorMax.x - outcomeCardRect.anchorMin.x) * RefCanvasWidth;
        float cardH = (outcomeCardRect.anchorMax.y - outcomeCardRect.anchorMin.y) * RefCanvasHeight;
        float bottomY = 20f / cardH;

        if (compPrevButton != null && compPrevButtonText != null)
        {
            FitButtonToText(compPrevButton, compPrevButtonText, 0.17f, bottomY, cardW, cardH, 42f, 18f, 120f, 44f, 16.5f);
        }
        if (nextButton != null && nextButtonLabel != null)
        {
            FitButtonToText(nextButton, nextButtonLabel, 0.50f, bottomY, cardW, cardH, 48f, 18f, 140f, 44f, 17f);
        }
        if (compNextButton != null && compNextButtonText != null)
        {
            FitButtonToText(compNextButton, compNextButtonText, 0.83f, bottomY, cardW, cardH, 42f, 18f, 130f, 44f, 16.5f);
        }
    }

    public void ShowFactPopupByKey(string key)
    {
        if (string.Equals(key, "OPEN_URL", StringComparison.OrdinalIgnoreCase))
        {
            if (!string.IsNullOrEmpty(currentFactUrl))
            {
                Application.OpenURL(currentFactUrl);
            }
            return;
        }

        if (!string.IsNullOrEmpty(key) && key.StartsWith("URL:", StringComparison.OrdinalIgnoreCase))
        {
            string url = key.Substring(4);
            if (!string.IsNullOrEmpty(url))
            {
                Application.OpenURL(url);
            }
            return;
        }

        if (factPopupOverlay == null || gameManager == null)
        {
            return;
        }

        GlossaryEntry entry = gameManager.FindFactByKey(key);
        if (entry == null)
        {
            Debug.LogWarning($"[MissionUI] Clicked inline fact key '{key}' was not found in kidGlossary.");
            return;
        }

        if (factPopupOverlay.activeSelf && string.Equals(activeFactKey, entry.EffectiveKey, StringComparison.OrdinalIgnoreCase))
        {
            HideFactPopup();
            return;
        }

        activeFactKey = entry.EffectiveKey;
        currentFactUrl = entry.sourceUrl;

        factPopupOverlay.transform.SetAsLastSibling();
        factPopupOverlay.SetActive(true);
        missionGuide?.SetOverlayActive(true);

        if (factPopupTermText != null)
        {
            string quoteTerm = !string.IsNullOrEmpty(entry.term) ? $"\"{entry.term}\"  —  " : string.Empty;
            factPopupTermText.text = $"{quoteTerm}{entry.EffectiveTitle}";
        }

        Texture2D tex = LoadNasaTexture(entry.imageFile);
        bool hasImage = tex != null && factPhotoWidget != null;
        if (factPhotoWidget != null)
        {
            factPhotoWidget.root.SetActive(hasImage);
            if (hasImage)
            {
                factPhotoWidget.rawImage.texture = tex;
                factPhotoWidget.fitter.aspectRatio = (float)tex.width / Mathf.Max(1, tex.height);
                string cap = !string.IsNullOrEmpty(entry.imageCaption) ? entry.imageCaption : entry.EffectiveTitle;
                factPhotoWidget.rawCaption = cap;
                factPhotoWidget.captionText.text = $"<color=#FFD166><b>ABOUT THIS IMAGE:</b></color> {cap}";
            }
        }

        if (factPopupBodyText != null)
        {
            string meaningSection = !string.IsNullOrEmpty(entry.termMeaning)
                ? $"<color=#00F5D4><b>WHAT \"{entry.term.ToUpperInvariant()}\" MEANS:</b></color>\n{entry.termMeaning}\n\n"
                : string.Empty;

            string historySection = $"<color=#FFD166><b>2018 NASA MISSION ARCHIVE:</b></color>\n{entry.simpleDefinition}";
            string sourceSection = BuildClickableSourceLine(entry.nasaSource, entry.nasaId, entry.sourceUrl);

            factPopupBodyText.text = $"{meaningSection}{historySection}{sourceSection}";
        }

        FitFactPopupToText(hasImage);
    }

    private void FitFactPopupToText(bool hasImage)
    {
        if (factCardRect == null)
        {
            return;
        }

        Vector2 tagPref = factTagText != null ? MeasureText(factTagText, factTagText.text, 0f) : new Vector2(360f, 22f);
        Vector2 termUn = factPopupTermText != null ? MeasureText(factPopupTermText, factPopupTermText.text, 0f) : new Vector2(560f, 34f);

        float rightMaxW = hasImage ? 760f : 1040f;
        Vector2 bodyPref = factPopupBodyText != null ? MeasureText(factPopupBodyText, factPopupBodyText.text, rightMaxW - 28f) : Vector2.zero;

        float bodyBoxW = Mathf.Clamp(Mathf.Ceil(bodyPref.x + 30f), 380f, rightMaxW);
        float bodyBoxH = Mathf.Clamp(Mathf.Ceil(bodyPref.y + 22f), 110f, 520f);

        float leftPhotoW = hasImage ? 480f : 0f;
        float contentSpanW = hasImage ? (leftPhotoW + 24f + bodyBoxW) : bodyBoxW;

        Vector2 termPref = factPopupTermText != null
            ? MeasureText(factPopupTermText, factPopupTermText.text, Mathf.Max(contentSpanW, 740f))
            : termUn;

        Vector2 closePref = factCloseButtonLabel != null ? MeasureText(factCloseButtonLabel, factCloseButtonLabel.text, 0f) : new Vector2(72f, 24f);
        float closeBtnH = Mathf.Clamp(Mathf.Ceil(closePref.y + 18f), 42f, 58f);

        float cardW = Mathf.Clamp(Mathf.Ceil(Mathf.Max(contentSpanW, Mathf.Max(tagPref.x, termPref.x)) + 64f), 740f, 1480f);
        float middleH = hasImage ? Mathf.Max(bodyBoxH, 400f) : bodyBoxH;
        float cardH = Mathf.Clamp(Mathf.Ceil(18f + tagPref.y + 6f + termPref.y + 14f + middleH + 16f + closeBtnH + 18f), 340f, 880f);

        SetCenteredAnchors(factCardRect, RefCanvasWidth, RefCanvasHeight, cardW, cardH);

        float topCursor = 1f - 18f / cardH;
        float sidePad = 32f / cardW;

        if (factTagText != null)
        {
            float th = Mathf.Ceil(tagPref.y + 4f) / cardH;
            SetAnchors(factTagText.rectTransform, sidePad, topCursor - th, 1f - sidePad, topCursor);
            topCursor -= th + 6f / cardH;
        }

        if (factPopupTermText != null)
        {
            float tmh = Mathf.Ceil(termPref.y + 4f) / cardH;
            SetAnchors(factPopupTermText.rectTransform, sidePad, topCursor - tmh, 1f - sidePad, topCursor);
            topCursor -= tmh + 14f / cardH;
        }

        float bottomCloseTop = (18f + closeBtnH + 14f) / cardH;
        float availMiddleH = (topCursor - bottomCloseTop) * cardH;

        float rightStartX = hasImage ? (32f + leftPhotoW + 24f) / cardW : sidePad;
        if (hasImage && factPhotoWidget != null)
        {
            float photoMaxX = (32f + leftPhotoW) / cardW;
            SetAnchors(factPhotoWidget.rootRect, sidePad, bottomCloseTop, photoMaxX, topCursor);
            FitBoxToCaption(factPhotoWidget, leftPhotoW, availMiddleH);
        }

        if (factBodyBoxRect != null)
        {
            float bWNorm = bodyBoxW / cardW;
            float bHNorm = Mathf.Min(bodyBoxH / cardH, topCursor - bottomCloseTop);
            float bMaxX = Mathf.Min(1f - sidePad, rightStartX + bWNorm);
            SetAnchors(factBodyBoxRect, rightStartX, topCursor - bHNorm, bMaxX, topCursor);
        }

        if (factCloseButton != null && factCloseButtonLabel != null)
        {
            FitButtonToText(factCloseButton, factCloseButtonLabel, 0.5f, 18f / cardH, cardW, cardH, 46f, 18f, 120f, 42f, 17f);
        }
    }

    private static string BuildClickableSourceLine(string nasaSource, string nasaId, string sourceUrl)
    {
        if (string.IsNullOrEmpty(nasaSource) && string.IsNullOrEmpty(sourceUrl))
        {
            return string.Empty;
        }

        string idNote = !string.IsNullOrEmpty(nasaId) ? $" [{nasaId}]" : string.Empty;
        string header = !string.IsNullOrEmpty(nasaSource) ? $"{nasaSource}{idNote}" : $"NASA Archive{idNote}";
        if (!string.IsNullOrEmpty(sourceUrl))
        {
            return $"\n\n<size=14><color=#FFD166><b>SOURCE:</b></color> <link=\"OPEN_URL\"><u><color=#A8B8CC>{header}</color></u></link>  " +
                   $"<link=\"OPEN_URL\"><u><color=#00F5D4>{sourceUrl}</color></u></link></size>";
        }
        return $"\n\n<size=14><color=#FFD166><b>SOURCE:</b></color> <color=#A8B8CC>{header}</color></size>";
    }

    public void HideFactPopup()
    {
        HidePhotoZoom();
        activeFactKey = null;
        currentFactUrl = null;
        if (factPopupOverlay != null)
        {
            factPopupOverlay.SetActive(false);
            missionGuide?.SetOverlayActive(false);
        }
    }

    private Texture2D LoadNasaTexture(string imageFile)
    {
        if (string.IsNullOrWhiteSpace(imageFile))
        {
            return null;
        }

        string cleanName = Path.GetFileNameWithoutExtension(imageFile.Trim());
        if (loadedTextures.TryGetValue(cleanName, out Texture2D cached) && cached != null)
        {
            return cached;
        }

        Texture2D resTex = Resources.Load<Texture2D>($"NasaImages/{cleanName}");
        if (resTex != null)
        {
            loadedTextures[cleanName] = resTex;
            return resTex;
        }

        string diskPath = Path.Combine(Application.dataPath, "Resources", "NasaImages", $"{cleanName}.jpg");
        if (File.Exists(diskPath))
        {
            byte[] bytes = File.ReadAllBytes(diskPath);
            Texture2D tex = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (ImageConversion.LoadImage(tex, bytes))
            {
                loadedTextures[cleanName] = tex;
                return tex;
            }
            Destroy(tex);
        }

        return null;
    }

    public void UpdateHud(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (solCounterText != null)
        {
            solCounterText.text = $"<b><color=#FFD166>SOL {state.sol} / {GameState.MaxSols}</color></b>\n<size=14><color=#A8B8CC>{state.realDate}</color></size>";
        }

        for (int i = 0; i < solPips.Count; i++)
        {
            int solNum = i + 1;
            if (solNum < state.sol)
            {
                solPips[i].color = Palette.FunkyMint;
            }
            else if (solNum == state.sol)
            {
                solPips[i].color = Palette.FunkyGold;
            }
            else
            {
                solPips[i].color = new Color(0.20f, 0.27f, 0.38f, 0.7f);
            }
        }

        if (dustTauText != null)
        {
            string skyStatus = state.sunlightBlockedPercent >= 90
                ? "<color=#FF5E7E>NOON BLACKOUT</color>"
                : state.sunlightBlockedPercent >= 50
                    ? "<color=#E87A3E>HEAVY DUST</color>"
                    : state.sunlightBlockedPercent >= 25
                        ? "<color=#FFD166>GROWING STORM</color>"
                        : "<color=#00F5D4>CLEARING SKY</color>";
            dustTauText.text = $"<b>{skyStatus}</b>";
        }

        Color powerColor = state.power <= 20
            ? Palette.DangerRed
            : state.power <= 40
                ? Palette.MarsAccent
                : Palette.FunkyGold;
        UpdateStatBar(powerBarFill, powerValueText, state.power, powerColor);

        Color healthColor = state.crewHealth <= 20
            ? Palette.DangerRed
            : state.crewHealth <= 50
                ? Palette.MarsAccent
                : Palette.FunkyMint;
        UpdateStatBar(integrityBarFill, integrityValueText, state.crewHealth, healthColor);

        Color blockColor = state.sunlightBlockedPercent >= 80
            ? Palette.FunkyCoral
            : state.sunlightBlockedPercent >= 50
                ? Palette.MarsAccent
                : Palette.FunkyCyan;
        UpdateStatBar(scienceBarFill, scienceValueText, state.sunlightBlockedPercent, blockColor);

        // Update Incited Sub-Indicators sized strictly to each pill's text
        float pPillsW = UpdatePill(fuelPill, state.fuelTag, state.fuelGood, state.fuelJustIncited)
            + UpdatePill(tempPill, state.tempTag, state.tempGood, state.tempJustIncited)
            + UpdatePill(reactorPill, state.reactorTag, state.reactorGood, state.reactorJustIncited);
        int pCount = (!string.IsNullOrEmpty(state.fuelTag) ? 1 : 0)
            + (!string.IsNullOrEmpty(state.tempTag) ? 1 : 0)
            + (!string.IsNullOrEmpty(state.reactorTag) ? 1 : 0);

        float hPillsW = UpdatePill(oxygenPill, state.oxygenTag, state.oxygenGood, state.oxygenJustIncited)
            + UpdatePill(foodPill, state.foodTag, state.foodGood, state.foodJustIncited)
            + UpdatePill(airPill, state.airTag, state.airGood, state.airJustIncited);
        int hCount = (!string.IsNullOrEmpty(state.oxygenTag) ? 1 : 0)
            + (!string.IsNullOrEmpty(state.foodTag) ? 1 : 0)
            + (!string.IsNullOrEmpty(state.airTag) ? 1 : 0);

        FitTopHudBoxesToContent(pCount, pPillsW, hCount, hPillsW);

        bool anyJustIncited = state.fuelJustIncited
            || state.tempJustIncited
            || state.reactorJustIncited
            || state.oxygenJustIncited
            || state.foodJustIncited
            || state.airJustIncited;
        if (anyJustIncited)
        {
            bool incitedGood = (!state.fuelJustIncited || state.fuelGood)
                && (!state.tempJustIncited || state.tempGood)
                && (!state.reactorJustIncited || state.reactorGood)
                && (!state.oxygenJustIncited || state.oxygenGood)
                && (!state.foodJustIncited || state.foodGood)
                && (!state.airJustIncited || state.airGood);
            missionAudio?.PlaySubsystemIncite(incitedGood);
        }

        missionAudio?.SyncEnvironment(state, decisionPanel != null && decisionPanel.activeSelf);
        baseView?.SyncState(state);
    }

    private void FitTopHudBoxesToContent(int powerPillCount, float powerPillsSumW, int healthPillCount, float healthPillsSumW)
    {
        if (topHudRect == null)
        {
            return;
        }

        float trayW = (topHudRect.anchorMax.x - topHudRect.anchorMin.x) * RefCanvasWidth;
        float trayH = (topHudRect.anchorMax.y - topHudRect.anchorMin.y) * RefCanvasHeight;

        // Measure Box 1: SolCounterBox
        Vector2 solPref = solCounterText != null ? MeasureText(solCounterText, solCounterText.text, 0f) : new Vector2(180f, 44f);
        float b1W = Mathf.Clamp(Mathf.Ceil(solPref.x + 36f), 205f, 320f);
        float b1H = Mathf.Clamp(Mathf.Ceil(solPref.y + 42f), 84f, trayH - 12f);

        // Measure Box 2: PowerBarGroup
        Vector2 pTitlePref = powerTitleText != null ? MeasureText(powerTitleText, powerTitleText.text, 0f) : new Vector2(120f, 20f);
        Vector2 pValPref = powerValueText != null ? MeasureText(powerValueText, powerValueText.text, 0f) : new Vector2(56f, 20f);
        float b2PillsW = powerPillCount > 0 ? powerPillsSumW + (powerPillCount - 1) * 6f + 26f : 0f;
        float b2W = Mathf.Clamp(Mathf.Ceil(Mathf.Max(pTitlePref.x + pValPref.x + 48f, Mathf.Max(290f, b2PillsW))), 290f, 520f);
        float b2H = powerPillCount > 0 ? Mathf.Clamp(124f, 108f, trayH - 10f) : 76f;

        // Measure Box 3: IntegrityBarGroup
        Vector2 hTitlePref = healthTitleText != null ? MeasureText(healthTitleText, healthTitleText.text, 0f) : new Vector2(120f, 20f);
        Vector2 hValPref = integrityValueText != null ? MeasureText(integrityValueText, integrityValueText.text, 0f) : new Vector2(56f, 20f);
        float b3PillsW = healthPillCount > 0 ? healthPillsSumW + (healthPillCount - 1) * 6f + 26f : 0f;
        float b3W = Mathf.Clamp(Mathf.Ceil(Mathf.Max(hTitlePref.x + hValPref.x + 48f, Mathf.Max(290f, b3PillsW))), 290f, 520f);
        float b3H = healthPillCount > 0 ? Mathf.Clamp(124f, 108f, trayH - 10f) : 76f;

        // Measure Box 4: DustTauBox & Archive Button
        Vector2 dustPref = dustTauText != null ? MeasureText(dustTauText, dustTauText.text, 0f) : new Vector2(160f, 24f);
        Vector2 sciTitlePref = scienceTitleText != null ? MeasureText(scienceTitleText, scienceTitleText.text, 0f) : new Vector2(140f, 16f);
        Vector2 btnPref = stormArchiveLabel != null ? MeasureText(stormArchiveLabel, stormArchiveLabel.text, 0f) : new Vector2(110f, 16f);
        float archBtnW = Mathf.Ceil(btnPref.x + 24f);
        float archBtnH = Mathf.Clamp(Mathf.Ceil(btnPref.y + 12f), 26f, 38f);
        float b4W = Mathf.Clamp(Mathf.Ceil(Mathf.Max(dustPref.x + 32f, Mathf.Max(sciTitlePref.x + 64f, archBtnW + 32f))), 240f, 350f);
        float b4H = Mathf.Clamp(Mathf.Ceil(dustPref.y + 42f + archBtnH + 22f), 110f, trayH - 10f);

        // Scale proportionally only if combined width exceeds trayW
        const float gap = 18f;
        float totalW = b1W + b2W + b3W + b4W + gap * 3f;
        if (totalW > trayW)
        {
            float scale = (trayW - gap * 3f) / (b1W + b2W + b3W + b4W);
            b1W *= scale;
            b2W *= scale;
            b3W *= scale;
            b4W *= scale;
            totalW = trayW;
        }

        float curX = (trayW - totalW) * 0.5f;
        const float topAnchor = 0.96f;

        if (solBoxRect != null)
        {
            float minY = topAnchor - b1H / trayH;
            SetAnchors(solBoxRect, curX / trayW, minY, (curX + b1W) / trayW, topAnchor);
            if (solCounterText != null)
            {
                SetAnchors(solCounterText.rectTransform, 0.05f, 0.25f, 0.95f, 0.95f);
            }
            curX += b1W + gap;
        }

        if (powerBoxRect != null)
        {
            float minY = topAnchor - b2H / trayH;
            SetAnchors(powerBoxRect, curX / trayW, minY, (curX + b2W) / trayW, topAnchor);
            LayoutBarBoxInternals(powerBoxRect, powerPillCount > 0);
            LayoutPillRow(powerPillRowRect, b2W, fuelPill, tempPill, reactorPill);
            curX += b2W + gap;
        }

        if (healthBoxRect != null)
        {
            float minY = topAnchor - b3H / trayH;
            SetAnchors(healthBoxRect, curX / trayW, minY, (curX + b3W) / trayW, topAnchor);
            LayoutBarBoxInternals(healthBoxRect, healthPillCount > 0);
            LayoutPillRow(healthPillRowRect, b3W, oxygenPill, foodPill, airPill);
            curX += b3W + gap;
        }

        if (dustBoxRect != null)
        {
            float minY = topAnchor - b4H / trayH;
            SetAnchors(dustBoxRect, curX / trayW, minY, (curX + b4W) / trayW, topAnchor);
            if (dustTauText != null)
            {
                SetAnchors(dustTauText.rectTransform, 0.05f, 0.68f, 0.95f, 0.95f);
            }
            if (scienceBoxRect != null)
            {
                SetAnchors(scienceBoxRect, 0.06f, 0.36f, 0.94f, 0.65f);
            }
            if (stormArchiveButton != null && stormArchiveLabel != null)
            {
                FitButtonToText(stormArchiveButton, stormArchiveLabel, 0.5f, 0.06f, b4W, b4H, 24f, 12f, 96f, 26f, 12f);
            }
        }
    }

    private static void LayoutBarBoxInternals(RectTransform boxRect, bool hasPills)
    {
        if (boxRect == null)
        {
            return;
        }

        Transform titleTf = boxRect.Find("Title");
        Transform valueTf = boxRect.Find("ValueText");
        Transform trackTf = boxRect.Find("BarTrack");

        if (hasPills)
        {
            if (titleTf != null)
            {
                SetAnchors(titleTf.GetComponent<RectTransform>(), 0.04f, 0.66f, 0.72f, 0.93f);
            }
            if (valueTf != null)
            {
                SetAnchors(valueTf.GetComponent<RectTransform>(), 0.72f, 0.66f, 0.96f, 0.93f);
            }
            if (trackTf != null)
            {
                SetAnchors(trackTf.GetComponent<RectTransform>(), 0.04f, 0.44f, 0.96f, 0.62f);
            }
        }
        else
        {
            if (titleTf != null)
            {
                SetAnchors(titleTf.GetComponent<RectTransform>(), 0.04f, 0.46f, 0.72f, 0.92f);
            }
            if (valueTf != null)
            {
                SetAnchors(valueTf.GetComponent<RectTransform>(), 0.72f, 0.46f, 0.96f, 0.92f);
            }
            if (trackTf != null)
            {
                SetAnchors(trackTf.GetComponent<RectTransform>(), 0.04f, 0.14f, 0.96f, 0.40f);
            }
        }
    }

    private static void LayoutPillRow(RectTransform rowRect, float boxWidth, params SubsystemPillWidget[] pills)
    {
        if (rowRect == null || pills == null || boxWidth <= 0f)
        {
            return;
        }

        int activeCount = 0;
        float sumWidths = 0f;
        for (int i = 0; i < pills.Length; i++)
        {
            SubsystemPillWidget pill = pills[i];
            if (pill != null && pill.root != null && pill.root.activeSelf && pill.rootRect != null)
            {
                activeCount++;
                sumWidths += Mathf.Max(48f, pill.fittedWidth);
            }
        }

        if (activeCount == 0)
        {
            rowRect.gameObject.SetActive(false);
            return;
        }

        rowRect.gameObject.SetActive(true);
        const float gap = 6f;
        float rowWidth = Mathf.Max(40f, boxWidth * 0.92f);
        float totalGap = gap * Mathf.Max(0, activeCount - 1);
        float scale = (sumWidths + totalGap > rowWidth && sumWidths > 0f)
            ? Mathf.Max(0.4f, (rowWidth - totalGap) / sumWidths)
            : 1f;

        float xPx = 0f;
        for (int i = 0; i < pills.Length; i++)
        {
            SubsystemPillWidget pill = pills[i];
            if (pill == null || pill.root == null || !pill.root.activeSelf || pill.rootRect == null)
            {
                continue;
            }

            float wPx = Mathf.Max(48f, pill.fittedWidth) * scale;
            float minX = Mathf.Clamp01(xPx / rowWidth);
            float maxX = Mathf.Clamp01((xPx + wPx) / rowWidth);
            SetAnchors(pill.rootRect, minX, 0f, maxX, 1f);

            if (pill.label != null)
            {
                SetAnchors(pill.label.rectTransform, 0.04f, 0.06f, 0.96f, 0.94f);
                pill.label.ForceMeshUpdate(true, true);
            }

            xPx += wPx + gap;
        }
    }

    private static float UpdatePill(SubsystemPillWidget pill, string tag, bool isGood, bool justIncited)
    {
        if (pill == null || pill.root == null)
        {
            return 0f;
        }

        bool visible = !string.IsNullOrEmpty(tag);
        pill.root.SetActive(visible);
        if (!visible)
        {
            pill.fittedWidth = 0f;
            return 0f;
        }

        Color accent = isGood ? Palette.FunkyMint : Palette.FunkyCoral;
        Color bg = isGood ? new Color(0.06f, 0.24f, 0.22f, 1f) : new Color(0.28f, 0.10f, 0.14f, 1f);

        if (pill.background != null)
        {
            pill.background.color = bg;
        }
        if (pill.outline != null)
        {
            pill.outline.effectColor = accent;
        }

        float fittedWidth = 80f;
        if (pill.label != null)
        {
            pill.label.color = isGood ? Palette.TextPrimary : Palette.FunkyGold;
            Vector2 pref = MeasureText(pill.label, tag, 0f);
            fittedWidth = Mathf.Clamp(Mathf.Ceil(pref.x + 20f), 72f, 190f);
        }
        pill.fittedWidth = fittedWidth;

        if (pill.effect != null)
        {
            pill.effect.SetColors(bg, accent, Palette.FunkyGold);
            if (justIncited)
            {
                pill.effect.TriggerPop();
            }
        }

        return fittedWidth;
    }

    private static void UpdateStatBar(Image fill, TMP_Text valueText, int value, Color barColor)
    {
        int clamped = Mathf.Clamp(value, 0, 100);
        if (fill != null)
        {
            RectTransform rt = fill.rectTransform;
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = new Vector2(clamped / 100f, 1f);
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            fill.color = barColor;
        }
        if (valueText != null)
        {
            valueText.text = $"{clamped}%";
        }
    }

    private float RebuildHorizontalChoiceCards(GameState state, List<Choice> availableChoices)
    {
        for (int i = 0; i < spawnedChoiceButtons.Count; i++)
        {
            if (spawnedChoiceButtons[i] != null)
            {
                Destroy(spawnedChoiceButtons[i]);
            }
        }
        spawnedChoiceButtons.Clear();

        if (choicesContainer == null || choiceButtonTemplate == null || availableChoices == null || availableChoices.Count == 0)
        {
            return 0.24f;
        }

        Color[] cardSurfaces = { Palette.CardSurfaceOption1, Palette.CardSurfaceOption2, Palette.CardSurfaceOption3 };
        Color[] cardAccents = { Palette.FunkyCyan, Palette.FunkyGold, Palette.FunkyCoral };

        int count = availableChoices.Count;
        var labels = new List<TMP_Text>(count);
        var layoutElements = new List<LayoutElement>(count);
        var cardWidths = new float[count];
        var cardHeights = new float[count];
        var plainLengths = new int[count];
        float sumPlainLen = 0f;
        for (int i = 0; i < count; i++)
        {
            Choice c = availableChoices[i];
            int len = (c.EffectiveLabel?.Length ?? 0)
                + (c.EffectiveDescription?.Length ?? 0)
                + (c.subsystemTag?.Length ?? 0);
            plainLengths[i] = len;
            sumPlainLen += len;
        }
        float avgLen = count > 0 ? sumPlainLen / count : 0f;

        for (int i = 0; i < count; i++)
        {
            Choice choice = availableChoices[i];
            Button spawnedButton = Instantiate(choiceButtonTemplate, choicesContainer);
            spawnedButton.gameObject.name = $"OptionCard_{i + 1}";
            spawnedButton.gameObject.SetActive(true);

            Color surfaceColor = cardSurfaces[i % cardSurfaces.Length];
            Color accentColor = cardAccents[i % cardAccents.Length];
            string accentHex = "#" + ColorUtility.ToHtmlStringRGB(accentColor);

            Image cardImg = spawnedButton.GetComponent<Image>();
            if (cardImg != null)
            {
                cardImg.color = surfaceColor;
            }

            Outline cardOutline = spawnedButton.GetComponent<Outline>();
            if (cardOutline == null)
            {
                cardOutline = spawnedButton.gameObject.AddComponent<Outline>();
            }
            cardOutline.effectColor = accentColor;
            cardOutline.effectDistance = new Vector2(2f, -2f);

            LayoutElement layoutElem = spawnedButton.GetComponent<LayoutElement>();
            if (layoutElem == null)
            {
                layoutElem = spawnedButton.gameObject.AddComponent<LayoutElement>();
            }

            AddFunkyEffect(spawnedButton.gameObject, surfaceColor, accentColor, 1.03f);

            TMP_Text label = spawnedButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                RectTransform labelRt = label.rectTransform;
                labelRt.anchorMin = Vector2.zero;
                labelRt.anchorMax = Vector2.one;
                labelRt.offsetMin = new Vector2(14f, 12f);
                labelRt.offsetMax = new Vector2(-14f, -12f);

                label.enableAutoSizing = false;
                label.fontSize = 16f;
                label.lineSpacing = -2f;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.alignment = TextAlignmentOptions.TopLeft;

                int effPower = state != null ? state.GetEffectivePowerDelta(choice) : choice.powerDelta;
                int effHealth = state != null ? state.GetEffectiveHealthDelta(choice) : choice.EffectiveCrewHealthDelta;
                bool hasBonus = state != null && state.HasChoiceBonus(choice);

                string pLabel = FormatPowerActionRichText(effPower);
                string hLabel = effHealth > 0
                    ? $"<color=#06D6A0>+{effHealth}% Health</color>"
                    : effHealth == 0
                        ? "<color=#A8B8CC>0% Health</color>"
                        : $"<color=#FF5E7E>{effHealth}% Health</color>";

                string bonusTag = hasBonus ? "  <b><color=#06D6A0>[BONUS!]</color></b>" : string.Empty;
                string systemHint = !string.IsNullOrEmpty(choice.subsystemTag)
                    ? $"\n<size=13.5><color=#FFD166>{choice.subsystemTag}</color></size>"
                    : string.Empty;

                label.text =
                    $"<size=13.5><b><color={accentHex}>OPTION {i + 1}</color></b></size>{bonusTag}\n" +
                    $"<size=19.5><b><color=#F2F5FA>{choice.EffectiveLabel}</color></b></size>\n" +
                    $"<size=15><color=#B8C7DA>{choice.EffectiveDescription}</color></size>\n" +
                    $"<size=14.5><b>{pLabel}  |  {hLabel}</b></size>{systemHint}";

                float diffFromAvg = plainLengths[i] - avgLen;
                float targetWrapW = Mathf.Clamp(210f + plainLengths[i] * 1.35f + diffFromAvg * 4.5f, 240f, 510f);

                Vector2 wrapped = MeasureText(label, label.text, targetWrapW);
                float finalTextW = Mathf.Clamp(Mathf.Min(targetWrapW, wrapped.x + 10f), 210f, 510f);
                Vector2 exact = MeasureText(label, label.text, finalTextW);

                cardWidths[i] = Mathf.Ceil(finalTextW + 30f);
                cardHeights[i] = Mathf.Ceil(exact.y + 32f);
            }

            Choice capturedChoice = choice;
            int optionIndex = i;
            spawnedButton.onClick.RemoveAllListeners();
            spawnedButton.onClick.AddListener(() =>
            {
                missionAudio?.PlayChoiceClick(capturedChoice.subsystemGood);
                baseView?.ClearChoiceHighlight();
                gameManager?.SelectChoice(capturedChoice);
            });

            ChoiceHoverRelay hoverRelay = spawnedButton.GetComponent<ChoiceHoverRelay>();
            if (hoverRelay == null)
            {
                hoverRelay = spawnedButton.gameObject.AddComponent<ChoiceHoverRelay>();
            }
            hoverRelay.Bind(
                () =>
                {
                    if (baseView != null)
                    {
                        baseView.HighlightForChoice(capturedChoice, optionIndex);
                    }
                    else
                    {
                        missionAudio?.PlayHoverTick();
                    }
                    missionGuide?.OnChoiceHover(capturedChoice);
                },
                () =>
                {
                    baseView?.ClearChoiceHighlight();
                    missionGuide?.OnChoiceUnhover();
                });

            labels.Add(label);
            layoutElements.Add(layoutElem);
            spawnedChoiceButtons.Add(spawnedButton.gameObject);
        }

        if (count >= 2)
        {
            float minW = cardWidths[0];
            float maxW = cardWidths[0];
            for (int i = 1; i < count; i++)
            {
                if (cardWidths[i] < minW) minW = cardWidths[i];
                if (cardWidths[i] > maxW) maxW = cardWidths[i];
            }

            if (maxW - minW < 22f)
            {
                int[] rankOrder = new int[count];
                for (int i = 0; i < count; i++) rankOrder[i] = i;
                System.Array.Sort(rankOrder, (a, b) =>
                {
                    int cmp = plainLengths[a].CompareTo(plainLengths[b]);
                    return cmp != 0 ? cmp : a.CompareTo(b);
                });

                float halfSpan = (count - 1) * 0.5f;
                for (int rank = 0; rank < count; rank++)
                {
                    int idx = rankOrder[rank];
                    float delta = (rank - halfSpan) * 26f;
                    float staggeredW = Mathf.Clamp(cardWidths[idx] + delta, 240f, 540f);
                    if (labels[idx] != null)
                    {
                        float textWrapW = Mathf.Max(210f, staggeredW - 30f);
                        Vector2 remeasured = MeasureText(labels[idx], labels[idx].text, textWrapW);
                        cardWidths[idx] = Mathf.Ceil(staggeredW);
                        cardHeights[idx] = Mathf.Ceil(remeasured.y + 32f);
                    }
                    else
                    {
                        cardWidths[idx] = Mathf.Ceil(staggeredW);
                    }
                }
            }
        }

        // Keep total row width within the safe display corridor while preserving per-card width differences
        const float maxRowWidth = 1660f;
        const float spacing = 16f;
        float sumWidths = 0f;
        for (int i = 0; i < count; i++)
        {
            sumWidths += cardWidths[i];
        }
        float totalWithGaps = sumWidths + spacing * Mathf.Max(0, count - 1);
        if (totalWithGaps > maxRowWidth && sumWidths > 0f)
        {
            float scale = (maxRowWidth - spacing * Mathf.Max(0, count - 1)) / sumWidths;
            for (int i = 0; i < count; i++)
            {
                if (labels[i] == null)
                {
                    continue;
                }
                float scaledTextW = Mathf.Max(190f, (cardWidths[i] - 30f) * scale);
                Vector2 remeasured = MeasureText(labels[i], labels[i].text, scaledTextW);
                cardWidths[i] = Mathf.Ceil(Mathf.Min(scaledTextW, remeasured.x + 10f) + 30f);
                cardHeights[i] = Mathf.Ceil(remeasured.y + 32f);
            }
        }

        float maxCardH = 0f;
        for (int i = 0; i < count; i++)
        {
            LayoutElement le = layoutElements[i];
            if (le != null)
            {
                le.minWidth = cardWidths[i];
                le.preferredWidth = cardWidths[i];
                le.flexibleWidth = 0f;
                le.minHeight = cardHeights[i];
                le.preferredHeight = cardHeights[i];
                le.flexibleHeight = 0f;
            }
            if (cardHeights[i] > maxCardH)
            {
                maxCardH = cardHeights[i];
            }
        }

        const float choiceMinY = 0.035f;
        float choiceMaxY = Mathf.Clamp(choiceMinY + (maxCardH + 8f) / RefCanvasHeight, 0.14f, 0.44f);
        SetAnchors(choicesContainer, 0.04f, choiceMinY, 0.96f, choiceMaxY);
        LayoutRebuilder.ForceRebuildLayoutImmediate(choicesContainer);
        return choiceMaxY;
    }

    private static Vector2 MeasureText(TMP_Text tmp, string text, float maxWidth = 0f)
    {
        if (tmp == null)
        {
            return Vector2.zero;
        }

        tmp.text = text ?? string.Empty;
        tmp.enableAutoSizing = false;
        tmp.ForceMeshUpdate(true, true);

        if (maxWidth > 0f)
        {
            Vector2 pref = tmp.GetPreferredValues(tmp.text, maxWidth, 0f);
            return new Vector2(Mathf.Min(pref.x, maxWidth), pref.y);
        }

        return tmp.GetPreferredValues(tmp.text);
    }

    private static void FitBoxToCaption(PhotoCaptionWidget widget, float widgetPixelWidth, float widgetPixelHeight)
    {
        if (widget == null || widget.captionText == null || widget.captionBoxRect == null || widget.frameRect == null)
        {
            return;
        }

        widget.captionText.enableAutoSizing = false;
        widget.captionText.fontSize = 13.5f;
        Vector2 pref = MeasureText(widget.captionText, widget.captionText.text, Mathf.Max(140f, widgetPixelWidth - 20f));

        float captionShare = Mathf.Clamp((pref.y + 18f) / Mathf.Max(120f, widgetPixelHeight), 0.14f, 0.52f);
        float wShare = Mathf.Clamp((pref.x + 24f) / Mathf.Max(160f, widgetPixelWidth), 0.45f, 1f);
        float minX = 0.5f - wShare * 0.5f;
        float maxX = 0.5f + wShare * 0.5f;

        SetAnchors(widget.captionBoxRect, minX, 0f, maxX, captionShare);
        SetAnchors(widget.frameRect, 0f, captionShare + 0.025f, 1f, 1f);
    }

    private static void FitButtonToText(
        Button btn,
        TMP_Text label,
        float centerX,
        float bottomY,
        float parentW,
        float parentH,
        float padX = 40f,
        float padY = 18f,
        float minW = 120f,
        float minH = 42f,
        float fixedFontSize = 18f)
    {
        if (btn == null || label == null || parentW <= 0f || parentH <= 0f)
        {
            return;
        }

        RectTransform labelRt = label.rectTransform;
        labelRt.anchorMin = Vector2.zero;
        labelRt.anchorMax = Vector2.one;
        labelRt.offsetMin = new Vector2(8f, 4f);
        labelRt.offsetMax = new Vector2(-8f, -4f);

        label.enableAutoSizing = false;
        label.fontSize = fixedFontSize;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        Vector2 pref = MeasureText(label, label.text, 0f);

        float btnW = Mathf.Clamp(Mathf.Ceil(pref.x + padX), minW, parentW * 0.94f);
        float btnH = Mathf.Clamp(Mathf.Ceil(pref.y + padY), minH, parentH * 0.45f);
        float normW = btnW / parentW;
        float normH = btnH / parentH;
        float minX = Mathf.Clamp(centerX - normW * 0.5f, 0.02f, 0.98f - normW);
        float maxY = Mathf.Clamp(bottomY + normH, bottomY + 0.04f, 0.98f);

        SetAnchors(btn.GetComponent<RectTransform>(), minX, bottomY, minX + normW, maxY);
    }

    private static void SetCenteredAnchors(RectTransform rt, float parentW, float parentH, float width, float height)
    {
        if (rt == null || parentW <= 0f || parentH <= 0f)
        {
            return;
        }
        float hw = Mathf.Clamp((width / parentW) * 0.5f, 0.08f, 0.48f);
        float hh = Mathf.Clamp((height / parentH) * 0.5f, 0.08f, 0.48f);
        SetAnchors(rt, 0.5f - hw, 0.5f - hh, 0.5f + hw, 0.5f + hh);
    }

    private void ToggleComparisonMode(bool showComparison)
    {
        if (outcomeBodyBoxRect != null)
        {
            outcomeBodyBoxRect.gameObject.SetActive(!showComparison);
        }
        if (outcomeBodyText != null)
        {
            outcomeBodyText.gameObject.SetActive(!showComparison);
        }
        if (nasaScienceBox != null)
        {
            nasaScienceBox.SetActive(!showComparison);
        }
        if (outcomePhotoWidget != null)
        {
            outcomePhotoWidget.root.SetActive(!showComparison);
        }
        if (comparisonContainer != null)
        {
            comparisonContainer.gameObject.SetActive(showComparison);
        }
        if (compPrevButton != null)
        {
            compPrevButton.gameObject.SetActive(showComparison);
        }
        if (compNextButton != null)
        {
            compNextButton.gameObject.SetActive(showComparison);
        }
        missionGuide?.SetVisible(!showComparison);
    }

    private static string FormatPowerActionText(int powerDelta)
    {
        if (powerDelta > 0)
        {
            return $"+{powerDelta}% Generated";
        }
        if (powerDelta == 0)
        {
            return "0% Conserved";
        }
        return $"{powerDelta}% Used";
    }

    private static string FormatPowerActionRichText(int powerDelta)
    {
        if (powerDelta > 0)
        {
            return $"<color=#FFD166>+{powerDelta}% Elec Generated</color>";
        }
        if (powerDelta == 0)
        {
            return "<color=#00F5D4>0% Elec Conserved</color>";
        }
        return $"<color=#E87A3E>{powerDelta}% Elec Used</color>";
    }

    private static Color ResolveEndingHeaderColor(string endingId)
    {
        switch (endingId)
        {
            case "ENDING_PERFECT":
                return Palette.FunkyMint;
            case "ENDING_OPPORTUNITY_FATE":
                return Palette.DangerRed;
            case "ENDING_LOW_POWER":
            case "ENDING_LOW_HEALTH":
                return Palette.FunkyGold;
            default:
                return Palette.MarsAccent;
        }
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

    private static void StyleHudCard(GameObject cardObj, Color fillColor, Color borderColor, float outlineDist = 1.8f)
    {
        if (cardObj == null)
        {
            return;
        }

        Image bg = cardObj.GetComponent<Image>();
        if (bg == null)
        {
            bg = cardObj.AddComponent<Image>();
        }
        bg.color = fillColor;

        Outline outline = cardObj.GetComponent<Outline>();
        if (outline == null)
        {
            outline = cardObj.AddComponent<Outline>();
        }
        outline.effectColor = borderColor;
        outline.effectDistance = new Vector2(outlineDist, -outlineDist);
    }

    private static FunkyCardEffect AddFunkyEffect(
        GameObject target,
        Color baseColor,
        Color accentColor,
        float hoverScale = 1.035f)
    {
        if (target == null)
        {
            return null;
        }

        FunkyCardEffect effect = target.GetComponent<FunkyCardEffect>();
        if (effect == null)
        {
            effect = target.AddComponent<FunkyCardEffect>();
        }
        effect.Configure(baseColor, accentColor, hoverScale);
        return effect;
    }

    private void SetActivePanel(GameObject targetPanel)
    {
        HidePhotoZoom();
        if (titlePanel != null)
        {
            titlePanel.SetActive(titlePanel == targetPanel);
        }
        if (decisionPanel != null)
        {
            decisionPanel.SetActive(decisionPanel == targetPanel);
        }
        if (outcomePanel != null)
        {
            outcomePanel.SetActive(outcomePanel == targetPanel);
        }
        missionAudio?.SyncEnvironment(
            gameManager != null ? gameManager.State : null,
            decisionPanel != null && targetPanel == decisionPanel);
    }

    private void HandleStartOrBriefingClicked()
    {
        gameManager?.OnTitleOrBriefingAdvance();
    }

    private void HandleNextClicked()
    {
        gameManager?.ProceedFromOutcome();
    }

    private sealed class SubsystemPillWidget
    {
        public GameObject root;
        public RectTransform rootRect;
        public Image background;
        public Outline outline;
        public TMP_Text label;
        public FunkyCardEffect effect;
        public float fittedWidth;
    }

    public sealed class PhotoCaptionWidget
    {
        public GameObject root;
        public RectTransform rootRect;
        public RectTransform frameRect;
        public RectTransform captionBoxRect;
        public Button zoomButton;
        public RawImage rawImage;
        public AspectRatioFitter fitter;
        public TMP_Text captionText;
        public string rawCaption;
    }
}

/// <summary>
/// Interactive hover/press bounce and neon outline glow for funky Mars Mission cards and pills.
/// </summary>
public class FunkyCardEffect : MonoBehaviour,
    IPointerEnterHandler,
    IPointerExitHandler,
    IPointerDownHandler,
    IPointerUpHandler
{
    private float targetScale = 1f;
    private float hoverScaleAmount = 1.035f;
    private Color baseBgColor;
    private Color hoverBgColor;
    private Color baseBorderColor;
    private Image bgImage;
    private Outline borderOutline;
    private Coroutine popRoutine;

    public void Configure(Color baseFill, Color accentBorder, float hoverScale = 1.035f)
    {
        hoverScaleAmount = hoverScale;
        bgImage = GetComponent<Image>();
        borderOutline = GetComponent<Outline>();
        SetColors(baseFill, accentBorder, Palette.FunkyGold);
    }

    public void SetColors(Color baseFill, Color accentBorder, Color brightAccent)
    {
        baseBgColor = baseFill;
        hoverBgColor = Color.Lerp(baseFill, accentBorder, 0.16f);
        baseBorderColor = accentBorder;

        if (bgImage != null)
        {
            bgImage.color = baseBgColor;
        }
        if (borderOutline != null)
        {
            borderOutline.effectColor = baseBorderColor;
        }
    }

    public void TriggerPop()
    {
        if (!gameObject.activeInHierarchy)
        {
            return;
        }
        if (popRoutine != null)
        {
            StopCoroutine(popRoutine);
        }
        popRoutine = StartCoroutine(PopCoroutine());
    }

    private IEnumerator PopCoroutine()
    {
        transform.localScale = new Vector3(1.25f, 1.25f, 1f);
        float elapsed = 0f;
        const float duration = 0.25f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float s = Mathf.Lerp(1.25f, targetScale, t);
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
        popRoutine = null;
    }

    private void Update()
    {
        if (popRoutine != null)
        {
            return;
        }
        float current = transform.localScale.x;
        if (!Mathf.Approximately(current, targetScale))
        {
            float next = Mathf.Lerp(current, targetScale, Time.unscaledDeltaTime * 14f);
            transform.localScale = new Vector3(next, next, 1f);
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        targetScale = hoverScaleAmount;
        if (bgImage != null)
        {
            bgImage.color = hoverBgColor;
        }
        if (borderOutline != null)
        {
            borderOutline.effectColor = Palette.TextPrimary;
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        targetScale = 1f;
        if (bgImage != null)
        {
            bgImage.color = baseBgColor;
        }
        if (borderOutline != null)
        {
            borderOutline.effectColor = baseBorderColor;
        }
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        targetScale = 0.97f;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        targetScale = hoverScaleAmount;
    }
}

/// <summary>
/// Detects clicks on TMP rich-text &lt;link="id"&gt; spans inside briefing, dilemma, outcome, and comparison text.
/// </summary>
public class TmpLinkClickHandler : MonoBehaviour, IPointerClickHandler
{
    private TMP_Text targetText;
    private Action<string> onLinkClicked;

    public void Bind(TMP_Text textComponent, Action<string> callback)
    {
        targetText = textComponent;
        onLinkClicked = callback;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (targetText == null || eventData == null || onLinkClicked == null)
        {
            return;
        }

        Camera eventCamera = eventData.pressEventCamera != null ? eventData.pressEventCamera : Camera.main;
        int linkIndex = TMP_TextUtilities.FindIntersectingLink(targetText, eventData.position, eventCamera);
        if (linkIndex < 0)
        {
            linkIndex = TMP_TextUtilities.FindIntersectingLink(targetText, eventData.position, null);
        }
        if (linkIndex < 0)
        {
            return;
        }

        TMP_LinkInfo linkInfo = targetText.textInfo.linkInfo[linkIndex];
        string linkId = linkInfo.GetLinkID();
        if (!string.IsNullOrEmpty(linkId))
        {
            onLinkClicked.Invoke(linkId);
        }
    }
}
