using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime Ares Base telemetry, past-decision flags, and storm state across the 10-Sol Mars mission.
/// </summary>
public class GameState
{
    public const int MinStat = 0;
    public const int MaxStat = 100;
    public const int MaxSols = 10;

    public int sol = 1;
    public int power = 90;
    public int crewHealth = 85;
    public int sunlightBlockedPercent = 10;
    public string realDate = "May 30, 2038";
    public string skyColorHex = "#C88B5A";
    public float dustParticleIntensity = 0.15f;
    public string activeConsequenceText;
    public int lastChoicePowerDelta;
    public int lastChoiceHealthDelta;
    public string lastChoiceOutcomeText;
    public HashSet<string> flags = new HashSet<string>();

    // Box 2 (Electricity) incited sub-indicators — empty/hidden until player action incites them.
    public string fuelTag = string.Empty;
    public bool fuelGood = true;
    public bool fuelJustIncited;

    public string tempTag = string.Empty;
    public bool tempGood = true;
    public bool tempJustIncited;

    public string reactorTag = string.Empty;
    public bool reactorGood = true;
    public bool reactorJustIncited;

    // Box 3 (Crew Health) incited sub-indicators — empty/hidden until player action incites them.
    public string oxygenTag = string.Empty;
    public bool oxygenGood = true;
    public bool oxygenJustIncited;

    public string foodTag = string.Empty;
    public bool foodGood = true;
    public bool foodJustIncited;

    public string airTag = string.Empty;
    public bool airGood = true;
    public bool airJustIncited;

    public List<PlayerChoiceRecord> choiceHistory = new List<PlayerChoiceRecord>();

    // Backward-compatible alias for earlier integrity references.
    public int integrity
    {
        get => crewHealth;
        set => crewHealth = value;
    }

    public void Reset(
        int startPower = 90,
        int startCrewHealth = 85,
        int startSunlightBlocked = 10,
        string startRealDate = "May 30, 2038",
        string startSkyColorHex = "#C88B5A",
        float startDustIntensity = 0.15f)
    {
        sol = 1;
        power = Mathf.Clamp(startPower, MinStat, MaxStat);
        crewHealth = Mathf.Clamp(startCrewHealth, MinStat, MaxStat);
        sunlightBlockedPercent = Mathf.Clamp(startSunlightBlocked, MinStat, MaxStat);
        realDate = string.IsNullOrEmpty(startRealDate) ? "May 30, 2038" : startRealDate;
        skyColorHex = string.IsNullOrEmpty(startSkyColorHex) ? "#C88B5A" : startSkyColorHex;
        dustParticleIntensity = Mathf.Clamp01(startDustIntensity);
        activeConsequenceText = null;
        lastChoicePowerDelta = 0;
        lastChoiceHealthDelta = 0;
        lastChoiceOutcomeText = null;

        fuelTag = string.Empty;
        fuelGood = true;
        fuelJustIncited = false;
        tempTag = string.Empty;
        tempGood = true;
        tempJustIncited = false;
        reactorTag = string.Empty;
        reactorGood = true;
        reactorJustIncited = false;

        oxygenTag = string.Empty;
        oxygenGood = true;
        oxygenJustIncited = false;
        foodTag = string.Empty;
        foodGood = true;
        foodJustIncited = false;
        airTag = string.Empty;
        airGood = true;
        airJustIncited = false;

        if (choiceHistory == null)
        {
            choiceHistory = new List<PlayerChoiceRecord>();
        }
        else
        {
            choiceHistory.Clear();
        }

        if (flags == null)
        {
            flags = new HashSet<string>();
        }
        else
        {
            flags.Clear();
        }
    }

