# Agent instructions

Read [README.md](README.md), [Definition of Done](docs/definition-of-done.md) and the relevant [ADRs](docs/adr/README.md) before editing. Inspect existing code and tests for the feature first. Keep PRs small, commits and PR descriptions in English, and scope changes to the requested work.

## Architecture and behavior

- Preserve the existing .NET 10 Minimal API, application services with flow descriptors, domain policies and MongoDB repositories. API owns HTTP and composition; Application owns use cases and PDF generation; Domain owns business rules and contracts; Infrastructure owns MongoDB and the ProductsCatalog adapter.
- Checkout calls ProductsCatalog once to create immutable product snapshots and writes snapshots, order and cleared cart in one MongoDB transaction. Do not add ProductsCatalog calls to cart, order reads or invoice generation. MongoDB integration tests need a replica set; end-to-end checkout tests also need ProductsCatalog with SQL Server.
- Only `Created → Paid` and `Created → Cancelled` are valid status changes. A repeat `Paid` is `400`; an actual concurrent stale write is `409`. Invoices are reserved by unique `OrderId` and published only after PDF completion. Do not describe local `file://` storage as shared storage.
- Review persistence mapping, unique indexes, transaction rollback and invoice recovery whenever writes change. Keep secrets out of source and example settings except clearly local, non-production samples already present in Compose.

## Contracts and testing

- For each endpoint change, update DTOs, validation policy, executed flow descriptor, `.Produces`, generated OpenAPI and relevant acceptance scenarios together. Cover each distinct cause of an HTTP outcome and inspect MongoDB state after failed writes.
- Put pure rule tests in Domain, use-case tests in Application, actual indexes and transactions in Infrastructure, ProductsCatalog protocol tests in ExternalProviders, and HTTP behavior in Acceptance. Use the existing Testcontainers fixtures for MongoDB, ProductsCatalog and SQL Server when needed.
- Edit `.feature` source and step definitions; do not hand-edit generated `.feature.cs`. Export OpenAPI from the API and keep operation → flow → policy → scenario checks passing. Do not hand-maintain a second OpenAPI file.
- Do not skip tests, add `continue-on-error`, or lower coverage or security checks to make a PR green. Restore audits direct and transitive dependencies and fails on high/critical NuGet advisories (`NU1903`/`NU1904`); ordinary compiler warnings remain visible but are not a build blocker. Domain, Application and Infrastructure each require at least 70% line coverage. Infrastructure excludes only the generated Kiota client; keep configuration, DI and handwritten code in the measurement.

## Verification and handoff

Run the relevant focused suite during development. The full local check is:

```bash
bash scripts/verify.sh
```

It needs .NET SDK 10.0.100 or a newer .NET 10 feature band (selected by `global.json`), Bash, Python 3, Node.js, Docker and (for PDF acceptance tests) PowerShell/Playwright. CI's `.github/workflows/ci.yml` is the source of truth for required remote checks. In the PR, report the commands actually run, their outcomes, any unavailable checks, changed public contracts and remaining risks. See [Definition of Done](docs/definition-of-done.md).
