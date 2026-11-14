using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private const string DefaultResourcePath = "mission_data";

    [Header("Data Source (Assets/Resources/mission_data.json)")]
    [SerializeField] private string resourceName = DefaultResourcePath;

    [Header("Starting Rover Telemetry")]
    [SerializeField] private int startingPower = 80;
    [SerializeField] private int startingIntegrity = 90;
    [SerializeField] private int startingScience = 0;

    [Header("UI Reference")]
    [SerializeField] private MissionUI missionUI;

    private MissionData missionData;
    private readonly Dictionary<string, StoryNode> nodeLookup = new Dictionary<string, StoryNode>();
    private StoryNode currentNode;
    private StoryNode pendingNextNode;
    private bool isCurrentOutcomeEnding;

    public GameState State { get; private set; } = new GameState();
    public StoryNode CurrentNode => currentNode;
    public IReadOnlyDictionary<string, StoryNode> NodeLookup => nodeLookup;

    private void Awake()
    {
        if (missionUI == null)
        {
            missionUI = FindAnyObjectByType<MissionUI>();
        }

        LoadMissionData();
    }

    private void Start()
    {
        if (missionUI != null)
        {
            missionUI.Initialize(this);
            missionUI.ShowTitlePanel();
        }
        else
        {
            Debug.LogError("[GameManager] MissionUI reference not found in scene.");
        }
    }

    public bool LoadMissionData()
    {
        nodeLookup.Clear();

        string path = string.IsNullOrWhiteSpace(resourceName) ? DefaultResourcePath : resourceName.Trim();
        TextAsset jsonAsset = Resources.Load<TextAsset>(path);
        if (jsonAsset == null)
        {
            Debug.LogError($"[GameManager] Could not find Assets/Resources/{path}.json via Resources.Load<TextAsset>.");
            return false;
        }

        missionData = JsonUtility.FromJson<MissionData>(jsonAsset.text);
        if (missionData == null)
        {
            Debug.LogError($"[GameManager] Failed to parse JSON in Assets/Resources/{path}.json.");
            return false;
        }

        List<StoryNode> nodes = missionData.GetNodes();
        foreach (StoryNode node in nodes)
        {
            if (node != null && !string.IsNullOrWhiteSpace(node.id))
            {
                nodeLookup[node.id.Trim()] = node;
            }
        }

        Debug.Log($"[GameManager] Loaded {nodeLookup.Count} story nodes from Resources/{path}.json (startNodeId='{missionData.startNodeId}').");
        return nodeLookup.Count > 0;
    }

    public void StartMission()
    {
        if (nodeLookup.Count == 0 && !LoadMissionData())
        {
            return;
        }

        StoryNode startNode = ResolveStartNode();
        if (startNode == null)
        {
            Debug.LogError("[GameManager] No valid start StoryNode found in mission_data.json.");
            return;
        }

        State.Reset(startingPower, startingIntegrity, startingScience, startNode.dustTau);
        pendingNextNode = null;
        isCurrentOutcomeEnding = false;

        EnterNode(startNode);
    }

    public List<Choice> GetAvailableChoices(StoryNode node)
    {
        List<Choice> filtered = new List<Choice>();
        if (node == null || node.choices == null)
        {
            return filtered;
        }

        foreach (Choice choice in node.choices)
        {
            if (State.IsChoiceEligible(choice))
            {
                filtered.Add(choice);
            }
        }

        return filtered;
    }

    public void SelectChoice(Choice choice)
    {
        if (choice == null)
        {
            return;
        }

        int prevPower = State.power;
        int prevIntegrity = State.integrity;
        int prevScience = State.science;

        State.ApplyChoice(choice);

        Debug.Log(
            $"[GameManager] Choice '{choice.label}' applied on Sol {State.sol}: " +
            $"Power {prevPower}->{State.power}, Integrity {prevIntegrity}->{State.integrity}, Science {prevScience}->{State.science}, " +
            $"SetFlag='{choice.setFlag}'");

        if (State.IsEarlyLoss())
        {
            StoryNode lossNode = BuildEarlyLossNode();
            pendingNextNode = lossNode;
            isCurrentOutcomeEnding = true;

            if (missionUI != null)
            {
                missionUI.ShowOutcomePanel(State, choice, lossNode, true);
            }
            return;
        }

        StoryNode nextNode = null;
        if (!string.IsNullOrWhiteSpace(choice.nextNodeId))
        {
            nodeLookup.TryGetValue(choice.nextNodeId.Trim(), out nextNode);
        }

        if (nextNode == null)
        {
            nextNode = BuildFallbackCompletionNode();
        }

        pendingNextNode = nextNode;
        isCurrentOutcomeEnding = nextNode.isEnding;

        if (isCurrentOutcomeEnding)
        {
            State.SyncWithNode(nextNode);
        }

        if (missionUI != null)
        {
            missionUI.ShowOutcomePanel(State, choice, isCurrentOutcomeEnding ? nextNode : null, isCurrentOutcomeEnding);
        }
    }

    public void ProceedFromOutcome()
    {
        if (isCurrentOutcomeEnding || pendingNextNode == null)
        {
            StartMission();
            return;
        }

        EnterNode(pendingNextNode);
    }

    private void EnterNode(StoryNode node)
    {
        currentNode = node;
        State.SyncWithNode(node);

        if (node.isEnding)
        {
            pendingNextNode = null;
            isCurrentOutcomeEnding = true;
        }

        List<Choice> availableChoices = GetAvailableChoices(node);
        if (missionUI != null)
        {
            missionUI.ShowDecisionPanel(State, node, availableChoices);
        }
    }

    private StoryNode ResolveStartNode()
    {
        if (missionData != null && !string.IsNullOrWhiteSpace(missionData.startNodeId))
        {
            if (nodeLookup.TryGetValue(missionData.startNodeId.Trim(), out StoryNode found))
            {
                return found;
            }
        }

        if (missionData != null)
        {
            List<StoryNode> nodes = missionData.GetNodes();
            if (nodes.Count > 0)
            {
                return nodes[0];
            }
        }

        return null;
    }

    private StoryNode BuildEarlyLossNode()
    {
        bool powerLost = State.power <= 0;
        string lossId = powerLost ? "early_loss_power" : "early_loss_integrity";

        if (nodeLookup.TryGetValue(lossId, out StoryNode customLossNode))
        {
            return customLossNode;
        }

        return new StoryNode
        {
            id = lossId,
            sol = State.sol,
            dustTau = State.dustTau,
            isEnding = true,
            endingType = powerLost ? "LOSS_LOW_POWER_FAULT" : "LOSS_HARDWARE_FAILURE",
            title = powerLost
                ? $"MISSION ENDED ON SOL {State.sol}: Low-Power Fault (Battery 0%)"
                : $"MISSION ENDED ON SOL {State.sol}: Critical Rover Integrity Failure (0%)",
            prompt = powerLost
                ? "Thick Martian dust blocked too much sunlight for your solar arrays to recover. With battery charge at 0%, the rover entered an emergency low-power fault and could no longer run its survival heaters through the -80°C Martian night."
                : "Blowing abrasive dust and freezing temperatures pushed the rover's mechanical integrity to 0%. Key actuators and thermal lines failed before the storm cleared.",
            choices = new List<Choice>()
        };
    }

    private StoryNode BuildFallbackCompletionNode()
    {
        return new StoryNode
        {
            id = "mission_complete_fallback",
            sol = State.sol,
            dustTau = State.dustTau,
            isEnding = true,
            endingType = "MISSION_COMPLETE",
            title = "MISSION COMPLETE: Final Telemetry Received",
            prompt = $"Your rover completed its storm sequence with {State.power}% Power, {State.integrity}% Integrity, and {State.science} Science points collected!",
            choices = new List<Choice>()
        };
    }
}
