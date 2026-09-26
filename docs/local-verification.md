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

Each run clears the previous test and coverage outputs and generated OpenAPI
before executing. The script exits nonzero at the first failing stage, reporting
its name and exit code. Each test suite must produce exactly one TRX with
nonzero total and passed counts and no failures. A missing coverage input or
report fails verification; Domain, Application and Infrastructure each require
at least 70% line coverage. The Infrastructure runsettings exclude only the
generated Kiota client, and the scope check validates the collected report.
OpenAPI validation and Docker build must succeed. CI also runs dependency,
secret and image vulnerability gates; `verify.sh` does not replace them.
