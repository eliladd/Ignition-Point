using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controls the Ares Base 10-Sol mission loop: title screen, 3-part 2018->2038 NASA briefing,
/// 25-node branching decisions with stateful consequences, instant cause-and-effect outcomes,
/// Sol 10 ending evaluation, and real 2018 NASA science comparison.
/// </summary>
public class GameManager : MonoBehaviour
{
    private const string DefaultResourcePath = "mission_data";
    private const string FallbackResourcePath = "MarsStorm2018_Story";

    private enum GameFlowPhase
    {
        Title,
        IntroBriefing,
        NodeDecision,
        ChoiceOutcome,
        EndingSummary,
        RealLifeComparison
    }

    [Header("Data Source (Assets/Resources/mission_data.json)")]
    [SerializeField] private string resourceName = DefaultResourcePath;

    [Header("Fallback Starting Telemetry (overridden by JSON startingStats)")]
    [SerializeField] private int startingPower = 75;
    [SerializeField] private int startingIntegrity = 75;
#pragma warning disable 0414
    [SerializeField] private int startingScience = 0;
#pragma warning restore 0414

    [Header("UI Reference")]
    [SerializeField] private MissionUI missionUI = null;

    private MissionData missionData;
    private readonly Dictionary<string, StoryNode> nodeLookup = new Dictionary<string, StoryNode>();
    private StoryNode currentNode;
    private string pendingNextNodeId;
    private int introScreenIndex;
    private GameFlowPhase phase = GameFlowPhase.Title;

    public GameState State { get; private set; } = new GameState();

    public GlossaryEntry FindFactByKey(string key)
    {
        return missionData?.FindGlossaryEntry(key);
    }

    private void Awake()
    {
        LoadMissionData();
    }

    private void Start()
    {
        if (missionData == null)
        {
            LoadMissionData();
        }

        if (missionUI == null)
        {
            missionUI = FindAnyObjectByType<MissionUI>();
        }

        if (missionUI != null)
        {
            missionUI.Initialize(this);
            ShowTitleScreen();
        }
        else
        {
            Debug.LogError("[GameManager] MissionUI reference missing. Could not display mission interface.");
        }
    }

    private void LoadMissionData()
    {
        nodeLookup.Clear();

        string targetResource = string.IsNullOrWhiteSpace(resourceName) ? DefaultResourcePath : resourceName.Trim();
        TextAsset jsonAsset = Resources.Load<TextAsset>(targetResource);
        if (jsonAsset == null && !string.Equals(targetResource, FallbackResourcePath, StringComparison.Ordinal))
        {
            jsonAsset = Resources.Load<TextAsset>(FallbackResourcePath);
        }

        if (jsonAsset == null)
        {
            Debug.LogError($"[GameManager] Could not load Resources/{targetResource}.json or Resources/{FallbackResourcePath}.json.");
            missionData = new GameStoryData();
            return;
        }

        missionData = JsonUtility.FromJson<GameStoryData>(jsonAsset.text);
        if (missionData == null)
        {
            Debug.LogError($"[GameManager] Failed to parse JSON from '{jsonAsset.name}'.");
            missionData = new GameStoryData();
            return;
        }

        List<StoryNode> allNodes = missionData.GetNodes();
        for (int i = 0; i < allNodes.Count; i++)
        {
            StoryNode node = allNodes[i];
            if (node == null || string.IsNullOrWhiteSpace(node.EffectiveId))
            {
                continue;
            }
            nodeLookup[node.EffectiveId.Trim()] = node;
        }
    }

    public void ShowTitleScreen()
    {
        phase = GameFlowPhase.Title;
        currentNode = null;
        pendingNextNodeId = null;
        introScreenIndex = 0;

        ApplyStartingStats();

        if (missionUI != null)
        {
            missionUI.ApplyAtmosphere(State.skyColorHex, 0.15f);
            string title = !string.IsNullOrEmpty(missionData?.gameTitle)
                ? missionData.gameTitle
                : "Red Planet Blackout: The 2018 Mars Storm";
            missionUI.ShowTitlePanel(title);
        }
    }

