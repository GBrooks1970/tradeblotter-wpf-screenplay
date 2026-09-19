# Screenplay core (TB-02)

`TradeBlotter.Screenplay` references the vendor-neutral framework contracts. Its
assembly has no direct FlaUI, Ranorex or WebDriver reference. `Actor`, `IAbility`,
`ITask` and covariant `IQuestion<T>` provide synchronous task and question dispatch.

```csharp
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay;
using TradeBlotter.Screenplay.Tasks;
using TradeBlotter.Screenplay.Questions;

// driver is an IWindowsAutomationDriver owned by the surrounding fixture.
var trader = TradingActors.TommyTrader(driver);
var auditor = TradingActors.AdamAuditor(driver);
trader.AttemptsTo(
    new LaunchApplication(executablePath),
    new ClickElement(By.AutomationId("BtnNewOrder")),
    new SwitchWindow(By.AutomationId("DlgOrderEntry")),
    new EnterText(By.AutomationId("TxtQuantity"), "250000"));
string quantity = auditor.AsksFor(new TextOf(By.AutomationId("TxtQuantity")));
```

Actor ability registration is immutable and keyed by concrete ability type.
`AbilityTo<T>()` reports missing abilities with actor and ability names; duplicate
types are rejected. `AttemptsTo` validates the task list before execution, executes
in order, stops on the first exception and returns the actor for fluent chaining.
Earlier successful actions are not rolled back. `AsksFor` returns the typed answer.

## Actor roles and ownership

`TommyTrader` receives `BrowseTheDesktop.Using(driver)`. `AdamAuditor` receives
`BrowseTheDesktop.Inspecting(driver)` and rejects all task dispatch. The inspection
ability also guards launch, click, type, window switching and close, so directly
invoking a task or attempting mutation from a question cannot bypass this boundary.
Window switching is guarded because the driver implementation changes focus.

Both modes return `IDesktopObservation` wrappers for reads, including nested results.
These do not implement `IAutomationElement` and expose no interaction methods.
The raw driver is private. Ability registrations cannot be replaced after actor
creation. These are automation API safeguards, not an application security boundary:
trusted fixture code still owns the driver and arbitrary custom code can retain
external references. This does not implement SUT authentication or authorisation.

The fixture owns driver disposal; actors and abilities do not dispose it. Sharing
a driver means sharing its active window and application lifetime. Execute actors
sequentially. An auditor inspects the current scope selected by the trader or
fixture. Lookup waits and stale-element behaviour come from the driver; the core
does not add retries or promise atomic operations.

## Delivered scope and validation

Foundational tasks are `LaunchApplication`, `ClickElement`, `EnterText`,
`SwitchWindow` and `CloseApplication`. Questions are `TextOf` and `CountOf`.
TB-03 now adds placement tasks and blotter questions; see
[order-placement BDD](order-placement-bdd.md). Cancellation and ticker behaviour
remain TB-05 and TB-06. They were not delivered by the TB-02 core acceptance item.
The backlog's broader Risk #2 refactor roadmap is retained with that allocation.

Fourteen in-memory NUnit tests cover dispatch, fluent return, failures, ability
registration, both actors, nested read wrappers and mutation denial. The mock
driver validates exact call order and arguments. These tests prove the core API,
not live trading workflows or native UI behaviour. See the
[project contract](project-contract.md) for required build and test commands.
