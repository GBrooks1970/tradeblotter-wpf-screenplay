# TB-01 to TB-08A delivery and parking — 2026-09-28

## Session Summary

This retrospective log records the implementation batch delivered on 2026-09-19
to 2026-09-21, following the separate Phase 0 log. The project now has driver
abstraction, Screenplay actors, ten desktop BDD scenarios, hosted CI, verified
WinAppDriver parity and a benchmark harness. The user parked the project on
2026-09-21 pending a licensed Ranorex SDK; TB-08B remains incomplete. This wrap-up
adds documentation only and does not claim a new native test execution.

---

## Objectives

1. ✅ Deliver TB-01–TB-06: vendor-neutral driver, Screenplay, placement,
   cancellation, ticker verification and Windows CI.
2. ✅ Deliver TB-07: run the ten unchanged BDD scenarios through WinAppDriver.
3. ✅ Deliver approved TB-08A: native observations, explicit mock plumbing,
   unavailable records and reproducible CSV/Markdown evidence.
4. ⏸️ Defer TB-08B: native Ranorex adapter and genuine three-driver comparison.
5. ✅ Record parked status, durable lessons and a versioned session handover.

---

## Test Results

Historical execution evidence, not tests rerun while writing this log:

| Stack | Suite | Before | After | Status |
|---|---|---|---|---|
| Framework | Unit/lifecycle | TB-01: 16/16 | 29/29 | PASS |
| Screenplay | Actor/tasks/questions | TB-02: 14/14 | 34/34 | PASS |
| FlaUI | BDD | TB-03: 4/4 | 10/10 | PASS |
| FlaUI | Native smoke | 1/1 | 1/1 | PASS |
| Harness | Reporting/cleanup/no fallback | Not present | 12/12 | PASS |
| WinAppDriver | Unchanged BDD | Not present | 10/10 | PASS |
| Native benchmark | Measured repetitions | Not present | 3 FlaUI + 3 WinAppDriver | PASS; not a test-count contribution |
| Ranorex | Native parity/benchmark | Not available | Not executed | BLOCKED |

After-counts trace to [main run 35581840286](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35581840286)
at `0f09e20`: 86 default tests plus 10 WinAppDriver scenarios. The archived
[CSV](../benchmarks/2026-09-21_tb08a-main-35581840286/samples.csv),
[report](../benchmarks/2026-09-21_tb08a-main-35581840286/report.md) and
[metadata](../benchmarks/2026-09-21_tb08a-main-35581840286/metadata.json)
preserve benchmark evidence beyond short-lived CI artifacts. Earlier counts are
recorded in the [foundation walkthrough](../walkthroughs/2026-09-19_tb01-tb02-driver-and-screenplay-foundation.md)
and project worklist. The latest main run checked during wrap-up,
[35600309082](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35600309082),
is successful at `45e7933`. No new runtime tests were needed for these documents.

---

## Changes Implemented

### Shared driver, Screenplay and scenarios

- `src/TradeBlotter.Framework/`: vendor-neutral locators, element/driver contracts,
  bounded waits and owned FlaUI lifecycle (`67ce217`, merge `743fb30`).
- `src/TradeBlotter.Screenplay/`: actor capabilities and guarded auditing
  (`4f90572`, merge `70285bb`); order placement (`36246c1`, merge `05fa195`),
  cancellation (`066d4ca`, merge `de928d3`) and ticker observation (`e1ef4ea`,
  merge `6eab0c5`).
- `tests/TradeBlotter.Specs/`: four placement, four cancellation and two ticker
  examples. Driver choice is confined to the fixture; shared scenarios remain unchanged.
- `.github/workflows/ci.yml` and `scripts/verify-ci.ps1`: hosted Windows rendering,
  exact-count TRX gates, diagnostics and cleanup checks. TB-04 merged as `f01b91b`.

### WinAppDriver interoperability

