using UnityEngine;

int width = Screen.width > 0 ? Screen.width : 1920;
int height = Screen.height > 0 ? Screen.height : 1080;

RenderTexture rt = new RenderTexture(width, height, 24);
Camera cam = Camera.main != null ? Camera.main : Object.FindAnyObjectByType<Camera>();

if (cam != null)
{
    var oldRt = cam.targetTexture;
    cam.targetTexture = rt;
    cam.Render();
    cam.targetTexture = oldRt;

    RenderTexture.active = rt;
    Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
    tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
    tex.Apply();
    RenderTexture.active = null;

    byte[] bytes = tex.EncodeToPNG();
    Object.DestroyImmediate(tex);
    Object.DestroyImmediate(rt);

    string outPath = System.IO.Path.GetFullPath("screenshot_sol1_captured.png");
    System.IO.File.WriteAllBytes(outPath, bytes);
    return "Saved synchronous screenshot to: " + outPath + " (" + bytes.Length + " bytes)";
}

return "Camera not found!";
