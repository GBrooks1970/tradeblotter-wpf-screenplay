<!--
  AUDIENCE: Engineers, AI agents, and project leads maintaining work-in-progress tracking.
  PURPOSE:  Single source of truth for outstanding parity gaps, risks, migration plans,
            and sprint planning for TradeBlotter.WPF.
  LOCATION: tradeblotter-wpf-screenplay/docs/backlog.md
  TEMPLATE: templates/backlog.template.md
-->

# TradeBlotter.WPF — Backlog

**Version:** 1 — Initial backlog derived from Candidate 1 specification and Phase 0 feasibility probe  
**Last Updated:** 2026-09-18  
**Based on:** [`project-specs/potential-project-outlines/tradeblotter-wpf-screenplay.md`](../../project-specs/potential-project-outlines/tradeblotter-wpf-screenplay.md) and [`portfolio-docs/PORTFOLIO_TRADEBLOTTER_PROBE_2026-09-18.md`](../../portfolio-docs/PORTFOLIO_TRADEBLOTTER_PROBE_2026-09-18.md)

This backlog tracks the architecture, test automation harness, Screenplay pattern implementation, multi-driver abstraction, and CI delivery for the TradeBlotter.WPF desktop automation project.

**Priority Scoring System:**
- **Score = Security Impact (0–10) + Breakage Probability (0–10) + Maintenance Burden (0–10)**
- **HIGH (20–30):** Critical — immediate action required
- **MEDIUM (10–19):** Important — schedule within current sprint cycle
- **LOW (0–9):** Desirable — schedule when capacity allows

---

## Outstanding Risks

Risks are ordered by priority score (highest first). Each risk includes a priority score breakdown.

**Status vocabulary:** `READY TO START`, `IN PROGRESS` and `BLOCKED` are stages of open work; `COMPLETE` is a delivery; `RECORDED` is the accepted-risk terminal state.

### HIGH Priority (Score: 20–30)

#### Risk #1: Driver Abstraction Layer & Core Driver Interfaces — Score: 26

**Priority Score:** Security Impact (7) + Breakage Probability (10) + Maintenance Burden (9) = **26 points**  
**Impact:** Essential architectural interface decoupling test logic from specific automation vendor libraries.  
**Effort:** 6–8 hrs  
**Status:** READY TO START  
**Affected Stacks:** Core Automation Framework (`TradeBlotter.Framework`)  

**Problem:**
Without a robust driver abstraction layer, test step definitions directly couple to FlaUI, Ranorex, or WinAppDriver API calls. This creates tight vendor lock-in, prevents interchangeable CI execution, and invalidates the core portfolio goal of empirical multi-driver benchmarking.

**Impact Analysis:**
- **Security (7/10):** Uncontrolled binary execution and process handling without centralized driver lifecycle management.
- **Breakage (10/10):** Direct framework coupling makes replacing or substituting automation drivers require rewriting entire step definitions.
- **Maintenance (9/10):** Triplication of test code across multiple frameworks if driver abstraction is omitted.

**Refactor Strategy:**
1. Define `IWindowsAutomationDriver` in `TradeBlotter.Framework.Abstractions`.
2. Define element abstractions `IAutomationElement` and locator strategies `By.AutomationId`, `By.Name`, `By.XPath`.
3. Implement `FlaUiDriverAdapter` wrapping `FlaUI.Core.Application` and `UIA3Automation`.
4. Implement driver lifecycle manager handling cold boot, timeout configurations, and clean teardown.

**Success Criteria:**
- [ ] `IWindowsAutomationDriver` interface authored with element finding, clicking, typing, and window switching.
- [ ] `FlaUiDriverAdapter` implements `IWindowsAutomationDriver` cleanly with zero FlaUI leak in interface signatures.
- [ ] Unit tests verify adapter methods against mock/in-memory elements.

---

#### Risk #2: Screenplay Pattern Architecture Core & Trading Actors — Score: 24

