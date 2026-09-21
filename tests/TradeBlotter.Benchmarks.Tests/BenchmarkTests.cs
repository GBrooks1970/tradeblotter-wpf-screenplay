using NUnit.Framework;
using TradeBlotter.Benchmarks;
using TradeBlotter.Framework.Abstractions;

namespace TradeBlotter.Benchmarks.Tests;

public class BenchmarkTests
{
    [Test]
    public void NativeRanorexNeverSubstitutesMock()
    {
        var engine = EngineCatalog.Resolve("ranorex");
        Assert.That(engine.Create, Is.Null);
        Assert.That(engine.Mode, Is.EqualTo(ExecutionMode.Native));
        var rows = BenchmarkRunner.Run(engine, "unused", new());
        Assert.That(rows.Single().Status, Is.EqualTo("UNAVAILABLE"));
        Assert.That(BenchmarkRunner.ExitCode(rows, false), Is.EqualTo(2));
        Assert.That(BenchmarkRunner.ExitCode(rows, true), Is.Zero);
    }

    [Test]
    public void MockRunsRequestedIterationsWithoutProducingMeasurements()
    {
        var rows = BenchmarkRunner.Run(EngineCatalog.Resolve("ranorex-mock"), "unused", new(3, 4, 1));
        Assert.That(rows, Has.Count.EqualTo(4));
        Assert.That(rows.Count(s => s.Phase == "warmup"), Is.EqualTo(1));
        Assert.That(rows.All(s => s.Status == "MOCK" && s.ProcessId is null && s.StartupMs is null && s.LookupMeanMs is null && s.SutWorkingSetMiB is null), Is.True);
        Assert.That(Reports.Markdown(rows), Does.Contain("MOCK | 0 | N/A | N/A | N/A"));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void FailedLaunchOrLookupDisposesAndStops(bool failLaunch)
    {
        var driver = new FaultDriver(failLaunch);
        var rows = BenchmarkRunner.Run(new("broken", ExecutionMode.Mock, () => driver), "unused", new(3, 1, 0));
        Assert.That(rows.Single().Status, Is.EqualTo("ERROR"));
        Assert.That(driver.Disposed, Is.True);
        Assert.That(BenchmarkRunner.ExitCode(rows, true), Is.EqualTo(1));
    }

    [Test]
    public void CleanupFailureCannotBeReportedAsSuccess()
    {
        var driver = new FaultDriver(true, true);
        var rows = BenchmarkRunner.Run(new("broken", ExecutionMode.Mock, () => driver), "unused", new());
        Assert.That(rows.Any(s => s.Phase == "cleanup" && s.Status == "ERROR"), Is.True);
        Assert.That(BenchmarkRunner.ExitCode(rows, true), Is.EqualTo(1));
    }

    [Test]
    public void SummaryExcludesWarmupMockAndFailedRuns()
    {
        Sample Row(string engine, string mode, string status, string phase, double value) =>
            new(engine, mode, status, 1, phase, DateTimeOffset.UtcNow, StartupMs: value, LookupMeanMs: value, SutWorkingSetMiB: value);
        var rows = new[] { Row("real", "Native", "PASS", "warmup", 900), Row("real", "Native", "PASS", "measured", 2),
            Row("real", "Native", "PASS", "measured", 4), Row("fake", "Mock", "MOCK", "measured", 800),
            Row("failed", "Native", "PASS", "measured", 700), Row("failed", "Native", "ERROR", "cleanup", 0) };
        var report = Reports.Markdown(rows);
        Assert.That(report, Does.Contain("real | Native | PASS | 2 | 3.0000"));
        Assert.That(report, Does.Not.Contain("900.0000").And.Not.Contain("800.0000").And.Not.Contain("700.0000"));
        Assert.That(report, Does.Contain("failed | Native | ERROR | 0 | N/A"));
    }

    [Test]
    public void ReportsEscapeDiagnosticText()
    {
        var row = new Sample("driver", "Native", "ERROR", 1, "measured", DateTimeOffset.UtcNow, Detail: "a,\"b\"|c\nnext");
        Assert.That(Reports.CsvText([row]), Does.Contain("\"a,\"\"b\"\"|c\nnext\""));
        Assert.That(Reports.Markdown([row]), Does.Contain("a,\"b\"\\|c next"));
    }

    [TestCase(0, 1, 0)]
    [TestCase(1, 0, 0)]
    [TestCase(1, 1, -1)]
    public void InvalidOptionsFailBeforeExecution(int repetitions, int lookups, int warmups) =>
        Assert.Throws<ArgumentOutOfRangeException>(() => new BenchmarkOptions(repetitions, lookups, warmups).Validate());

    [Test]
    public void UnknownDriverFailsClearly() => Assert.Throws<ArgumentException>(() => EngineCatalog.Resolve("ranorexx"));

    [Test]
    public void MockRejectsUnimplementedDesktopActions()
    {
        using var driver = new MockBenchmarkDriver();
        Assert.Throws<InvalidOperationException>(() => driver.Find(By.AutomationId("GridBlotterOrders")));
        driver.Launch("unused");
        Assert.Throws<NotSupportedException>(() => driver.Click(By.AutomationId("BtnNewOrder")));
        driver.Dispose();
        Assert.Throws<ObjectDisposedException>(() => driver.Launch("unused"));
    }

    private sealed class FaultDriver(bool failLaunch, bool failCleanup = false) : IWindowsAutomationDriver
    {
        public bool Disposed { get; private set; }
        public int? ProcessId => null;
        public void Launch(string executablePath, string arguments = "") { if (failLaunch) throw new InvalidOperationException("launch failure"); }
        public IAutomationElement Find(By locator) => throw new InvalidOperationException("lookup failure");
        public void Dispose() { Disposed = true; if (failCleanup) throw new InvalidOperationException("cleanup failure"); }
        public void Close() => Dispose();
        public IReadOnlyList<IAutomationElement> FindAll(By locator) => throw new NotSupportedException();
        public void Click(By locator) => throw new NotSupportedException();
        public void Type(By locator, string text) => throw new NotSupportedException();
        public void Select(By locator, string text) => throw new NotSupportedException();
        public void SwitchToWindow(By locator) => throw new NotSupportedException();
    }
}
