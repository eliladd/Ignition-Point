using System.IO;
using System.Reflection;
using UnityEngine;

var gm = Object.FindAnyObjectByType<GameManager>();
var ui = Object.FindAnyObjectByType<MissionUI>();

var fTitle = ui.GetType().GetField("titlePanel", BindingFlags.NonPublic | BindingFlags.Instance);
var fDec = ui.GetType().GetField("decisionPanel", BindingFlags.NonPublic | BindingFlags.Instance);

var titleObj = fTitle.GetValue(ui) as GameObject;
var decObj = fDec.GetValue(ui) as GameObject;

return "titlePanel active: " + (titleObj != null ? titleObj.activeSelf : false) +
       ", decisionPanel active: " + (decObj != null ? decObj.activeSelf : false);