**Priority Score:** Security Impact (6) + Breakage Probability (9) + Maintenance Burden (9) = **24 points**  
**Impact:** Core domain task abstraction providing narrative readability and maintainable UI interactions.  
**Effort:** 6–8 hrs  
**Status:** READY TO START  
**Affected Stacks:** Screenplay Domain Layer (`TradeBlotter.Screenplay`)  

**Problem:**
Traditional Page Object Models in desktop automation suffer from monolithic classes and fragile locator coupling. Screenplay pattern solves this by separating Actors, Abilities, Tasks, and Questions, but requires a clean C# implementation.

**Impact Analysis:**
- **Security (6/10):** Actor role permissions (e.g. `TommyTrader` vs `AdamAuditor`) must be strictly segregated in automation abilities.
- **Breakage (9/10):** Fragile UI synchronization without Screenplay interaction waiting strategies causes intermittent flakiness.
- **Maintenance (9/10):** Tasks and Questions provide reusable, composable business building blocks that reduce code duplication by >60%.

**Refactor Strategy:**
1. Implement Screenplay core: `Actor`, `IAbility`, `ITask`, `IQuestion<T>`.
2. Implement `BrowseTheDesktop` ability wrapping `IWindowsAutomationDriver`.
3. Define Screenplay actors: `TommyTrader` (trading actions) and `AdamAuditor` (read-only audit/inspection).
4. Implement foundational tasks: `LaunchApplication`, `PlaceOrder`, `CancelOrder`.
5. Implement foundational questions: `BlotterOrdersCount`, `OrderStatusOf`, `TickerPrice`.

**Success Criteria:**
- [ ] Screenplay core interfaces and classes authored under `TradeBlotter.Screenplay`.
- [ ] `BrowseTheDesktop` ability enables actors to execute driver operations fluently.
- [ ] `TommyTrader` and `AdamAuditor` actors instantiated and verified with appropriate abilities.

---

#### Risk #3: Order Entry & Blotter Verification BDD Feature Suite — Score: 23

**Priority Score:** Security Impact (6) + Breakage Probability (9) + Maintenance Burden (8) = **23 points**  
**Impact:** Living documentation and executable specification proving end-to-end trading workflows.  
**Effort:** 8–10 hrs  
**Status:** READY TO START  
**Affected Stacks:** Test Specifications (`TradeBlotter.Specs`)  

**Problem:**
Lack of executable BDD feature specifications leaves business rules for order placement (LIMIT/MARKET orders, quantities, pricing validation) and blotter state transitions unverified.

**Impact Analysis:**
- **Security (6/10):** Financial trading requires verifiable audit trails; automation must assert exact order states and limits.
- **Breakage (9/10):** High risk of regression when WPF UI layout or two-way bindings change without BDD regression gates.
- **Maintenance (8/10):** Reqnroll Gherkin provides stakeholder-readable specifications that serve as executable documentation.

**Refactor Strategy:**
1. Scaffold `TradeBlotter.Specs` referencing Reqnroll, NUnit, and project dependencies.
2. Author `OrderPlacement.feature` defining LIMIT and MARKET order submission scenarios.
3. Author step definitions connecting Gherkin steps to `TommyTrader` Screenplay tasks and questions.
4. Execute test suite locally against `TradeBlotter.Sut` and assert 100% scenario pass rate.

**Success Criteria:**
- [ ] `OrderPlacement.feature` authored with minimum 3 complete scenarios (LIMIT order, MARKET order, validation error).
- [ ] Reqnroll step definitions execute cleanly without raw UI driver calls.
- [ ] All scenarios pass under NUnit test runner.

---

#### Risk #4: Zero-Cost Headless GitHub Actions Windows CI Pipeline — Score: 22

**Priority Score:** Security Impact (5) + Breakage Probability (9) + Maintenance Burden (8) = **22 points**  
**Impact:** Continuous integration proving automated verification runs on standard cloud runners without paid licenses.  
**Effort:** 4–6 hrs  
**Status:** READY TO START  
**Affected Stacks:** CI/CD Infrastructure (`.github/workflows/ci.yml`)  

