using NUnit.Framework;
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Framework.FlaUI;

namespace TradeBlotter.Framework.Tests;

public class DriverTests
{
    private static DriverOptions Fast => new()
    {
        LaunchTimeout = TimeSpan.FromMilliseconds(30),
        ElementTimeout = TimeSpan.FromMilliseconds(30),
        PollInterval = TimeSpan.FromMilliseconds(1)
    };

    [TestCase(LocatorStrategy.AutomationId)]
    [TestCase(LocatorStrategy.Name)]
    [TestCase(LocatorStrategy.XPath)]
    public void LocateInteractAndReadThroughPublicContract(LocatorStrategy strategy)
    {
        var locator = strategy switch
        {
            LocatorStrategy.AutomationId => By.AutomationId("quantity"),
            LocatorStrategy.Name => By.Name("Quantity"),
            _ => By.XPath(".//*[@AutomationId='quantity']")
        };
        var input = new MemoryElement("quantity", "Quantity");
        var session = new MemorySession();
        session.Root.Children[locator] = [input];
        using IWindowsAutomationDriver driver = new FlaUiDriverAdapter(Fast, (_, _) => session);
        driver.Launch("app.exe");
        driver.Type(locator, "250000");
        driver.Type(locator, "10");
        driver.Click(locator);
        var found = driver.Find(locator);
        Assert.Multiple(() =>
        {
            Assert.That(found.AutomationId, Is.EqualTo("quantity"));
            Assert.That(found.Name, Is.EqualTo("Quantity"));
            Assert.That(found.Text, Is.EqualTo("10"));
            Assert.That(found.IsEnabled, Is.True);
            Assert.That(input.Clicks, Is.EqualTo(1));
            Assert.That(driver.FindAll(locator), Has.Count.EqualTo(1));
            Assert.That(driver.ProcessId, Is.EqualTo(123));
        });
    }

    [Test]
    public void NestedSearchIsScopedToParent()
    {
        var session = new MemorySession();
        var parent = new MemoryElement("parent", "Panel");
        parent.Children[By.Name("Child")] = [new("child", "Child")];
        session.Root.Children[By.AutomationId("parent")] = [parent];
        using var driver = new FlaUiDriverAdapter(Fast, (_, _) => session);
        driver.Launch("app");
        Assert.That(driver.FindAll(By.Name("Child")), Is.Empty);
        Assert.That(driver.Find(By.AutomationId("parent")).Find(By.Name("Child")).AutomationId, Is.EqualTo("child"));
    }

    [Test]
    public void WindowSwitchChangesScopeAndFocus()
    {
        var session = new MemorySession();
        var dialog = new MemoryElement("dialog", "Dialog");
        dialog.Children[By.Name("Input")] = [new("input", "Input")];
        session.Windows[By.AutomationId("dialog")] = dialog;
        using var driver = new FlaUiDriverAdapter(Fast, (_, _) => session);
        driver.Launch("app");
        driver.SwitchToWindow(By.AutomationId("dialog"));
        Assert.That(driver.Find(By.Name("Input")).Name, Is.EqualTo("Input"));
        Assert.That(dialog.Focused, Is.True);
        Assert.Throws<TimeoutException>(() => driver.SwitchToWindow(By.Name("absent")));
        Assert.That(driver.Find(By.Name("Input")).Name, Is.EqualTo("Input"), "A failed switch preserves the current window.");
    }

    [Test]
    public void MissingElementTimesOutWithLocatorAndFindAllReturnsEmpty()
    {
        using var driver = new FlaUiDriverAdapter(Fast, (_, _) => new MemorySession());
        driver.Launch("app");
        var error = Assert.Throws<TimeoutException>(() => driver.Find(By.Name("Missing")));
        Assert.That(error!.Message, Does.Contain("Name=Missing"));
        Assert.That(driver.FindAll(By.Name("Missing")), Is.Empty);
    }

    [Test]
    public void PollingFindsDelayedElement()
    {
        var session = new MemorySession();
        session.Root.OnFind = () =>
        {
            if (session.Root.Lookups == 3) session.Root.Children[By.Name("Later")] = [new("later", "Later")];
        };
        using var driver = new FlaUiDriverAdapter(Fast with { ElementTimeout = TimeSpan.FromSeconds(1) }, (_, _) => session);
        driver.Launch("app");
        Assert.That(driver.Find(By.Name("Later")).AutomationId, Is.EqualTo("later"));
        Assert.That(session.Root.Lookups, Is.EqualTo(3));
    }

    [Test]
    public void LaunchTimeoutDisposesSessionAndAllowsRetry()
    {
        var failed = new MemorySession { NoMainWindow = true };
        var good = new MemorySession();
        var calls = 0;
        using var driver = new FlaUiDriverAdapter(Fast, (_, _) => ++calls == 1 ? failed : good);
        Assert.Throws<TimeoutException>(() => driver.Launch("app"));
        Assert.That(failed.Disposals, Is.EqualTo(1));
        Assert.That(driver.ProcessId, Is.Null);
        driver.Launch("app");
        Assert.That(driver.ProcessId, Is.EqualTo(123));
    }

    [Test]
    public void LaunchWaitsForDelayedMainWindow()
    {
        var session = new MemorySession { MainWindowAfter = 3 };
        using var driver = new FlaUiDriverAdapter(Fast with { LaunchTimeout = TimeSpan.FromSeconds(1) }, (_, _) => session);
        driver.Launch("app");
        Assert.That(session.MainWindowLookups, Is.EqualTo(3));
    }

