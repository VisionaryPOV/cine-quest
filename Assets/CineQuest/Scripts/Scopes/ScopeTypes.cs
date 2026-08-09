namespace CineQuest.Scopes
{
    public enum ScopeType
    {
        Waveform = 0,
        RgbParade = 1,
        Vectorscope = 2,
        Histogram = 3
    }

    public enum ScopeQualityMode
    {
        /// <summary>Full analysis resolution, every frame.</summary>
        High = 0,
        /// <summary>Medium downsample, every other frame.</summary>
        Balanced = 1,
        /// <summary>Aggressive downsample, ~15–20 Hz update.</summary>
        Performance = 2
    }
}
