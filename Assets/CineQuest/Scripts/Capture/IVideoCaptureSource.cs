// Cine Quest — Video capture source contract.
// All backends (UVC4Unity, native usb-video AAR, Editor synthetic) implement this.
// Keeps the locked display path independent of vendor plugins.

using System;
using UnityEngine;

namespace CineQuest.Capture
{
    /// <summary>
    /// Minimal, raw-leaning video input interface.
    /// Implementations MUST avoid Camera2 auto-exposure / auto-white-balance
    /// and must not apply creative image processing before handing off the texture.
    /// </summary>
    public interface IVideoCaptureSource : IDisposable
    {
        /// <summary>True when a device is open and producing frames.</summary>
        bool IsRunning { get; }

        /// <summary>Latest decoded frame as a Unity Texture (may be ExternalOES / Texture2D / RT).</summary>
        Texture CurrentFrame { get; }

        /// <summary>Latest status snapshot.</summary>
        CaptureStatus Status { get; }

        /// <summary>Shared event hub (status / errors / texture swaps).</summary>
        CaptureEvents Events { get; }

        /// <summary>Preferred request: 1920×1080 @ 60 when available.</summary>
        void Configure(int preferredWidth, int preferredHeight, float preferredFps);

        /// <summary>Open device and start streaming. Safe to call when already running.</summary>
        void StartCapture();

        /// <summary>Stop streaming and release the device (keep object reusable).</summary>
        void StopCapture();

        /// <summary>
        /// Per-frame tick from CaptureService. Backends that need a main-thread pump
        /// (WebCamTexture, some JNI bridges) update here. Compute-heavy work is forbidden.
        /// </summary>
        void Tick();
    }

    /// <summary>Optional USB audio class path from the same capture card.</summary>
    public interface IAudioCaptureSource : IDisposable
    {
        bool IsRunning { get; }
        bool HasAudio { get; }

        /// <summary>Unity AudioClip or null if PCM is pushed via OnAudioFilterRead-style callback.</summary>
        AudioClip Clip { get; }

        void StartAudio();
        void StopAudio();
    }
}
