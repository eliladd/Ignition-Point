using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionUI : MonoBehaviour
{
    [Header("Root Toggleable Panels")]
    [SerializeField] private GameObject titlePanel;
    [SerializeField] private GameObject decisionPanel;
    [SerializeField] private GameObject outcomePanel;

    [Header("Title Panel Elements")]
    [SerializeField] private TMP_Text titleHeaderText;
    [SerializeField] private TMP_Text titlePremiseText;
    [SerializeField] private Button startButton;

    [Header("Decision Panel - Top HUD")]
    [SerializeField] private TMP_Text solCounterText;
    [SerializeField] private TMP_Text dustTauText;
    [SerializeField] private Image powerBarFill;
    [SerializeField] private TMP_Text powerValueText;
    [SerializeField] private Image integrityBarFill;
    [SerializeField] private TMP_Text integrityValueText;
    [SerializeField] private Image scienceBarFill;
    [SerializeField] private TMP_Text scienceValueText;

    [Header("Decision Panel - Dilemma & Choices")]
    [SerializeField] private TMP_Text nodeTitleText;
    [SerializeField] private TMP_Text dilemmaPromptText;
    [SerializeField] private RectTransform choicesContainer;
    [SerializeField] private Button choiceButtonTemplate;

    [Header("Outcome Panel Elements")]
    [SerializeField] private TMP_Text outcomeHeaderText;
    [SerializeField] private TMP_Text outcomeBodyText;
    [SerializeField] private TMP_Text outcomeTelemetryText;
    [SerializeField] private GameObject nasaScienceBox;
    [SerializeField] private TMP_Text nasaScienceTitleText;
    [SerializeField] private TMP_Text nasaNoteText;
    [SerializeField] private Button nextButton;
    [SerializeField] private TMP_Text nextButtonLabel;

    private GameManager gameManager;
    private readonly List<GameObject> spawnedChoiceButtons = new List<GameObject>();

    public void Initialize(GameManager manager)
    {
        gameManager = manager;

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStartButtonClicked);
        }

        if (nextButton != null)
        {
            nextButton.onClick.RemoveAllListeners();
            nextButton.onClick.AddListener(OnNextButtonClicked);
        }

        if (choiceButtonTemplate != null)
        {
            choiceButtonTemplate.gameObject.SetActive(false);
        }
    }

    public void ShowTitlePanel()
    {
        SetActivePanel(titlePanel);
        Debug.Log("[MissionUI] Showing TitlePanel.");
    }

    public void ShowDecisionPanel(GameState state, StoryNode node, List<Choice> availableChoices)
    {
        SetActivePanel(decisionPanel);
        UpdateHud(state);

        if (nodeTitleText != null)
        {
            nodeTitleText.text = node != null && !string.IsNullOrWhiteSpace(node.title)
                ? node.title
                : $"Sol {state.sol} Mission Dilemma";
        }

        if (dilemmaPromptText != null)
        {
            dilemmaPromptText.text = node != null ? node.prompt : string.Empty;
        }

        RebuildChoiceButtons(availableChoices);
        Debug.Log($"[MissionUI] Showing DecisionPanel for node '{node?.id}' (Sol {state.sol}/{GameState.MaxSols}, tau={state.dustTau:0.0}, choices={availableChoices?.Count ?? 0}).");
    }

    public void ShowOutcomePanel(GameState state, Choice choice, StoryNode endingNode, bool isEnding)
    {
        SetActivePanel(outcomePanel);
        UpdateHud(state);

        if (outcomeHeaderText != null)
        {
            if (isEnding && endingNode != null && !string.IsNullOrWhiteSpace(endingNode.endingType))
            {
                outcomeHeaderText.text = $"MISSION END — {endingNode.endingType.ToUpperInvariant()}";
                outcomeHeaderText.color = state.IsEarlyLoss() ? Palette.DangerRed : Palette.ScienceCyan;
            }
            else
            {
                outcomeHeaderText.text = $"SOL {state.sol} TELEMETRY OUTCOME";
                outcomeHeaderText.color = Palette.MarsAccent;
            }
        }

        if (outcomeBodyText != null)
        {
            string outcomeText = choice != null ? choice.outcome : string.Empty;
            if (isEnding && endingNode != null)
            {
                string endingTitle = !string.IsNullOrWhiteSpace(endingNode.title) ? $"<b>{endingNode.title}</b>\n" : string.Empty;
                outcomeBodyText.text = $"{outcomeText}\n\n{endingTitle}{endingNode.prompt}";
            }
            else
            {
                outcomeBodyText.text = outcomeText;
            }
        }

        if (outcomeTelemetryText != null)
        {
            string deltaSummary = choice != null
                ? $"Deltas: Power {FormatDelta(choice.powerDelta)} | Integrity {FormatDelta(choice.integrityDelta)} | Science {FormatDelta(choice.scienceDelta)}"
                : string.Empty;
            outcomeTelemetryText.text =
                $"{deltaSummary}    •    Current: Power {state.power}% | Integrity {state.integrity}% | Science {state.science}";
        }

        if (nasaNoteText != null)
        {
            nasaNoteText.text = choice != null && !string.IsNullOrWhiteSpace(choice.nasaNote)
                ? choice.nasaNote
                : "NASA rovers rely on solar arrays and thermal management to survive Martian dust storms.";
        }

        if (nextButtonLabel != null)
        {
            nextButtonLabel.text = isEnding ? "Play Again" : "Next Sol";
        }

        Debug.Log($"[MissionUI] Showing OutcomePanel (isEnding={isEnding}, button='{nextButtonLabel?.text}').");
    }

    public void UpdateHud(GameState state)
    {
        if (state == null)
        {
            return;
        }

        if (solCounterText != null)
        {
            solCounterText.text = $"SOL {state.sol}/{GameState.MaxSols}";
        }

        if (dustTauText != null)
        {
            dustTauText.text = $"DUST OPACITY (tau): {state.dustTau:0.0}";
        }

        UpdateBar(powerBarFill, powerValueText, state.power, $"{state.power}%",
            state.power <= 25 ? Palette.DangerRed : Palette.PowerGold);

        UpdateBar(integrityBarFill, integrityValueText, state.integrity, $"{state.integrity}%",
            state.integrity <= 25 ? Palette.DangerRed : Palette.IntegrityGreen);

        UpdateBar(scienceBarFill, scienceValueText, state.science, $"{state.science}",
            Palette.ScienceCyan);
    }

    private void UpdateBar(Image fillImage, TMP_Text valueText, int value, string label, Color barColor)
    {
        float normalized = Mathf.Clamp01(value / 100f);
        if (fillImage != null)
        {
            fillImage.color = barColor;
            if (fillImage.type == Image.Type.Filled)
            {
                fillImage.fillAmount = normalized;
            }
            else
            {
                RectTransform rt = fillImage.rectTransform;
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = new Vector2(normalized, 1f);
                rt.offsetMin = Vector2.zero;
                rt.offsetMax = Vector2.zero;
            }
        }

        if (valueText != null)
        {
            valueText.text = label;
        }
    }

    private void RebuildChoiceButtons(List<Choice> choices)
    {
        foreach (GameObject spawned in spawnedChoiceButtons)
        {
            if (spawned != null)
            {
                Destroy(spawned);
            }
        }
        spawnedChoiceButtons.Clear();

        if (choicesContainer == null || choices == null)
        {
            return;
        }

        for (int i = 0; i < choices.Count; i++)
        {
            Choice capturedChoice = choices[i];
            Button btn = SpawnChoiceButton(i, capturedChoice);
            if (btn != null)
            {
                btn.onClick.RemoveAllListeners();
                btn.onClick.AddListener(() => OnChoiceClicked(capturedChoice));
                spawnedChoiceButtons.Add(btn.gameObject);
            }
        }
    }

    private Button SpawnChoiceButton(int index, Choice choice)
    {
        string deltaHint = $"Power {FormatDelta(choice.powerDelta)}   |   Integrity {FormatDelta(choice.integrityDelta)}   |   Science {FormatDelta(choice.scienceDelta)}";
        string fullLabel = $"{choice.label}\n<size=78%><color=#A8B8CC>{deltaHint}</color></size>";

        if (choiceButtonTemplate != null)
        {
            Button instance = Instantiate(choiceButtonTemplate, choicesContainer);
            instance.name = $"ChoiceButton_{index + 1}";
            instance.gameObject.SetActive(true);
            TMP_Text labelText = instance.GetComponentInChildren<TMP_Text>(true);
            if (labelText != null)
            {
                labelText.text = fullLabel;
            }
            return instance;
        }

        GameObject buttonObj = new GameObject($"ChoiceButton_{index + 1}", typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        buttonObj.transform.SetParent(choicesContainer, false);

        Image bg = buttonObj.GetComponent<Image>();
        bg.color = Palette.PanelElevated;

        LayoutElement le = buttonObj.GetComponent<LayoutElement>();
        le.minHeight = 76f;
        le.preferredHeight = 82f;
        le.flexibleWidth = 1f;

        Button button = buttonObj.GetComponent<Button>();
        ColorBlock cb = button.colors;
        cb.normalColor = Color.white;
        cb.highlightedColor = new Color(1.15f, 1.15f, 1.15f, 1f);
        cb.pressedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        button.colors = cb;

        GameObject textObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        textObj.transform.SetParent(buttonObj.transform, false);
        RectTransform textRt = textObj.GetComponent<RectTransform>();
        textRt.anchorMin = Vector2.zero;
        textRt.anchorMax = Vector2.one;
        textRt.offsetMin = new Vector2(20f, 10f);
        textRt.offsetMax = new Vector2(-20f, -10f);

        TextMeshProUGUI tmp = textObj.GetComponent<TextMeshProUGUI>();
        tmp.text = fullLabel;
        tmp.fontSize = 20f;
        tmp.color = Palette.TextPrimary;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.raycastTarget = false;

        return button;
    }

    private void SetActivePanel(GameObject targetPanel)
    {
        if (titlePanel != null) titlePanel.SetActive(targetPanel == titlePanel);
        if (decisionPanel != null) decisionPanel.SetActive(targetPanel == decisionPanel);
        if (outcomePanel != null) outcomePanel.SetActive(targetPanel == outcomePanel);
    }

    private void OnStartButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.StartMission();
        }
    }

    private void OnChoiceClicked(Choice choice)
    {
        if (gameManager != null)
        {
            gameManager.SelectChoice(choice);
        }
    }

    private void OnNextButtonClicked()
    {
        if (gameManager != null)
        {
            gameManager.ProceedFromOutcome();
        }
    }

    private static string FormatDelta(int delta)
    {
        if (delta > 0) return $"+{delta}";
        return delta.ToString();
    }
}
