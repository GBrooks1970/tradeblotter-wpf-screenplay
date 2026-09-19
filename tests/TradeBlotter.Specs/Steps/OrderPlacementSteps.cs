using System.Globalization;
using NUnit.Framework;
using Reqnroll;
using TradeBlotter.Screenplay.Questions;
using TradeBlotter.Screenplay.Tasks;
using TradeBlotter.Specs.Support;

namespace TradeBlotter.Specs.Steps;

[Binding]
public sealed class OrderPlacementSteps(DesktopSession session)
{
    private IReadOnlyList<BlotterOrder> original = [];
    private BlotterOrder? placed;

    [Given("TommyTrader has a fresh trading desktop with {int} seeded orders")]
    public void FreshDesktop(int count)
    {
        original = session.Auditor.AsksFor(new BlotterOrders());
        Assert.That(original, Has.Count.EqualTo(count));
        Assert.That(original.Select(o => o.Id), Is.Unique);
    }

    [When("TommyTrader submits a {string} order for {string} with quantity {string} and limit price {string}")]
    public void Submit(string type, string symbol, string quantity, string price) =>
        session.Trader.AttemptsTo(new PlaceOrder(symbol, type, quantity, price));

    [Then("AdamAuditor sees {int} orders including {string} for {string} with quantity {int} and price {string}")]
    public void VerifyOrder(int count, string id, string symbol, int quantity, string price)
    {
        session.Trader.AttemptsTo(new ReturnToBlotter());
        var orders = session.Auditor.AsksFor(new BlotterOrders());
        Assert.That(orders, Has.Count.EqualTo(count));
        placed = orders.Single(o => o.Id == id);
        Assert.Multiple(() =>
        {
            Assert.That(placed.Symbol, Is.EqualTo(symbol));
            Assert.That(placed.Quantity, Is.EqualTo(quantity));
            Assert.That(placed.Price, Is.EqualTo(decimal.Parse(price, CultureInfo.InvariantCulture)));
            Assert.That(orders.Where(o => o.Id != id), Is.EqualTo(original));
        });
    }

    [Then("the new order is BUY and PENDING with no filled quantity")]
    public void VerifyState()
    {
        Assert.That(placed, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(placed!.Side, Is.EqualTo("BUY"));
            Assert.That(placed.Status, Is.EqualTo("PENDING"));
            Assert.That(placed.FilledQuantity, Is.Zero);
        });
    }

    [Then("the validation message is {string}")]
    public void VerifyError(string expected)
    {
        session.Trader.AttemptsTo(new InspectValidationError());
        Assert.That(session.Auditor.AsksFor(new ValidationMessage(expected)), Is.EqualTo(expected));
    }

    [Then("dismissing the invalid order leaves the original blotter unchanged")]
    public void Unchanged()
    {
        session.Trader.AttemptsTo(new DismissInvalidOrder());
        Assert.That(session.Auditor.AsksFor(new BlotterOrders()), Is.EqualTo(original));
    }
}
