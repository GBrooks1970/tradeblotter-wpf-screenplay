using TradeBlotter.Framework.Abstractions;

namespace TradeBlotter.Screenplay.Abilities;

/// <summary>Inspection view; never returns the underlying mutable driver element.</summary>
public interface IDesktopObservation
{
    string AutomationId { get; }
    string Name { get; }
    string Text { get; }
    bool IsEnabled { get; }
    IDesktopObservation Find(By locator);
    IReadOnlyList<IDesktopObservation> FindAll(By locator);
}

/// <summary>The caller owns driver disposal. Sharing a driver also shares its window scope.</summary>
public sealed class BrowseTheDesktop : IAbility
{
    private readonly IWindowsAutomationDriver driver;
    public bool CanInteract { get; }

    private BrowseTheDesktop(IWindowsAutomationDriver driver, bool canInteract)
    {
        ArgumentNullException.ThrowIfNull(driver);
        this.driver = driver;
        CanInteract = canInteract;
    }

    public static BrowseTheDesktop Using(IWindowsAutomationDriver driver) => new(driver, true);
    public static BrowseTheDesktop Inspecting(IWindowsAutomationDriver driver) => new(driver, false);
    public IDesktopObservation Find(By locator) => new Observation(driver.Find(locator));
    public IReadOnlyList<IDesktopObservation> FindAll(By locator) => driver.FindAll(locator).Select(e => (IDesktopObservation)new Observation(e)).ToArray();

    public void Launch(string executablePath, string arguments = "") { RequireInteraction(); driver.Launch(executablePath, arguments); }
    public void Click(By locator) { RequireInteraction(); driver.Click(locator); }
    public void Type(By locator, string text) { RequireInteraction(); driver.Type(locator, text); }
    public void SwitchToWindow(By locator) { RequireInteraction(); driver.SwitchToWindow(locator); }
    public void Close() { RequireInteraction(); driver.Close(); }

    private void RequireInteraction()
    {
        if (!CanInteract) throw new InvalidOperationException("This desktop ability permits inspection only.");
    }

    private sealed class Observation(IAutomationElement element) : IDesktopObservation
    {
        public string AutomationId => element.AutomationId;
        public string Name => element.Name;
        public string Text => element.Text;
        public bool IsEnabled => element.IsEnabled;
        public IDesktopObservation Find(By locator) => new Observation(element.Find(locator));
        public IReadOnlyList<IDesktopObservation> FindAll(By locator) => element.FindAll(locator).Select(e => (IDesktopObservation)new Observation(e)).ToArray();
    }
}