**Problem:**
Desktop automation frequently fails in cloud CI environments due to missing interactive desktop sessions, display resolution constraints, or dependency on commercial licenses.

**Impact Analysis:**
- **Security (5/10):** CI workflows must use principle of least privilege (`contents: read`).
- **Breakage (9/10):** Desktop automation must run reliably in CI without headless UI rendering crashes or timeout deadlocks.
- **Maintenance (8/10):** Standardized GitHub Actions workflow ensures zero ongoing license costs.

**Refactor Strategy:**
1. Author `.github/workflows/ci.yml` targeting `windows-latest`.
2. Configure workflow steps: .NET 9 SDK setup, dependency restore, SUT build, and test execution.
3. Configure screen resolution / virtual desktop settings if needed for FlaUI interaction.
4. Publish test results as NUnit / TRX test artifacts.

**Success Criteria:**
- [ ] `.github/workflows/ci.yml` authored and committed.
- [ ] Workflow executes `dotnet test` with FlaUI.UIA3 driver successfully on `windows-latest`.
- [ ] Minimum 3 consecutive green CI runs recorded.

---

### MEDIUM Priority (Score: 10–19)

#### Risk #5: Order Cancellation & State Transition Verification — Score: 18

**Priority Score:** Security Impact (5) + Breakage Probability (7) + Maintenance Burden (6) = **18 points**  
**Impact:** Verification of trading blotter state lifecycle and order modification rules.  
**Effort:** 4–6 hrs  
**Status:** READY TO START  
**Affected Stacks:** Test Specifications (`TradeBlotter.Specs`)  

**Problem:**
Orders in `PENDING` state must be cancellable by traders, transitioning to `CANCELLED`, while `FILLED` orders must reject cancellation.

**Impact Analysis:**
- **Security (5/10):** Unauthorized order cancellations risk trade desk reconciliation discrepancies.
- **Breakage (7/10):** State machine logic regressions in WPF ViewModel.
- **Maintenance (6/10):** Clean Gherkin scenarios for state transitions.

**Refactor Strategy:**
1. Author `OrderCancellation.feature` covering pending cancellation and filled order rejection.
2. Implement `CancelOrder` task and `SelectedOrder` interaction.
3. Assert grid cell status updates in real time.

**Success Criteria:**
- [ ] `OrderCancellation.feature` authored with pending and invalid cancellation scenarios.
- [ ] Step definitions implemented and green in NUnit.

---

#### Risk #6: Real-time Asynchronous Pricing Ticker Feed Verification — Score: 16

**Priority Score:** Security Impact (4) + Breakage Probability (6) + Maintenance Burden (6) = **16 points**  
**Impact:** Verification of asynchronous background updates without UI thread blocking.  
**Effort:** 4–5 hrs  
**Status:** READY TO START  
**Affected Stacks:** Screenplay & Specs  

**Problem:**
Financial blotters feature real-time live pricing feeds. Automation must verify that asynchronous ticks update UI components without causing COM proxy deadlocks or race conditions.

**Impact Analysis:**
- **Security (4/10):** Stale price display risks mispriced order execution.
- **Breakage (6/10):** Background DispatcherTimer updates can cause intermittent test failures if assertions don't poll reactive state.
- **Maintenance (6/10):** Screenplay `Eventually` waiting pattern handles async updates cleanly.

**Refactor Strategy:**
1. Author `PriceTicker.feature` verifying live ticker feed changes.
2. Implement `TickerPrice` Question with polling tolerance.
3. Assert that ticker values fluctuate deterministically.

**Success Criteria:**
- [ ] Gherkin scenarios verify dynamic ticker updates without arbitrary `Thread.Sleep`.
- [ ] Verified green locally and in CI.

---

#### Risk #7: Microsoft WinAppDriver Adapter & Appium Protocol Parity — Score: 14

