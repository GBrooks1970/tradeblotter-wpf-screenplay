using System.Diagnostics;
using TradeBlotter.Framework.Abstractions;

namespace TradeBlotter.Framework.FlaUI;

public sealed class FlaUiDriverAdapter : IWindowsAutomationDriver
{
    private readonly DriverOptions options;
    private readonly Func<string, string, IDesktopSession> start;
    private IDesktopSession? session;
    private IDesktopElement? window;
    private bool disposed;

    public FlaUiDriverAdapter(DriverOptions? options = null)
        : this(options ?? new DriverOptions(), null) { }

    internal FlaUiDriverAdapter(DriverOptions options, Func<string, string, IDesktopSession>? start)
    {
        options.Validate();
        this.options = options;
        this.start = start ?? ((path, args) => new NativeSession(path, args, options));
    }

    public int? ProcessId => session?.ProcessId;

    public void Launch(string executablePath, string arguments = "")
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        if (session is not null) throw new InvalidOperationException("Close the current application before launching another.");
        session = start(executablePath, arguments);
        try
        {
            window = WaitFor(session.MainWindow, options.LaunchTimeout, "main window");
        }
        catch
        {
            Close();
            throw;
        }
    }

    public IAutomationElement Find(By locator) => Wrap(FindElement(CurrentWindow(), locator));
    public IReadOnlyList<IAutomationElement> FindAll(By locator) => FindElements(CurrentWindow(), locator).Select(Wrap).ToArray();
    public void Click(By locator) => Find(locator).Click();
    public void Type(By locator, string text) => Find(locator).Type(text);

    public void SwitchToWindow(By locator)
    {
        CurrentWindow();
        ArgumentNullException.ThrowIfNull(locator);
        var found = WaitFor(() => session!.FindWindow(locator), options.ElementTimeout, $"window {locator}");
        found.Focus();
        window = found;
    }

    public void Close()
    {
        var owned = session;
        session = null;
        window = null;
        owned?.Dispose();
    }

    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        Close();
    }

    private IDesktopElement CurrentWindow()
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        return window ?? throw new InvalidOperationException("Launch an application first.");
    }

    private IReadOnlyList<IDesktopElement> FindElements(IDesktopElement root, By locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        return root.FindAll(locator);
    }

    private IDesktopElement FindElement(IDesktopElement root, By locator) =>
        WaitFor(() => FindElements(root, locator).FirstOrDefault(), options.ElementTimeout, $"element {locator}");

    private IAutomationElement Wrap(IDesktopElement element) => new Element(this, session!, element);

    private IDesktopElement WaitFor(Func<IDesktopElement?> lookup, TimeSpan timeout, string description)
    {
        var watch = Stopwatch.StartNew();
        do
        {
            var result = lookup();
            if (result is not null) return result;
            var remaining = timeout - watch.Elapsed;
            if (remaining <= TimeSpan.Zero) break;
            Thread.Sleep(remaining < options.PollInterval ? remaining : options.PollInterval);
        } while (watch.Elapsed < timeout);
        throw new TimeoutException($"Timed out after {timeout} waiting for {description}.");
    }

    private sealed class Element(FlaUiDriverAdapter owner, IDesktopSession originatingSession, IDesktopElement native) : IAutomationElement
    {
        private IDesktopElement Live
        {
            get
            {
                owner.CurrentWindow();
                if (!ReferenceEquals(owner.session, originatingSession))
                    throw new InvalidOperationException("Element belongs to a closed application session.");
                return native;
            }
        }
        public string AutomationId => Live.AutomationId;
        public string Name => Live.Name;
        public string Text => Live.Text;
        public bool IsEnabled => Live.IsEnabled;
        public void Click() => Live.Click();
        public void Type(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            Live.Type(text);
        }
        public IAutomationElement Find(By locator) => owner.Wrap(owner.FindElement(Live, locator));
        public IReadOnlyList<IAutomationElement> FindAll(By locator) => owner.FindElements(Live, locator).Select(owner.Wrap).ToArray();
    }
}