`src/TradeBlotter.Framework.WinAppDriver/`, `scripts/run-bdd.ps1`,
`scripts/setup-winappdriver-ci.ps1` and `tools/winappdriver/` provide the optional
adapter and pinned Appium proxy. Diagnostic runs exposed empty relative-XPath
results, console occlusion, Selenium Name-to-CSS conversion and null Selection
attributes. The final implementation anchors XPath by RuntimeId, lets WinAppDriver
launch the application, translates native names through XPath and verifies keyboard
combo selection in the XML tree. Native acceptance passed at `154486b`; PR #11
merged as `0ed423c`. Failed experiments are diagnostic history, not acceptance.

### Benchmark evidence and deferred Ranorex

`tools/TradeBlotter.Benchmarks/`, `tests/TradeBlotter.Benchmarks.Tests/` and
`scripts/run-benchmarks.ps1` measure fresh-process startup, verified grid lookup and
SUT working-set snapshots. Errors invalidate summaries; unavailable/native/mock
states are distinct and existing evidence directories cannot be overwritten.
Implementation `9acd5b2` merged as `0f09e20`; evidence `ff320c2` merged as `45e7933`.
The premature-merge concern was checked: both branch runs and merged-main CI passed.

---

## Technical Decisions

Structural decisions are recorded in [ADR 0001](../adr/0001-driver-isolation-and-benchmark-evidence.md)
and [DR-001](../../DECISION_REGISTER.md), retrospectively rather than backdated.

| Decision | Rationale | Alternatives rejected |
|---|---|---|
| Keep the project parked with TB-08B open | User direction on 2026-09-21; licensed SDK unavailable | Marking native Ranorex complete from mock evidence |
| Use disposable hosted Windows for native WinAppDriver acceptance | Local Developer Mode was unavailable during the batch | Enabling personal-machine settings without a separate instruction |
| Retain raw evidence and narrowly defined metrics | Repeatable interpretation without overstating performance | Treating separate machines or mock timings as a controlled three-driver ranking |

---

## Documentation Updates

Delivered documentation comprises `README.md`, `CHANGELOG.md`, `docs/backlog.md`,
`docs/project-contract.md`, `docs/driver-abstraction.md`, `docs/screenplay-core.md`,
`docs/order-placement-bdd.md`, `docs/windows-ci.md`, `docs/order-cancellation-bdd.md`,
`docs/price-ticker-bdd.md`, `docs/winappdriver-parity.md`, `docs/benchmark-harness.md`
and `src/TradeBlotter.Sut/README.md` (pedagogical guide, merge `4fdafce`).
The existing foundation and TB-06 walkthroughs and the three archived benchmark
files remain immutable. Portfolio worklist records are maintained separately.

This wrap-up adds this log, ADR 0001 and the decision register; updates the backlog
to v12 with the dated parking decision; and adds handover v2 Markdown/HTML under
the portfolio root's `session-notes/`, with a regenerated ignored manifest.

---

## Lessons Learned

- Run native desktop suites serially. Own and stop only processes started by the run.
- Native UIA XML and WebDriver attribute endpoints can expose different information;
  inspect captured trees and logs before changing shared assertions.
- A successful click response does not establish combo selection. Verify the selected
  identity and preserve the end-to-end order assertions.
- Modern Appium clients need the Windows proxy for legacy WinAppDriver protocol.
- Distinguish unavailable prerequisites from execution errors and explicit simulation.
- Keep the nested project Git boundary separate from root worklists and handovers.

---

## Recommendations / Next Steps

- [ ] **MEDIUM / TB-08B:** remain parked until a licensed SDK and Windows execution
  environment are supplied; see [Risk #8](../backlog.md).
- [ ] **MEDIUM / TB-08B:** verify SDK/runtime compatibility with the .NET 9 contract,
  then implement the native adapter, pass unchanged BDD and collect genuine metrics.
- [ ] Publish this documentation through the project's and portfolio root's separate
  review flows; no merge or publication is claimed by this log.

---

*Session logged: 2026-09-28. Author: Codex.*
