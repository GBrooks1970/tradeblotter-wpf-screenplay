namespace TradeBlotter.Framework.Abstractions;

public enum LocatorStrategy { AutomationId, Name, XPath }

/// <summary>A vendor-neutral locator. XPath is relative to the current search root.</summary>
public sealed record By
{
    public LocatorStrategy Strategy { get; }
    public string Value { get; }

    private By(LocatorStrategy strategy, string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        Strategy = strategy;
        Value = value;
    }

    public static By AutomationId(string value) => new(LocatorStrategy.AutomationId, value);
    public static By Name(string value) => new(LocatorStrategy.Name, value);
    public static By XPath(string value) => new(LocatorStrategy.XPath, value);
    public override string ToString() => $"{Strategy}={Value}";
}
