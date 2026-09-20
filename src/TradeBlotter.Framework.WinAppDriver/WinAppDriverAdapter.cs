using System.Diagnostics;
using OpenQA.Selenium;
using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;
using TradeBlotter.Framework.Abstractions;
using Locator = TradeBlotter.Framework.Abstractions.By;
using WebBy = OpenQA.Selenium.By;

namespace TradeBlotter.Framework.WinAppDriver;

/// <summary>Owns a SUT process and a W3C Appium session backed by WinAppDriver.</summary>
public sealed class WinAppDriverAdapter : IWindowsAutomationDriver
{
    private readonly Uri server;
    private readonly Uri wad;
    private WindowsDriver? session;
    private IWebElement? window;
    private Process? process;
    private OwnedWindow? raisedWindow;
    private bool disposed;
    public int? ProcessId => process?.Id;

    public WinAppDriverAdapter(string serverUrl = "http://127.0.0.1:4723", string wadUrl = "http://127.0.0.1:4724")
    {
        server = LocalEndpoint(serverUrl);
        wad = LocalEndpoint(wadUrl);
    }

    private static Uri LocalEndpoint(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != "http" ||
            uri.Host != "127.0.0.1" || !string.IsNullOrEmpty(uri.UserInfo))
            throw new ArgumentException("Desktop automation endpoints must use http://127.0.0.1 with no credentials.", nameof(value));
        return uri;
    }

    public void Launch(string executablePath, string arguments = "")
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (process is not null) throw new InvalidOperationException("Close the current application before launching another.");
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentNullException.ThrowIfNull(arguments);
        if (!File.Exists(executablePath)) throw new FileNotFoundException("Application executable not found.", executablePath);
        try
        {
            process = Process.Start(new ProcessStartInfo(Path.GetFullPath(executablePath), arguments) { UseShellExecute = false })
                ?? throw new InvalidOperationException("Failed to launch application.");
            if (!process.WaitForInputIdle(10000))
                throw new TimeoutException("SUT did not finish initialising its input loop.");
            var handle = WaitFor(() =>
            {
                process.Refresh();
                if (process.HasExited) throw new InvalidOperationException("Application exited before its window became available.");
                return process.MainWindowHandle == IntPtr.Zero ? null : process.MainWindowHandle.ToInt64().ToString("x");
            }, "application window", TimeSpan.FromSeconds(15));
            raisedWindow = new OwnedWindow(process.MainWindowHandle, process.Id);
            var options = new AppiumOptions { PlatformName = "Windows", AutomationName = "Windows" };
            options.AddAdditionalAppiumOption("appTopLevelWindow", handle);
            options.AddAdditionalAppiumOption("wadUrl", wad.AbsoluteUri.TrimEnd('/'));
            options.AddAdditionalAppiumOption("newCommandTimeout", 60);
            session = new WindowsDriver(server, options, TimeSpan.FromSeconds(30));
            session.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            // Attaching by HWND does not guarantee that a newly launched process
            // owns the foreground after a previous scenario has closed.
            session.SwitchTo().Window(session.CurrentWindowHandle);
            window = session.FindElement(WebBy.XPath("/*"));
        }
        catch
        {
            Close();
            throw;
        }
    }

    private WindowsDriver Live
    {
        get
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            return session ?? throw new InvalidOperationException("Launch an application first.");
        }
    }

    private static WebBy Translate(Locator locator) => locator.Strategy switch
    {
        LocatorStrategy.AutomationId => MobileBy.AccessibilityId(locator.Value),
        LocatorStrategy.Name => WebBy.Name(locator.Value),
        LocatorStrategy.XPath => WebBy.XPath(locator.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(locator))
    };

    private static T WaitFor<T>(Func<T?> lookup, string description, TimeSpan? timeout = null) where T : class
    {
        var limit = timeout ?? TimeSpan.FromSeconds(5);
        var watch = Stopwatch.StartNew();
        do
        {
            var found = lookup();
            if (found is not null) return found;
            var remaining = limit - watch.Elapsed;
            if (remaining <= TimeSpan.Zero) break;
            Task.Delay(remaining < TimeSpan.FromMilliseconds(100) ? remaining : TimeSpan.FromMilliseconds(100)).GetAwaiter().GetResult();
        } while (watch.Elapsed < limit);
        throw new TimeoutException($"Timed out after {limit} waiting for {description}.");
    }

    private IAutomationElement Wrap(IWebElement element) => new Element(this, Live, element);
    private IReadOnlyList<IWebElement> FindWithin(IWebElement root, Locator locator)
    {
        ArgumentNullException.ThrowIfNull(locator);
        // WinAppDriver does not preserve the UIA element-relative XPath context.
        // Anchor to the current element's native RuntimeId in the session tree.
        if (locator.Strategy == LocatorStrategy.XPath && locator.Value.StartsWith("./", StringComparison.Ordinal))
        {
            var runtimeId = root.GetAttribute("RuntimeId");
            if (string.IsNullOrEmpty(runtimeId) || runtimeId.Any(c => !char.IsAsciiDigit(c) && c != '.' && c != '-'))
                throw new InvalidOperationException("WinAppDriver returned an invalid element RuntimeId.");
            var found = Live.FindElements(WebBy.XPath($"//*[@RuntimeId='{runtimeId}']{locator.Value[1..]}"));
            return found;
        }
        return root.FindElements(Translate(locator));
    }
    public IAutomationElement Find(Locator locator)
    {
        _ = Live;
        return Wrap(WaitFor(() => FindWithin(window!, locator).FirstOrDefault(), $"element {locator}"));
    }
    public IReadOnlyList<IAutomationElement> FindAll(Locator locator)
    {
        _ = Live;
        return FindWithin(window!, locator).Select(Wrap).ToArray();
    }
    public void Click(Locator locator) => Find(locator).Click();
    public void Type(Locator locator, string text) => Find(locator).Type(text);
    public void Select(Locator locator, string text) => Find(locator).Select(text);

    public void SwitchToWindow(Locator locator)
    {
        var driver = Live;
        var previousHandle = driver.CurrentWindowHandle;
        try
        {
            window = WaitFor(() =>
            {
                foreach (var handle in driver.WindowHandles)
                {
                    driver.SwitchTo().Window(handle);
                    var found = driver.FindElements(Translate(locator)).FirstOrDefault();
                    if (found is not null) return found;
                }
                return null;
            }, $"window {locator}");
        }
        catch
        {
            if (driver.WindowHandles.Contains(previousHandle)) driver.SwitchTo().Window(previousHandle);
            throw;
        }
    }

    public void Close()
    {
        var ownedSession = session;
        var ownedProcess = process;
        var ownedWindow = raisedWindow;
        raisedWindow = null;
        var diagnostics = Environment.GetEnvironmentVariable("TRADEBLOTTER_DIAGNOSTICS_DIRECTORY");
        if (ownedSession is not null && !string.IsNullOrWhiteSpace(diagnostics))
        {
            try
            {
                Directory.CreateDirectory(diagnostics);
                var prefix = Path.Combine(diagnostics, $"sut-{ownedProcess?.Id}-{Guid.NewGuid():N}");
                File.WriteAllText(prefix + ".xml", ownedSession.PageSource, System.Text.Encoding.Unicode);
                ownedSession.GetScreenshot().SaveAsFile(prefix + ".png");
            }
            catch (Exception error) { Console.Error.WriteLine($"Desktop diagnostics unavailable: {error.Message}"); }
        }
        session = null;
        process = null;
        window = null;
        try { ownedSession?.Quit(); }
        finally
        {
            try
            {
                if (ownedProcess is not null && !ownedProcess.HasExited)
                {
                    ownedProcess.CloseMainWindow();
                    if (!ownedProcess.WaitForExit(5000))
                    {
                        ownedProcess.Kill(entireProcessTree: true);
                        if (!ownedProcess.WaitForExit(5000)) throw new TimeoutException("Owned SUT did not exit.");
                    }
                }
            }
            finally { ownedWindow?.Dispose(); ownedProcess?.Dispose(); ownedSession?.Dispose(); }
        }
    }

    public void Dispose()
    {
        if (disposed) return;
        try { Close(); }
        finally { disposed = true; }
    }

    private sealed class Element(WinAppDriverAdapter owner, WindowsDriver originalSession, IWebElement element) : IAutomationElement
    {
        private IWebElement LiveElement
        {
            get
            {
                if (!ReferenceEquals(owner.Live, originalSession)) throw new InvalidOperationException("Element belongs to a closed application session.");
                return element;
            }
        }
        public string AutomationId => LiveElement.GetAttribute("AutomationId") ?? "";
        public string Name => LiveElement.GetAttribute("Name") ?? "";
        public string Text => LiveElement.Text;
        public bool IsEnabled => LiveElement.Enabled;
        public bool IsSelected => LiveElement.Selected;
        private void RequireEnabled()
        {
            if (!IsEnabled) throw new InvalidOperationException("Cannot interact with a disabled element.");
            owner.raisedWindow!.EnsureVisible();
        }
        public void Click() { RequireEnabled(); LiveElement.Click(); }
        public void SelectItem() { RequireEnabled(); LiveElement.Click(); }
        public void Type(string text)
        {
            ArgumentNullException.ThrowIfNull(text);
            RequireEnabled();
            LiveElement.Clear();
            LiveElement.SendKeys(text);
        }
        public void Select(string text)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            RequireEnabled();
            LiveElement.Click();
            WaitFor(() => LiveElement.FindElements(WebBy.Name(text)).FirstOrDefault(), $"option {text}").Click();
        }
        public IAutomationElement Find(Locator locator) => owner.Wrap(WaitFor(
            () => owner.FindWithin(LiveElement, locator).FirstOrDefault(), $"element {locator}"));
        public IReadOnlyList<IAutomationElement> FindAll(Locator locator) => owner.FindWithin(LiveElement, locator).Select(owner.Wrap).ToArray();
    }
}
