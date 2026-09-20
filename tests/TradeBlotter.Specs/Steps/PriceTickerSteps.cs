using System.Globalization;
using NUnit.Framework;
using Reqnroll;
using TradeBlotter.Screenplay.Questions;
using TradeBlotter.Screenplay.Tasks;
using TradeBlotter.Specs.Support;

namespace TradeBlotter.Specs.Steps;

[Binding]
public sealed class PriceTickerSteps(DesktopSession session)
{
    private IReadOnlyList<BlotterOrder> original = [];

    [When("AdamAuditor observes {string} reach {string} then {string} then {string}")]
    public void ObserveCycle(string symbol, string first, string second, string repeated)
    {
        original = session.Auditor.AsksFor(new BlotterOrders());
        foreach (var value in new[] { first, second, repeated })
        {
            var expected = decimal.Parse(value, CultureInfo.InvariantCulture);
            var actual = session.Auditor.AsksFor(new Eventually<decimal>(new TickerPrice(symbol),
                price => price == expected, TimeSpan.FromSeconds(5), TimeSpan.FromMilliseconds(100),
                $"{symbol} price {value}"));
            Assert.That(actual, Is.EqualTo(expected));
        }
    }

    [Then("TommyTrader can select an order and the seeded blotter is unchanged")]
    public void VerifyResponsiveDesktop()
    {
        session.Trader.AttemptsTo(new SelectedOrder("ORD-2026-0902"));
        Assert.That(session.Auditor.AsksFor(new BlotterOrders()), Is.EqualTo(original));
    }
}
