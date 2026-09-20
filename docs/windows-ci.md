# Windows desktop CI (TB-04)

The [workflow](../.github/workflows/ci.yml) runs on `windows-latest` for pushes to
`main` and `codex/**`, pull requests to `main`, and manual dispatch. It grants only
`contents: read`, disables persisted checkout credentials, pins all third-party
actions to commit SHAs, and limits each job to 15 minutes. Superseded runs of the
same event/ref are cancelled. No scheduled workflow is introduced.

Run the same gate locally in PowerShell 7 on Windows with .NET 9:

```powershell
./scripts/verify-ci.ps1
```

The [script](../scripts/verify-ci.ps1) restores and builds all seven projects, then
executes four suites sequentially: 16 framework tests, 34 Screenplay tests,
10 real-WPF Reqnroll examples and 1 explicit native smoke test. Each suite writes
a distinct TRX file. Counters must report the expected executed and passed totals;
missing reports, discovery returning zero tests, skips and failures fail the gate.
Each test execution has a 90-second inactivity timeout and retains the blame sequence on a hang. Update the expected counts deliberately when adding tests. Native desktop suites
must not run concurrently with other UI automation.

Every execution uses a unique results directory beneath ignored `TestResults/ci/`
so older TRX reports cannot satisfy the gate. Artifacts include the four reports
and desktop diagnostics (OS, image version, session and screen bounds), are uploaded
even after a test failure, and expire after seven days. A final check rejects
remaining SUT processes. Native scenario teardown already closes its owned process.

## What the runner evidence means

FlaUI uses Windows UI Automation against a real WPF window in the runner's desktop
session. This is unattended desktop testing, not browser-style headless rendering
or a separately provisioned virtual desktop. No paid driver licence, self-hosted
machine or external driver daemon is required. The repository is public and the
workflow selects the standard hosted runner, not a paid larger runner; GitHub's
applicable billing terms remain authoritative.

The recorded hosted executions below establish TB-04 acceptance. No custom
resolution, RDP access or credential configuration was needed.

GitHub provisions a fresh VM for each hosted job; see
[GitHub-hosted runners](https://docs.github.com/en/actions/how-tos/manage-runners/github-hosted-runners/use-github-hosted-runners).
See [artifact retention](https://docs.github.com/en/actions/tutorials/store-and-share-data)
for the uploaded-evidence mechanism. Action refs were resolved from the official
`actions/checkout`, `actions/setup-dotnet` and `actions/upload-artifact` repositories.

## Local reliability correction

The first full local run stalled while discovering the order-entry window. Temporary tracing isolated the stall to modal lookup; the adapter scanned all desktop descendants. Window discovery now starts with top-level windows belonging to the SUT and searches only their descendants for owned dialogs. AutomationId/name lookup avoids unrelated application providers; desktop-root XPath expressions retain their existing semantics. Diagnostic tracing was removed after investigation.

The current 61-test gate includes TB-05 cancellation and TB-06 ticker coverage. The evidence below
is the historical 36-test TB-04 acceptance baseline.

## Acceptance evidence

Four consecutive successful hosted runs were captured on 2026-09-19. All are
distinct run IDs, attempt 1; no failed or cancelled hosted runs preceded them.
Each built all seven projects with zero warnings/errors and passed exactly
16 framework + 15 Screenplay + 4 BDD + 1 native tests, with no skips or remaining
SUT process. This exceeds the three-green-run acceptance criterion.

| Run | Event | Checked-out revision | Job seconds | BDD TRX seconds | Native TRX seconds |
|---|---|---|---:|---:|---:|
| [35464147718](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35464147718) | push | `d8ce041` | 126 | 11.9530 | 7.3529 |
| [35464160765](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35464160765) | pull_request | `71cc15e` | 135 | 9.3154 | 6.8066 |
| [35464309574](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35464309574) | push | `accd534` | 125 | 12.0170 | 7.3722 |
| [35464311504](https://github.com/GBrooks1970/tradeblotter-wpf-screenplay/actions/runs/35464311504) | pull_request | `dae47d7` | 120 | 10.6241 | 7.1146 |

Pull-request runs test GitHub's temporary merge revisions; the corresponding
source heads are `d8ce041` and `accd534`. The second head changes documentation
only. Full SHAs, job timestamps, desktop metadata, suite counters/durations and
SHA-256 hashes of downloaded TRX reports are preserved in the
[captured evidence](ci-evidence/2026-09-19-tb04.json). Durations use job timestamps
and TRX start/finish intervals respectively; these are not performance benchmarks.

All four runners reported OS build `10.0.26100.0`, image `20260907.229.1`, session 2,
`UserInteractive: true` and a 1024x768 primary screen. Artifacts and job logs were
downloaded and inspected before recording this summary. The logs end with:

```text
PASS: 36 tests, four TRX reports, no remaining SUT process.
```

The local full gate also passed 36/36 after the lookup correction (BDD test-run
time 1.7291 minutes; native 18.2569 seconds). An earlier zero-match BDD filter was
rejected by the count guard and corrected before publication. Earlier local
desktop stalls were diagnosed and their owned SUT processes stopped; none are
represented as passes. Hosted acceptance currently concerns this PR branch and
its merge previews; a default-branch run will follow the user's merge.
