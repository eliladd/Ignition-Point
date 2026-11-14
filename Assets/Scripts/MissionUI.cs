using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Manages the Ares Base Mission Control uGUI across Title, 3-screen Briefing,
/// 10-Sol Decision Console, Outcome Popups, Inline Clickable 2018 NASA Archive Facts,
/// Ending Summary, and Real NASA Science Comparison.
/// </summary>
public class MissionUI : MonoBehaviour
{
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
    private readonly List<GameObject> spawnedComparisonCards = new List<GameObject>();

    // Runtime-located or runtime-built UI elements (avoids modifying scene YAML).
    private TMP_Text badgeLabelText;
    private TMP_Text startButtonLabelText;
    private Image consoleBackgroundImage;
    private Image titlePanelBackdrop;
    private Image decisionPanelBackdrop;
    private Image outcomePanelBackdrop;

    private RectTransform archiveTipBar;
    private GameObject factPopupOverlay;
    private TMP_Text factPopupTermText;
    private TMP_Text factPopupBodyText;
    private string activeFactKey;

    private RectTransform comparisonCardsContainer;
    private TMP_FontAsset uiFont;

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
        ConfigureExistingLayout();
        BuildArchiveTipBar();
        BuildFactPopupOverlay();
        BuildComparisonContainer();

        AttachLinkHandler(titlePremiseText);
        AttachLinkHandler(dilemmaPromptText);
        AttachLinkHandler(outcomeBodyText);

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(HandleStartOrBriefingClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(HandleNextClicked);
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
            Transform badgeTf = titlePanel.transform.Find("TitleCard/MissionBadge/BadgeLabel");
            if (badgeTf != null)
            {
                badgeLabelText = badgeTf.GetComponent<TMP_Text>();
            }
        }

        if (startButton != null)
        {
            startButtonLabelText = startButton.GetComponentInChildren<TMP_Text>(true);
        }

        if (decisionPanel != null)
        {
            decisionPanelBackdrop = decisionPanel.GetComponent<Image>();
        }

        if (outcomePanel != null)
        {
            outcomePanelBackdrop = outcomePanel.GetComponent<Image>();
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

        // Rename TopHUD meters cleanly from placeholder names to Ares Base telemetry.
        SetMeterLabel(powerBarFill, "POWER RESERVES");
        SetMeterLabel(integrityBarFill, "CREW HEALTH");
        SetMeterLabel(scienceBarFill, "SUNLIGHT BLOCKED");
    }

    private static void SetMeterLabel(Image barFill, string newLabel)
    {
        if (barFill == null || barFill.transform.parent == null || barFill.transform.parent.parent == null)
        {
            return;
        }

        Transform titleTf = barFill.transform.parent.parent.Find("Title");
        if (titleTf != null)
        {
            TMP_Text label = titleTf.GetComponent<TMP_Text>();
            if (label != null)
            {
                label.text = newLabel;
            }
        }
    }