    public void SyncWithNode(StoryNode node)
    {
        activeConsequenceText = null;
        if (node == null)
        {
            return;
        }

        sol = Mathf.Clamp(node.EffectiveSol, 1, MaxSols);
        sunlightBlockedPercent = Mathf.Clamp(node.sunlightBlockedPercent, MinStat, MaxStat);
        if (!string.IsNullOrEmpty(node.realDate))
        {
            realDate = node.realDate;
        }
        if (!string.IsNullOrEmpty(node.skyColorHex))
        {
            skyColorHex = node.skyColorHex;
        }
        dustParticleIntensity = Mathf.Clamp01(node.dustParticleIntensity);

        List<string> impacts = new List<string>();
        bool legacyApplied = false;

        if (node.consequences != null)
        {
            for (int i = 0; i < node.consequences.Count; i++)
            {
                NodeConsequence c = node.consequences[i];
                if (c == null || string.IsNullOrWhiteSpace(c.checkFlag) || !HasFlag(c.checkFlag))
                {
                    continue;
                }

                if (string.Equals(c.checkFlag.Trim(), node.consequenceCheckFlag?.Trim()))
                {
                    legacyApplied = true;
                }

                // Clamp consequences to [1, 100] so earlier choices impact bars without skipping a Sol's choice.
                power = Mathf.Clamp(power + c.powerDelta, 1, MaxStat);
                crewHealth = Mathf.Clamp(crewHealth + c.healthDelta, 1, MaxStat);
                if (!string.IsNullOrEmpty(c.text))
                {
                    impacts.Add(c.text);
                }
            }
        }

        if (!legacyApplied
            && !string.IsNullOrWhiteSpace(node.consequenceCheckFlag)
            && HasFlag(node.consequenceCheckFlag))
        {
            power = Mathf.Clamp(power + node.consequencePowerDelta, 1, MaxStat);
            crewHealth = Mathf.Clamp(crewHealth + node.consequenceHealthDelta, 1, MaxStat);
            if (!string.IsNullOrEmpty(node.consequenceText))
            {
                impacts.Add(node.consequenceText);
            }
        }

        if (impacts.Count > 0)
        {
            activeConsequenceText = string.Join("\n", impacts);
        }
    }

    public bool HasChoiceBonus(Choice choice)
    {
        return choice != null
            && !string.IsNullOrWhiteSpace(choice.bonusIfFlag)
            && HasFlag(choice.bonusIfFlag);
    }

    public int GetEffectivePowerDelta(Choice choice)
    {
        if (choice == null)
        {
            return 0;
        }
        return choice.powerDelta + (HasChoiceBonus(choice) ? choice.bonusPowerDelta : 0);
    }

    public int GetEffectiveHealthDelta(Choice choice)
    {
        if (choice == null)
        {
            return 0;
        }
        return choice.EffectiveCrewHealthDelta + (HasChoiceBonus(choice) ? choice.bonusHealthDelta : 0);
    }

    public string GetEffectiveOutcomeText(Choice choice)
    {
        if (choice == null)
        {
            return string.Empty;
        }

        string baseOutcome = choice.EffectiveOutcome ?? string.Empty;
        if (HasChoiceBonus(choice) && !string.IsNullOrEmpty(choice.bonusNote))
        {
            return $"{baseOutcome}\n\n<color=#38C976><b>PAST CHOICE BONUS:</b> {choice.bonusNote}</color>";
        }
        return baseOutcome;
    }

    public void ApplyChoice(Choice choice, StoryNode sourceNode = null)
    {
        if (choice == null)
        {
            return;
        }

        fuelJustIncited = false;
        tempJustIncited = false;
        reactorJustIncited = false;
        oxygenJustIncited = false;
        foodJustIncited = false;
        airJustIncited = false;

        lastChoicePowerDelta = GetEffectivePowerDelta(choice);
        lastChoiceHealthDelta = GetEffectiveHealthDelta(choice);
        lastChoiceOutcomeText = GetEffectiveOutcomeText(choice);

        power = Mathf.Clamp(power + lastChoicePowerDelta, MinStat, MaxStat);
        crewHealth = Mathf.Clamp(crewHealth + lastChoiceHealthDelta, MinStat, MaxStat);

        if (!string.IsNullOrWhiteSpace(choice.setFlag))
        {
            SetFlag(choice.setFlag);
        }

        RecordSubsystemImpact(choice);
        RecordChoiceHistory(sourceNode, choice);
    }

