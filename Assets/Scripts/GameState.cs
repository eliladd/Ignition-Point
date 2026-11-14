using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Runtime Ares Base telemetry and storm state across the 5-Sol Mars mission.
/// </summary>
public class GameState
{
    public const int MinStat = 0;
    public const int MaxStat = 100;
    public const int MaxSols = 10;

    public int sol = 1;
    public int power = 75;
    public int crewHealth = 75;
    public int sunlightBlockedPercent = 10;
    public string realDate = "May 30, 2038";
    public string skyColorHex = "#C88B5A";
    public float dustParticleIntensity = 0.15f;
    public string activeConsequenceText;
    public HashSet<string> flags = new HashSet<string>();

    // Backward-compatible alias for earlier integrity references.
    public int integrity
    {
        get => crewHealth;
        set => crewHealth = value;
    }

    public void Reset(
        int startPower = 75,
        int startCrewHealth = 75,
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

        if (!string.IsNullOrWhiteSpace(node.consequenceCheckFlag) && HasFlag(node.consequenceCheckFlag))
        {
            power = Mathf.Clamp(power + node.consequencePowerDelta, MinStat, MaxStat);
            crewHealth = Mathf.Clamp(crewHealth + node.consequenceHealthDelta, MinStat, MaxStat);
            if (!string.IsNullOrEmpty(node.consequenceText))
            {
                activeConsequenceText = node.consequenceText;
            }
        }
    }

    public void ApplyChoice(Choice choice)
    {
        if (choice == null)
        {
            return;
        }

        power = Mathf.Clamp(power + choice.powerDelta, MinStat, MaxStat);
        crewHealth = Mathf.Clamp(crewHealth + choice.EffectiveCrewHealthDelta, MinStat, MaxStat);

        if (!string.IsNullOrWhiteSpace(choice.setFlag))
        {
            SetFlag(choice.setFlag);
        }
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
