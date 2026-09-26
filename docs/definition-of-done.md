# Definition of Done

Use this checklist for each change where the item applies; it does not claim that every existing path already meets every item.

- The PR explains the behavior and scope, names the relevant backlog item where one exists, and justifies public API, persistence, dependency or infrastructure changes.
- The dependency boundaries remain intact. Changes to checkout preserve atomic snapshots/order/cart writes; status updates preserve the transition rule and stale-write conflict; invoice changes preserve one invoice per order and recovery after PDF or completion failures.
- An endpoint change aligns request and response DTOs, validation, safe `application/problem+json` errors, status/media type declarations, generated OpenAPI, the executed flow, policy descriptions and affected HTTP acceptance tests. Distinct reasons for `400`, `404`, `409` or `500` have separate scenarios where relevant. For writes, tests inspect stored state after success and failure.
- Domain and Application tests cover their behavior; Infrastructure tests exercise real MongoDB for indexes, sessions, transactions and concurrency; ExternalProviders and Acceptance tests use the ProductsCatalog and SQL Server containers when checkout requires them. No scenario is silently skipped or hidden by `continue-on-error`.
- The focused test suite passes. Run `bash scripts/verify.sh` when its prerequisites are available; check format, Release build, all suites, operation links, generated OpenAPI lint/response comparison and Docker build. CI additionally requires secret scan, PR dependency review, quality gate and image scan. State clearly what was run and what was unavailable.
- A numeric coverage floor is **not currently enforced** in `.github/workflows/ci.yml`; adding it is a separate CI follow-up. Coverage alone never substitutes for response-path and persistence assertions. Normal compiler warnings remain visible without automatically failing CI.
- Update README and relevant ADRs when behavior or decisions change. Note remaining operational limits such as instance-local `file://` PDFs and mixed-currency order totals until implementation changes them.

The PR is ready for merge only when its applicable checks have passed and any limitation is recorded for review.
