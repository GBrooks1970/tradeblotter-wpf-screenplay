# Desktop benchmark observations

See metadata.json for environment, revision, options and executable SHA-256.

Startup includes adapter launch and first verified grid lookup; each repetition uses a fresh SUT process. Lookup includes AutomationId/enabled validation. Memory is a single SUT working-set snapshot, excluding the runner and automation servers. Warmups are retained in CSV but excluded below. These observations are not a controlled cross-machine ranking.

| Engine | Mode | Status | Samples | Median startup ms | Median lookup mean ms | Median SUT MiB | Detail |
|---|---|---|---:|---:|---:|---:|---|
| flaui | Native | PASS | 3 | 730.4654 | 151.3618 | 108.8828 |  |
| winappdriver | Native | PASS | 3 | 3896.3263 | 1059.5089 | 110.6719 |  |
| ranorex-mock | Mock | MOCK | 0 | N/A | N/A | N/A | Simulated routing only; no WPF automation or performance measurement. |
| ranorex | Native | UNAVAILABLE | 0 | N/A | N/A | N/A | Native Ranorex adapter, SDK and licensed execution are deferred to TB-08B. No mock substitution. |

MOCK verifies harness plumbing only. UNAVAILABLE is missing implementation or prerequisites. Neither is native Ranorex evidence. See samples.csv for every attempt, including warmup and cleanup failures.
