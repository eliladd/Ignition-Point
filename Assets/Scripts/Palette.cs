using UnityEngine;

/// <summary>
/// Art Bible single-source palette for Ignition Point: Mars Dust Storm 2018 Mission Control.
/// Style: NASA JPL Deep-Space Telemetry Console — dark obsidian-slate panels, Martian amber headers,
/// high-contrast resource bars, and glowing cyan Real NASA Science callout boxes.
/// </summary>
public static class Palette
{
    public static readonly Color BackgroundDeep   = FromHex("#0D121A");
    public static readonly Color PanelSurface     = FromHex("#17202E");
    public static readonly Color PanelElevated    = FromHex("#212E42");
    public static readonly Color ButtonHover      = FromHex("#2C3E59");
    public static readonly Color ButtonPressed    = FromHex("#1A2536");
    public static readonly Color PanelBorder      = FromHex("#364B68");

    public static readonly Color TextPrimary      = FromHex("#F2F5FA");
    public static readonly Color TextSecondary    = FromHex("#A8B8CC");

    public static readonly Color MarsAccent       = FromHex("#E87A3E");
    public static readonly Color PowerGold        = FromHex("#F5B841");
    public static readonly Color IntegrityGreen   = FromHex("#38C976");
    public static readonly Color DangerRed        = FromHex("#E64848");
    public static readonly Color ScienceCyan      = FromHex("#2CC5E8");

    public static readonly Color NasaBoxSurface   = FromHex("#132C3D");
    public static readonly Color BarTrack         = FromHex("#0A0E14");

    // Interactive & funky Mars Mission Console accents
    public static readonly Color FunkyCyan        = FromHex("#00F5D4");
    public static readonly Color FunkyGold        = FromHex("#FFD166");
    public static readonly Color FunkyCoral       = FromHex("#FF5E7E");
    public static readonly Color FunkyPurple      = FromHex("#9B5DE5");
    public static readonly Color FunkyBlue        = FromHex("#00BBF9");
    public static readonly Color FunkyMint        = FromHex("#06D6A0");

    public static readonly Color CardSurfaceOption1 = FromHex("#15283E");
    public static readonly Color CardSurfaceOption2 = FromHex("#241F3D");
    public static readonly Color CardSurfaceOption3 = FromHex("#143336");
    public static readonly Color CardSurfaceHud     = FromHex("#182538");

    public static Color FromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c))
        {
            return c;
        }
        return Color.white;
    }
}
