using System.Globalization;
using NUnit.Framework;
using TradeBlotter.Screenplay.Questions;

namespace TradeBlotter.Screenplay.Tests;

public partial class ScreenplayTests
{
    [TestCase("EUR/USD", "1.0844")]
    [TestCase("GBP/USD", "1.2712")]
    public void TickerPriceReadsTheRequestedSymbolWithInvariantDecimals(string symbol, string expected)
    {
        var driver = new FakeDriver();
        driver.Type(Field, "EUR/USD 1.0844 ▲ | GBP/USD 1.2712 ▼");
        var before = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            var actual = TradingActors.AdamAuditor(driver).AsksFor(new TickerPrice(symbol));
            Assert.That(actual, Is.EqualTo(decimal.Parse(expected, CultureInfo.InvariantCulture)));
            Assert.That(driver.Calls.Last(), Is.EqualTo("find:AutomationId=TxtPriceTicker"));
        }
        finally { CultureInfo.CurrentCulture = before; }
    }

    [TestCase("GBP/USD 1.2712 ▼")]
    [TestCase("EUR/USD invalid ▲")]
    [TestCase("EUR/USD 1.0844 ▲ | EUR/USD 1.0840 ▼")]
    public void InvalidTickerObservationsFailRatherThanReturningAFabricatedPrice(string display)
    {
        var driver = new FakeDriver();
        driver.Type(Field, display);
        Assert.Throws<FormatException>(() => TradingActors.AdamAuditor(driver).AsksFor(new TickerPrice("EUR/USD")));
    }

    [Test]
    public void EventuallyReobservesUntilThePredicateMatchesOnTheCallingThread()
    {
        var caller = Environment.CurrentManagedThreadId;
        var calls = 0;
        var question = new ProbeQuestion(() =>
        {
            Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(caller));
            return ++calls;
        });
        var result = new Actor("Observer").AsksFor(new Eventually<int>(question, n => n == 3,
            TimeSpan.FromSeconds(2), TimeSpan.FromMilliseconds(1), "third observation"));
        Assert.That(result, Is.EqualTo(3));
        Assert.That(calls, Is.EqualTo(3));
    }

    [Test]
    public void EventuallyReturnsAnImmediateMatchWithoutAnotherObservation()
    {
        var calls = 0;
        var result = new Actor("Observer").AsksFor(new Eventually<int>(new ProbeQuestion(() => ++calls),
            n => n == 1, TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(5), "first observation"));
        Assert.That(result, Is.EqualTo(1));
        Assert.That(calls, Is.EqualTo(1));
    }

    [Test]
    public void EventuallyReportsTheExpectationAndLastObservationOnTimeout()
    {
        var error = Assert.Throws<TimeoutException>(() => new Actor("Observer").AsksFor(
            new Eventually<int>(new ProbeQuestion(() => 7), n => n == 9,
                TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(2), "price 9")));
        Assert.That(error!.Message, Does.Contain("price 9").And.Contain("Last observed: 7"));
    }

    [Test]
    public void EventuallyPropagatesObservationAndPredicateErrorsWithoutRetrying()
    {
        var error = new FormatException("broken quote");
        var calls = 0;
        var actor = new Actor("Observer");
        var actual = Assert.Throws<FormatException>(() => actor.AsksFor(new Eventually<int>(
            new ProbeQuestion(() => { calls++; throw error; }), _ => true,
            TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1), "valid quote")));
        Assert.That(actual, Is.SameAs(error));
        Assert.That(calls, Is.EqualTo(1));
        Assert.That(Assert.Throws<FormatException>(() => actor.AsksFor(new Eventually<int>(
            new ProbeQuestion(() => 1), _ => throw error,
            TimeSpan.FromSeconds(1), TimeSpan.FromMilliseconds(1), "valid quote"))), Is.SameAs(error));
    }

    [TestCase(0, 1)]
    [TestCase(-1, 1)]
    [TestCase(1, 0)]
    [TestCase(1, -1)]
    public void EventuallyRejectsNonPositiveTiming(int timeoutMs, int intervalMs)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new Eventually<int>(new ProbeQuestion(() => 1),
            _ => true, TimeSpan.FromMilliseconds(timeoutMs), TimeSpan.FromMilliseconds(intervalMs), "value"));
    }

    [Test]
    public void EventuallyRejectsMissingQuestionPredicateOrExpectation()
    {
        var question = new ProbeQuestion(() => 1);
        var duration = TimeSpan.FromSeconds(1);
        Assert.Throws<ArgumentNullException>(() => new Eventually<int>(null!, _ => true, duration, duration, "value"));
        Assert.Throws<ArgumentNullException>(() => new Eventually<int>(question, null!, duration, duration, "value"));
        Assert.Throws<ArgumentException>(() => new Eventually<int>(question, _ => true, duration, duration, " "));
    }

    private sealed record ProbeQuestion(Func<int> Observe) : IQuestion<int>
    {
        public int AnsweredBy(Actor actor) => Observe();
    }
}
