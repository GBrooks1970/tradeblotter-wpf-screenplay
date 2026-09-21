using TradeBlotter.Framework.Abstractions;

namespace TradeBlotter.Benchmarks;

/// <summary>Explicit in-memory benchmark fixture, not a Ranorex API implementation.</summary>
public sealed class MockBenchmarkDriver : IWindowsAutomationDriver
{
    private bool launched;
    private bool disposed;
    public int? ProcessId => null;
    public void Launch(string executablePath, string arguments = "")
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (launched) throw new InvalidOperationException("Session already open.");
        launched = true;
    }
    public IAutomationElement Find(By locator)
    {
        ObjectDisposedException.ThrowIf(disposed, this);
        if (!launched) throw new InvalidOperationException("Launch first.");
        if (locator != By.AutomationId("GridBlotterOrders")) throw new InvalidOperationException("Mock fixture contains only the blotter grid.");
        return new Grid();
    }
    public IReadOnlyList<IAutomationElement> FindAll(By locator) => [Find(locator)];
    public void Close() => launched = false;
    public void Dispose() { Close(); disposed = true; }
    public void Click(By locator) => throw Unsupported();
    public void Type(By locator, string text) => throw Unsupported();
    public void Select(By locator, string text) => throw Unsupported();
    public void SwitchToWindow(By locator) => throw Unsupported();
    private static NotSupportedException Unsupported() => new("Benchmark mock supports launch, grid lookup and cleanup only; it cannot run BDD scenarios.");
    private sealed class Grid : IAutomationElement
    {
        public string AutomationId => "GridBlotterOrders";
        public string Name => "Mock blotter";
        public string Text => "";
        public bool IsEnabled => true;
        public bool IsSelected => false;
        public void SelectItem() => throw Unsupported();
        public void Click() => throw Unsupported();
        public void Type(string text) => throw Unsupported();
        public void Select(string text) => throw Unsupported();
        public IAutomationElement Find(By locator) => throw Unsupported();
        public IReadOnlyList<IAutomationElement> FindAll(By locator) => throw Unsupported();
    }
}
