using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Captures a player-facing HDRP frame for animation review without using
/// Camera.Render(), which bypasses HDRP's render-request history.
/// </summary>
public static class HdrpAnimationAuditCapture
{
    private const int CaptureWidth = 1280;
    private const int CaptureHeight = 720;
    private const int WarmupPassCount = 4;

    [MenuItem("Tools/Main Hackathon Game/Capture HDRP Animation Audit Frame")]
    public static void Capture()
    {
        if (!Application.isPlaying)
        {
            throw new InvalidOperationException("Enter Play Mode before capturing an HDRP animation audit frame.");
        }

        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new InvalidOperationException("No active Main Camera was found for the animation audit capture.");
        }

        RenderTextureDescriptor descriptor = new RenderTextureDescriptor(
            CaptureWidth,
            CaptureHeight,
            RenderTextureFormat.ARGB32,
            24)
        {
            msaaSamples = 1,
            sRGB = true,
            useMipMap = false,
            autoGenerateMips = false
        };

        RenderTexture target = RenderTexture.GetTemporary(descriptor);
        RenderPipeline.StandardRequest request = new RenderPipeline.StandardRequest
        {
            destination = target
        };

        if (!RenderPipeline.SupportsRenderRequest(camera, request))
        {
            RenderTexture.ReleaseTemporary(target);
            throw new InvalidOperationException(
                "The active render pipeline does not support StandardRequest capture for the Main Camera.");
        }

        string relativePath = $"Assets/Screenshots/animation_audit_hdrp_{DateTime.UtcNow:yyyyMMdd_HHmmss}.png";
        string absolutePath = Path.GetFullPath(relativePath);
        Texture2D pixels = null;
        RenderTexture previousTarget = RenderTexture.active;

        try
        {
            // HDRP assigns render requests their own camera history. A few passes
            // make temporal effects settle before the saved review frame is read.
            for (int pass = 0; pass < WarmupPassCount; pass++)
            {
                RenderPipeline.SubmitRenderRequest(camera, request);
            }

            RenderTexture.active = target;
            pixels = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            pixels.ReadPixels(new Rect(0f, 0f, CaptureWidth, CaptureHeight), 0, 0);
            pixels.Apply(false, false);

            Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
            File.WriteAllBytes(absolutePath, pixels.EncodeToPNG());
        }
        finally
        {
            RenderTexture.active = previousTarget;
            if (pixels != null)
            {
                UnityEngine.Object.DestroyImmediate(pixels);
            }

            RenderTexture.ReleaseTemporary(target);
        }

        AssetDatabase.ImportAsset(relativePath, ImportAssetOptions.ForceUpdate);
        Debug.Log($"[HdrpAnimationAuditCapture] Saved {relativePath}");
    }
}
