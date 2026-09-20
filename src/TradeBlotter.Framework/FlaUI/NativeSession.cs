using System.Diagnostics;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.UIA3;
using TradeBlotter.Framework.Abstractions;

namespace TradeBlotter.Framework.FlaUI;

internal sealed class NativeSession : IDesktopSession
{
    private readonly UIA3Automation automation;
    private readonly Application app;
    private readonly DriverOptions options;
    private bool disposed;
    public int ProcessId => app.ProcessId;

    public NativeSession(string path, string arguments, DriverOptions options)
    {
        if (!File.Exists(path)) throw new FileNotFoundException("Application executable not found.", path);
        this.options = options;
        automation = new UIA3Automation();
        try
        {
            app = Application.Launch(new ProcessStartInfo(Path.GetFullPath(path), arguments) { UseShellExecute = false });
        }
        catch
        {
            automation.Dispose();
            throw;
        }
    }

    public IDesktopElement? MainWindow()
    {
        if (app.HasExited) throw new InvalidOperationException("Application exited before its main window became available.");
        var result = app.GetMainWindow(automation, TimeSpan.Zero);
        return result is null ? null : new NativeElement(result);
    }

    public IDesktopElement? FindWindow(By locator)
    {
        // Enumerate only top-level SUT windows before descending into owned dialogs.
        // A desktop-wide descendant scan can block on unrelated UIA providers.
        var root = automation.GetDesktop();
        var topLevel = root.FindAllChildren(cf => cf.ByProcessId(app.ProcessId));
        var windows = topLevel.Concat(topLevel.SelectMany(w => w.FindAllDescendants(cf =>
            cf.ByProcessId(app.ProcessId).And(cf.ByControlType(global::FlaUI.Core.Definitions.ControlType.Window)))))
            .ToArray();
        var result = locator.Strategy switch
        {
            LocatorStrategy.AutomationId => windows.FirstOrDefault(w => w.AutomationId == locator.Value),
            LocatorStrategy.Name => windows.FirstOrDefault(w => w.Name == locator.Value),
            LocatorStrategy.XPath => NativeElement.Find(root, locator).FirstOrDefault(candidate => windows.Any(w => w.Equals(candidate))),
            _ => throw new ArgumentOutOfRangeException(nameof(locator))
        };
        return result is null ? null : new NativeElement(result);
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        try
        {
            if (!app.HasExited)
            {
                using var process = Process.GetProcessById(app.ProcessId);
                process.CloseMainWindow();
                if (!process.WaitForExit((int)options.ShutdownTimeout.TotalMilliseconds))
                {
                    process.Kill(entireProcessTree: true);
                    if (!process.WaitForExit((int)options.ShutdownTimeout.TotalMilliseconds))
                        throw new TimeoutException($"Application {app.ProcessId} did not exit after termination.");
                }
            }
        }
        finally
        {
            try { app.Dispose(); }
            finally { automation.Dispose(); }
        }
    }
}

internal sealed class NativeElement(AutomationElement element) : IDesktopElement
{
    public string AutomationId => element.AutomationId;
    public string Name => element.Name;
    public string Text => element.Patterns.Value.IsSupported ? element.Patterns.Value.Pattern.Value.Value : element.Name;
    public bool IsEnabled => element.IsEnabled;
    public bool IsSelected => element.Patterns.SelectionItem.IsSupported && element.Patterns.SelectionItem.Pattern.IsSelected.Value;
    public void SelectItem()
    {
        if (!IsEnabled) throw new InvalidOperationException("Cannot select a disabled element.");
        element.Patterns.SelectionItem.Pattern.Select();
    }
    public void Click()
    {
        if (!IsEnabled) throw new InvalidOperationException("Cannot click a disabled element.");
        if (element.Patterns.Invoke.IsSupported) element.Patterns.Invoke.Pattern.Invoke();
        else element.Click();
    }
    public void Type(string text)
    {
        if (!IsEnabled) throw new InvalidOperationException("Cannot type into a disabled element.");
        element.Patterns.Value.Pattern.SetValue(text);
    }
    public void Select(string text)
    {
        if (!IsEnabled) throw new InvalidOperationException("Cannot select a disabled element.");
        element.AsComboBox().Select(text);
    }
    public void Focus() => element.Focus();
    public IReadOnlyList<IDesktopElement> FindAll(By locator) => Find(element, locator).Select(e => (IDesktopElement)new NativeElement(e)).ToArray();

    internal static AutomationElement[] Find(AutomationElement root, By locator) => locator.Strategy switch
    {
        LocatorStrategy.AutomationId => root.FindAllDescendants(cf => cf.ByAutomationId(locator.Value)),
        LocatorStrategy.Name => root.FindAllDescendants(cf => cf.ByName(locator.Value)),
        LocatorStrategy.XPath => root.FindAllByXPath(locator.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(locator))
    };
}
