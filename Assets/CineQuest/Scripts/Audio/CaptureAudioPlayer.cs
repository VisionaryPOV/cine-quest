// Cine Quest — Plays UAC audio from capture card when available.
// Document residual latency: Unity audio path is not frame-locked to video.

using CineQuest.Capture;
using UnityEngine;

namespace CineQuest.Audio
{
    [RequireComponent(typeof(AudioSource))]
    public sealed class CaptureAudioPlayer : MonoBehaviour
    {
        [SerializeField] CaptureService captureService;
        [SerializeField] bool startMuted = true;
        [SerializeField] float volume = 0.8f;

        AudioSource _source;
        bool _muted;

        public bool IsMuted => _muted;

        void Awake()
        {
            _source = GetComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = true;
            _source.spatialBlend = 0f; // stereo monitor path
            _source.volume = volume;
            _muted = startMuted;
            _source.mute = _muted;
        }

        void Start()
        {
            if (captureService == null) captureService = CaptureService.Instance;
            captureService?.StartAudioIfAvailable();

            // If UVC backend exposes an AudioClip later, assign here.
            if (captureService?.Source is IAudioCaptureSource audio && audio.Clip != null)
            {
                _source.clip = audio.Clip;
                if (!_muted) _source.Play();
            }
        }

        public void SetMuted(bool muted)
        {
            _muted = muted;
            if (_source != null)
            {
                _source.mute = muted;
                if (!muted && _source.clip != null && !_source.isPlaying)
                    _source.Play();
            }
        }

        public void ToggleMute() => SetMuted(!_muted);
    }
}