    private void RecordSubsystemImpact(Choice choice)
    {
        string cat = choice?.subsystemCategory != null
            ? choice.subsystemCategory.Trim().ToUpperInvariant()
            : string.Empty;
        string tag = choice?.subsystemTag != null
            ? choice.subsystemTag.Trim()
            : string.Empty;

        if (string.IsNullOrEmpty(cat) || string.IsNullOrEmpty(tag))
        {
            return;
        }

        bool isGood = choice.subsystemGood;
        switch (cat)
        {
            case "FUEL":
                fuelTag = tag;
                fuelGood = isGood;
                fuelJustIncited = true;
                break;
            case "TEMP":
            case "TEMPERATURE":
                tempTag = tag;
                tempGood = isGood;
                tempJustIncited = true;
                break;
            case "REACTOR":
                reactorTag = tag;
                reactorGood = isGood;
                reactorJustIncited = true;
                break;
            case "OXYGEN":
                oxygenTag = tag;
                oxygenGood = isGood;
                oxygenJustIncited = true;
                break;
            case "FOOD":
            case "GREENHOUSE":
                foodTag = tag;
                foodGood = isGood;
                foodJustIncited = true;
                break;
            case "AIR":
            case "DUST":
                airTag = tag;
                airGood = isGood;
                airJustIncited = true;
                break;
        }
    }

    private void RecordChoiceHistory(StoryNode sourceNode, Choice choice)
    {
        if (choiceHistory == null)
        {
            choiceHistory = new List<PlayerChoiceRecord>();
        }

        PlayerChoiceRecord record = new PlayerChoiceRecord
        {
            sol = sourceNode != null ? sourceNode.EffectiveSol : sol,
            realDate = sourceNode != null && !string.IsNullOrEmpty(sourceNode.realDate) ? sourceNode.realDate : realDate,
            nodeTitle = sourceNode != null && !string.IsNullOrEmpty(sourceNode.title) ? sourceNode.title : $"Sol {sol} Decision",
            buttonLabel = choice.EffectiveLabel,
            description = choice.EffectiveDescription,
            outcomeText = lastChoiceOutcomeText,
            netPowerDelta = lastChoicePowerDelta,
            netHealthDelta = lastChoiceHealthDelta,
            powerAfter = power,
            healthAfter = crewHealth,
            subsystemCategory = choice.subsystemCategory,
            subsystemTag = choice.subsystemTag,
            subsystemGood = choice.subsystemGood,
            nasaComparisonTitle = choice.nasaComparisonTitle,
            nasaRealDecision = choice.nasaRealDecision,
            nasaFactEmphasis = choice.nasaFactEmphasis,
            nasaFactId = choice.nasaFactId,
            nasaId = choice.nasaId,
            imageFile = choice.imageFile,
            imageCaption = choice.imageCaption,
            nasaSource = choice.nasaSource,
            sourceUrl = choice.sourceUrl
        };

        choiceHistory.Add(record);
    }

    public bool HasFlag(string flagName)
    {
        return !string.IsNullOrWhiteSpace(flagName) && flags != null && flags.Contains(flagName.Trim());
    }

    public void SetFlag(string flagName)
    {
        if (string.IsNullOrWhiteSpace(flagName))
        {
            return;
        }
        if (flags == null)
        {
            flags = new HashSet<string>();
        }
        flags.Add(flagName.Trim());
    }

    public bool IsChoiceEligible(Choice choice)
    {
        if (choice == null)
        {
            return false;
        }
        if (!string.IsNullOrWhiteSpace(choice.requiredFlag) && !HasFlag(choice.requiredFlag))
        {
            return false;
        }
        if (choice.minPower > 0 && power < choice.minPower)
        {
            return false;
        }
        return true;
    }

    public bool IsEarlyLoss()
    {
        return power <= 0 || crewHealth <= 0;
    }

    public static string FormatSignedDelta(int delta)
    {
        if (delta > 0)
        {
            return $"+{delta}%";
        }
        return $"{delta}%";
    }
}
