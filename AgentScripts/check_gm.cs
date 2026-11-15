using System.Reflection;
using UnityEngine;

var gm = Object.FindAnyObjectByType<GameManager>();
var fPhase = gm.GetType().GetField("phase", BindingFlags.NonPublic | BindingFlags.Instance);
var fIdx = gm.GetType().GetField("introScreenIndex", BindingFlags.NonPublic | BindingFlags.Instance);
var fData = gm.GetType().GetField("missionData", BindingFlags.NonPublic | BindingFlags.Instance);
var data = fData.GetValue(gm) as MissionData;

return "phase: " + fPhase.GetValue(gm) + ", introScreenIndex: " + fIdx.GetValue(gm) + ", data != null: " + (data != null) + ", storyNodes: " + (data != null && data.storyNodes != null ? data.storyNodes.Count : -1);
