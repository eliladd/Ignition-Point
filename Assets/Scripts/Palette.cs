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

    public static Color FromHex(string hex)
    {
        if (ColorUtility.TryParseHtmlString(hex, out Color c))
        {
            return c;
        }
        return Color.white;
    }
}
