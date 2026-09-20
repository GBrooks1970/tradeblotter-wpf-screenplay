using NUnit.Framework;
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Framework.WinAppDriver;

namespace TradeBlotter.Framework.Tests;

[TestFixture]
public sealed class WinAppDriverTests
{
    [Test, Combinatorial]
    public void RejectsNonLocalEndpoints(
        [Values("https://127.0.0.1:4723", "http://example.com:4723", "http://user:secret@127.0.0.1:4723", "not-a-url")] string endpoint,
        [Values(true, false)] bool proxy)
    {
        Assert.Throws<ArgumentException>(() =>
        {
            using var driver = proxy ? new WinAppDriverAdapter(serverUrl: endpoint) : new WinAppDriverAdapter(wadUrl: endpoint);
        });
    }

    [Test]
    public void CloseAndDisposeAreIdempotentWithoutASession()
    {
        var driver = new WinAppDriverAdapter();
        driver.Close();
        driver.Close();
        driver.Dispose();
        driver.Dispose();
        Assert.That(driver.ProcessId, Is.Null);
    }

    [Test]
    public void FindBeforeLaunchFailsWithoutConnecting()
    {
        using var driver = new WinAppDriverAdapter();
        Assert.Throws<InvalidOperationException>(() => driver.Find(By.AutomationId("missing")));
    }

    [Test]
    public void MissingExecutableDoesNotCreateAProcess()
    {
        using var driver = new WinAppDriverAdapter();
        Assert.Throws<FileNotFoundException>(() => driver.Launch(Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".exe")));
        Assert.That(driver.ProcessId, Is.Null);
    }

    [Test]
    public void ExistingApplicationIsNeverAdoptedOrClosed()
    {
        using var current = System.Diagnostics.Process.GetCurrentProcess();
        var driver = new WinAppDriverAdapter();
        Assert.Throws<InvalidOperationException>(() => driver.Launch(Environment.ProcessPath!));
        driver.Dispose();
        Assert.Multiple(() =>
        {
            Assert.That(driver.ProcessId, Is.Null);
            Assert.That(current.HasExited, Is.False);
        });
    }

    [Test]
    public void DisposedAdapterRejectsLaunchBeforeTouchingTheFileSystem()
    {
        var driver = new WinAppDriverAdapter();
        driver.Dispose();
        Assert.Throws<ObjectDisposedException>(() => driver.Launch("unused.exe"));
    }
}
