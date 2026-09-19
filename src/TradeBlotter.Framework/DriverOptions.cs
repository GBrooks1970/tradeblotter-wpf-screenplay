namespace TradeBlotter.Framework;

public sealed record DriverOptions
{
    public TimeSpan LaunchTimeout { get; init; } = TimeSpan.FromSeconds(10);
    public TimeSpan ElementTimeout { get; init; } = TimeSpan.FromSeconds(5);
    public TimeSpan PollInterval { get; init; } = TimeSpan.FromMilliseconds(100);
    public TimeSpan ShutdownTimeout { get; init; } = TimeSpan.FromSeconds(5);

    internal void Validate()
    {
        foreach (var value in new[] { LaunchTimeout, ElementTimeout, PollInterval, ShutdownTimeout })
            if (value <= TimeSpan.Zero || value.TotalMilliseconds > int.MaxValue)
                throw new ArgumentOutOfRangeException(nameof(DriverOptions), "Timeouts must be positive and fit a millisecond timer.");
    }
}