    private void ConfigureExistingLayout()
    {
        if (titleHeaderText != null)
        {
            RectTransform rt = titleHeaderText.rectTransform;
            rt.anchorMin = new Vector2(0.08f, 0.64f);
            rt.anchorMax = new Vector2(0.92f, 0.80f);
            titleHeaderText.enableAutoSizing = true;
            titleHeaderText.fontSizeMin = 28f;
            titleHeaderText.fontSizeMax = 48f;
            titleHeaderText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (titlePremiseText != null)
        {
            RectTransform rt = titlePremiseText.rectTransform;
            rt.anchorMin = new Vector2(0.08f, 0.22f);
            rt.anchorMax = new Vector2(0.92f, 0.62f);
            titlePremiseText.enableAutoSizing = true;
            titlePremiseText.fontSizeMin = 17f;
            titlePremiseText.fontSizeMax = 25f;
            titlePremiseText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (dilemmaPromptText != null)
        {
            dilemmaPromptText.enableAutoSizing = true;
            dilemmaPromptText.fontSizeMin = 15f;
            dilemmaPromptText.fontSizeMax = 24f;
            dilemmaPromptText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (solCounterText != null)
        {
            solCounterText.enableAutoSizing = true;
            solCounterText.fontSizeMin = 15f;
            solCounterText.fontSizeMax = 25f;
            solCounterText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (dustTauText != null)
        {
            dustTauText.enableAutoSizing = true;
            dustTauText.fontSizeMin = 15f;
            dustTauText.fontSizeMax = 22f;
            dustTauText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (choicesContainer != null)
        {
            // Expand ChoicesContainer cleanly while leaving room for the compact archive tip strip.
            choicesContainer.anchorMin = new Vector2(0.06f, 0.095f);
            choicesContainer.anchorMax = new Vector2(0.94f, 0.475f);
            choicesContainer.offsetMin = Vector2.zero;
            choicesContainer.offsetMax = Vector2.zero;
        }

        if (outcomeBodyText != null)
        {
            outcomeBodyText.enableAutoSizing = true;
            outcomeBodyText.fontSizeMin = 17f;
            outcomeBodyText.fontSizeMax = 27f;
            outcomeBodyText.textWrappingMode = TextWrappingModes.Normal;
        }

        if (nasaNoteText != null)
        {
            nasaNoteText.enableAutoSizing = true;
            nasaNoteText.fontSizeMin = 16f;
            nasaNoteText.fontSizeMax = 22f;
            nasaNoteText.textWrappingMode = TextWrappingModes.Normal;
        }
    }

    private void AttachLinkHandler(TMP_Text textComponent)
    {
        if (textComponent == null)
        {
            return;
        }

        // RaycastTarget must be enabled so IPointerClickHandler detects clicks on inline <link> tags.
        textComponent.raycastTarget = true;

        TmpLinkClickHandler handler = textComponent.GetComponent<TmpLinkClickHandler>();
        if (handler == null)
        {
            handler = textComponent.gameObject.AddComponent<TmpLinkClickHandler>();
        }
        handler.Bind(textComponent, ShowFactPopupByKey);
    }

    private void BuildArchiveTipBar()
    {
        if (decisionPanel == null)
        {
            return;
        }

        GameObject tipObj = new GameObject("ArchiveTipBar", typeof(RectTransform), typeof(Image));
        tipObj.transform.SetParent(decisionPanel.transform, false);
        archiveTipBar = tipObj.GetComponent<RectTransform>();
        archiveTipBar.anchorMin = new Vector2(0.06f, 0.025f);
        archiveTipBar.anchorMax = new Vector2(0.94f, 0.08f);
        archiveTipBar.offsetMin = Vector2.zero;
        archiveTipBar.offsetMax = Vector2.zero;

        Image tipBg = tipObj.GetComponent<Image>();
        tipBg.color = new Color(0.09f, 0.13f, 0.19f, 0.90f);
        tipBg.raycastTarget = false;

        GameObject txtObj = new GameObject("TipLabel", typeof(RectTransform), typeof(TextMeshProUGUI));
        txtObj.transform.SetParent(archiveTipBar, false);
        RectTransform txtRt = txtObj.GetComponent<RectTransform>();
        txtRt.anchorMin = Vector2.zero;
        txtRt.anchorMax = Vector2.one;
        txtRt.offsetMin = new Vector2(18f, 4f);
        txtRt.offsetMax = new Vector2(-18f, -4f);

        TextMeshProUGUI tipText = txtObj.GetComponent<TextMeshProUGUI>();
        tipText.font = uiFont;
        tipText.enableAutoSizing = true;
        tipText.fontSizeMin = 13f;
        tipText.fontSizeMax = 18f;
        tipText.color = Palette.TextSecondary;
        tipText.alignment = TextAlignmentOptions.Center;
        tipText.raycastTarget = false;
        tipText.text =
            "<color=#5CE1E6><b>2018 NASA ARCHIVE TIP:</b></color> " +
            "Click any <b><color=#5CE1E6><u>highlighted blue word or sentence</u></color></b> in the story above to see what really happened in 2018!";
    }

    private void BuildFactPopupOverlay()
    {
        Transform canvasRoot = titlePanel != null && titlePanel.transform.parent != null
            ? titlePanel.transform.parent
            : decisionPanel != null ? decisionPanel.transform : transform;

        factPopupOverlay = new GameObject("NasaFactPopupOverlay", typeof(RectTransform), typeof(Image), typeof(Button));
        factPopupOverlay.transform.SetParent(canvasRoot, false);
        RectTransform overlayRt = factPopupOverlay.GetComponent<RectTransform>();
        overlayRt.anchorMin = Vector2.zero;
        overlayRt.anchorMax = Vector2.one;
        overlayRt.offsetMin = Vector2.zero;
        overlayRt.offsetMax = Vector2.zero;

        Image overlayDim = factPopupOverlay.GetComponent<Image>();
        overlayDim.color = new Color(0.03f, 0.05f, 0.08f, 0.80f);

        Button dismissBgBtn = factPopupOverlay.GetComponent<Button>();
        dismissBgBtn.transition = Selectable.Transition.None;
        dismissBgBtn.onClick.AddListener(HideFactPopup);

        GameObject cardObj = new GameObject("NasaFactCard", typeof(RectTransform), typeof(Image));
        cardObj.transform.SetParent(factPopupOverlay.transform, false);
        RectTransform cardRt = cardObj.GetComponent<RectTransform>();
        cardRt.anchorMin = new Vector2(0.20f, 0.25f);
        cardRt.anchorMax = new Vector2(0.80f, 0.75f);
        cardRt.offsetMin = Vector2.zero;
        cardRt.offsetMax = Vector2.zero;
        Image cardBg = cardObj.GetComponent<Image>();
        cardBg.color = Palette.PanelSurface;
        cardBg.raycastTarget = true;

        GameObject tagObj = new GameObject("ArchiveBadge", typeof(RectTransform), typeof(TextMeshProUGUI));
        tagObj.transform.SetParent(cardRt, false);
        RectTransform tagRt = tagObj.GetComponent<RectTransform>();
        tagRt.anchorMin = new Vector2(0.06f, 0.84f);
        tagRt.anchorMax = new Vector2(0.94f, 0.94f);
        tagRt.offsetMin = Vector2.zero;
        tagRt.offsetMax = Vector2.zero;
        TextMeshProUGUI tagText = tagObj.GetComponent<TextMeshProUGUI>();
        tagText.font = uiFont;
        tagText.text = "2018 NASA HISTORICAL ARCHIVE";
        tagText.fontSize = 18f;
        tagText.fontStyle = FontStyles.Bold;
        tagText.color = Palette.PowerGold;
        tagText.alignment = TextAlignmentOptions.MidlineLeft;
        tagText.raycastTarget = false;

        GameObject termObj = new GameObject("FactHeadline", typeof(RectTransform), typeof(TextMeshProUGUI));
        termObj.transform.SetParent(cardRt, false);
        RectTransform termRt = termObj.GetComponent<RectTransform>();
        termRt.anchorMin = new Vector2(0.06f, 0.68f);
        termRt.anchorMax = new Vector2(0.94f, 0.83f);
        termRt.offsetMin = Vector2.zero;
        termRt.offsetMax = Vector2.zero;
        factPopupTermText = termObj.GetComponent<TextMeshProUGUI>();
        factPopupTermText.font = uiFont;
        factPopupTermText.enableAutoSizing = true;
        factPopupTermText.fontSizeMin = 22f;
        factPopupTermText.fontSizeMax = 32f;
        factPopupTermText.fontStyle = FontStyles.Bold;
        factPopupTermText.color = Palette.ScienceCyan;
        factPopupTermText.alignment = TextAlignmentOptions.MidlineLeft;
        factPopupTermText.raycastTarget = false;

        GameObject defObj = new GameObject("FactBody", typeof(RectTransform), typeof(TextMeshProUGUI));
        defObj.transform.SetParent(cardRt, false);
        RectTransform defRt = defObj.GetComponent<RectTransform>();
        defRt.anchorMin = new Vector2(0.06f, 0.24f);
        defRt.anchorMax = new Vector2(0.94f, 0.66f);
        defRt.offsetMin = Vector2.zero;
        defRt.offsetMax = Vector2.zero;
        factPopupBodyText = defObj.GetComponent<TextMeshProUGUI>();
        factPopupBodyText.font = uiFont;
        factPopupBodyText.enableAutoSizing = true;
        factPopupBodyText.fontSizeMin = 18f;
        factPopupBodyText.fontSizeMax = 25f;
        factPopupBodyText.color = Palette.TextPrimary;
        factPopupBodyText.textWrappingMode = TextWrappingModes.Normal;
        factPopupBodyText.alignment = TextAlignmentOptions.TopLeft;
        factPopupBodyText.raycastTarget = false;

        GameObject closeBtnObj = new GameObject("CloseFactButton", typeof(RectTransform), typeof(Image), typeof(Button));
        closeBtnObj.transform.SetParent(cardRt, false);
        RectTransform closeRt = closeBtnObj.GetComponent<RectTransform>();
        closeRt.anchorMin = new Vector2(0.34f, 0.06f);
        closeRt.anchorMax = new Vector2(0.66f, 0.19f);
        closeRt.offsetMin = Vector2.zero;
        closeRt.offsetMax = Vector2.zero;
        Image closeImg = closeBtnObj.GetComponent<Image>();
        closeImg.color = Palette.MarsAccent;
        Button closeBtn = closeBtnObj.GetComponent<Button>();
        closeBtn.targetGraphic = closeImg;
        closeBtn.onClick.AddListener(HideFactPopup);

        GameObject closeTxtObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        closeTxtObj.transform.SetParent(closeRt, false);
        RectTransform closeTxtRt = closeTxtObj.GetComponent<RectTransform>();
        closeTxtRt.anchorMin = Vector2.zero;
        closeTxtRt.anchorMax = Vector2.one;
        closeTxtRt.offsetMin = Vector2.zero;
        closeTxtRt.offsetMax = Vector2.zero;
        TextMeshProUGUI closeTxt = closeTxtObj.GetComponent<TextMeshProUGUI>();
        closeTxt.font = uiFont;
        closeTxt.text = "BACK TO MISSION";
        closeTxt.fontSize = 20f;
        closeTxt.fontStyle = FontStyles.Bold;
        closeTxt.color = Palette.TextPrimary;
        closeTxt.alignment = TextAlignmentOptions.Center;
        closeTxt.raycastTarget = false;

        factPopupOverlay.SetActive(false);
    }

    private void BuildComparisonContainer()
    {
        if (outcomePanel == null)
        {
            return;
        }

        Transform outcomeCard = outcomePanel.transform.Find("OutcomeCard");
        Transform parentTarget = outcomeCard != null ? outcomeCard : outcomePanel.transform;

        GameObject containerObj = new GameObject("ComparisonCardsContainer", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        containerObj.transform.SetParent(parentTarget, false);
        comparisonCardsContainer = containerObj.GetComponent<RectTransform>();
        comparisonCardsContainer.anchorMin = new Vector2(0.04f, 0.17f);
        comparisonCardsContainer.anchorMax = new Vector2(0.96f, 0.77f);
        comparisonCardsContainer.offsetMin = Vector2.zero;
        comparisonCardsContainer.offsetMax = Vector2.zero;

        HorizontalLayoutGroup layout = containerObj.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 18f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = true;

        containerObj.SetActive(false);
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
        backdropColor.a = 1f;

        if (consoleBackgroundImage != null)
        {
            consoleBackgroundImage.color = backdropColor;
        }
        if (titlePanelBackdrop != null)
        {
            titlePanelBackdrop.color = backdropColor;
        }
        if (decisionPanelBackdrop != null)
        {
            decisionPanelBackdrop.color = backdropColor;
        }
        if (outcomePanelBackdrop != null)
        {
            outcomePanelBackdrop.color = backdropColor;
        }
    }

    public void ShowTitlePanel(string customTitle = null)
    {
        HideFactPopup();
        SetActivePanel(titlePanel);

        if (badgeLabelText != null)
        {
            badgeLabelText.text = "ARES BASE 2038  |  INSPIRED BY NASA'S 2018 MARS DUST STORM";
        }

        if (titleHeaderText != null)
        {
            titleHeaderText.text = !string.IsNullOrEmpty(customTitle)
                ? customTitle
                : "Red Planet Blackout: Ares Base 2038";
        }

        if (titlePremiseText != null)
        {
            titlePremiseText.text =
                "In 2018, a planet-wide dust storm tested NASA's rovers on Mars—shutting down solar-powered Opportunity while nuclear-powered Curiosity kept working.\n\n" +
                "Now jump ahead to the year 2038, when NASA sends human astronauts to live at Ares Base. Across 10 Martian Sols, there is always a compromise—and every decision you make changes what happens next!\n\n" +
                "Press Start to review the real 2018 NASA event and prepare your future crew.";
            titlePremiseText.alignment = TextAlignmentOptions.Center;
        }

        if (startButtonLabelText != null)
        {
            startButtonLabelText.text = "START MISSION BRIEFING";
        }
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

        if (screen == null)
        {
            return;
        }

        if (badgeLabelText != null)
        {
            string prefix = !string.IsNullOrEmpty(gameTitle) ? gameTitle : "ARES BASE BRIEFING";
            badgeLabelText.text = $"{prefix.ToUpperInvariant()}  —  BRIEFING {screenIndex + 1} OF {totalScreens}";
        }

        if (titleHeaderText != null)
        {
            titleHeaderText.text = screen.headline;
        }

        if (titlePremiseText != null)
        {
            string sourceLine = !string.IsNullOrEmpty(screen.nasaSource)
                ? $"\n\n<size=18><color=#2CC5E8>NASA SOURCE: {screen.nasaSource}</color></size>"
                : string.Empty;
            string linkHint = "\n<size=16><color=#A8B8CC>Tip: Click any bold blue highlighted phrase above to read more about 2018.</color></size>";

            titlePremiseText.alignment = TextAlignmentOptions.Left;
            titlePremiseText.text = $"{screen.bodyText}{sourceLine}{linkHint}";
        }

        if (startButtonLabelText != null)
        {
            bool isLast = screenIndex >= totalScreens - 1;
            startButtonLabelText.text = isLast
                ? "LAUNCH ARES BASE 2038 (SOL 1)"
                : "NEXT BRIEFING  >";
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
                ? $"  <size=21><color=#2CC5E8>[{node.realDate}]</color></size>"
                : string.Empty;
            nodeTitleText.text = node != null ? $"{node.title}{dateTag}" : "ARES BASE DECISION";
        }

        if (dilemmaPromptText != null && node != null)
        {
            string consequenceBlock = !string.IsNullOrEmpty(state?.activeConsequenceText)
                ? $"\n\n<size=19><color=#F5B841><b>PAST DECISION IMPACT:</b> {state.activeConsequenceText}</color></size>"
                : string.Empty;

            string banner = !string.IsNullOrEmpty(node.nasaFactBanner)
                ? $"\n\n<size=19><color=#2CC5E8>{node.nasaFactBanner}</color></size>"
                : string.Empty;

            dilemmaPromptText.text = $"{node.EffectiveStoryText}{consequenceBlock}{banner}";
        }

        RebuildChoiceButtons(availableChoices);
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

        if (outcomeHeaderText != null)
        {
            outcomeHeaderText.color = Palette.TextPrimary;
            string choiceName = chosenChoice != null ? chosenChoice.EffectiveLabel : "Action Taken";
            outcomeHeaderText.text = $"SOL {state.sol} RESULT — {choiceName}";
        }

        if (outcomeTelemetryText != null)
        {
            string powerDeltaText = chosenChoice != null ? GameState.FormatSignedDelta(chosenChoice.powerDelta) : "0%";
            string healthDeltaText = chosenChoice != null ? GameState.FormatSignedDelta(chosenChoice.EffectiveCrewHealthDelta) : "0%";
            outcomeTelemetryText.text =
                $"POWER: {state.power}% ({powerDeltaText})   |   " +
                $"CREW HEALTH: {state.crewHealth}% ({healthDeltaText})   |   " +
                $"SUNLIGHT BLOCKED: {state.sunlightBlockedPercent}%";
        }

        if (outcomeBodyText != null)
        {
            outcomeBodyText.text = chosenChoice != null ? chosenChoice.EffectiveOutcome : string.Empty;
        }

        bool hasNasaFact = sourceNode != null && !string.IsNullOrEmpty(sourceNode.nasaFactBanner);
        if (nasaScienceBox != null)
        {
            nasaScienceBox.SetActive(hasNasaFact);
        }
        if (hasNasaFact)
        {
            if (nasaScienceTitleText != null)
            {
                nasaScienceTitleText.text = "2018 NASA REFERENCE TELEMETRY";
            }
            if (nasaNoteText != null)
            {
                nasaNoteText.text = sourceNode.nasaFactBanner;
            }
        }

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = isFinalSolOutcome
                ? "VIEW FINAL MISSION OUTCOME  >"
                : $"ADVANCE TO SOL {Mathf.Min(state.sol + 1, GameState.MaxSols)}  >";
        }
    }

    public void ShowEndingPanel(GameState state, StoryEnding ending)
    {
        HideFactPopup();
        SetActivePanel(outcomePanel);
        ToggleComparisonMode(false);
        UpdateHud(state);

        if (outcomeHeaderText != null)
        {
            outcomeHeaderText.color = ResolveEndingHeaderColor(ending?.endingId);
            outcomeHeaderText.text = ending != null ? ending.headline : $"Sol {GameState.MaxSols}: Mission Complete";
        }

        if (outcomeTelemetryText != null)
        {
            string dateText = !string.IsNullOrEmpty(ending?.realDate) ? ending.realDate : "July 23, 2038";
            outcomeTelemetryText.text =
                $"{dateText.ToUpperInvariant()}   |   FINAL POWER: {state.power}%   |   FINAL CREW HEALTH: {state.crewHealth}%";
        }

        if (outcomeBodyText != null)
        {
            outcomeBodyText.text = ending != null ? ending.storyText : string.Empty;
        }

        bool hasCondition = ending != null && !string.IsNullOrEmpty(ending.conditionDescription);
        if (nasaScienceBox != null)
        {
            nasaScienceBox.SetActive(hasCondition);
        }
        if (hasCondition)
        {
            if (nasaScienceTitleText != null)
            {
                nasaScienceTitleText.text = $"SOL {GameState.MaxSols} MISSION OUTCOME CRITERIA";
            }
            if (nasaNoteText != null)
            {
                nasaNoteText.text = $"Outcome Triggered: {ending.conditionDescription}";
            }
        }

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = "COMPARE TO REAL 2018 NASA SCIENCE  >";
        }
    }

    public void ShowComparisonPanel(GameState state, RealLifeComparison comparison)
    {
        HideFactPopup();
        SetActivePanel(outcomePanel);
        ToggleComparisonMode(true);

        if (outcomeHeaderText != null)
        {
            outcomeHeaderText.color = Palette.ScienceCyan;
            outcomeHeaderText.text = !string.IsNullOrEmpty(comparison?.title)
                ? comparison.title
                : "Compare Your Future Mission to Real 2018 NASA Science";
        }

        if (outcomeTelemetryText != null)
        {
            outcomeTelemetryText.text =
                $"ARES BASE 2038 FINAL REPORT   |   POWER: {state.power}%   |   CREW HEALTH: {state.crewHealth}%";
        }

        RebuildComparisonCards(comparison?.cards);

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = "PLAY MISSION AGAIN";
        }
    }

    public void ShowFactPopupByKey(string key)
    {
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
        if (factPopupTermText != null)
        {
            factPopupTermText.text = entry.EffectiveTitle;
        }
        if (factPopupBodyText != null)
        {
            factPopupBodyText.text = entry.simpleDefinition;
        }

        factPopupOverlay.transform.SetAsLastSibling();
        factPopupOverlay.SetActive(true);
    }

    public void HideFactPopup()
    {
        activeFactKey = null;
        if (factPopupOverlay != null)
        {
            factPopupOverlay.SetActive(false);
        }
    }

    public void UpdateHud(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (solCounterText != null)
        {
            solCounterText.text = $"SOL {state.sol} / {GameState.MaxSols}\n<size=17><color=#A8B8CC>{state.realDate}</color></size>";
        }

        if (dustTauText != null)
        {
            int dustPercent = Mathf.RoundToInt(state.dustParticleIntensity * 100f);
            string skyStatus = state.sunlightBlockedPercent >= 90
                ? "NOON BLACKOUT"
                : state.sunlightBlockedPercent >= 50
                    ? "HEAVY STORM"
                    : state.sunlightBlockedPercent >= 25
                        ? "GROWING STORM"
                        : "STORM ONSET";
            dustTauText.text = $"{skyStatus}\n<size=16><color=#A8B8CC>DUST DENSITY: {dustPercent}%</color></size>";
        }

        Color powerColor = state.power <= 20
            ? Palette.DangerRed
            : state.power <= 40
                ? Palette.MarsAccent
                : Palette.PowerGold;
        UpdateStatBar(powerBarFill, powerValueText, state.power, powerColor);

        Color healthColor = state.crewHealth <= 20
            ? Palette.DangerRed
            : state.crewHealth <= 50
                ? Palette.MarsAccent
                : Palette.IntegrityGreen;
        UpdateStatBar(integrityBarFill, integrityValueText, state.crewHealth, healthColor);

        Color blockColor = state.sunlightBlockedPercent >= 80
            ? Palette.DangerRed
            : state.sunlightBlockedPercent >= 50
                ? Palette.MarsAccent
                : Palette.ScienceCyan;
        UpdateStatBar(scienceBarFill, scienceValueText, state.sunlightBlockedPercent, blockColor);
    }

    private static void UpdateStatBar(Image fill, TMP_Text valueText, int value, Color barColor)
    {
        int clamped = Mathf.Clamp(value, 0, 100);
        if (fill != null)
        {
            fill.fillAmount = clamped / 100f;
            fill.color = barColor;
        }
        if (valueText != null)
        {
            valueText.text = $"{clamped}%";
        }
    }

    private void RebuildChoiceButtons(List<Choice> availableChoices)
    {
        for (int i = 0; i < spawnedChoiceButtons.Count; i++)
        {
            if (spawnedChoiceButtons[i] != null)
            {
                Destroy(spawnedChoiceButtons[i]);
            }
        }
        spawnedChoiceButtons.Clear();

        if (choicesContainer == null || choiceButtonTemplate == null || availableChoices == null)
        {
            return;
        }

        for (int i = 0; i < availableChoices.Count; i++)
        {
            Choice choice = availableChoices[i];
            Button spawnedButton = Instantiate(choiceButtonTemplate, choicesContainer);
            spawnedButton.gameObject.name = $"ChoiceButton_{i + 1}";
            spawnedButton.gameObject.SetActive(true);

            LayoutElement layoutElem = spawnedButton.GetComponent<LayoutElement>();
            if (layoutElem != null)
            {
                layoutElem.minHeight = 148f;
                layoutElem.preferredHeight = 166f;
                layoutElem.flexibleHeight = 1f;
            }

            TMP_Text label = spawnedButton.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
            {
                RectTransform labelRt = label.rectTransform;
                labelRt.offsetMin = new Vector2(24f, 10f);
                labelRt.offsetMax = new Vector2(-24f, -10f);

                label.enableAutoSizing = true;
                label.fontSizeMin = 14f;
                label.fontSizeMax = 24f;
                label.textWrappingMode = TextWrappingModes.Normal;
                label.alignment = TextAlignmentOptions.MidlineLeft;

                string descLine = !string.IsNullOrEmpty(choice.EffectiveDescription)
                    ? $"\n<size=18><color=#A8B8CC>{choice.EffectiveDescription}</color></size>"
                    : string.Empty;

                label.text =
                    $"<size=23><b><color=#F2F5FA>{choice.EffectiveLabel}</color></b></size>   " +
                    $"<size=19><color=#F5B841>[{choice.EffectiveSubLabel}]</color></size>{descLine}";
            }

            Choice capturedChoice = choice;
            spawnedButton.onClick.RemoveAllListeners();
            spawnedButton.onClick.AddListener(() => gameManager?.SelectChoice(capturedChoice));

            spawnedChoiceButtons.Add(spawnedButton.gameObject);
        }
    }

    private void RebuildComparisonCards(List<ComparisonCard> cards)
    {
        for (int i = 0; i < spawnedComparisonCards.Count; i++)
        {
            if (spawnedComparisonCards[i] != null)
            {
                Destroy(spawnedComparisonCards[i]);
            }
        }
        spawnedComparisonCards.Clear();

        if (comparisonCardsContainer == null || cards == null)
        {
            return;
        }

        for (int i = 0; i < cards.Count; i++)
        {
            ComparisonCard card = cards[i];
            if (card == null)
            {
                continue;
            }

            GameObject cardObj = new GameObject($"ComparisonCard_{i + 1}", typeof(RectTransform), typeof(Image));
            cardObj.transform.SetParent(comparisonCardsContainer, false);
            Image cardBg = cardObj.GetComponent<Image>();
            cardBg.color = Palette.PanelElevated;

            GameObject headerStrip = new GameObject("AccentBar", typeof(RectTransform), typeof(Image));
            headerStrip.transform.SetParent(cardObj.transform, false);
            RectTransform stripRt = headerStrip.GetComponent<RectTransform>();
            stripRt.anchorMin = new Vector2(0f, 0.97f);
            stripRt.anchorMax = Vector2.one;
            stripRt.offsetMin = Vector2.zero;
            stripRt.offsetMax = Vector2.zero;
            headerStrip.GetComponent<Image>().color = i == 0
                ? Palette.MarsAccent
                : i == 1
                    ? Palette.PowerGold
                    : Palette.ScienceCyan;

            GameObject bodyTxtObj = new GameObject("CardText", typeof(RectTransform), typeof(TextMeshProUGUI));
            bodyTxtObj.transform.SetParent(cardObj.transform, false);
            RectTransform bodyRt = bodyTxtObj.GetComponent<RectTransform>();
            bodyRt.anchorMin = Vector2.zero;
            bodyRt.anchorMax = new Vector2(1f, 0.96f);
            bodyRt.offsetMin = new Vector2(18f, 16f);
            bodyRt.offsetMax = new Vector2(-18f, -14f);

            TextMeshProUGUI bodyTxt = bodyTxtObj.GetComponent<TextMeshProUGUI>();
            bodyTxt.font = uiFont;
            bodyTxt.enableAutoSizing = true;
            bodyTxt.fontSizeMin = 13f;
            bodyTxt.fontSizeMax = 19f;
            bodyTxt.textWrappingMode = TextWrappingModes.Normal;
            bodyTxt.alignment = TextAlignmentOptions.TopLeft;
            bodyTxt.color = Palette.TextPrimary;

            bodyTxt.text =
                $"<size=21><b><color=#F5B841>{i + 1}. {card.topic}</color></b></size>\n\n" +
                $"<size=15><b><color=#2CC5E8>REAL 2018 NASA EVENT</color></b></size>\n" +
                $"{card.whatHappenedInRealLife}\n\n" +
                $"<size=15><b><color=#38C976>YOUR 2038 MISSION</color></b></size>\n" +
                $"{card.howItConnectsToGame}";

            spawnedComparisonCards.Add(cardObj);
        }
    }

    private void ToggleComparisonMode(bool showComparison)
    {
        if (outcomeBodyText != null)
        {
            outcomeBodyText.gameObject.SetActive(!showComparison);
        }
        if (nasaScienceBox != null)
        {
            nasaScienceBox.SetActive(!showComparison);
        }
        if (comparisonCardsContainer != null)
        {
            comparisonCardsContainer.gameObject.SetActive(showComparison);
        }
    }

    private static Color ResolveEndingHeaderColor(string endingId)
    {
        switch (endingId)
        {
            case "ENDING_PERFECT":
                return Palette.IntegrityGreen;
            case "ENDING_OPPORTUNITY_FATE":
                return Palette.DangerRed;
            case "ENDING_LOW_POWER":
            case "ENDING_LOW_HEALTH":
                return Palette.PowerGold;
            default:
                return Palette.MarsAccent;
        }
    }

    private void SetActivePanel(GameObject targetPanel)
    {
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
    }

    private void HandleStartOrBriefingClicked()
    {
        gameManager?.OnTitleOrBriefingAdvance();
    }

    private void HandleNextClicked()
    {
        gameManager?.ProceedFromOutcome();
    }
}

/// <summary>
/// Detects clicks on TMP rich-text &lt;link="id"&gt; spans inside briefing, dilemma, and outcome text.
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
