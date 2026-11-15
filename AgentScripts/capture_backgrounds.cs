using System;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public static class CaptureHelper
{
    public static void SaveCanvasScreenshot(Canvas canvas, string outPath)
    {
        Camera cam = Camera.main;
        if (cam == null)
        {
            var camGo = new GameObject("TempCaptureCam");
            cam = camGo.AddComponent<Camera>();
        }

        var oldMode = canvas.renderMode;
        var oldCam = canvas.worldCamera;
        var oldPlaneDist = canvas.planeDistance;

        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        var prevTarget = cam.targetTexture;
        var prevActive = RenderTexture.active;

        try
        {
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = cam;
            canvas.planeDistance = 10f;
            Canvas.ForceUpdateCanvases();

            cam.targetTexture = rt;
            cam.Render();

            RenderTexture.active = rt;
            var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            tex.Apply();

            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            File.WriteAllBytes(outPath, tex.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(tex);
        }
        finally
        {
            cam.targetTexture = prevTarget;
            RenderTexture.active = prevActive;
            rt.Release();
            UnityEngine.Object.DestroyImmediate(rt);

            canvas.renderMode = oldMode;
            canvas.worldCamera = oldCam;
            canvas.planeDistance = oldPlaneDist;
            Canvas.ForceUpdateCanvases();
        }
    }

    public static bool ClickButtonContaining(Canvas canvas, string textPart)
    {
        var buttons = canvas.GetComponentsInChildren<Button>(false);
        foreach (var b in buttons)
        {
            if (!b.gameObject.activeInHierarchy || !b.interactable) continue;
            var tmp = b.GetComponentInChildren<TMP_Text>();
            if (tmp != null && tmp.text != null && tmp.text.IndexOf(textPart, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                b.onClick.Invoke();
                return true;
            }
        }
        return false;
    }
}

string outDir = @"C:\Users\elith\.gemini\antigravity\brain\c5e2c043-08ee-42e5-8ffc-73ea935cb6a9\scratch";
var canvas = UnityEngine.Object.FindAnyObjectByType<Canvas>();
if (canvas == null) return "No Canvas found";

// Wait a moment if GameManager hasn't finished Start
await Task.Delay(400);
CaptureHelper.SaveCanvasScreenshot(canvas, Path.Combine(outDir, "verify_01_title.png"));

// Click START BRIEFING -> NEXT -> LAUNCH MISSION until Decision Panel is active
for (int i = 0; i < 8; i++)
{
    if (CaptureHelper.ClickButtonContaining(canvas, "START BRIEFING") ||
        CaptureHelper.ClickButtonContaining(canvas, "NEXT") ||
        CaptureHelper.ClickButtonContaining(canvas, "LAUNCH") ||
        CaptureHelper.ClickButtonContaining(canvas, "BEGIN"))
    {
        await Task.Delay(300);
    }
    var dc = GameObject.Find("DilemmaCard");
    if (dc != null && dc.activeInHierarchy) break;
}

await Task.Delay(400);
CaptureHelper.SaveCanvasScreenshot(canvas, Path.Combine(outDir, "verify_02_sol1_decision.png"));

// Click first choice card
var buttons = canvas.GetComponentsInChildren<Button>(false);
foreach (var b in buttons)
{
    if (!b.gameObject.activeInHierarchy) continue;
    if (b.gameObject.name.Contains("ChoiceCard") || b.gameObject.name.Contains("Option"))
    {
        b.onClick.Invoke();
        break;
    }
}

await Task.Delay(400);
CaptureHelper.SaveCanvasScreenshot(canvas, Path.Combine(outDir, "verify_03_sol1_outcome.png"));

// Click SOL 2 >
CaptureHelper.ClickButtonContaining(canvas, "SOL 2");
await Task.Delay(1200);
CaptureHelper.SaveCanvasScreenshot(canvas, Path.Combine(outDir, "verify_04_sol2_video_bg.png"));

return "Captured all 4 screens";
