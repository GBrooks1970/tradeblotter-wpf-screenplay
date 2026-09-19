# Driver abstraction (TB-01)

`TradeBlotter.Framework.Abstractions` defines `IWindowsAutomationDriver`,
`IAutomationElement` and `By`. Public signatures expose only project and .NET types.
`TradeBlotter.Framework.FlaUI.FlaUiDriverAdapter` owns the FlaUI `Application` and
`UIA3Automation` behind an internal native boundary. Unit tests inject in-memory
sessions through an internal constructor; consumers use the public constructor.

```csharp
using TradeBlotter.Framework.Abstractions;
using TradeBlotter.Framework.FlaUI;

using IWindowsAutomationDriver driver = new FlaUiDriverAdapter();
driver.Launch(@"C:\apps\TradeBlotter.Sut.exe");
driver.Click(By.AutomationId("BtnNewOrder"));
driver.SwitchToWindow(By.AutomationId("DlgOrderEntry"));
driver.Type(By.AutomationId("TxtQuantity"), "250000");
string quantity = driver.Find(By.AutomationId("TxtQuantity")).Text;
```

## Search and interaction semantics

- `Find` polls until a descendant exists, then returns the first match. A missing
  element raises `TimeoutException` containing the locator. Native errors propagate
  immediately; unsupported patterns are not disguised as missing elements.
- `FindAll` returns an immediate snapshot, including an empty list when absent.
  Searches on an element are scoped to that element's descendants.
- Automation ID and name match exactly. XPath uses FlaUI's UI Automation tree
  dialect, relative to the search root; it is not browser DOM XPath. For window
  switching, XPath is evaluated from the desktop and results are restricted to
  windows belonging to the owned process. Portability to future adapters is not
  yet proven.
- Switching windows changes the search scope and focuses the window. A failed
  switch leaves the prior scope intact. Other applications cannot be selected.
- `Type` replaces an editable Value-pattern control's value. `Text` reads that
  value when supported, otherwise the accessible name. `Click` invokes the Invoke
  pattern where supported, falling back to a physical click. Disabled controls
  reject typing/clicking. Unsupported or read-only value patterns report errors.

`Select` chooses a combo-box item by exact text through the native adapter;
disabled controls are rejected. TB-03 adds this operation for order entry.

## Lifecycle and timing

Launch creates a new process; attaching to existing applications is not supported.
Launching twice without closing fails. `Close` requests graceful process exit,
then terminates only the owned process tree if shutdown exceeds the configured
timeout. Native application and automation resources are disposed even if cleanup
fails. `Close` and `Dispose` are idempotent. A closed driver can launch again;
a disposed driver cannot. Elements from a previous session cannot be reused.

`DriverOptions` supplies positive launch, element, polling and shutdown intervals.
Defaults are 10 seconds, 5 seconds, 100 milliseconds and 5 seconds respectively.
Polling delays are bounded by the remaining deadline. Deadlines bound polling,
not an individual blocking Windows COM call. Use one driver on one thread; neither
concurrent use nor recovery from a hung UI Automation provider is implemented.

The internal session seam tests adapter routing and lifecycle without Windows UI
interaction. The explicit native smoke test exercises the real SUT separately;
its original local run did not prove unattended GitHub Actions support. See
[Windows CI](windows-ci.md) for subsequent hosted execution evidence. TB-04 scopes
AutomationId/name window discovery to SUT top-level windows and their descendants,
avoiding desktop-wide traversal of unrelated providers. Desktop-root XPath window
expressions retain the original semantics.
See [the validation contract](project-contract.md) for exact commands.
