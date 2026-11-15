using UnityEngine;

var gm = Object.FindAnyObjectByType<GameManager>();
var ui = Object.FindAnyObjectByType<MissionUI>();
var guide = Object.FindAnyObjectByType<MissionGuide>();

gm.StartMission();

string path = System.IO.Path.GetFullPath("screenshot_sol1_guide.png");
ScreenCapture.CaptureScreenshot(path);

return "StartMission called. Guide present: " + (guide != null) + ", File scheduled to: " + path;
