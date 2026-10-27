# Local verification

Run `bash scripts/verify.sh` from the repository root. The script accepts the
same tool prerequisites as described in the README. It runs the stages in this
order: **Source checks**, **Restore and build**, **Format**, **Domain**,
**Application**, **Infrastructure**, **ExternalProviders**, **Acceptance**,
**OpenAPI**, **Docker**. ExternalProviders is specific to Invoices.

`scripts/verify.sh` and the CI jobs call the same portable entry points in
`scripts/ci.sh`: `source`, `build`, `format`, `contract`, and `test <suite>`.
Each test invocation restores the solution in its own runner. Set
`VERIFY_RESULTS_DIR` to change the artifact root and `VERIFY_SUMMARY_FILE`
to append suite and coverage results to a chosen Markdown file. GitHub
Actions supplies its step summary path and uploads the generated artifacts;
the scripts contain the quality checks and failure rules.

The ignored `artifacts/verification/` directory has the following layout:

| Output | Path |
| --- | --- |
| Test results | `{domain,application,infrastructure,external-providers,acceptance}/<suite>.trx` |
| Coverage input | `{domain,application,infrastructure}/<collector-id>/coverage.cobertura.xml` |
| Coverage reports | `{domain,application,infrastructure}-coverage/Summary.txt` and `index.html` |
| Combined local summary | `summary.md` |
| Generated API contract | `openapi.json` |
| Source-derived operation links | `operation-links.json` |
| Generated flow and policy descriptions | `flows.json`, `validation-policies.json` |

Acceptance tests write real Allure results/attachments to the ignored
`artifacts/allure-results/acceptance/` directory; the acceptance command clears
previous results first. The filtered read-only API export writes OpenAPI, flows
and validation policies without starting MongoDB. CI uploads these exports as
`generated-contract` and packages them with actual BDD results and feature sources
using the pinned LiveDocs workflow. Local verification does not upload reports or
require the Blob SAS secret.

Each run clears the previous test and coverage outputs and generated OpenAPI
before executing. The script exits nonzero at the first failing stage, reporting
its name and exit code. Each test suite must produce exactly one TRX with
nonzero total and passed counts and no failures. A missing coverage input or
report fails verification; Domain, Application and Infrastructure each require
at least 70% line coverage. The Infrastructure runsettings exclude only the
generated Kiota client, and the scope check validates the collected report.
OpenAPI validation and Docker build must succeed. CI also runs dependency,
secret and image vulnerability gates; `verify.sh` does not replace them.

INV-BDD-02 adds shared HTTP evidence and 18 standalone recorder cases while
retaining the 77 BDD cases and eight original standalone checks: 103 acceptance
cases total. See [the evidence contract](bdd-02-http-evidence.md) for actual byte
formats, PDF attachments, identity metadata and the Swagger-bootstrap exclusion.

The Mongo Infrastructure/Acceptance jobs temporarily use `ubuntu-22.04` after
repeat startup failures on the rolling Ubuntu 24 runner. Both scripts log the
actual Docker kernel and Mongo image/CLI before the unchanged full suites;
the CLI check does not replace a running replica set. See
[the observed failure and compatibility limit](bdd-02-http-evidence.md#mongo-runner-compatibility).
