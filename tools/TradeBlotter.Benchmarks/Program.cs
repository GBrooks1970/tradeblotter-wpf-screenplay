using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using TradeBlotter.Benchmarks;

try
{
    var values = new Dictionary<string, string>(StringComparer.Ordinal);
    foreach (var arg in args)
    {
        var parts = arg.Split('=', 2);
        if (parts.Length != 2 || !new[] { "--drivers", "--sut", "--output", "--repetitions", "--lookups", "--warmups", "--revision", "--allow-unavailable" }.Contains(parts[0]) || !values.TryAdd(parts[0], parts[1]))
            throw new ArgumentException($"Unknown, duplicate or malformed argument: {arg}");
    }
    string Get(string key, string fallback) => values.GetValueOrDefault(key, fallback);
    var names = Get("--drivers", "flaui,winappdriver,ranorex").Split(',');
    if (names.Distinct().Count() != names.Length) throw new ArgumentException("Duplicate drivers are not allowed.");
    var engines = names.Select(EngineCatalog.Resolve).ToArray();
    var sut = Path.GetFullPath(Get("--sut", "src/TradeBlotter.Sut/bin/Release/net9.0-windows/TradeBlotter.Sut.exe"));
    if (!File.Exists(sut)) throw new FileNotFoundException("Build the SUT before benchmarking.", sut);
    var options = new BenchmarkOptions(int.Parse(Get("--repetitions", "3")), int.Parse(Get("--lookups", "20")), int.Parse(Get("--warmups", "1")));
    options.Validate();
    var allowUnavailable = bool.Parse(Get("--allow-unavailable", "false"));
    var output = Path.GetFullPath(Get("--output", $"TestResults/benchmarks/{Guid.NewGuid():N}"));
    if (Directory.Exists(output)) throw new IOException("Output directory already exists; refusing to overwrite benchmark evidence.");
    Directory.CreateDirectory(output);
    var metadata = new
    {
        SchemaVersion = 1, StartedUtc = DateTimeOffset.UtcNow, Revision = Get("--revision", "UNKNOWN"),
        OS = RuntimeInformation.OSDescription, Runtime = RuntimeInformation.FrameworkDescription,
        Architecture = RuntimeInformation.ProcessArchitecture.ToString(), Environment.ProcessorCount,
        Environment.UserInteractive, ImageVersion = Environment.GetEnvironmentVariable("ImageVersion"),
        CiRun = Environment.GetEnvironmentVariable("GITHUB_RUN_ID"), Drivers = names, Options = options,
        SutSha256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(sut))),
        Arguments = "", Ticker = "Normal SUT timer remains enabled", AllowUnavailable = allowUnavailable
    };
    File.WriteAllText(Path.Combine(output, "metadata.json"), JsonSerializer.Serialize(metadata, new JsonSerializerOptions { WriteIndented = true }));
    var samples = new List<Sample>();
    foreach (var engine in engines)
    {
        samples.AddRange(BenchmarkRunner.Run(engine, sut, options));
        // Persist after each engine so a later failure cannot erase earlier evidence.
        File.WriteAllText(Path.Combine(output, "samples.csv"), Reports.CsvText(samples));
        File.WriteAllText(Path.Combine(output, "report.md"), Reports.Markdown(samples));
    }
    Console.WriteLine(Reports.Markdown(samples));
    Console.WriteLine($"Evidence: {output}");
    return BenchmarkRunner.ExitCode(samples, allowUnavailable);
}
catch (Exception error)
{
    Console.Error.WriteLine(error.Message);
    return 1;
}
