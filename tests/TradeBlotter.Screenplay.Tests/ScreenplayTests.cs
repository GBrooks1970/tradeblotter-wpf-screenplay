using NUnit.Framework;
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Screenplay.Abilities;
using TradeBlotter.Screenplay.Questions;
using TradeBlotter.Screenplay.Tasks;

namespace TradeBlotter.Screenplay.Tests;

public class ScreenplayTests
{
    private static readonly By Field = By.AutomationId("quantity");

    [Test]
    public void TraderPerformsOrderedTasksAndBothActorsReadTheResult()
    {
        var driver = new FakeDriver();
        var trader = TradingActors.TommyTrader(driver);
        var auditor = TradingActors.AdamAuditor(driver);
        var returned = trader.AttemptsTo(new LaunchApplication("app.exe", "--demo"),
            new ClickElement(Field), new EnterText(Field, "250000"), new SwitchWindow(By.Name("Dialog")));
        Assert.That(returned, Is.SameAs(trader));
        Assert.That(trader.Name, Is.EqualTo("TommyTrader"));
        Assert.That(auditor.Name, Is.EqualTo("AdamAuditor"));
        Assert.That(driver.Calls, Is.EqualTo(new[] { "launch:app.exe:--demo", "click:AutomationId=quantity", "type:AutomationId=quantity:250000", "window:Name=Dialog" }));
        Assert.That(trader.AsksFor(new TextOf(Field)), Is.EqualTo("250000"));
        Assert.That(auditor.AsksFor(new TextOf(Field)), Is.EqualTo("250000"));
        Assert.That(auditor.AsksFor(new CountOf(Field)), Is.EqualTo(1));
        trader.AttemptsTo(new CloseApplication());
        Assert.That(driver.Calls.Last(), Is.EqualTo("close"));
    }

    [TestCase("launch")]
    [TestCase("click")]
    [TestCase("type")]
    [TestCase("window")]
    [TestCase("close")]
    [TestCase("select")]
    [TestCase("selected-order")]
    [TestCase("cancel-order")]
    public void AuditorCannotMutateEvenWhenTaskIsInvokedDirectly(string operation)
    {
        var driver = new FakeDriver();
        var auditor = TradingActors.AdamAuditor(driver);
        ITask task = operation switch
        {
            "launch" => new LaunchApplication("app"),
            "click" => new ClickElement(Field),
            "type" => new EnterText(Field, "x"),
            "window" => new SwitchWindow(Field),
            "select" => new SelectOption(Field, "MARKET"),
            "selected-order" => new SelectedOrder("ORD-2026-0902"),
            "cancel-order" => new CancelOrder("ORD-2026-0902"),
            _ => new CloseApplication()
        };
        Assert.Throws<InvalidOperationException>(() => auditor.AttemptsTo(task));
        Assert.Throws<InvalidOperationException>(() => task.PerformAs(auditor));
        Assert.That(auditor.AbilityTo<BrowseTheDesktop>().CanInteract, Is.False);
        Assert.That(driver.Calls, Is.Empty);
    }

    [Test]
    public void QuestionCannotBypassReadOnlyDesktopAbility()
    {
        var driver = new FakeDriver();
        var auditor = TradingActors.AdamAuditor(driver);
        Assert.Throws<InvalidOperationException>(() => auditor.AsksFor(new MutatingQuestion()));
        Assert.That(driver.Calls, Is.Empty);
    }

    [Test]
    public void ObservationsDoNotLeakMutableElementsIncludingNestedResults()
    {
        var driver = new FakeDriver();
        var ability = TradingActors.AdamAuditor(driver).AbilityTo<BrowseTheDesktop>();
        var observation = ability.Find(Field);
        Assert.That(observation, Is.Not.InstanceOf<IAutomationElement>());
        Assert.That(observation.Find(Field), Is.Not.InstanceOf<IAutomationElement>());
        Assert.That(observation.FindAll(Field).Single(), Is.Not.InstanceOf<IAutomationElement>());
        Assert.That(ability.FindAll(Field).Single(), Is.Not.InstanceOf<IAutomationElement>());
        Assert.That(observation.AutomationId, Is.EqualTo("quantity"));
        Assert.That(observation.Name, Is.EqualTo("Quantity"));
        Assert.That(observation.IsEnabled, Is.True);
    }

    [Test]
    public void MissingAndDuplicateAbilitiesFailClearly()
    {
        var actor = new Actor("NoDesktop");
        var error = Assert.Throws<InvalidOperationException>(() => actor.AsksFor(new TextOf(Field)));
        Assert.That(error!.Message, Does.Contain("NoDesktop").And.Contain("BrowseTheDesktop"));
        var ability = BrowseTheDesktop.Using(new FakeDriver());
        Assert.Throws<ArgumentException>(() => new Actor("Duplicate", ability, ability));
    }

    [Test]
    public void AbilityRegistrationIsCopiedFromCallerArray()
    {
        var first = BrowseTheDesktop.Inspecting(new FakeDriver());
        IAbility[] input = [first];
        var actor = new Actor("Reader", input);
        input[0] = BrowseTheDesktop.Using(new FakeDriver());
        Assert.That(actor.AbilityTo<BrowseTheDesktop>(), Is.SameAs(first));
    }

