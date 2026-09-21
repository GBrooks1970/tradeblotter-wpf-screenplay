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
        var fullPath = Path.GetFullPath(executablePath);
        if (FindApplication(fullPath) is { } existing)
        {
            existing.Dispose();
            throw new InvalidOperationException("Close the existing application before launching an owned session.");
        }
        try
        {
            var options = new AppiumOptions { PlatformName = "Windows", AutomationName = "Windows", App = fullPath };
            options.AddAdditionalAppiumOption("appArguments", arguments);
            options.AddAdditionalAppiumOption("wadUrl", wad.AbsoluteUri.TrimEnd('/'));
            options.AddAdditionalAppiumOption("newCommandTimeout", 60);
            session = new WindowsDriver(server, options, TimeSpan.FromSeconds(30));
            process = FindApplication(fullPath)
                ?? throw new InvalidOperationException("WinAppDriver did not launch the requested executable.");
            session.Manage().Timeouts().ImplicitWait = TimeSpan.Zero;
            window = session.FindElement(WebBy.XPath("/*"));
            if (window.GetAttribute("ProcessId") != process.Id.ToString())
                throw new InvalidOperationException("WinAppDriver attached to an unexpected application process.");
        }
        catch
        {
            // Desktop suites are serial; reject existing instances above and
            // recover only the uniquely launched exact-path process on failure.
            process ??= FindApplication(fullPath);
            Close();
            throw;
        }
    }

    private static Process? FindApplication(string executable)
    {
        Process? found = null;
        foreach (var candidate in Process.GetProcessesByName(Path.GetFileNameWithoutExtension(executable)))
        {
            bool matches;
            try { matches = string.Equals(candidate.MainModule?.FileName, executable, StringComparison.OrdinalIgnoreCase); }
            catch { candidate.Dispose(); throw; }
            if (!matches) { candidate.Dispose(); continue; }
            if (found is not null)
            {
                candidate.Dispose();
                found.Dispose();
                throw new InvalidOperationException("Application ownership is ambiguous; run desktop suites serially.");
            }
            found = candidate;
        }
        return found;
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
        LocatorStrategy.Name => WebBy.XPath($"//*[@Name={XPathLiteral(locator.Value)}]"),
        LocatorStrategy.XPath => WebBy.XPath(locator.Value),
        _ => throw new ArgumentOutOfRangeException(nameof(locator))
    };

    private static string XPathLiteral(string value)
    {
        if (!value.Contains('\'')) return $"'{value}'";
        if (!value.Contains('"')) return $"\"{value}\"";
        return "concat(" + string.Join(",\"'\",", value.Split('\'').Select(part => $"'{part}'")) + ")";
    }

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
        // Selenium's By.Name rewrites element-scoped searches as browser CSS.
        // Native Name is an XML attribute, anchored to the same parent scope.
        if (locator.Strategy == LocatorStrategy.Name)
            locator = Locator.XPath($".//*[@Name={XPathLiteral(locator.Value)}]");
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
            finally { ownedProcess?.Dispose(); ownedSession?.Dispose(); }
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
            var items = WaitFor(() =>
            {
                var found = owner.FindWithin(LiveElement, Locator.XPath("./ListItem"));
                return found.Count == 0 ? null : found;
            }, "expanded combo-box items");
            var index = items.ToList().FindIndex(item => item.GetAttribute("Name") == text);
            if (index < 0) throw new InvalidOperationException($"Combo-box option '{text}' was not found.");
            var target = items[index].GetAttribute("RuntimeId");
            // WPF logical ListItem peers may acknowledge Click without selecting.
            // Navigate the actual option order and confirm the selection pattern.
            LiveElement.SendKeys(Keys.Home + string.Concat(Enumerable.Repeat(Keys.ArrowDown, index)) + Keys.Enter);
            // Selection is exposed in WinAppDriver's XML tree, but its attribute
            // endpoint returns null for this UIA pattern property.
            var comboId = LiveElement.GetAttribute("RuntimeId");
            WaitFor(() => owner.Live.FindElements(WebBy.XPath(
                $"//*[@RuntimeId={XPathLiteral(comboId)} and @Selection={XPathLiteral(target)}]"))
                .FirstOrDefault(), $"selected option {text}");
        }
        public IAutomationElement Find(Locator locator) => owner.Wrap(WaitFor(
            () => owner.FindWithin(LiveElement, locator).FirstOrDefault(), $"element {locator}"));
        public IReadOnlyList<IAutomationElement> FindAll(Locator locator) => owner.FindWithin(LiveElement, locator).Select(owner.Wrap).ToArray();
    }
}
