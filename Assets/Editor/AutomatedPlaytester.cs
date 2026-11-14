using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

public static class AutomatedPlaytester
{
    public static void RunAllBranchPlaytest()
    {
        Debug.Log("=== [PLAYTEST] STARTING AUTOMATED STORY RUNS ===");

        string jsonPath = Path.Combine(Application.dataPath, "Resources", "mission_data.json");
        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"[PLAYTEST] mission_data.json not found at {jsonPath}");
            EditorApplication.Exit(1);
            return;
        }

        string json = File.ReadAllText(jsonPath);
        GameStoryData missionData = JsonUtility.FromJson<GameStoryData>(json);
        List<StoryNode> allNodes = missionData?.GetNodes();
        if (allNodes == null || allNodes.Count == 0)
        {
            Debug.LogError("[PLAYTEST] Failed to parse GameStoryData or GetNodes() returned empty");
            EditorApplication.Exit(1);
            return;
        }

        Dictionary<string, StoryNode> nodeLookup = new Dictionary<string, StoryNode>();
        foreach (var node in allNodes)
        {
            if (node != null && !string.IsNullOrEmpty(node.EffectiveId))
            {
                nodeLookup[node.EffectiveId.Trim()] = node;
            }
        }

        string[] strategyNames = new string[]
        {
            "Strategy 1: Always Option 1 (Conservative)",
            "Strategy 2: Always Option 2 (Balanced / Engineering)",
            "Strategy 3: Always Option 3 (Extreme / High-tech)",
            "Strategy 4: Cyclic (Cycle 1 -> 2 -> 3)"
        };

        for (int strat = 0; strat < strategyNames.Length; strat++)
        {
            Debug.Log($"\n>>> [PLAYTEST] Running: {strategyNames[strat]} <<<");
            GameState state = new GameState();
            state.Reset(missionData.startingStats?.power ?? 100, missionData.startingStats?.crewHealth ?? 100);

            StoryNode current = allNodes[0];
            int solCount = 0;
            StringBuilder log = new StringBuilder();

            while (current != null && solCount < GameState.MaxSols)
            {
                solCount++;
                List<Choice> choices = current.GetChoices();
                if (choices == null || choices.Count == 0)
                {
                    log.AppendLine($"  [Sol {solCount}] {current.EffectiveId}: NO CHOICES AVAILABLE!");
                    break;
                }

                int choiceIdx = 0;
                if (strat == 1) choiceIdx = Mathf.Min(1, choices.Count - 1);
                else if (strat == 2) choiceIdx = Mathf.Min(2, choices.Count - 1);
                else if (strat == 3) choiceIdx = (solCount - 1) % choices.Count;

                Choice chosen = choices[choiceIdx];
                state.ApplyChoice(chosen, current);

                string subTags = $"Fuel={state.fuelTag ?? "-"}, Temp={state.tempTag ?? "-"}, Reactor={state.reactorTag ?? "-"}, O2={state.oxygenTag ?? "-"}, Food={state.foodTag ?? "-"}, Air={state.airTag ?? "-"}";

                log.AppendLine($"  [Sol {solCount}] ({current.EffectiveId}) -> Picked Option {choiceIdx + 1}: \"{chosen.EffectiveLabel}\"");
                log.AppendLine($"       Stats: Power {state.power}% | Crew Health {state.crewHealth}% | Subsystems: {subTags}");

                if (state.IsEarlyLoss())
                {
                    log.AppendLine($"       [FAIL] EARLY MISSION LOSS on Sol {solCount}!");
                    break;
                }

                if (string.Equals(chosen.nextNodeId, "EVALUATE_ENDING", System.StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(chosen.nextNodeId))
                {
                    log.AppendLine("       [END] Reached finale evaluation.");
                    break;
                }

                if (!nodeLookup.TryGetValue(chosen.nextNodeId.Trim(), out current))
                {
                    log.AppendLine($"       [ERROR] Target node '{chosen.nextNodeId}' missing from story!");
                    break;
                }
            }

            Debug.Log(log.ToString());
            
            // Evaluate ending using GameManager criteria
            string endingId = "ENDING_LOW_HEALTH";
            if (state.power <= 20 || state.crewHealth <= 20)
            {
                endingId = "ENDING_OPPORTUNITY_FATE";
            }
            else if (state.power > 40 && state.crewHealth > 50)
            {
                endingId = "ENDING_PERFECT";
            }
            else if (state.crewHealth > 50)
            {
                endingId = "ENDING_LOW_POWER";
            }

            StoryEnding ending = missionData.GetEnding(endingId);
            string endingName = ending != null ? ending.headline : endingId;
            Debug.Log($"[PLAYTEST SUMMARY] Completed {solCount}/10 Sols | Survived: {!state.IsEarlyLoss()} | Final Power: {state.power}% | Final Crew Health: {state.crewHealth}% | Ending: \"{endingName}\" ({endingId})");
        }

        Debug.Log("=== [PLAYTEST] ALL 4 PLAYTEST RUNS COMPLETED SUCCESSFULLY ===");
    }
}
