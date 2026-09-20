# Changelog

Notable changes to TradeBlotter WPF are recorded here. No numbered release has
been published in this record.

## Unreleased

### Added

- TB-07: optional Appium-backed WinAppDriver adapter, owned server lifecycle,
  driver selection and a separate unchanged-BDD parity CI job (acceptance pending).
  Thirteen endpoint/lifecycle cases extend the default gate to 74 tests.

- TB-06: symbol-specific ticker price questions, bounded Eventually observation
  polling, two repeating-cycle desktop scenarios and an expanded 61-test gate.

- TB-05: four cancellation/state BDD examples, verified row selection, guarded
  CancelOrder/SelectedOrder tasks and expanded 45-test CI gate.

- TB-04: Windows-hosted CI workflow, a reproducible 36-test gate with hang
  timeouts, exact-count checks, TRX artifacts and desktop diagnostics.
  Modal lookup now scopes AutomationId/name discovery to SUT windows to avoid
  desktop-wide UI Automation traversal.

- TB-03: four Reqnroll/NUnit desktop order-placement scenarios, PlaceOrder tasks,
  typed BlotterOrders observations and guarded combo-box selection.

- TB-02: Screenplay actor, ability, task and question contracts; desktop ability
  with guarded inspection; TommyTrader and AdamAuditor factories; core tasks,
  questions and 14 in-memory tests.

- TB-01: vendor-neutral driver/element contracts, locators, managed FlaUI.UIA3
  lifecycle, in-memory unit tests and an explicit native desktop smoke test.

- WPF trading blotter and FlaUI.UIA3 feasibility probe, delivered in commit
  `5ef2eb5` on 2026-09-18.
- Initial backlog and immutable Phase 0 implementation record, merged through PR #1.
- Onboarding README, project contract with explicit build gates, and reusable
  implementation-log template.

Additional driver adapters remain planned in
[the backlog](docs/backlog.md).
