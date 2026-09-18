namespace TradeBlotter.Framework.Abstractions;

public interface IAutomationElement
{
    string AutomationId { get; }
    string Name { get; }
    string Text { get; }
    bool IsEnabled { get; }
    void Click();
    /// <summary>Replace the existing value; does not append text.</summary>
    void Type(string text);
    IAutomationElement Find(By locator);
    /// <summary>Return a snapshot of matching descendants without waiting.</summary>
    IReadOnlyList<IAutomationElement> FindAll(By locator);
}

/// <summary>A single owned application session. Instances are not thread-safe.</summary>
public interface IWindowsAutomationDriver : IDisposable
{
    int? ProcessId { get; }
    void Launch(string executablePath, string arguments = "");
    IAutomationElement Find(By locator);
    IReadOnlyList<IAutomationElement> FindAll(By locator);
    void Click(By locator);
    void Type(By locator, string text);
    /// <summary>Switch search scope to a window of the owned process.</summary>
    void SwitchToWindow(By locator);
    /// <summary>Close the owned process. A subsequent Launch starts a fresh session.</summary>
    void Close();
}
