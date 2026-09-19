using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;

namespace TradeBlotter.Screenplay;

public static class TradingActors
{
    public static Actor TommyTrader(IWindowsAutomationDriver driver) =>
        new("TommyTrader", true, BrowseTheDesktop.Using(driver));

    public static Actor AdamAuditor(IWindowsAutomationDriver driver) =>
        new("AdamAuditor", false, BrowseTheDesktop.Inspecting(driver));
}
