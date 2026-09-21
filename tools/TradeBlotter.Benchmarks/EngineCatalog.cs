using System.Net.Http;
using TradeBlotter.Framework.FlaUI;
using TradeBlotter.Framework.WinAppDriver;

namespace TradeBlotter.Benchmarks;

public static class EngineCatalog
{
    public static Engine Resolve(string name) => name switch
    {
        "flaui" => new(name, ExecutionMode.Native, () => new FlaUiDriverAdapter()),
        "winappdriver" => WinAppDriver(name),
        "ranorex" => new(name, ExecutionMode.Native, null, "Native Ranorex adapter, SDK and licensed execution are deferred to TB-08B. No mock substitution."),
        "ranorex-mock" => new(name, ExecutionMode.Mock, () => new MockBenchmarkDriver()),
        _ => throw new ArgumentException($"Unknown driver '{name}'. Use flaui, winappdriver, ranorex or ranorex-mock.")
    };
    private static Engine WinAppDriver(string name)
    {
        using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
        try
        {
            foreach (var port in new[] { 4723, 4724 })
            {
                using var response = client.GetAsync($"http://127.0.0.1:{port}/status").GetAwaiter().GetResult();
                response.EnsureSuccessStatusCode();
            }
            return new(name, ExecutionMode.Native, () => new WinAppDriverAdapter());
        }
        catch (Exception error) when (error is HttpRequestException or TaskCanceledException)
        {
            return new(name, ExecutionMode.Native, null, "WinAppDriver/Appium status endpoint unavailable. Use run-bdd.ps1 -Driver winappdriver -Benchmark on a prepared desktop.");
        }
    }
}
