using UnityEngine;

Texture2D tex = ScreenCapture.CaptureScreenshotAsTexture();
if (tex != null)
{
    byte[] bytes = tex.EncodeToPNG();
    Object.DestroyImmediate(tex);
    string path = System.IO.Path.GetFullPath("gameview_screen.png");
    System.IO.File.WriteAllBytes(path, bytes);
    return "Saved gameview to: " + path + " (" + bytes.Length + " bytes)";
}
return "tex is null";
