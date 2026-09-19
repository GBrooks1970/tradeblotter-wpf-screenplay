# Project Contract — TradeBlotter WPF

## Gates

Run from this repository root on Windows with the .NET 9 SDK:

```powershell
dotnet build src/TradeBlotter.Sut/TradeBlotter.Sut.csproj --configuration Release
dotnet build probes/TradeBlotter.Probe/TradeBlotter.Probe.csproj --configuration Release
dotnet build src/TradeBlotter.Framework/TradeBlotter.Framework.csproj --configuration Release
dotnet test tests/TradeBlotter.Framework.Tests/TradeBlotter.Framework.Tests.csproj --configuration Release --filter FullyQualifiedName~DriverTests
```

All four commands must succeed. The framework tests use in-memory desktop elements
and include a reflection check that exported signatures contain no FlaUI types.
The application and probe builds do not run desktop automation. CI is still planned.

## Native smoke test

The optional native integration test requires a Windows desktop and a built SUT:

```powershell
$env:TRADEBLOTTER_SUT = Join-Path (Get-Location) 'src/TradeBlotter.Sut/bin/Release/net9.0-windows/TradeBlotter.Sut.exe'
dotnet test tests/TradeBlotter.Framework.Tests/TradeBlotter.Framework.Tests.csproj --configuration Release --filter FullyQualifiedName~DesktopSmokeTests
```

This explicitly selected test launches and closes its own application. Run it when
changing the native adapter; it is excluded from the default unit gate. Subsequent
items must exercise their own acceptance criteria and extend these gates as needed.

For documentation changes, run `git diff --check`, verify newly added relative
links, and scan live documents for unresolved template placeholders. Reusable
`*.template.md` files intentionally retain their placeholders.

The Phase 0 UI probe is historical feasibility evidence. Running desktop automation
requires a suitable Windows desktop session; unattended/cloud operation remains
unverified until the CI backlog item supplies evidence. Do not start the SUT or run
the interactive probe as part of documentation-only validation.

## Working norms

- This folder is an independent Git repository. Stage only intended project paths;
  portfolio worklists and handovers belong to the portfolio support repository.
- Deliver changes through a branch and PR. The user controls each merge.
- Use en-GB spelling. Treat [the backlog](backlog.md) as the authority for status and
  priority; preserve completed implementation logs as immutable history.
- Keep future step definitions dependent on Screenplay tasks/questions and
  `IWindowsAutomationDriver`, without FlaUI, Ranorex or Appium types in public
  abstraction signatures.
- Use alliterative PascalCase actor names, including `TommyTrader` and `AdamAuditor`.
- Default automation is intended to use FlaUI.UIA3 without paid licences or an
  external driver daemon. Record empirical evidence before claiming CI support.
- Report actual test counts, commands and results. Never describe planned adapters,
  BDD scenarios or benchmarks as delivered functionality.
