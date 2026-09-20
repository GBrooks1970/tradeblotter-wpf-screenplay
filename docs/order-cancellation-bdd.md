# Order cancellation and state verification (TB-05)

[OrderCancellation.feature](../tests/TradeBlotter.Specs/Features/OrderCancellation.feature)
executes four examples against a fresh real WPF process each time:

| Seeded state | Expected state after Cancel Selected | Filled quantity preserved |
|---|---|---:|
| PENDING | CANCELLED | 0 |
| PARTIAL | CANCELLED | 200000 |
| FILLED | FILLED | 250000 |
| CANCELLED | CANCELLED | 0 |

Each example checks the initial target state and fill, cancels by order ID, then
compares the complete ordered blotter snapshot with the original snapshot modified
only at the target status. This verifies row count, order, unrelated rows and all
other displayed target values, including filled quantity. Every example repeats
the cancellation and verifies that the entire blotter remains unchanged.

## Selection is a verified precondition

The [CancelOrder task](../src/TradeBlotter.Screenplay/Tasks/CancelOrder.cs) first
invokes the `SelectedOrder` interaction. It selects the realised row by its
automation ID through the vendor-neutral `SelectItem` operation and reads
`IsSelected` back before allowing the toolbar click. A failed selection throws;
it must not cancel whichever row happened to be selected previously. The FILLED
example therefore cannot succeed merely because no row was selected.

The FlaUI adapter uses the UIA SelectionItem pattern rather than a coordinate
click or an editable grid cell. `IsSelected` returns false for elements without
that pattern; `SelectItem` requires it and rejects disabled elements. This
operation differs from `Select(text)`, which selects a combo-box option.
The desktop ability guards row selection against auditor mutation and exposes
only its read-only state through observations. No native UI types enter the
Screenplay layer or step definitions.

`AdamAuditor` reads `BlotterOrders`; NUnit polls snapshot equality for up to five
seconds at 100 ms intervals to observe the UI update. There is no fixed sleep.
The shared scenario hooks own application startup and teardown. The new examples
reuse the existing fresh-desktop Given step without changing placement scenarios.

## Reproduce and interpret

Run from the repository root on a Windows desktop:

```powershell
dotnet test tests/TradeBlotter.Specs/TradeBlotter.Specs.csproj --configuration Release --filter FullyQualifiedName~OrderCancellation --logger "console;verbosity=normal"
```

The complete gate is `./scripts/verify-ci.ps1` in PowerShell 7. The TB-05 baseline expected
16 framework tests, 20 Screenplay tests, 8 desktop BDD examples and 1 native smoke
test: **45 tests**. See the project contract for current totals. The Screenplay tests include confirmed-selection sequencing,
refusal to cancel after unconfirmed selection, invalid IDs and auditor denial.
Framework tests cover selection-state forwarding through the public element
contract. Desktop tests remain sequential.

The SUT already implemented these cancellation rules; this item changes no SUT
business logic. FILLED rejection means the order remains unchanged, not that an
error dialog appears. PARTIAL cancellation preserves existing fills. Coverage
concerns four realised seeded rows and the toolbar path, not off-screen
virtualisation, direct cell edits, application authorisation or a production
execution engine. TB-06 supplies [ticker verification](price-ticker-bdd.md).

See the [project contract](project-contract.md) for gates and the
[SUT learning guide](../src/TradeBlotter.Sut/README.md) for application behaviour.

## Captured local verification

On 2026-09-20, `./scripts/verify-ci.ps1` completed all seven Release builds with
zero warnings/errors. NUnit reported 16/16 framework tests (1.5189 s), 20/20
Screenplay tests (1.5110 s), 8/8 BDD examples (35.4049 s) and 1/1 native smoke
(9.9248 s). These are reported total test-run times, not benchmark measurements.
The eight BDD examples include all four new cancellation rows and all four
placement regressions. The gate verified four TRX reports and no remaining SUT
process. Hosted checks are reported separately by the delivery PR.
