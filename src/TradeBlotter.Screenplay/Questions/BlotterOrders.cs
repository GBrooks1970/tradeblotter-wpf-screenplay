using System.Globalization;
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;

namespace TradeBlotter.Screenplay.Questions;

public sealed record BlotterOrder(string Id, string Symbol, string Side, int Quantity, decimal Price, string Status, int FilledQuantity);

public sealed record BlotterOrders : IQuestion<IReadOnlyList<BlotterOrder>>
{
    public IReadOnlyList<BlotterOrder> AnsweredBy(Actor actor)
    {
        var grid = actor.AbilityTo<BrowseTheDesktop>().Find(By.AutomationId("GridBlotterOrders"));
        return grid.FindAll(By.XPath("./*[starts-with(@AutomationId, 'ORD-')]"))
            .Select(row =>
            {
                var cells = row.FindAll(By.XPath("./*")).Select(cell => cell.Text).ToArray();
                if (cells.Length != 7) throw new InvalidOperationException($"Expected 7 blotter cells for {row.AutomationId}, found {cells.Length}.");
                return new BlotterOrder(cells[0], cells[1], cells[2],
                    int.Parse(cells[3], NumberStyles.AllowThousands, CultureInfo.InvariantCulture),
                    decimal.Parse(cells[4], CultureInfo.InvariantCulture), cells[5],
                    int.Parse(cells[6], NumberStyles.AllowThousands, CultureInfo.InvariantCulture));
            }).ToArray();
    }
}

public sealed record ValidationMessage(string Expected) : IQuestion<string>
{
    public string AnsweredBy(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().Find(By.Name(Expected)).Text;
}
