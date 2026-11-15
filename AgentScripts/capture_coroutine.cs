using System.Collections;
using System.IO;
using UnityEngine;

public class ScreenCapturer : MonoBehaviour
{
    public static void RunCapture(string filename)
    {
        var go = new GameObject("TempScreenCapturer");
        var comp = go.AddComponent<ScreenCapturer>();
        comp.StartCoroutine(comp.DoCapture(filename));
    }

    private IEnumerator DoCapture(string filename)
    {
        yield return new WaitForEndOfFrame();
        Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
        if (tex != null)
        {
            byte[] bytes = tex.EncodeToPNG();
            DestroyImmediate(tex);
            string fullPath = Path.GetFullPath(filename);
            File.WriteAllBytes(fullPath, bytes);
            Debug.Log("[ScreenCapturer] Wrote " + bytes.Length + " bytes to " + fullPath);
        }
        else
        {
            Debug.LogWarning("[ScreenCapturer] tex was null!");
        }
        Destroy(gameObject);
    }
}

ScreenCapturer.RunCapture("sol1_companion_verified.png");
return "Scheduled coroutine capture.";
