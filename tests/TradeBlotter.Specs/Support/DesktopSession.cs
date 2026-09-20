using System.Diagnostics;
using NUnit.Framework;
using Reqnroll;
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Framework.FlaUI;
using TradeBlotter.Framework.WinAppDriver;
using TradeBlotter.Screenplay;
using TradeBlotter.Screenplay.Tasks;

[assembly: LevelOfParallelism(1)]

namespace TradeBlotter.Specs.Support;

// Reqnroll constructs one context per scenario; only this composition boundary selects the native adapter.
public sealed class DesktopSession
{
    private IWindowsAutomationDriver? driver;
    public Actor Trader { get; private set; } = null!;
    public Actor Auditor { get; private set; } = null!;

    public void Start()
    {
        driver = (Environment.GetEnvironmentVariable("TRADEBLOTTER_DRIVER") ?? "flaui") switch
        {
            "flaui" => new FlaUiDriverAdapter(),
            "winappdriver" => new WinAppDriverAdapter(),
            var value => throw new ArgumentException($"Unknown desktop driver '{value}'. Use flaui or winappdriver.")
        };
        Trader = TradingActors.TommyTrader(driver);
        Auditor = TradingActors.AdamAuditor(driver);
        var path = Path.Combine(AppContext.BaseDirectory, "sut", "TradeBlotter.Sut.exe");
        Trader.AttemptsTo(new LaunchApplication(path));
    }

    public void Stop()
    {
        if (driver is null) return;
        using var process = driver.ProcessId is int id ? Process.GetProcessById(id) : null;
        try { driver.Dispose(); }
        finally { driver = null; }
        Assert.That(process is null || process.HasExited, Is.True, "Scenario must leave no owned SUT process.");
    }
}

[Binding]
public sealed class DesktopHooks(DesktopSession session)
{
    [BeforeScenario]
    public void Start() => session.Start();

    [AfterScenario]
    public void Stop() => session.Stop();
}
