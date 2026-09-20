using System.Globalization;
using System.Text.RegularExpressions;
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;

namespace TradeBlotter.Screenplay.Questions;

/// <summary>Read one symbol's displayed price; malformed or ambiguous observations fail immediately.</summary>
public sealed record TickerPrice(string Symbol) : IQuestion<decimal>
{
    public decimal AnsweredBy(Actor actor)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Symbol);
        var text = actor.AbilityTo<BrowseTheDesktop>().Find(By.AutomationId("TxtPriceTicker")).Text;
        var quotes = text.Split('|').Select(part => part.Trim())
            .Where(part => part.StartsWith(Symbol + " ", StringComparison.Ordinal)).ToArray();
        if (quotes.Length != 1)
            throw new FormatException($"Expected exactly one ticker quote for {Symbol}; observed: {text}");
        var match = Regex.Match(quotes[0], @"^[A-Z]{3}/[A-Z]{3} (?<price>[0-9]+\.[0-9]{4}) [▲▼]$");
        if (!match.Success) throw new FormatException($"Malformed ticker quote for {Symbol}: {quotes[0]}");
        return decimal.Parse(match.Groups["price"].Value, CultureInfo.InvariantCulture);
    }
}