    [Test]
    public void TaskFailureStopsTheSequenceAndPropagatesOriginalException()
    {
        var driver = new FakeDriver();
        var error = new ApplicationException("failed task");
        var actor = TradingActors.TommyTrader(driver);
        var actual = Assert.Throws<ApplicationException>(() => actor.AttemptsTo(
            new ClickElement(Field), new FailingTask(error), new CloseApplication()));
        Assert.That(actual, Is.SameAs(error));
        Assert.That(driver.Calls, Has.Count.EqualTo(1));
    }

    [Test]
    public void NullTaskIsRejectedBeforeAnyAction()
    {
        var driver = new FakeDriver();
        var actor = TradingActors.TommyTrader(driver);
        Assert.Throws<ArgumentException>(() => actor.AttemptsTo(new ClickElement(Field), null!));
        Assert.That(driver.Calls, Is.Empty);
        Assert.Throws<ArgumentNullException>(() => actor.AsksFor<string>(null!));
        Assert.Throws<ArgumentException>(() => new Actor(" "));
        Assert.Throws<ArgumentNullException>(() => BrowseTheDesktop.Using(null!));
    }

    [Test]
    public void AuditTaskIsRejectedBeforeItsCodeRuns()
    {
        var auditor = TradingActors.AdamAuditor(new FakeDriver());
        Assert.Throws<InvalidOperationException>(() => auditor.AttemptsTo(new FailingTask(new ApplicationException())));
    }

    [Test]
    public void ScreenplayAssemblyDoesNotReferenceVendorAssemblies()
    {
        var names = typeof(Actor).Assembly.GetReferencedAssemblies().Select(a => a.Name!);
        Assert.That(names.Any(n => n.StartsWith("FlaUI") || n.StartsWith("Ranorex") || n.StartsWith("WebDriver")), Is.False);
        foreach (var method in typeof(BrowseTheDesktop).GetMethods())
            Assert.That(method.ReturnType, Is.Not.EqualTo(typeof(IWindowsAutomationDriver)).And.Not.EqualTo(typeof(IAutomationElement)));
    }

    [Test]
    public void CancellationSelectsTargetBeforeInvokingToolbar()
    {
        var driver = new FakeDriver();
        TradingActors.TommyTrader(driver).AttemptsTo(new CancelOrder("ORD-2026-0902"));
        Assert.That(driver.Calls, Is.EqualTo(new[] { "find:AutomationId=ORD-2026-0902", "select-item", "find:AutomationId=ORD-2026-0902", "click:AutomationId=BtnCancelOrder" }));
    }

    [Test]
    public void UnconfirmedSelectionNeverInvokesCancellation()
    {
        var driver = new FakeDriver { SelectionSucceeds = false };
        var error = Assert.Throws<InvalidOperationException>(() =>
            TradingActors.TommyTrader(driver).AttemptsTo(new CancelOrder("ORD-2026-0902")));
        Assert.That(error!.Message, Does.Contain("was not selected"));
        Assert.That(driver.Calls.Any(c => c.StartsWith("click:")), Is.False);
    }

    [Test]
    public void BlankOrderIdIsRejectedBeforeInteraction()
    {
        var driver = new FakeDriver();
        Assert.Throws<ArgumentException>(() => TradingActors.TommyTrader(driver).AttemptsTo(new CancelOrder(" ")));
        Assert.That(driver.Calls, Is.Empty);
    }

    private sealed record FailingTask(Exception Error) : ITask
    {
        public void PerformAs(Actor actor) => throw Error;
    }
    private sealed class MutatingQuestion : IQuestion<string>
    {
        public string AnsweredBy(Actor actor)
        {
            actor.AbilityTo<BrowseTheDesktop>().Click(Field);
            return "unexpected";
        }
    }
    private sealed class FakeDriver : IWindowsAutomationDriver
    {
        public List<string> Calls { get; } = [];
        private readonly FakeElement element;
        public bool SelectionSucceeds { get; init; } = true;
        public FakeDriver() => element = new FakeElement(() => { Calls.Add("select-item"); return SelectionSucceeds; });
        public int? ProcessId => 123;
        public void Launch(string executablePath, string arguments = "") => Calls.Add($"launch:{executablePath}:{arguments}");
        public IAutomationElement Find(By locator) { Calls.Add($"find:{locator}"); return element; }
        public IReadOnlyList<IAutomationElement> FindAll(By locator) => [element];
        public void Click(By locator) => Calls.Add($"click:{locator}");
        public void Type(By locator, string text) { Calls.Add($"type:{locator}:{text}"); element.Type(text); }
        public void SwitchToWindow(By locator) => Calls.Add($"window:{locator}");
        public void Select(By locator, string text) => Calls.Add($"select:{locator}:{text}");
        public void Close() => Calls.Add("close");
        public void Dispose() => Calls.Add("dispose");
    }
    private sealed class FakeElement(Func<bool> selectItem) : IAutomationElement
    {
        public string AutomationId => "quantity";
        public string Name => "Quantity";
        public string Text { get; private set; } = "";
        public bool IsEnabled => true;
        public bool IsSelected { get; private set; }
        public void SelectItem() => IsSelected = selectItem();
        public void Click() => throw new AssertionException("Mutable element escaped observation boundary.");
        public void Type(string text) => Text = text;
        public void Select(string text) => Text = text;
        public IAutomationElement Find(By locator) => this;
        public IReadOnlyList<IAutomationElement> FindAll(By locator) => [this];
    }
}
