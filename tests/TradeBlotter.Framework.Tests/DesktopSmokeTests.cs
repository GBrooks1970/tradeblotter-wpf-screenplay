using System.Diagnostics;
using NUnit.Framework;
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Framework.FlaUI;

namespace TradeBlotter.Framework.Tests;

public class DesktopSmokeTests
{
    [Test, Explicit("Requires a Windows desktop and TRADEBLOTTER_SUT pointing to the built executable.")]
    public void LaunchLocateTypeSwitchAndCloseRealApplication()
    {
        var path = Environment.GetEnvironmentVariable("TRADEBLOTTER_SUT");
        Assert.That(path, Is.Not.Null.And.Not.Empty);
        using var driver = new FlaUiDriverAdapter();
        driver.Launch(path!);
        using var process = Process.GetProcessById(driver.ProcessId!.Value);
        Assert.That(driver.Find(By.AutomationId("BtnNewOrder")).IsEnabled, Is.True);
        driver.Click(By.AutomationId("BtnNewOrder"));
        driver.SwitchToWindow(By.AutomationId("DlgOrderEntry"));
        driver.Type(By.AutomationId("TxtQuantity"), "250000");
        Assert.That(driver.Find(By.XPath(".//*[@AutomationId='TxtQuantity']")).Text, Is.EqualTo("250000"));
        Assert.That(driver.FindAll(By.Name("Submit New Order")), Has.Count.EqualTo(1));
        driver.Close();
        Assert.That(process.HasExited, Is.True);
        Assert.That(driver.ProcessId, Is.Null);
    }
}