**Priority Score:** Security Impact (3) + Breakage Probability (6) + Maintenance Burden (5) = **14 points**  
**Impact:** Secondary driver implementation demonstrating W3C WebDriver / Appium protocol compliance.  
**Effort:** 6–8 hrs  
**Status:** READY TO START  
**Affected Stacks:** Driver Abstraction (`TradeBlotter.Framework.WinAppDriver`)  

**Problem:**
To prove true driver interchangeability, a second driver implementing `IWindowsAutomationDriver` must execute the exact same BDD feature suite without test modifications.

**Impact Analysis:**
- **Security (3/10):** Local HTTP REST server communication (`127.0.0.1:4723`).
- **Breakage (6/10):** Protocol differences between Appium W3C and native UIA3.
- **Maintenance (5/10):** Reusable driver interface minimizes maintenance across drivers.

**Refactor Strategy:**
1. Implement `WinAppDriverAdapter` implementing `IWindowsAutomationDriver` using `Appium.WebDriver`.
2. Configure automated WinAppDriver process lifecycle management.
3. Execute feature suite using `--driver=winappdriver`.

**Success Criteria:**
- [ ] `WinAppDriverAdapter` passes full BDD test suite with 0 scenario changes.

---

#### Risk #8: Ranorex Studio Core API Adapter & Tri-Framework Benchmark Harness — Score: 12

**Priority Score:** Security Impact (3) + Breakage Probability (5) + Maintenance Burden (4) = **12 points**  
**Impact:** Commercial enterprise driver adapter and empirical benchmarking suite.  
**Effort:** 6–8 hrs  
**Status:** READY TO START  
**Affected Stacks:** Driver Abstraction & Benchmarking  

**Problem:**
Commercial test automation capabilities must be demonstrated alongside open source, and an empirical performance comparison is required.

**Impact Analysis:**
- **Security (3/10):** Commercial license handling and API wrappers.
- **Breakage (5/10):** Ranorex Core API dependencies and mock mode fallbacks.
- **Maintenance (4/10):** Decoupled architecture isolates commercial dependencies.

**Refactor Strategy:**
1. Author `RanorexDriverAdapter` implementing `IWindowsAutomationDriver` with mock licensing fallback.
2. Author benchmark runner comparing FlaUI, WinAppDriver, and Ranorex across cold boot, element location, and memory consumption.
3. Publish benchmark results report.

**Success Criteria:**
- [ ] Tri-framework benchmark runner executes and produces comparative CSV/Markdown reports.

---

### Resolved Risks

#### Phase 0 SUT Scaffolding & Feasibility Probe Validation ✅ Resolved 2026-09-18

**Resolution:** Scaffolded dark-mode WPF trading blotter (`TradeBlotter.Sut`) and executed 7-step feasibility probe (`TradeBlotter.Probe`) using FlaUI.UIA3 on .NET 9. Validated cold boot latency (2,576 ms), UIA3 tree discovery (5/5 controls), DataGrid row access (4/4 seed rows), modal dialog order submission (`ORD-2026-0905`), memory telemetry (162.97 MB), and clean process teardown (2,118 ms, 0 orphans). Formal verdict: **FEASIBLE — GO**.  
**See:** [`portfolio-docs/PORTFOLIO_TRADEBLOTTER_PROBE_2026-09-18.md`](../../portfolio-docs/PORTFOLIO_TRADEBLOTTER_PROBE_2026-09-18.md) and commit `5ef2eb5` (`tradeblotter-wpf-screenplay` repo).

---

## Risk Summary

| Priority | Count | Total Effort | Status Distribution |
|---|---|---|---|
| HIGH (20–30) | 4 | 24–32 hrs | 4 READY TO START |
| MEDIUM (10–19) | 4 | 20–27 hrs | 4 READY TO START |
| LOW (0–9) | 0 | 0 hrs | — |
| **Total Outstanding** | **8** | **44–59 hrs** | 8 READY TO START |
| Resolved | 1 | ~8 hrs completed | 1 COMPLETE |