    [Test]
    public void FactoryFailureAllowsAnotherLaunch()
    {
        var calls = 0;
        using var driver = new FlaUiDriverAdapter(Fast, (_, _) => ++calls == 1
            ? throw new FileNotFoundException("missing application") : new MemorySession());
        Assert.Throws<FileNotFoundException>(() => driver.Launch("missing"));
        Assert.That(driver.ProcessId, Is.Null);
        driver.Launch("app");
        Assert.That(driver.ProcessId, Is.EqualTo(123));
    }

    [Test]
    public void NativeLookupFailureIsNotHiddenAsTimeout()
    {
        var session = new MemorySession();
        session.Root.OnFind = () => throw new NotSupportedException("unsupported pattern");
        using var driver = new FlaUiDriverAdapter(Fast, (_, _) => session);
        driver.Launch("app");
        Assert.Throws<NotSupportedException>(() => driver.Find(By.Name("anything")));
    }

    [Test]
    public void LifecyclePreconditionsAndIdempotentDisposal()
    {
        var session = new MemorySession();
        var driver = new FlaUiDriverAdapter(Fast, (_, _) => session);
        Assert.Throws<InvalidOperationException>(() => driver.Find(By.Name("x")));
        driver.Launch("app");
        Assert.Throws<InvalidOperationException>(() => driver.Launch("second"));
        driver.Close();
        driver.Close();
        driver.Dispose();
        driver.Dispose();
        Assert.That(session.Disposals, Is.EqualTo(1));
        Assert.Throws<ObjectDisposedException>(() => driver.Launch("app"));
    }

    [Test]
    public void ElementsCannotBeReusedAcrossSessions()
    {
        var first = new MemorySession();
        first.Root.Children[By.Name("Old")] = [new("old", "Old")];
        var starts = 0;
        using var driver = new FlaUiDriverAdapter(Fast, (_, _) => ++starts == 1 ? first : new MemorySession());
        driver.Launch("app");
        var old = driver.Find(By.Name("Old"));
        driver.Close();
        driver.Launch("app");
        Assert.Throws<InvalidOperationException>(() => old.Click());
    }

    [Test]
    public void ArgumentsAreForwardedAndNullTextRejected()
    {
        var session = new MemorySession();
        session.Root.Children[By.Name("Input")] = [new("input", "Input")];
        using var driver = new FlaUiDriverAdapter(Fast, (path, args) =>
        {
            Assert.That(path, Is.EqualTo("app.exe"));
            Assert.That(args, Is.EqualTo("--demo"));
            return session;
        });
        driver.Launch("app.exe", "--demo");
        Assert.Throws<ArgumentNullException>(() => driver.Type(By.Name("Input"), null!));
    }

    [Test]
    public void InvalidOptionsAndLocatorsAreRejected()
    {
        Assert.Throws<ArgumentException>(() => By.Name(" "));
        Assert.Throws<ArgumentNullException>(() => By.XPath(null!));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlaUiDriverAdapter(Fast with { PollInterval = TimeSpan.Zero }));
        Assert.Throws<ArgumentOutOfRangeException>(() => new FlaUiDriverAdapter(Fast with { ShutdownTimeout = TimeSpan.FromDays(100) }));
    }

    [Test]
    public void PublicSignaturesDoNotExposeVendorTypes()
    {
        static void Check(Type type)
        {
            if (type.HasElementType) Check(type.GetElementType()!);
            foreach (var argument in type.GetGenericArguments()) Check(argument);
            Assert.That(type.Assembly.GetName().Name, Does.Not.StartWith("FlaUI"));
        }
        foreach (var type in typeof(IWindowsAutomationDriver).Assembly.GetExportedTypes())
        {
            foreach (var method in type.GetMethods().Where(m => m.DeclaringType == type))
            {
                Check(method.ReturnType);
                foreach (var parameter in method.GetParameters()) Check(parameter.ParameterType);
            }
            foreach (var constructor in type.GetConstructors())
                foreach (var parameter in constructor.GetParameters()) Check(parameter.ParameterType);
        }
    }

    private sealed class MemorySession : IDesktopSession
    {
        public int ProcessId => 123;
        public MemoryElement Root { get; } = new("main", "Main");
        public Dictionary<By, MemoryElement> Windows { get; } = [];
        public bool NoMainWindow { get; init; }
        public int MainWindowAfter { get; init; } = 1;
        public int MainWindowLookups { get; private set; }
        public int Disposals { get; private set; }
        public IDesktopElement? MainWindow() => ++MainWindowLookups < MainWindowAfter || NoMainWindow ? null : Root;
        public IDesktopElement? FindWindow(By locator) => Windows.GetValueOrDefault(locator);
        public void Dispose() => Disposals++;
    }

    private sealed class MemoryElement(string id, string name) : IDesktopElement
    {
        public string AutomationId => id;
        public string Name => name;
        public string Text { get; private set; } = "";
        public bool IsEnabled => true;
        public int Clicks { get; private set; }
        public int Lookups { get; private set; }
        public bool Focused { get; private set; }
        public Action? OnFind { get; set; }
        public Dictionary<By, MemoryElement[]> Children { get; } = [];
        public void Click() => Clicks++;
        public void Type(string text) => Text = text;
        public void Focus() => Focused = true;
        public IReadOnlyList<IDesktopElement> FindAll(By locator)
        {
            Lookups++;
            OnFind?.Invoke();
            return Children.GetValueOrDefault(locator) ?? [];
        }
    }
}
