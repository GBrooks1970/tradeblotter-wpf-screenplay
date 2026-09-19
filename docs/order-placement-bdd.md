# Order-placement BDD (TB-03)

The [feature](../tests/TradeBlotter.Specs/Features/OrderPlacement.feature) contains
two successful scenarios and a two-row validation outline: LIMIT, MARKET, zero
quantity and invalid limit price. Reqnroll 3.3.4 generates NUnit tests into `obj/`.

Run on Windows with .NET 9 and an available interactive desktop:

```powershell
dotnet test tests/TradeBlotter.Specs/TradeBlotter.Specs.csproj --configuration Release --logger "console;verbosity=normal"
```

The project reference builds the SUT and a build target copies its runtime files
beside the test output in `sut/`. No environment variable or working-directory
assumption is required. The test assembly limits workers to one and each scenario
owns a new driver and application; teardown disposes the driver and checks process
exit. Tests interact with the desktop and must not run concurrently with other
desktop automation. Cloud/headless support remains TB-04.

## Data flow and assertions

The scenario hook is the sole driver composition boundary and selects FlaUI.
Step definitions call `TommyTrader` tasks and `AdamAuditor` questions; they contain
no native UI calls. `PlaceOrder` opens the form, selects symbol/type through the
vendor-neutral `Select` operation, enters raw values and submits. Invalid input
reaches the SUT unchanged. Expected outcome tasks select either the main window
or the validation dialog; the task does not guess success from a closed form.

`BlotterOrders` reads seven UI cells into typed snapshots: ID, symbol, side,
quantity, price, status and filled quantity. Successful scenarios assert five rows,
the exact new ID `ORD-2026-0905`, requested symbol/quantity/price, BUY/PENDING/zero
filled state, and equality of all four original rows. Validation scenarios assert
the exact message, dismiss the dialog and form, and compare the complete original
snapshot. No arbitrary sleeps are added; driver lookup uses its existing bounded
polling.

The native `Select` implementation selects a combo-box item by text and rejects
disabled controls. The inspection ability also rejects selection, preserving the
auditor's read-only boundary. In-memory tests cover selection routing and mutation
denial; the BDD scenarios exercise real combo boxes.

## Evidence and boundaries

The first local execution passed 4/4 scenarios in 31.5906 seconds total test-run
time. This is local desktop evidence, not CI evidence. No SUT business rules were
changed: both order types remain PENDING, and MARKET uses the fixed mock price
`1.0842` for any selected symbol. There is no market execution or live price oracle.

The current small fixture contains four seed rows and one added row. The question
reads realised rows and expects seven cells in the existing column order; it does
not claim coverage of virtualised rows outside the viewport. Numeric display is
parsed using invariant comma-grouping/dot-decimal formatting, matching the current
WPF display. Locale expansion, cancellation and ticker behaviour are outside this
item. Use the [project contract](project-contract.md) for the complete gates.
