using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;

namespace TradeBlotter.Screenplay.Tasks;

public sealed record LaunchApplication(string ExecutablePath, string Arguments = "") : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().Launch(ExecutablePath, Arguments);
}

public sealed record ClickElement(By Locator) : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().Click(Locator);
}

public sealed record EnterText(By Locator, string Text) : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().Type(Locator, Text);
}

public sealed record SwitchWindow(By Locator) : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().SwitchToWindow(Locator);
}

public sealed record CloseApplication : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().Close();
}

public sealed record SelectOption(By Locator, string Text) : ITask
{
    public void PerformAs(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().Select(Locator, Text);
}
