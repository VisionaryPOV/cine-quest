// Cine Quest — Capture types, status, and error codes.
// Priority: expose raw signal state so UI/HUD can warn the DP without guessing.

using System;
using UnityEngine;

namespace CineQuest.Capture
{
    /// <summary>How the capture path believes USB is linked (heuristic if not OS-reported).</summary>
    public enum UsbLinkSpeed
    {
        Unknown = 0,
        FullSpeed = 1,   // USB 1.x — not usable for 1080p60
        HiSpeed = 2,     // USB 2.0 — often drops frames at 1080p60
        SuperSpeed = 3,  // USB 3.x — preferred
        SuperSpeedPlus = 4
    }

    /// <summary>Color / sample format as reported by the capture backend (best effort).</summary>
    public enum CaptureColorFormat
    {
        Unknown = 0,
        RGB24,
        RGBA32,
        YUY2,
        UYVY,
        NV12,
        NV21,
        MJPEG,
        H264,
        Other
    }

    /// <summary>User-facing capture health for the video panel and status HUD.</summary>
    public enum CaptureErrorCode
    {
        None = 0,
        NoDevice,
        PermissionDenied,
        UnsupportedResolution,
        UsbSpeedWarning,
        HdcpBlanked,
        SignalLost,
        BackendUnavailable,
        InternalError
    }

    /// <summary>Live snapshot of the capture pipeline for HUD / diagnostics.</summary>
    [Serializable]
    public struct CaptureStatus
    {
        public bool IsStreaming;
        public int Width;
        public int Height;
        public float MeasuredFps;
        public float EstimatedLatencyMs;
        public UsbLinkSpeed UsbSpeed;
        public CaptureColorFormat ColorFormat;
        public bool HasAudio;
        public CaptureErrorCode Error;
        public string ErrorMessage;
        public string DeviceName;

        public string ResolutionLabel =>
            Width > 0 && Height > 0 ? $"{Width}×{Height}" : "—";

        public string UsbSpeedLabel => UsbSpeed switch
        {
            UsbLinkSpeed.SuperSpeedPlus => "SuperSpeed+",
            UsbLinkSpeed.SuperSpeed => "SuperSpeed (USB 3)",
            UsbLinkSpeed.HiSpeed => "Hi-Speed (USB 2)",
            UsbLinkSpeed.FullSpeed => "Full-Speed (USB 1)",
            _ => "USB ?"
        };

        public static CaptureStatus Empty => new CaptureStatus
        {
            IsStreaming = false,
            Error = CaptureErrorCode.NoDevice,
            ErrorMessage = "No capture device",
            UsbSpeed = UsbLinkSpeed.Unknown,
            ColorFormat = CaptureColorFormat.Unknown
        };
    }

    /// <summary>Events raised by any <see cref="IVideoCaptureSource"/>.</summary>
    public sealed class CaptureEvents
    {
        public event Action<CaptureStatus> StatusChanged;
        public event Action<Texture> FrameTextureChanged;
        public event Action<CaptureErrorCode, string> ErrorRaised;

        public void RaiseStatus(CaptureStatus status) => StatusChanged?.Invoke(status);
        public void RaiseFrame(Texture texture) => FrameTextureChanged?.Invoke(texture);
        public void RaiseError(CaptureErrorCode code, string message) => ErrorRaised?.Invoke(code, message);
    }
}
