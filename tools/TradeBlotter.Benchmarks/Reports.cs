using System.Globalization;
using System.Text;

namespace TradeBlotter.Benchmarks;

public static class Reports
{
    private static string Number(double? value) => value?.ToString("F4", CultureInfo.InvariantCulture) ?? "";
    private static string Csv(string? value) => "\"" + (value ?? "").Replace("\"", "\"\"") + "\"";
    private static string Cell(string? value) => (value ?? "").Replace("|", "\\|").Replace("\r", " ").Replace("\n", " ");
    public static string CsvText(IEnumerable<Sample> samples)
    {
        var output = new StringBuilder("engine,mode,status,iteration,phase,started_utc,pid,startup_ms,lookup_mean_ms,sut_working_set_mib,detail\n");
        foreach (var s in samples)
            output.AppendLine(string.Join(",", new[] { s.Engine, s.Mode, s.Status, s.Iteration.ToString(CultureInfo.InvariantCulture),
                s.Phase, s.StartedUtc.ToString("O"), s.ProcessId?.ToString(CultureInfo.InvariantCulture),
                Number(s.StartupMs), Number(s.LookupMeanMs), Number(s.SutWorkingSetMiB), s.Detail }.Select(Csv)));
        return output.ToString();
    }

    public static string Markdown(IEnumerable<Sample> samples)
    {
        var output = new StringBuilder("# Desktop benchmark observations\n\nSee metadata.json for environment, revision, options and executable SHA-256.\n\n");
        output.AppendLine("Startup includes adapter launch and first verified grid lookup; each repetition uses a fresh SUT process. Lookup includes AutomationId/enabled validation. Memory is a single SUT working-set snapshot, excluding the runner and automation servers. Warmups are retained in CSV but excluded below. These observations are not a controlled cross-machine ranking.\n");
        output.AppendLine("| Engine | Mode | Status | Samples | Median startup ms | Median lookup mean ms | Median SUT MiB | Detail |");
        output.AppendLine("|---|---|---|---:|---:|---:|---:|---|");
        foreach (var group in samples.GroupBy(s => (s.Engine, s.Mode)))
        {
            var errors = group.Where(s => s.Status is "ERROR" or "UNAVAILABLE").ToArray();
            var valid = group.Where(s => s.Status == "PASS" && s.Mode == "Native" && s.Phase == "measured").ToArray();
            var status = errors.Any(s => s.Status == "ERROR") ? "ERROR" : errors.Length > 0 ? "UNAVAILABLE" : group.Key.Mode == "Mock" ? "MOCK" : "PASS";
            // Never summarise partial/error runs or simulated samples as performance.
            var show = status == "PASS" && valid.Length > 0;
            output.AppendLine($"| {Cell(group.Key.Engine)} | {Cell(group.Key.Mode)} | {status} | {(show ? valid.Length : 0)} | {(show ? Number(Median(valid.Select(s => s.StartupMs!.Value))) : "N/A")} | {(show ? Number(Median(valid.Select(s => s.LookupMeanMs!.Value))) : "N/A")} | {(show ? Number(Median(valid.Select(s => s.SutWorkingSetMiB!.Value))) : "N/A")} | {Cell(string.Join("; ", group.Select(s => s.Detail).Where(s => s is not null).Distinct()))} |");
        }
        output.AppendLine("\nMOCK verifies harness plumbing only. UNAVAILABLE is missing implementation or prerequisites. Neither is native Ranorex evidence. See samples.csv for every attempt, including warmup and cleanup failures.");
        return output.ToString();
    }
    private static double Median(IEnumerable<double> values)
    {
        var sorted = values.Order().ToArray();
        return sorted.Length % 2 == 1 ? sorted[sorted.Length / 2] : (sorted[sorted.Length / 2 - 1] + sorted[sorted.Length / 2]) / 2;
    }
}
