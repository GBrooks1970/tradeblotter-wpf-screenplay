using TradeBlotter.Framework.Abstractions;

namespace TradeBlotter.Framework.FlaUI;

// Internal native boundary permits lifecycle/adapter testing without a desktop.
internal interface IDesktopSession : IDisposable
{
    int ProcessId { get; }
    IDesktopElement? MainWindow();
    IDesktopElement? FindWindow(By locator);
}

internal interface IDesktopElement
{
    string AutomationId { get; }
    string Name { get; }
    string Text { get; }
    bool IsEnabled { get; }
    void Click();
    void Type(string text);
    void Select(string text);
    void Focus();
    IReadOnlyList<IDesktopElement> FindAll(By locator);
}
