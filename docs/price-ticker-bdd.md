# Deterministic ticker verification (TB-06)

[PriceTicker.feature](../tests/TradeBlotter.Specs/Features/PriceTicker.feature)
contains two real-WPF examples. Each waits for a complete repeating cycle:

| Symbol | First observed target | Next target | Return target |
|---|---:|---:|---:|
| EUR/USD | 1.0844 | 1.0840 | 1.0844 |
| GBP/USD | 1.2712 | 1.2718 | 1.2712 |

The initial XAML values differ from these targets. Waiting for the first target
allows startup at any timer phase; the second and third targets prove a change
and return rather than just the presence of static text. Each scenario uses a
fresh SUT, compares the complete blotter with its pre-observation snapshot and
then verifies that the trader can still select a row. Tests do not change the
SUT's timer interval, manually invoke ticks or inject a replacement feed.

## Questions and polling

[`TickerPrice`](../src/TradeBlotter.Screenplay/Questions/TickerPrice.cs) reads
`TxtPriceTicker` through the read-only desktop ability, identifies exactly one
quote for the requested symbol and parses its decimal with invariant culture.
Missing, duplicate or malformed quotes raise `FormatException`; they are not
treated as transient prices. The display grammar includes a four-decimal price
and an up/down arrow. The scenarios verify prices, not the economic meaning of
the decorative arrows or every possible locale.

[`Eventually<T>`](../src/TradeBlotter.Screenplay/Questions/Eventually.cs) wraps an
ordinary question and a predicate. It observes immediately and returns the first
matching answer. Otherwise it measures elapsed time with `Stopwatch` and delays
only until the next poll or remaining deadline using `Task.Delay`. Observations
remain on the caller's thread, preserving the sequential desktop-session model.
There is no arbitrary `Thread.Sleep`, and no work is dispatched onto the SUT's
UI thread. Query and predicate exceptions propagate without retrying.

Each ticker target has a five-second polling budget and a 100 ms poll interval.
Timeout errors include the expectation and last observation. The budget bounds
polling; it cannot interrupt a blocking native UIA/COM call. The existing CI
test-host inactivity timeout supplies a separate outer limit. This is successful
execution evidence, not proof that all possible COM deadlocks are impossible.

Unit tests cover symbol choice, invariant parsing under a comma-decimal culture,
malformed/missing/duplicate quotes, eventual and immediate matches, same-thread
observation, timeout diagnostics, exception propagation and invalid configuration.
They use short condition-driven polling and do not assert brittle wall-clock
performance thresholds.

## Reproduce

From the repository root on a Windows desktop:

```powershell
dotnet test tests/TradeBlotter.Specs/TradeBlotter.Specs.csproj --configuration Release --filter FullyQualifiedName~DeterministicPriceTicker --logger "console;verbosity=normal"
```

The complete PowerShell 7 gate, `./scripts/verify-ci.ps1`, now requires **61 tests**:
16 framework, 34 Screenplay, 10 real-WPF BDD examples and 1 native smoke test.
The BDD total comprises four placement, four cancellation and two ticker examples.
Run desktop tests sequentially with no manually launched SUT.

## Evidence boundaries

The application alternates two hard-coded strings on a one-second WPF
`DispatcherTimer`; this is a simulated feed on the UI dispatcher, not an external
market-data service or background worker. A busy dispatcher can delay a tick.
These tests observe the known values within tolerance and check that the UI
remains usable; they do not measure scheduling precision, market correctness,
throughput or long-duration stability. Order MARKET pricing remains the separate
fixed value `1.0842` and is not driven by this ticker.

See the [SUT guide](../src/TradeBlotter.Sut/README.md),
[project contract](project-contract.md) and [backlog](backlog.md) for the wider
behaviour, gates and remaining adapter work.

## Captured acceptance evidence

On 2026-09-20, all 61 tests passed locally: framework 16/16 in 2.0073 s,
Screenplay 34/34 in 1.4979 s, BDD 10/10 in 47.2825 s and native smoke 1/1
in 10.4384 s (reported test-run durations). All seven Release builds had zero
warnings/errors, four TRX reports were verified and no SUT process remained.

At source commit `e1ef4ea2b49228295c375a60a0873e6298a5a92a`,
[push run 35480734919](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35480734919)
passed in 2m20s and
[PR run 35480747091](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35480747091)
passed in 2m29s (job durations). Both gates passed 61 tests; the PR run checked
GitHub merge preview `714630fb4a8afaf9e59932cb90452bd638a5cbf8`.
