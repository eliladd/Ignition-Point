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
    public string sourceUrl;
    public string guideLine;
}

[Serializable]
public class GlossaryEntry
{
    public string id;
    public string headline;
    public string term;
    public string termMeaning;
    public string simpleDefinition;
    public string nasaId;
    public string imageFile;
    public string imageCaption;
    public string nasaSource;
    public string sourceUrl;

    public string EffectiveKey => !string.IsNullOrEmpty(id) ? id : term;
    public string EffectiveTitle => !string.IsNullOrEmpty(headline) ? headline : term;
}

[Serializable]
public class NodeConsequence
{
    public string checkFlag;
    public string text;
    public int powerDelta;
    public int healthDelta;
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
    public string sourceUrl;
    public string storyText;
    public Choice choiceA;
    public Choice choiceB;
    public Choice choiceC;

    // Multi-flag stateful consequences evaluated when entering this Sol.
    public List<NodeConsequence> consequences = new List<NodeConsequence>();

    // Legacy single-flag consequence compatibility fields.
    public string guideTip;
    public string novaHint;
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
        List<Choice> result = new List<Choice>(3);
        if (choiceA != null && !string.IsNullOrEmpty(choiceA.EffectiveLabel))
        {
            result.Add(choiceA);
        }
        if (choiceB != null && !string.IsNullOrEmpty(choiceB.EffectiveLabel))
        {
            result.Add(choiceB);
        }
        if (choiceC != null && !string.IsNullOrEmpty(choiceC.EffectiveLabel))
        {
            result.Add(choiceC);
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
    public string guideReaction;
    public int powerDelta;
    public int crewHealthDelta;
    public string nextNodeId;

    // Past-decision bonus when an earlier engineering flag unlocks an upgrade.
    public string bonusIfFlag;
    public int bonusPowerDelta;
    public int bonusHealthDelta;
    public string bonusNote;

    // Dynamically incited HUD subsystem indicator (stays hidden until player triggers this category).
    public string subsystemCategory;
    public string subsystemTag;
    public bool subsystemGood = true;

    // Personalized NASA comparison data and unique NASA image per option.
    public string nasaComparisonTitle;
    public string nasaRealDecision;
    public string nasaFactEmphasis;
    public string nasaFactId;
    public string nasaId;
    public string imageFile;
    public string imageCaption;
    public string nasaSource;
    public string sourceUrl;

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
        string p = powerDelta > 0
            ? $"+{powerDelta}% Generated"
            : powerDelta == 0
                ? "0% Conserved"
                : $"{powerDelta}% Power";
        int health = EffectiveCrewHealthDelta;
        string h = health > 0 ? $"+{health}% Health" : health == 0 ? "0% Health" : $"{health}% Health";
        return $"{p} | {h}";
    }
}

[Serializable]
public class PlayerChoiceRecord
{
    public int sol;
    public string realDate;
    public string nodeTitle;
    public string buttonLabel;
    public string description;
    public string outcomeText;
    public int netPowerDelta;
    public int netHealthDelta;
    public int powerAfter;
    public int healthAfter;
    public string subsystemCategory;
    public string subsystemTag;
    public bool subsystemGood = true;
    public string nasaComparisonTitle;
    public string nasaRealDecision;
    public string nasaFactEmphasis;
    public string nasaFactId;
    public string nasaId;
    public string imageFile;
    public string imageCaption;
    public string nasaSource;
    public string sourceUrl;
}

[Serializable]
public class StoryEnding
{
    public string endingId;
    public string conditionDescription;
    public string guideLine;
    public int minPower;
    public int minCrewHealth;
    public string realDate = "July 23, 2038";
    public string headline;
    public string skyColorHex;
    public string storyText;
    public string nasaId;
    public string imageFile;
    public string imageCaption;
    public string nasaSource;
    public string sourceUrl;
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
