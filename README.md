# TradeBlotter WPF

A C#/.NET 9 Windows desktop automation project built around a WPF trading blotter.
The repository currently contains the subject under test (SUT) and a FlaUI.UIA3
feasibility probe. The reusable driver abstraction, Screenplay actors, Reqnroll
scenarios and CI pipeline are planned work, not delivered capabilities.

## Prerequisites and build

Use Windows with the .NET 9 SDK. Run these commands from the repository root:

```powershell
dotnet build src/TradeBlotter.Sut/TradeBlotter.Sut.csproj --configuration Release
dotnet build probes/TradeBlotter.Probe/TradeBlotter.Probe.csproj --configuration Release
```

Builds restore NuGet dependencies as required. The probe pins FlaUI.UIA3 5.0.0.
These are the initial mechanical gates; they do not execute UI automation.

To inspect the trading blotter interactively:

```powershell
dotnet run --project src/TradeBlotter.Sut/TradeBlotter.Sut.csproj --configuration Release --no-build
```

The probe source is in `probes/TradeBlotter.Probe/Program.cs`. Its current SUT path
discovery includes a portfolio-root-relative Debug build fallback; it is not yet
a portable repository-root smoke-test command. The recorded Phase 0 run does not
establish unattended or cloud CI support.

## Project records

- [Project contract](docs/project-contract.md): validation and working norms.
- [Backlog](docs/backlog.md): authoritative priorities and completion status.
- [Phase 0 implementation record](docs/implementation-logs/2026-09-18_phase-0-feasibility-probe-and-backlog-scaffolding.md): historical probe evidence.
- [Implementation-log template](docs/templates/implementation-log.template.md): reusable record for completed work.
- [Changelog](CHANGELOG.md): notable changes.

The backlog begins with driver abstraction (TB-01), followed by Screenplay core,
order-placement BDD scenarios, Windows CI, cancellation and ticker verification,
then WinAppDriver and Ranorex adapters with comparative benchmarking. The eight
open items remain governed by the backlog. Some historical records link to sibling
portfolio documents; those links require the full portfolio workspace.

Portfolio onboarding is staged: this scaffold must merge before the canonical
registry row is added, followed by the public landing entry.
