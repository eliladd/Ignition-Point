using System;
using System.Collections.Generic;

[Serializable]
public class MissionData
{
    public string startNodeId;
    public List<StoryNode> nodes = new List<StoryNode>();
    public List<StoryNode> storyNodes = new List<StoryNode>();

    public List<StoryNode> GetNodes()
    {
        if (nodes != null && nodes.Count > 0)
        {
            return nodes;
        }
        return storyNodes ?? new List<StoryNode>();
    }
}

[Serializable]
public class StoryNode
{
    public string id;
    public int sol = 1;
    public string title;
    public float dustTau;
    public string prompt;
    public bool isEnding;
    public string endingType;
    public List<Choice> choices = new List<Choice>();
}

[Serializable]
public class Choice
{
    public string label;
    public string nextNodeId;
    public int powerDelta;
    public int integrityDelta;
    public int scienceDelta;
    public string requiredFlag;
    public int minPower;
    public string setFlag;
    public string outcome;
    public string nasaNote;
}
