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
executes four suites sequentially: 16 framework tests, 15 Screenplay tests,
4 real-WPF Reqnroll scenarios and 1 explicit native smoke test. Each suite writes
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

Hosted execution must be proven by actual successful runs before TB-04 closes.
Local success alone is insufficient. No custom resolution, RDP access or credential
configuration is applied unless runner evidence establishes a need.

GitHub provisions a fresh VM for each hosted job; see
[GitHub-hosted runners](https://docs.github.com/en/actions/how-tos/manage-runners/github-hosted-runners/use-github-hosted-runners).
See [artifact retention](https://docs.github.com/en/actions/tutorials/store-and-share-data)
for the uploaded-evidence mechanism. Action refs were resolved from the official
`actions/checkout`, `actions/setup-dotnet` and `actions/upload-artifact` repositories.

## Local reliability correction

The first full local run stalled while discovering the order-entry window. Temporary tracing isolated the stall to modal lookup; the adapter scanned all desktop descendants. Window discovery now starts with top-level windows belonging to the SUT and searches only their descendants for owned dialogs. AutomationId/name lookup avoids unrelated application providers; desktop-root XPath expressions retain their existing semantics. Diagnostic tracing was removed after investigation.

## Acceptance evidence

Pending: record three consecutive successful hosted executions, identifying each
run ID, attempt and tested revision. Reruns must be labelled as attempts, not
misrepresented as distinct run IDs. No remote success is claimed by this scaffold.