    public void OnTitleOrBriefingAdvance()
    {
        List<IntroScreen> screens = missionData?.introScreens;

        if (phase == GameFlowPhase.Title)
        {
            if (screens != null && screens.Count > 0)
            {
                phase = GameFlowPhase.IntroBriefing;
                introScreenIndex = 0;
                missionUI?.ShowIntroScreen(
                    missionData.gameTitle,
                    screens[introScreenIndex],
                    introScreenIndex,
                    screens.Count,
                    missionData.kidGlossary);
                return;
            }

            StartMission();
            return;
        }

        if (phase == GameFlowPhase.IntroBriefing)
        {
            introScreenIndex++;
            if (screens != null && introScreenIndex < screens.Count)
            {
                missionUI?.ShowIntroScreen(
                    missionData.gameTitle,
                    screens[introScreenIndex],
                    introScreenIndex,
                    screens.Count,
                    missionData.kidGlossary);
                return;
            }

            StartMission();
        }
    }

    public void StartMission()
    {
        ApplyStartingStats();

        StoryNode startNode = ResolveStartNode();
        if (startNode == null)
        {
            Debug.LogError("[GameManager] No story nodes found in mission data.");
            ShowTitleScreen();
            return;
        }

        EnterNode(startNode);
    }

    private void ApplyStartingStats()
    {
        int startPower = missionData?.startingStats != null ? missionData.startingStats.power : startingPower;
        int startHealth = missionData?.startingStats != null ? missionData.startingStats.crewHealth : startingIntegrity;
        State.Reset(startPower, startHealth);
    }

    public List<Choice> GetAvailableChoices(StoryNode node)
    {
        List<Choice> available = new List<Choice>();
        if (node == null)
        {
            return available;
        }

        List<Choice> rawChoices = node.GetChoices();
        for (int i = 0; i < rawChoices.Count; i++)
        {
            Choice choice = rawChoices[i];
            if (State.IsChoiceEligible(choice))
            {
                available.Add(choice);
            }
        }

        return available;
    }

    public void SelectChoice(Choice choice)
    {
        if (choice == null || currentNode == null)
        {
            return;
        }

        State.ApplyChoice(choice);
        phase = GameFlowPhase.ChoiceOutcome;

        if (State.IsEarlyLoss())
        {
            pendingNextNodeId = "EVALUATE_ENDING";
        }
        else
        {
            pendingNextNodeId = choice.nextNodeId;
        }

        bool isGoingToEnding = State.IsEarlyLoss()
            || string.Equals(pendingNextNodeId, "EVALUATE_ENDING", StringComparison.OrdinalIgnoreCase)
            || string.IsNullOrWhiteSpace(pendingNextNodeId)
            || currentNode.EffectiveSol >= GameState.MaxSols;

        missionUI?.ShowOutcomePanel(State, choice, currentNode, isGoingToEnding);
    }

    public void ProceedFromOutcome()
    {
        if (phase == GameFlowPhase.ChoiceOutcome)
        {
            if (State.IsEarlyLoss()
                || string.Equals(pendingNextNodeId, "EVALUATE_ENDING", StringComparison.OrdinalIgnoreCase)
                || string.IsNullOrWhiteSpace(pendingNextNodeId))
            {
                ShowEvaluatedEnding();
                return;
            }

            if (nodeLookup.TryGetValue(pendingNextNodeId.Trim(), out StoryNode nextNode) && nextNode != null)
            {
                EnterNode(nextNode);
                return;
            }

            Debug.LogWarning($"[GameManager] Next node '{pendingNextNodeId}' not found; evaluating ending.");
            ShowEvaluatedEnding();
            return;
        }

        if (phase == GameFlowPhase.EndingSummary)
        {
            if (missionData?.realLifeComparison != null
                && missionData.realLifeComparison.cards != null
                && missionData.realLifeComparison.cards.Count > 0)
            {
                phase = GameFlowPhase.RealLifeComparison;
                missionUI?.ShowComparisonPanel(State, missionData.realLifeComparison);
                return;
            }

            ShowTitleScreen();
            return;
        }

        if (phase == GameFlowPhase.RealLifeComparison)
        {
            ShowTitleScreen();
        }
    }

