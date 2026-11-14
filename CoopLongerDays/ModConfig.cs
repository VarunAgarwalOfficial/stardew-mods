namespace CoopLongerDays;

public sealed class ModConfig
{
    /// <summary>How many times longer each day is than a vanilla day.</summary>
    public double DayLengthMultiplier { get; set; } = 2.0;
}

public sealed class ClockSettings
{
    public int ProtocolVersion { get; set; } = 1;

    public int MillisecondsPerGameMinute { get; set; } = 700;
}
