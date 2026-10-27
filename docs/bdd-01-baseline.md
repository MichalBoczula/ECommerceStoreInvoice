# INV-BDD-01: acceptance baseline and coverage guard

The immutable [source baseline](bdd-baseline.json) records commit
`43af2ef1720b3b66b0a5f7392d5321b4de5b7d87` and its successful
[master CI 40214951632](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40214951632).
That run and its actual TRX/Allure bundle contain:

| Source or execution | Count |
| --- | ---: |
| Feature files | 38 |
| Scenario declarations, including four outlines | 52 |
| Expanded BDD cases | 77 |
| Standalone acceptance cases | 8 |
| Acceptance-project total | 85 |
| Domain / Application / Infrastructure / ExternalProviders cases | 112 / 95 / 73 / 3 |

The eight standalone checks are three health cases, three request-body cases,
one Products container check and one read-only OpenAPI export. They remain
outside the Allure BDD inventory. Counts describe this baseline, not a reason to
reject added coverage. Preserve each original feature/title/behavioral example;
do not regenerate the baseline merely to make a removed case pass. A deliberate
rename or behavioral migration needs explicit review. Later scenarioId metadata
may be added without changing original behavioral values.

## Source and matrix checks

The existing `scripts/check-operation-links.py` now preserves the baseline and
joins actual endpoint registrations to all 77 expanded cases through
`acceptance-matrix.tsv`. It retains operation-to-service/flow/registered-policy
checks, includes shared failure/recovery features, rejects duplicate identities,
stale routes/scenarios/examples, unnamed operations and missing/duplicate status
metadata. Registration parsing includes nested response generics and metadata
before or after WithName; the known excluded order alias remains explicit.
The generated OpenAPI check rejects missing/extra/unnamed operations and compares
methods, normalized paths, response status sets and media/schema declarations.
This remains a repository-pattern source parser, not a general C# compiler.

The matrix's `scenarioId` is the assigned source-link identity. Task 2 will
publish it on actual scenario/exchange evidence. `example` contains the exact
behavioral Examples values, or JSON null for a plain scenario. `statuses` lists
outcomes guaranteed by that case's assertions; `optionalStatuses` lists allowed
race outcomes and does not close a guaranteed-coverage gap. Multi-stage retry
and concurrent cases keep multiple outcomes without inventing additional BDD
instances. Matrix paths are canonical OpenAPI routes; current legacy order
calls remain unchanged and will retain actual paths in task-2 capture.

There are **15 named operations and 56 declared operation/status pairs**.
Guaranteed scenario intent covers **54**, with exactly two reviewed entries in
`acceptance-gaps.tsv`:

- GetInvoiceByOrderId / 500 has no HTTP acceptance case yet: INV-BDD-10.
- UpdateOrderStatus / 409 is conditional in the competing-write case, which
  accepts 400 or 409 depending on read timing: INV-BDD-11.

A new uncovered pair or a stale/resolved gap fails the gate. Existing task-1
links do not prove every possible cause, observed exchange, field combination
or runtime branch. Error, framework, contract and RAG improvements remain
separate backlog packages. Application wire behavior is unchanged.

## Reproducible test sources

Reqnroll `.feature` files and step definitions remain authoritative. Thirty
previously tracked generated `.feature.cs` files are removed from tracking and
ignored; Reqnroll regenerates code during a clean build. No feature, behavioral
Examples value or step definition is removed. The source gate rejects tracked
generated outputs, removed feature inputs and disabled Gherkin tags.

The checkout fixture now uses
`docker.io/mb0101/product-catalog-api@sha256:01e6de37c57f108f843a7fad616b412b84b41c53f7d30e59b20b77889a3207cd`.
This is the scanned/published image for Products source
`59e1d8f827fac1d1c87cd67eaef884d92a06d1a3`, verified from
[CI 40398686283](https://github.com/MichalBoczula/ProductsCatalog/actions/runs/40398686283).
Full-SHA and latest publication logs report the same digest. Pin changes need
a checkout/container test run; this does not pin every tool or base image.

## Verification

`bash scripts/ci.sh source` runs the existing architecture checks, 18 focused
link/contract regression tests and the source projection. Regressions reject
lost or changed examples, stale routes/IDs/statuses, incomplete links, inflated
race coverage, stale gap declarations and unnamed/missing OpenAPI operations.
The baseline permits additive identity metadata and additional scenarios.

Run full CI and inspect its clean-checkout acceptance TRX and Allure package
before completing the PR: all 77 original BDD cases and eight standalone cases
must still execute, with no failures/skips. Domain/Application/Infrastructure
retain separate 70% floors, and all five suites, security and image checks remain.
Local .NET/Docker are unavailable; source/old-export checks run locally and real
build/format/runtime/image verification uses CI. No Azure deployment, selected
LiveDocs refresh or new infrastructure belongs to this task.
