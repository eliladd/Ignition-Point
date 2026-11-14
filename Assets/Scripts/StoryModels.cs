using System;
using System.Collections.Generic;

/// <summary>
/// Serializable data structures matching Assets/Resources/mission_data.json
/// and Assets/Resources/MarsStorm2018_Story.json for Unity JsonUtility.
/// </summary>
[Serializable]
public class MissionData
{
    public string gameTitle;
    public StartingStats startingStats;
    public List<IntroScreen> introScreens = new List<IntroScreen>();
    public List<GlossaryEntry> kidGlossary = new List<GlossaryEntry>();
    public List<StoryNode> storyNodes = new List<StoryNode>();
    public List<StoryEnding> endings = new List<StoryEnding>();
    public RealLifeComparison realLifeComparison;

    // Legacy placeholder compatibility fields.
    public string startNodeId;
    public List<StoryNode> nodes = new List<StoryNode>();

    public List<StoryNode> GetNodes()
    {
        if (storyNodes != null && storyNodes.Count > 0)
        {
            return storyNodes;
        }
        return nodes ?? new List<StoryNode>();
    }

    public GlossaryEntry FindGlossaryEntry(string key)
    {
        if (kidGlossary == null || string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        string trimmed = key.Trim();
        for (int i = 0; i < kidGlossary.Count; i++)
        {
            GlossaryEntry entry = kidGlossary[i];
            if (entry == null)
            {
                continue;
            }

            if (string.Equals(entry.id, trimmed, StringComparison.OrdinalIgnoreCase)
                || string.Equals(entry.term, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }
        return null;
    }

    public StoryEnding GetEnding(string endingId)
    {
        if (endings == null || string.IsNullOrEmpty(endingId))
        {
            return null;
        }

        for (int i = 0; i < endings.Count; i++)
        {
            StoryEnding candidate = endings[i];
            if (candidate != null && string.Equals(candidate.endingId, endingId, StringComparison.Ordinal))
            {
                return candidate;
            }
        }
        return null;
    }
}

/// <summary>
/// Typed subclass matching Story json.txt Unity loading snippet.
/// </summary>
[Serializable]
public class GameStoryData : MissionData
{
}

[Serializable]
public class StartingStats
{
    public int power = 75;
    public int crewHealth = 75;
}

[Serializable]
public class IntroScreen
{
    public int screenNumber;
    public string headline;
    public string bodyText;
    public string nasaSource;
}

[Serializable]
public class GlossaryEntry
{
    public string id;
    public string headline;
    public string term;
    public string simpleDefinition;

    public string EffectiveKey => !string.IsNullOrEmpty(id) ? id : term;
    public string EffectiveTitle => !string.IsNullOrEmpty(headline) ? headline : term;
}

[Serializable]
public class StoryNode
{
    public string nodeId;
    public int dayNumber = 1;
    public string realDate;
    public string title;
    public int sunlightBlockedPercent;
    public string skyColorHex;
    public float dustParticleIntensity;
    public string nasaFactBanner;
    public string storyText;
    public Choice choiceA;
    public Choice choiceB;

    // Stateful multi-Sol consequence triggered when an earlier decision flag is present.
    public string consequenceCheckFlag;
    public string consequenceText;
    public int consequencePowerDelta;
    public int consequenceHealthDelta;

    // Legacy placeholder compatibility fields.
    public string id;
    public int sol = 1;
    public float dustTau;
    public string prompt;
    public bool isEnding;
    public string endingType;
    public List<Choice> choices = new List<Choice>();

    public string EffectiveId => !string.IsNullOrEmpty(nodeId) ? nodeId : id;
    public int EffectiveSol => dayNumber > 0 ? dayNumber : sol;
    public string EffectiveStoryText => !string.IsNullOrEmpty(storyText) ? storyText : prompt;

    public List<Choice> GetChoices()
    {
        List<Choice> result = new List<Choice>(2);
        if (choiceA != null && !string.IsNullOrEmpty(choiceA.EffectiveLabel))
        {
            result.Add(choiceA);
        }
        if (choiceB != null && !string.IsNullOrEmpty(choiceB.EffectiveLabel))
        {
            result.Add(choiceB);
        }
        if (result.Count > 0)
        {
            return result;
        }
        return choices ?? new List<Choice>();
    }
}

[Serializable]
public class Choice
{
    public string buttonLabel;
    public string description;
    public string subLabel;
    public string outcomeText;
    public int powerDelta;
    public int crewHealthDelta;
    public string nextNodeId;

    // Legacy placeholder compatibility fields.
    public string label;
    public int integrityDelta;
    public int scienceDelta;
    public string requiredFlag;
    public int minPower;
    public string setFlag;
    public string outcome;
    public string nasaNote;

    public string EffectiveLabel => !string.IsNullOrEmpty(buttonLabel) ? buttonLabel : label;
    public string EffectiveDescription => description ?? string.Empty;
    public string EffectiveSubLabel => !string.IsNullOrEmpty(subLabel) ? subLabel : BuildFallbackSubLabel();
    public string EffectiveOutcome => !string.IsNullOrEmpty(outcomeText) ? outcomeText : outcome;
    public int EffectiveCrewHealthDelta => crewHealthDelta != 0 ? crewHealthDelta : integrityDelta;

    private string BuildFallbackSubLabel()
    {
        string p = powerDelta >= 0 ? $"+{powerDelta}" : powerDelta.ToString();
        int health = EffectiveCrewHealthDelta;
        string h = health >= 0 ? $"+{health}" : health.ToString();
        return $"{p} Power | {h} Crew Health";
    }
}

[Serializable]
public class StoryEnding
{
    public string endingId;
    public string conditionDescription;
    public int minPower;
    public int minCrewHealth;
    public string realDate = "July 23, 2018";
    public string headline;
    public string skyColorHex;
    public string storyText;
}

[Serializable]
public class RealLifeComparison
{
    public string title;
    public List<ComparisonCard> cards = new List<ComparisonCard>();
}

[Serializable]
public class ComparisonCard
{
    public string topic;
    public string whatHappenedInRealLife;
    public string howItConnectsToGame;
}
