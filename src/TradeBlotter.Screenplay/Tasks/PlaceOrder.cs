using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;

namespace TradeBlotter.Screenplay.Tasks;

/// <summary>Submits raw form values, allowing validation scenarios to reach the SUT.</summary>
public sealed record PlaceOrder(string Symbol, string OrderType, string Quantity, string LimitPrice) : ITask
{
    public void PerformAs(Actor actor)
    {
        if (OrderType is not ("LIMIT" or "MARKET")) throw new ArgumentException("Expected LIMIT or MARKET.", nameof(OrderType));
        var desktop = actor.AbilityTo<BrowseTheDesktop>();
        desktop.Click(By.AutomationId("BtnNewOrder"));
        desktop.SwitchToWindow(By.AutomationId("DlgOrderEntry"));
        desktop.Select(By.AutomationId("CmbSymbol"), Symbol);
        desktop.Select(By.AutomationId("CmbOrderType"), OrderType);
        desktop.Type(By.AutomationId("TxtQuantity"), Quantity);
        if (OrderType == "LIMIT") desktop.Type(By.AutomationId("TxtLimitPrice"), LimitPrice);
        desktop.Click(By.AutomationId("BtnSubmitOrder"));
        // Caller selects the expected outcome window: main blotter or validation dialog.
    }
}

public sealed record ReturnToBlotter : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().SwitchToWindow(By.AutomationId("TradeBlotterMainView"));
}

public sealed record InspectValidationError : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().SwitchToWindow(By.Name("Validation Error"));
}

public sealed record DismissInvalidOrder : ITask
{
    public void PerformAs(Actor actor)
    {
        var desktop = actor.AbilityTo<BrowseTheDesktop>();
        desktop.Click(By.Name("OK"));
        desktop.SwitchToWindow(By.AutomationId("DlgOrderEntry"));
        desktop.Click(By.AutomationId("BtnCancelModal"));
        new ReturnToBlotter().PerformAs(actor);
    }
}
