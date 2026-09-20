using NUnit.Framework;
using Reqnroll;
using TradeBlotter.Screenplay.Questions;
using TradeBlotter.Screenplay.Tasks;
using TradeBlotter.Specs.Support;

namespace TradeBlotter.Specs.Steps;

[Binding]
public sealed class OrderCancellationSteps(DesktopSession session)
{
    private IReadOnlyList<BlotterOrder> original = [];
    private IReadOnlyList<BlotterOrder> expected = [];
    private string targetId = "";

    [Given("the cancellation target {string} has status {string} and filled quantity {int}")]
    public void CaptureTarget(string orderId, string status, int filled)
    {
        original = session.Auditor.AsksFor(new BlotterOrders());
        targetId = orderId;
        var target = original.Single(o => o.Id == orderId);
        Assert.Multiple(() =>
        {
            Assert.That(target.Status, Is.EqualTo(status));
            Assert.That(target.FilledQuantity, Is.EqualTo(filled));
        });
    }

    [When("TommyTrader cancels order {string}")]
    public void Cancel(string orderId)
    {
        Assert.That(orderId, Is.EqualTo(targetId));
        session.Trader.AttemptsTo(new CancelOrder(orderId));
    }

    [Then("AdamAuditor sees only that order's status become {string}")]
    public void Verify(string status)
    {
        expected = original.Select(o => o.Id == targetId ? o with { Status = status } : o).ToArray();
        Assert.That(() => session.Auditor.AsksFor(new BlotterOrders()),
            Is.EqualTo(expected).After(5000, 100),
            "Only the target status may change; row count, order, fills and other values must be preserved.");
    }

    [Then("repeating the cancellation leaves the entire blotter unchanged")]
    public void Repeat()
    {
        session.Trader.AttemptsTo(new CancelOrder(targetId));
        Assert.That(session.Auditor.AsksFor(new BlotterOrders()), Is.EqualTo(expected));
    }
}
