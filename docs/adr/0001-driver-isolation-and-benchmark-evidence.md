# ADR 0001: Driver isolation and benchmark evidence

Date recorded: 2026-09-28. Status: accepted, retrospective record of the implementation
and user-approved TB-08 split delivered on 2026-09-19 to 2026-09-21.

## Context

The project demonstrates interchangeable Windows automation through shared BDD
and Screenplay code. FlaUI and WinAppDriver are verified; no licensed Ranorex SDK
was available during implementation. Mock execution cannot establish vendor parity.

## Decision

Keep vendor types behind `IWindowsAutomationDriver` and `IAutomationElement`.
Choose the native adapter at the BDD composition boundary. FlaUI stays the default;
the optional WinAppDriver adapter uses Appium's protocol proxy and owned loopback
servers. Preserve the ten shared scenarios and explicit process ownership.

Separate TB-08A (harness and explicit mock) from TB-08B (licensed native Ranorex).
The `ranorex` route reports UNAVAILABLE until implemented; only `ranorex-mock`
selects the in-memory fixture. Mock/unavailable rows have no performance numbers.
Capture raw samples and environment/SUT provenance; summarise only successful native
measured repetitions. Fresh-process startup is not a cold-machine benchmark.

## Alternatives and consequences

- Rejected vendor-specific scenario copies: they would weaken interchangeability.
- Rejected silent mock fallback and simulated performance comparisons: they would
  misrepresent native evidence.
- Deferred a Ranorex runtime bridge or shared targeting change until the supplied
  SDK's compatibility is verified. Neither approach has been implemented.
- Each native adapter and server has explicit ownership and cleanup. Additional
  driver support must retain this boundary and the unchanged BDD acceptance gate.

## Evidence

Implementation commits `67ce217`, `4f90572`, `154486b`, `9acd5b2`; merged acceptance
`45e7933`. See [protocol parity](../winappdriver-parity.md),
[benchmark definitions and acceptance](../benchmark-harness.md) and
[backlog Risk #8](../backlog.md).
