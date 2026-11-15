using System.IO;
using System.Reflection;
using UnityEngine;

var gm = Object.FindAnyObjectByType<GameManager>();
gm.SendMessage("Awake");
gm.SendMessage("Start");

var fData = gm.GetType().GetField("missionData", BindingFlags.NonPublic | BindingFlags.Instance);
var data = fData.GetValue(gm) as MissionData;

return "After Awake/Start: data != null: " + (data != null) + ", storyNodes: " + (data != null && data.storyNodes != null ? data.storyNodes.Count : -1);
