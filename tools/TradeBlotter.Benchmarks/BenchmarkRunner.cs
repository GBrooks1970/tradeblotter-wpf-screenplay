using System.Diagnostics;
using TradeBlotter.Framework.Abstractions;

namespace TradeBlotter.Benchmarks;

public enum ExecutionMode { Native, Mock }
public sealed record Engine(string Name, ExecutionMode Mode, Func<IWindowsAutomationDriver>? Create, string? UnavailableReason = null);
public sealed record Sample(string Engine, string Mode, string Status, int Iteration, string Phase,
    DateTimeOffset StartedUtc, int? ProcessId = null, double? StartupMs = null,
    double? LookupMeanMs = null, double? SutWorkingSetMiB = null, string? Detail = null);
public sealed record BenchmarkOptions(int Repetitions = 3, int Lookups = 20, int Warmups = 1)
{
    public void Validate()
    {
        if (Repetitions is < 1 or > 100 || Lookups is < 1 or > 1000 || Warmups is < 0 or > 10)
            throw new ArgumentOutOfRangeException(nameof(Repetitions), "Use 1-100 repetitions, 1-1000 lookups and 0-10 warmups.");
    }
}

public static class BenchmarkRunner
{
    public static IReadOnlyList<Sample> Run(Engine engine, string executable, BenchmarkOptions options)
    {
        options.Validate();
        if (engine.Create is null)
            return [new(engine.Name, engine.Mode.ToString(), "UNAVAILABLE", 0, "preflight", DateTimeOffset.UtcNow,
                Detail: engine.UnavailableReason ?? "No implementation available.")];
        var samples = new List<Sample>();
        for (var iteration = 1 - options.Warmups; iteration <= options.Repetitions; iteration++)
        {
            var phase = iteration <= 0 ? "warmup" : "measured";
            var started = DateTimeOffset.UtcNow;
            IWindowsAutomationDriver? driver = null;
            Process? process = null;
            Sample sample;
            try
            {
                driver = engine.Create();
                var timer = Stopwatch.StartNew();
                driver.Launch(executable);
                if (engine.Mode == ExecutionMode.Native)
                    process = Process.GetProcessById(driver.ProcessId ?? throw new InvalidOperationException("Native driver has no owned PID."));
                VerifyGrid(driver.Find(By.AutomationId("GridBlotterOrders")));
                var startup = timer.Elapsed.TotalMilliseconds;
                timer.Restart();
                for (var lookup = 0; lookup < options.Lookups; lookup++)
                    VerifyGrid(driver.Find(By.AutomationId("GridBlotterOrders")));
                var lookupMean = timer.Elapsed.TotalMilliseconds / options.Lookups;
                process?.Refresh();
                var native = engine.Mode == ExecutionMode.Native;
                sample = new(engine.Name, engine.Mode.ToString(), native ? "PASS" : "MOCK", iteration, phase, started,
                    process?.Id, native ? startup : null, native ? lookupMean : null,
                    native ? process!.WorkingSet64 / 1048576d : null,
                    native ? null : "Simulated routing only; no WPF automation or performance measurement.");
            }
            catch (Exception error)
            {
                sample = new(engine.Name, engine.Mode.ToString(), "ERROR", iteration, phase, started, Detail: error.Message);
            }
            finally
            {
                // Dispose even after partial launch or failed lookup. The adapter owns
                // the process; the harness only checks the captured process handle.
                try { driver?.Dispose(); }
                catch (Exception error)
                {
                    samples.Add(new(engine.Name, engine.Mode.ToString(), "ERROR", iteration, "cleanup", started, Detail: error.Message));
                }
                if (process is not null)
                {
                    if (!process.HasExited)
                        samples.Add(new(engine.Name, engine.Mode.ToString(), "ERROR", iteration, "cleanup", started,
                            ProcessId: process.Id, Detail: "Owned SUT remains after adapter disposal."));
                    process.Dispose();
                }
            }
            samples.Add(sample);
            if (samples.Any(s => s.Status == "ERROR")) break;
        }
        return samples;
    }

    private static void VerifyGrid(IAutomationElement grid)
    {
        if (grid.AutomationId != "GridBlotterOrders" || !grid.IsEnabled)
            throw new InvalidOperationException("Benchmark lookup did not resolve the enabled blotter grid.");
    }

    public static int ExitCode(IEnumerable<Sample> samples, bool allowUnavailable)
    {
        var rows = samples.ToArray();
        if (rows.Length == 0 || rows.Any(s => s.Status == "ERROR")) return 1;
        return !allowUnavailable && rows.Any(s => s.Status == "UNAVAILABLE") ? 2 : 0;
    }
}