    private void EnterNode(StoryNode node)
    {
        currentNode = node;
        pendingNextNodeId = null;
        phase = GameFlowPhase.NodeDecision;

        State.SyncWithNode(node);

        if (missionUI == null)
        {
            return;
        }

        missionUI.ApplyAtmosphere(State.skyColorHex, State.dustParticleIntensity);
        List<Choice> availableChoices = GetAvailableChoices(node);
        missionUI.ShowDecisionPanel(State, node, availableChoices, missionData?.kidGlossary);
    }

    private void ShowEvaluatedEnding()
    {
        phase = GameFlowPhase.EndingSummary;

        string endingId = EvaluateEndingId();
        StoryEnding ending = missionData?.GetEnding(endingId) ?? BuildFallbackEnding(endingId);

        State.sol = GameState.MaxSols;
        State.sunlightBlockedPercent = 15;
        State.realDate = !string.IsNullOrEmpty(ending.realDate) ? ending.realDate : "July 23, 2038";
        State.skyColorHex = !string.IsNullOrEmpty(ending.skyColorHex) ? ending.skyColorHex : "#D49B6A";
        State.dustParticleIntensity = 0.1f;

        if (missionUI != null)
        {
            missionUI.ApplyAtmosphere(State.skyColorHex, State.dustParticleIntensity);
            missionUI.ShowEndingPanel(State, ending);
        }
    }

    /// <summary>
    /// Evaluates Sol 5 ending in exact priority order from Story json.txt:
    /// 1) Power <= 20 or CrewHealth <= 20 -> ENDING_OPPORTUNITY_FATE
    /// 2) Power > 40 and CrewHealth > 50 -> ENDING_PERFECT
    /// 3) CrewHealth > 50 -> ENDING_LOW_POWER
    /// 4) Otherwise -> ENDING_LOW_HEALTH
    /// </summary>
    private string EvaluateEndingId()
    {
        if (State.power <= 20 || State.crewHealth <= 20)
        {
            return "ENDING_OPPORTUNITY_FATE";
        }
        if (State.power > 40 && State.crewHealth > 50)
        {
            return "ENDING_PERFECT";
        }
        if (State.crewHealth > 50)
        {
            return "ENDING_LOW_POWER";
        }
        return "ENDING_LOW_HEALTH";
    }

    private StoryNode ResolveStartNode()
    {
        if (nodeLookup.TryGetValue("DAY_1", out StoryNode day1))
        {
            return day1;
        }

        if (!string.IsNullOrWhiteSpace(missionData?.startNodeId)
            && nodeLookup.TryGetValue(missionData.startNodeId.Trim(), out StoryNode explicitStart))
        {
            return explicitStart;
        }

        List<StoryNode> nodes = missionData?.GetNodes();
        if (nodes != null && nodes.Count > 0)
        {
            return nodes[0];
        }

        return null;
    }

    private StoryEnding BuildFallbackEnding(string endingId)
    {
        return new StoryEnding
        {
            endingId = endingId,
            realDate = "July 23, 2038",
            skyColorHex = "#D49B6A",
            headline = $"Sol {GameState.MaxSols}: Storm Clears Over Ares Base",
            conditionDescription = $"Final Power: {State.power}% | Final Crew Health: {State.crewHealth}%",
            storyText = $"The 2038 global dust storm has cleared. Final Ares Base reserves stand at {State.power}% Power and {State.crewHealth}% Crew Health."
        };
    }
}
