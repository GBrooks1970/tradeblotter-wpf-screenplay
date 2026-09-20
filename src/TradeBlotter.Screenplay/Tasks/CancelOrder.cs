using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;

namespace TradeBlotter.Screenplay.Tasks;

/// <summary>Select a realised blotter row and verify selection before any toolbar action.</summary>
public sealed record SelectedOrder(string OrderId) : ITask
{
    public void PerformAs(Actor actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(OrderId);
        var desktop = actor.AbilityTo<BrowseTheDesktop>();
        var row = By.AutomationId(OrderId);
        desktop.SelectItem(row);
        if (!desktop.Find(row).IsSelected)
            throw new InvalidOperationException($"Order {OrderId} was not selected; toolbar action refused.");
    }
}

public sealed record CancelOrder(string OrderId) : ITask
{
    public void PerformAs(Actor actor)
    {
        new SelectedOrder(OrderId).PerformAs(actor);
        actor.AbilityTo<BrowseTheDesktop>().Click(By.AutomationId("BtnCancelOrder"));
    }
}
