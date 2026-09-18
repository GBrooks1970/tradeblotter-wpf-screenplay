# Project Contract — TradeBlotter WPF

## Gates

Run from this repository root on Windows with the .NET 9 SDK:

```powershell
dotnet build src/TradeBlotter.Sut/TradeBlotter.Sut.csproj --configuration Release
dotnet build probes/TradeBlotter.Probe/TradeBlotter.Probe.csproj --configuration Release
```

Both commands must succeed. These initial gates compile the existing SUT and probe;
they do not imply that a test suite or CI pipeline exists.

For TB-01, additionally build the new automation framework and execute its adapter
unit tests against mock/in-memory elements. Verify that public driver contracts
contain no vendor-specific types. Add the exact build and test commands here when
those projects are introduced; the current two builds alone cannot close TB-01.
Subsequent implementation items must run the tests that exercise their acceptance
criteria and keep these gates current as executable test projects are delivered.

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
