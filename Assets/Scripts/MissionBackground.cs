using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Full-screen rotating NASA background controller for Ares Sols: 2038.
/// Displays high-resolution official NASA Mars photos and videos edge-to-edge.
/// across the entire screen, rotating by Sol and game phase.
/// </summary>
public sealed class MissionBackground : MonoBehaviour
{
    private enum MediaKind
    {
        ImageResource,
        VideoStreamingAsset
    }

    private struct BackgroundEntry
    {
        public MediaKind kind;
        public string pathOrResource;
        public string fallbackImageResource;
    }

    private RectTransform rootRect;
    private RawImage mediaRawImage;
    private AspectRatioFitter aspectFitter;
    private Image readabilityOverlay;
    private VideoPlayer videoPlayer;
    private RenderTexture videoRenderTexture;
    private Texture2D currentLoadedTexture;

    private int lastAssignedSol = -999;
    private bool isShowingVideo;

    private static readonly BackgroundEntry[] SolRotationEntries = new[]
    {
        // Index 0: Title / Intro Briefing (Video: The Martian Dust Storm of June 2018)
        new BackgroundEntry
        {
            kind = MediaKind.VideoStreamingAsset,
            pathOrResource = "nasa_vid_01.mp4",
            fallbackImageResource = "NasaBackgrounds/nasa_web_PIA24937"
        },
        // Sol 1: Curiosity's Gale Crater Landscape (Official NASA)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_web_PIA24937",
            fallbackImageResource = "NasaBackgrounds/nasa_web_PIA15691"
        },
        // Sol 2: Global Spread of June 2018 Dust Storm (Attached Video)
        new BackgroundEntry
        {
            kind = MediaKind.VideoStreamingAsset,
            pathOrResource = "nasa_vid_02.mp4",
            fallbackImageResource = "NasaBackgrounds/nasa_local_03"
        },
        // Sol 3: Two Surface Images Showing Atmospheric Effects (Attached Image)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_local_03",
            fallbackImageResource = "NasaBackgrounds/nasa_web_PIA22519"
        },
        // Sol 4: 2018 Giant Dust Storm on Mars (Official NASA)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_web_PIA22519",
            fallbackImageResource = "NasaBackgrounds/nasa_local_02"
        },
        // Sol 5: Animation of Optical Depth During 2018 Storm (Attached Video)
        new BackgroundEntry
        {
            kind = MediaKind.VideoStreamingAsset,
            pathOrResource = "nasa_vid_03.webm",
            fallbackImageResource = "NasaBackgrounds/nasa_local_02"
        },
        // Sol 6: Atmospheric Opacity Around Rover (Attached Image)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_local_02",
            fallbackImageResource = "NasaBackgrounds/nasa_web_PIA23513"
        },
        // Sol 7: A Mars Dust Tower Stands Out (Official NASA)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_web_PIA23513",
            fallbackImageResource = "NasaBackgrounds/nasa_local_01"
        },
        // Sol 8: Rotating Globes May 28 - July 1 (Attached Video)
        new BackgroundEntry
        {
            kind = MediaKind.VideoStreamingAsset,
            pathOrResource = "nasa_vid_04.webm",
            fallbackImageResource = "NasaBackgrounds/nasa_local_01"
        },
        // Sol 9: June 2018 Dust Storm Science Observations (Attached Image)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_local_01",
            fallbackImageResource = "NasaBackgrounds/nasa_web_PIA26680"
        },
        // Sol 10: Curiosity Mount Sharp Boxwork Region (Official NASA)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_web_PIA26680",
            fallbackImageResource = "NasaBackgrounds/nasa_web_PIA15691"
        },
        // Index 11: Ending / Summary (Official NASA First Color Landscape)
        new BackgroundEntry
        {
            kind = MediaKind.ImageResource,
            pathOrResource = "NasaBackgrounds/nasa_web_PIA15691",
            fallbackImageResource = "NasaBackgrounds/nasa_web_PIA24937"
        }
    };

    public static MissionBackground Attach(Transform canvasRoot)
    {
        if (canvasRoot == null)
        {
            return null;
        }

        MissionBackground existing = canvasRoot.GetComponentInChildren<MissionBackground>(true);
        if (existing != null)
        {
            existing.EnsureBuilt();
            existing.transform.SetAsFirstSibling();
            return existing;
        }

        GameObject bgObj = new GameObject("MissionBackground", typeof(RectTransform), typeof(MissionBackground));
        bgObj.transform.SetParent(canvasRoot, false);
        bgObj.transform.SetAsFirstSibling();

        MissionBackground bg = bgObj.GetComponent<MissionBackground>();
        bg.EnsureBuilt();
        return bg;
    }

    private void EnsureBuilt()
    {
        if (rootRect != null)
        {
            return;
        }

        rootRect = GetComponent<RectTransform>();
        rootRect.anchorMin = Vector2.zero;
        rootRect.anchorMax = Vector2.one;
        rootRect.offsetMin = Vector2.zero;
        rootRect.offsetMax = Vector2.zero;

        // Add RectMask2D so EnvelopeParent overscan stays cleanly inside the screen bounds
        gameObject.AddComponent<RectMask2D>();

        // Media Display RawImage (covers entire screen using EnvelopeParent)
        GameObject mediaObj = new GameObject("BackgroundMedia", typeof(RectTransform), typeof(RawImage), typeof(AspectRatioFitter));
        mediaObj.transform.SetParent(rootRect, false);
        RectTransform mediaRt = mediaObj.GetComponent<RectTransform>();
        mediaRt.anchorMin = Vector2.zero;
        mediaRt.anchorMax = Vector2.one;
        mediaRt.offsetMin = Vector2.zero;
        mediaRt.offsetMax = Vector2.zero;

        mediaRawImage = mediaObj.GetComponent<RawImage>();
        mediaRawImage.color = Color.white;
        mediaRawImage.raycastTarget = false;

        aspectFitter = mediaObj.GetComponent<AspectRatioFitter>();
        aspectFitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        aspectFitter.aspectRatio = 16f / 9f;

        // Subtle atmospheric readability overlay so UI cards and text always pop cleanly
        GameObject tintObj = new GameObject("AtmosphericReadabilityTint", typeof(RectTransform), typeof(Image));
        tintObj.transform.SetParent(rootRect, false);
        RectTransform tintRt = tintObj.GetComponent<RectTransform>();
        tintRt.anchorMin = Vector2.zero;
        tintRt.anchorMax = Vector2.one;
        tintRt.offsetMin = Vector2.zero;
        tintRt.offsetMax = Vector2.zero;

        readabilityOverlay = tintObj.GetComponent<Image>();
        readabilityOverlay.color = new Color(0.05f, 0.06f, 0.09f, 0.34f);
        readabilityOverlay.raycastTarget = false;

        // VideoPlayer setup
        videoPlayer = gameObject.AddComponent<VideoPlayer>();
        videoPlayer.playOnAwake = false;
        videoPlayer.isLooping = true;
        videoPlayer.renderMode = VideoRenderMode.RenderTexture;
        videoPlayer.audioOutputMode = VideoAudioOutputMode.None;
        videoPlayer.aspectRatio = VideoAspectRatio.Stretch;
        videoPlayer.prepareCompleted += OnVideoPrepared;
        videoPlayer.errorReceived += OnVideoError;
    }

    public void UpdateTint(Color skyColor, float dustIntensity)
    {
        EnsureBuilt();
        if (readabilityOverlay == null)
        {
            return;
        }

        // Blend dark space navy with the Sol's Martian sky color for atmosphere + UI legibility
        Color baseDark = new Color(0.04f, 0.05f, 0.08f, 1f);
        Color tinted = Color.Lerp(baseDark, skyColor, Mathf.Lerp(0.18f, 0.45f, Mathf.Clamp01(dustIntensity)));
        tinted.a = Mathf.Lerp(0.30f, 0.48f, Mathf.Clamp01(dustIntensity));
        readabilityOverlay.color = tinted;
    }

    public void ShowForPhaseOrSol(int rotationIndex)
    {
        EnsureBuilt();
        int clamped = Mathf.Clamp(rotationIndex, 0, SolRotationEntries.Length - 1);
        if (clamped == lastAssignedSol)
        {
            return;
        }
        lastAssignedSol = clamped;

        BackgroundEntry entry = SolRotationEntries[clamped];
        if (entry.kind == MediaKind.VideoStreamingAsset)
        {
            string fullVideoPath = Path.Combine(Application.streamingAssetsPath, "NasaBackgrounds", entry.pathOrResource);
            if (File.Exists(fullVideoPath))
            {
                PlayVideo(fullVideoPath, entry.fallbackImageResource);
                return;
            }
        }

        LoadImage(entry.pathOrResource, entry.fallbackImageResource);
    }

    private void PlayVideo(string filePath, string fallbackResource)
    {
        StopVideo();
        // Show fallback image immediately while video prepares
        if (!string.IsNullOrEmpty(fallbackResource))
        {
            LoadImage(fallbackResource, null);
        }

        isShowingVideo = true;
        if (videoRenderTexture == null)
        {
            videoRenderTexture = new RenderTexture(1920, 1080, 0, RenderTextureFormat.ARGB32)
            {
                name = "NasaBgVideoRT",
                hideFlags = HideFlags.HideAndDontSave
            };
        }

        videoPlayer.targetTexture = videoRenderTexture;
        videoPlayer.source = VideoSource.Url;
        videoPlayer.url = new Uri(filePath).AbsoluteUri;
        videoPlayer.Prepare();
    }

    private void OnVideoPrepared(VideoPlayer vp)
    {
        if (!isShowingVideo || vp == null || mediaRawImage == null)
        {
            return;
        }

        if (vp.width > 0 && vp.height > 0 && aspectFitter != null)
        {
            aspectFitter.aspectRatio = (float)vp.width / vp.height;
        }

        mediaRawImage.texture = videoRenderTexture;
        mediaRawImage.color = Color.white;
        vp.Play();
    }

    private void OnVideoError(VideoPlayer vp, string message)
    {
        Debug.LogWarning($"[MissionBackground] Video fallback triggered: {message}");
        isShowingVideo = false;
    }

    private void StopVideo()
    {
        isShowingVideo = false;
        if (videoPlayer != null && (videoPlayer.isPlaying || videoPlayer.isPrepared))
        {
            videoPlayer.Stop();
        }
    }

    private void LoadImage(string resourcePath, string fallbackResource)
    {
        if (!isShowingVideo)
        {
            StopVideo();
        }

        Texture2D tex = !string.IsNullOrEmpty(resourcePath) ? Resources.Load<Texture2D>(resourcePath) : null;
        if (tex == null && !string.IsNullOrEmpty(fallbackResource))
        {
            tex = Resources.Load<Texture2D>(fallbackResource);
        }

        if (tex != null && mediaRawImage != null)
        {
            currentLoadedTexture = tex;
            mediaRawImage.texture = tex;
            mediaRawImage.color = Color.white;
            if (aspectFitter != null && tex.height > 0)
            {
                aspectFitter.aspectRatio = (float)tex.width / tex.height;
            }
        }
    }

    private void OnDestroy()
    {
        if (videoPlayer != null)
        {
            videoPlayer.prepareCompleted -= OnVideoPrepared;
            videoPlayer.errorReceived -= OnVideoError;
            videoPlayer.Stop();
        }
        if (videoRenderTexture != null)
        {
            videoRenderTexture.Release();
            DestroyImmediate(videoRenderTexture);
            videoRenderTexture = null;
        }
    }
}
