# Walkthrough — TB-01 Driver Abstraction and TB-02 Screenplay Core

## Executive Summary

This batch established the desktop automation foundation for TradeBlotter: vendor-neutral driver contracts and a managed FlaUI adapter, followed by Screenplay core contracts and trading/auditing actors. TB-01 and TB-02 are merged, with 16 framework tests, 14 Screenplay tests and a separately executed native smoke test recorded below. The backlog is now version 3 with six open items; order-specific BDD workflows and CI remain future work.

Scope: project changes from `e0cd264` through `70285bb7c3debde1d33e8207a33ac7e2c2583baa`. TB-01 implementation `67ce217` merged through [PR #3](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/pull/3) as `743fb30`; TB-02 implementation `4f90572` merged through [PR #4](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/pull/4) as `70285bb`. Onboarding is prior context, not part of this implementation diff.

## 1. Changes Implemented

Before TB-01, native automation existed only in the feasibility probe. Consumers now use `IWindowsAutomationDriver`; the adapter owns the process and UIA3 lifetime, and all public signatures remain vendor-neutral. Before TB-02, there was no actor/task/question layer. The new layer routes actions through an ability and supplies inspection wrappers without exposing mutable driver elements.

The following inventory covers all 24 changed or added files in the batch; there were no deletions.

| File | Purpose and rationale |
|---|---|
| [CHANGELOG.md](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/CHANGELOG.md) | Records TB-01 and TB-02 additions without claiming later BDD or CI delivery. |
| [README.md](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/README.md) | Updates delivered capabilities, remaining scope and links to contracts and usage. |
| [docs/backlog.md](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/docs/backlog.md) | Closes Risks #1 and #2 against their acceptance criteria; six items remain open. |
| [docs/driver-abstraction.md](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/docs/driver-abstraction.md) | Documents locator, lifecycle, timing and native-adapter limitations. |
| [docs/project-contract.md](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/docs/project-contract.md) | Extends the required gates to four builds and two unit suites; native smoke stays explicit. |
| [docs/screenplay-core.md](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/docs/screenplay-core.md) | Documents actor roles, immutable abilities, fixture ownership and deferred domain tasks. |
| [src/TradeBlotter.Framework/Abstractions/By.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Framework/Abstractions/By.cs) | Defines validated automation ID, name and XPath locators without vendor types. |
| [src/TradeBlotter.Framework/Abstractions/IWindowsAutomationDriver.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Framework/Abstractions/IWindowsAutomationDriver.cs) | Defines driver lifecycle, interaction and element-search contracts. |
| [src/TradeBlotter.Framework/DriverOptions.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Framework/DriverOptions.cs) | Validates launch, element, polling and shutdown durations. |
| [src/TradeBlotter.Framework/FlaUI/FlaUiDriverAdapter.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Framework/FlaUI/FlaUiDriverAdapter.cs) | Coordinates polling, current window, session ownership and stale-session rejection. |
| [src/TradeBlotter.Framework/FlaUI/IDesktopSession.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Framework/FlaUI/IDesktopSession.cs) | Internal native seam enables deterministic in-memory adapter tests. |
| [src/TradeBlotter.Framework/FlaUI/NativeSession.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Framework/FlaUI/NativeSession.cs) | Owns FlaUI application/UIA3 resources; maps lookup and interaction, and tears down the owned process. |
| [src/TradeBlotter.Framework/TradeBlotter.Framework.csproj](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Framework/TradeBlotter.Framework.csproj) | Adds the .NET 9 Windows framework with FlaUI.UIA3 5.0.0 and test-only internal access. |
| [src/TradeBlotter.Screenplay/Abilities/BrowseTheDesktop.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Screenplay/Abilities/BrowseTheDesktop.cs) | Guards mutation and wraps all returned elements in inspection-only observations. |
| [src/TradeBlotter.Screenplay/Actor.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Screenplay/Actor.cs) | Provides core contracts, immutable ability registration and sequential fail-fast task dispatch. |
| [src/TradeBlotter.Screenplay/Questions/DesktopQuestions.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Screenplay/Questions/DesktopQuestions.cs) | Provides typed text and count questions through the desktop ability. |
| [src/TradeBlotter.Screenplay/Tasks/DesktopTasks.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Screenplay/Tasks/DesktopTasks.cs) | Provides launch, click, text entry, window switch and close tasks. |
| [src/TradeBlotter.Screenplay/TradeBlotter.Screenplay.csproj](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Screenplay/TradeBlotter.Screenplay.csproj) | References framework contracts; compiled assembly has no direct vendor reference. |
| [src/TradeBlotter.Screenplay/TradingActors.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/src/TradeBlotter.Screenplay/TradingActors.cs) | Creates TommyTrader with interaction and AdamAuditor with inspection-only abilities. |
| [tests/TradeBlotter.Framework.Tests/DesktopSmokeTests.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/tests/TradeBlotter.Framework.Tests/DesktopSmokeTests.cs) | Explicit real-WPF test verifies native lookup, modal scope, typing and process exit. |
| [tests/TradeBlotter.Framework.Tests/DriverTests.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/tests/TradeBlotter.Framework.Tests/DriverTests.cs) | Sixteen in-memory cases cover adapter routing, polling, failures, lifecycle and vendor-signature isolation. |
| [tests/TradeBlotter.Framework.Tests/TradeBlotter.Framework.Tests.csproj](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/tests/TradeBlotter.Framework.Tests/TradeBlotter.Framework.Tests.csproj) | Pins NUnit 4.2.2, adapter 5.0.0 and test SDK 17.14.1 for framework tests. |
| [tests/TradeBlotter.Screenplay.Tests/ScreenplayTests.cs](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/tests/TradeBlotter.Screenplay.Tests/ScreenplayTests.cs) | Fourteen cases cover actor roles, call order, immutable abilities, mutation denial and nested wrappers. |
| [tests/TradeBlotter.Screenplay.Tests/TradeBlotter.Screenplay.Tests.csproj](D:/_CLAUDE_COWORK/PROJ001/claude-outputs/test-automation-portfolio/tradeblotter-wpf-screenplay/tests/TradeBlotter.Screenplay.Tests/TradeBlotter.Screenplay.Tests.csproj) | Adds the Screenplay NUnit suite with the same pinned test toolchain. |

The role distinction is enforced at two levels:

```csharp
public static Actor TommyTrader(IWindowsAutomationDriver driver) =>
    new("TommyTrader", true, BrowseTheDesktop.Using(driver));
public static Actor AdamAuditor(IWindowsAutomationDriver driver) =>
    new("AdamAuditor", false, BrowseTheDesktop.Inspecting(driver));
```

`AdamAuditor.AttemptsTo(...)` rejects task dispatch. Calling a task directly or attempting mutation from a question still encounters the inspection ability's guard. Returned observations, including nested results, cannot be cast to `IAutomationElement`. This is an automation API safeguard, not SUT authentication or a sandbox against arbitrary code holding its own driver reference.

## 2. Verification & Test Evidence

Evidence below is captured output from the implementation turns in this conversation, not a new test execution during walkthrough authorship. TB-01 tests ran against `67ce217`; TB-02 validation ran against the code committed as `4f90572`. Durations are exactly the reported build time, total test-run time or suite duration, as labelled.

All commands run from the project root with `--configuration Release`.

| Command | Quality Gate / Suite | Status | Metrics (Passed/Total) | Duration |
|---|---|---|---|---|
| `dotnet build src/TradeBlotter.Sut/TradeBlotter.Sut.csproj --configuration Release` | TB-02 SUT build | PASS | 0 warnings, 0 errors | 1.62 s |
| `dotnet build probes/TradeBlotter.Probe/TradeBlotter.Probe.csproj --configuration Release` | TB-02 probe build | PASS | 0 warnings, 0 errors | 1.74 s |
| `dotnet build src/TradeBlotter.Framework/TradeBlotter.Framework.csproj --configuration Release` | TB-02 framework build | PASS | 0 warnings, 0 errors | 1.41 s |
| `dotnet build src/TradeBlotter.Screenplay/TradeBlotter.Screenplay.csproj --configuration Release` | TB-02 Screenplay build | PASS | 0 warnings, 0 errors | 1.78 s |
| `dotnet test tests/TradeBlotter.Framework.Tests/TradeBlotter.Framework.Tests.csproj --configuration Release --filter FullyQualifiedName~DriverTests` | TB-02 framework regression | PASS | 16/16; 0 failed, 0 skipped | 209 ms suite duration |
| `dotnet test tests/TradeBlotter.Screenplay.Tests/TradeBlotter.Screenplay.Tests.csproj --configuration Release --logger "console;verbosity=normal"` | TB-02 Screenplay unit tests | PASS | 14/14 | 0.9214 s total run |
| `dotnet test tests/TradeBlotter.Framework.Tests/TradeBlotter.Framework.Tests.csproj --configuration Release --no-build --filter FullyQualifiedName~DesktopSmokeTests --logger "console;verbosity=normal"` | TB-01 native integration | PASS | 1/1 | 10.9284 s total run |
| PowerShell relative-link checks on changed documentation | TB-02 documentation | PASS | 16/16 links | Not captured |
| `git diff --cached --check` | Both implementation commits | PASS | Exit 0 | Not captured |

The native smoke command used `TRADEBLOTTER_SUT` set to the absolute path of `src/TradeBlotter.Sut/bin/Release/net9.0-windows/TradeBlotter.Sut.exe`. It verified ID/name/XPath lookup, modal window switching, text replacement and process exit. A subsequent process query returned no `TradeBlotter.Sut` processes. It was not rerun for TB-02 because the native adapter was unchanged.

Selected console captures:

```text
Build succeeded.
    0 Warning(s)
    0 Error(s)
Time Elapsed 00:00:01.78

Passed!  - Failed:     0, Passed:    16, Skipped:     0, Total:    16, Duration: 209 ms - TradeBlotter.Framework.Tests.dll (net9.0)

Test Run Successful.
Total tests: 14
     Passed: 14
 Total time: 0.9214 Seconds

Passed LaunchLocateTypeSwitchAndCloseRealApplication [9 s]
Test Run Successful.
Total tests: 1
     Passed: 1
 Total time: 10.9284 Seconds
```

No dependency vulnerability audit was run in this batch; no zero-vulnerability claim is made. A restore initially reported unavailable NUnit3TestAdapter 4.6.1; the dependency was corrected to the actually resolved 5.0.0 before clean final validation. The tests validate native adapter operation separately from deterministic Screenplay behaviour; they do not establish full order workflows.

## 3. Operational State & Invariants

- Verified on 2026-09-19: project PR #4 is `MERGED`, merge SHA `70285bb7c3debde1d33e8207a33ac7e2c2583baa`, merged at `2026-09-19T08:03:09Z`.
- After fetch and fast-forward, project `main` was clean at that SHA. `git rev-list --left-right --count HEAD...origin/main` returned `0 0`.
- Walkthrough authoring uses `codex/walkthrough-tb01-tb02` branched from that verified main. This new document is a pending documentation change; the pre-authoring clean status is not a claim that the walkthrough has been committed or merged.
- Live `gh run list --limit 5 --json databaseId,status,conclusion` returned `[]`; PR #4 `statusCheckRollup` is empty. Remote CI status is **NO RUNS**, not green. TB-04 owns CI delivery; local desktop success does not prove headless/cloud support.
- Backlog v3 has Risks #1 and #2 COMPLETE, plus the earlier resolved Phase 0 record. Six outstanding items remain: two HIGH and four MEDIUM.
- The portfolio-root worklist contains an uncommitted TB-02 completion update for `4f90572`. TB-01's root record was merged through root PR #211. The pending root update is separate from this project document and has not been published by this walkthrough task.
- The latest immutable handover predates onboarding and these implementations. The current backlog and project contract govern current state; no historical handover was rewritten.
- Native driver instances are single-threaded. Polling deadlines cannot interrupt a blocking COM call. Shared actors share driver scope and lifetime; the fixture owns disposal.
- No Docker operation was required. No Docker storage was moved; the workspace convention remains `E:\_DockerData`.
- Project changes stay in the independent project repository; root control records stay outside it. Documentation uses en-GB. This is Codex, not Antigravity, so the conditional native IDE walkthrough destination does not apply.

## 4. Recommended Next Actions

1. **Recommended: reconcile and publish the pending root TB-02 worklist record**, adding the verified PR #4 merge evidence before the next iteration.
2. Run one worklist iteration for **TB-03**, implementing order-placement BDD scenarios and order-specific Screenplay tasks/questions. Keep later cancellation and ticker work in TB-05/TB-06.
3. Deliver **TB-04** after its prerequisites, recording actual Windows CI results rather than extrapolating from local UI success.
4. At the session boundary, write a new versioned handover covering the merged driver/Screenplay architecture and six remaining items.
