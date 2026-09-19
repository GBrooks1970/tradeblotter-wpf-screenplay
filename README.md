# TradeBlotter WPF

A C#/.NET 9 Windows desktop automation project built around a WPF trading blotter.
The repository contains the subject under test (SUT), a FlaUI.UIA3 feasibility
probe, a vendor-neutral driver contract with a tested FlaUI adapter, and a
Screenplay core with trading and read-only auditing actors, and four Reqnroll
order-placement scenarios. CI remains planned work.

## Prerequisites and build

Use Windows with the .NET 9 SDK. Run these commands from the repository root:

```powershell
dotnet build src/TradeBlotter.Sut/TradeBlotter.Sut.csproj --configuration Release
dotnet build probes/TradeBlotter.Probe/TradeBlotter.Probe.csproj --configuration Release
```

Builds restore NuGet dependencies as required. The probe pins FlaUI.UIA3 5.0.0.
These compile the SUT and historical probe. The [project contract](docs/project-contract.md)
also requires framework and Screenplay builds and in-memory unit tests.

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

TB-01 delivers driver abstraction, TB-02 the Screenplay core and TB-03 order-placement
BDD. The remaining backlog covers Windows CI, cancellation and ticker verification,
then WinAppDriver and Ranorex adapters with comparative benchmarking. The five
open items remain governed by the backlog. Some historical records link to sibling
portfolio documents; those links require the full portfolio workspace.

The project is registered in the portfolio and has a public landing entry.
See [driver usage and lifecycle](docs/driver-abstraction.md) for the TB-01 API.

See [Screenplay core and actor roles](docs/screenplay-core.md) for TB-02 usage.

See [order-placement BDD](docs/order-placement-bdd.md) for the four real-desktop scenarios and run command.
