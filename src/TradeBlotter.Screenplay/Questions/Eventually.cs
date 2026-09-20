using System.Diagnostics;

namespace TradeBlotter.Screenplay.Questions;

/// <summary>Poll a read-only question on the calling thread until its answer satisfies a predicate.</summary>
public sealed class Eventually<T> : IQuestion<T>
{
    private readonly IQuestion<T> question;
    private readonly Func<T, bool> matches;
    private readonly TimeSpan timeout;
    private readonly TimeSpan pollInterval;
    private readonly string expectation;

    public Eventually(IQuestion<T> question, Func<T, bool> matches, TimeSpan timeout,
        TimeSpan pollInterval, string expectation)
    {
        ArgumentNullException.ThrowIfNull(question);
        ArgumentNullException.ThrowIfNull(matches);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectation);
        if (timeout <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(timeout));
        if (pollInterval <= TimeSpan.Zero) throw new ArgumentOutOfRangeException(nameof(pollInterval));
        this.question = question;
        this.matches = matches;
        this.timeout = timeout;
        this.pollInterval = pollInterval;
        this.expectation = expectation;
    }

    public T AnsweredBy(Actor actor)
    {
        var watch = Stopwatch.StartNew();
        while (true)
        {
            var answer = actor.AsksFor(question);
            if (matches(answer)) return answer;
            var remaining = timeout - watch.Elapsed;
            if (remaining <= TimeSpan.Zero)
                throw new TimeoutException($"Timed out after {timeout} waiting for {expectation}. Last observed: {answer}");
            // Keep UIA observations on the caller's thread; only the polling interval is delayed.
            Task.Delay(remaining < pollInterval ? remaining : pollInterval).GetAwaiter().GetResult();
        }
    }
}
