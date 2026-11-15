using System.IO;
using UnityEngine;

var guide = Object.FindAnyObjectByType<MissionGuide>();
if (guide != null)
{
    guide.SelectSpeaker(MissionGuide.Speaker.Nova, false);
    guide.SkipTypewriter();
}

var canvas = Object.FindAnyObjectByType<Canvas>();
var cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();
if (cam == null)
{
    var camObj = new GameObject("TempCaptureCam");
    cam = camObj.AddComponent<Camera>();
}

var oldMode = canvas.renderMode;
var oldCam = canvas.worldCamera;

canvas.renderMode = RenderMode.ScreenSpaceCamera;
canvas.worldCamera = cam;

int w = 1920;
int h = 1080;
RenderTexture rt = new RenderTexture(w, h, 24);
cam.targetTexture = rt;
cam.Render();
cam.targetTexture = null;

RenderTexture.active = rt;
Texture2D tex = new Texture2D(w, h, TextureFormat.RGB24, false);
tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
tex.Apply();
RenderTexture.active = null;

canvas.renderMode = oldMode;
canvas.worldCamera = oldCam;

byte[] bytes = tex.EncodeToPNG();
Object.DestroyImmediate(tex);
Object.DestroyImmediate(rt);

string outPath = Path.GetFullPath("sol1_nova_hint.png");
File.WriteAllBytes(outPath, bytes);

return "Nova hint captured! Saved " + bytes.Length + " bytes to " + outPath;
