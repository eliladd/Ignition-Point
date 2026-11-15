using UnityEngine;

var gm = Object.FindAnyObjectByType<GameManager>();
var ui = Object.FindAnyObjectByType<MissionUI>();
ui.Initialize(gm);
var guide = Object.FindAnyObjectByType<MissionGuide>();
return "After Initialize -> Guide found: " + (guide != null);
