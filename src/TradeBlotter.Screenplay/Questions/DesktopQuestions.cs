using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;

namespace TradeBlotter.Screenplay.Questions;

public sealed record TextOf(By Locator) : IQuestion<string>
{
    public string AnsweredBy(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().Find(Locator).Text;
}

public sealed record CountOf(By Locator) : IQuestion<int>
{
    public int AnsweredBy(Actor actor) => actor.AbilityTo<BrowseTheDesktop>().FindAll(Locator).Count;
}
