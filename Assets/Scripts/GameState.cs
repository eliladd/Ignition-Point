using System;
using System.Collections.Generic;
using UnityEngine;

public class GameState
{
    public const int MinStat = 0;
    public const int MaxStat = 100;
    public const int MaxSols = 5;

    public int sol = 1;
    public int power = 80;
    public int integrity = 90;
    public int science = 0;
    public float dustTau = 1.5f;
    public HashSet<string> flags = new HashSet<string>();

    public void Reset(int startPower = 80, int startIntegrity = 90, int startScience = 0, float startTau = 1.5f)
    {
        sol = 1;
        power = Mathf.Clamp(startPower, MinStat, MaxStat);
        integrity = Mathf.Clamp(startIntegrity, MinStat, MaxStat);
        science = Mathf.Clamp(startScience, MinStat, MaxStat);
        dustTau = startTau;
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
        if (node == null)
        {
            return;
        }
        sol = Mathf.Clamp(node.sol, 1, MaxSols);
        dustTau = node.dustTau;
    }

    public void ApplyChoice(Choice choice)
    {
        if (choice == null)
        {
            return;
        }

        power = Mathf.Clamp(power + choice.powerDelta, MinStat, MaxStat);
        integrity = Mathf.Clamp(integrity + choice.integrityDelta, MinStat, MaxStat);
        science = Mathf.Clamp(science + choice.scienceDelta, MinStat, MaxStat);

        if (!string.IsNullOrWhiteSpace(choice.setFlag))
        {
            SetFlag(choice.setFlag);
        }
    }

    public bool HasFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return true;
        }
        return flags != null && flags.Contains(flag.Trim());
    }

    public void SetFlag(string flag)
    {
        if (string.IsNullOrWhiteSpace(flag))
        {
            return;
        }
        if (flags == null)
        {
            flags = new HashSet<string>();
        }
        flags.Add(flag.Trim());
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
        return power <= 0 || integrity <= 0;
    }
}
