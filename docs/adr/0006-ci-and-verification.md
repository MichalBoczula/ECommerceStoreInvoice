# ADR-0006: CI gates and local verification

- Status: Accepted
- Date: 2026-09-26

## Context

A green pull request must mean every required layer test and contract check passed. A successful build alone does not establish that MongoDB transactions, ProductsCatalog integration or invoice recovery work.

## Decision

`global.json` selects SDK 10.0.100 or a newer .NET 10 feature band for local commands. Every CI job resolves the SDK using that file; the Docker build image uses SDK 10.0.100 and copies `global.json` before restore.

The workflow runs on PRs into and pushes to `master`. Build verifies architecture and operation links, audited restore, whole-solution formatting of handwritten C# files, Release compilation and generated OpenAPI. `Directory.Build.props` audits direct and transitive NuGet dependencies at high/critical severity and treats only `NU1903`/`NU1904` audit warnings as errors. Separate jobs run Domain, Application, Infrastructure, ExternalProviders and Acceptance tests with TRX artifacts and summaries. Domain, Application and Infrastructure collect line coverage and each enforce a 70% minimum. Infrastructure's runsettings exclude only the generated Kiota Products client. A scope check ensures that repositories, readiness, initialization and the handwritten Products adapter remain in the report. Gitleaks scans secrets; Dependency Review checks PR dependency changes. The quality gate requires every mandatory job to succeed. Only afterward does the workflow build an unpublished Docker image and scan high/critical vulnerabilities with Trivy. Other compiler warnings remain visible and are not an automatic build blocker.

`scripts/verify.sh` is the broader local check: it verifies whole-solution formatting, runs all suites with the same coverage rules, exports/lints OpenAPI and builds the Docker image. Tests using Testcontainers require a Docker daemon. The checkout acceptance fixture launches MongoDB, SQL Server and `mb0101/product-catalog-api:latest`; a public Docker Hub image can be pulled without a personal access token.

## Consequences

The five test jobs are mandatory; a failed Infrastructure job, missing coverage report or percentage below 70% blocks the quality gate. The generated Kiota source is compiled and exercised by adapter and ExternalProviders tests but excluded from the Infrastructure percentage. Configuration and DI remain included. CI's Docker image is scanned but not pushed to a registry.

CI and `scripts/verify.sh` share the whole-solution format check; generated Reqnroll `.feature.cs` files are excluded. Pre-existing formatting debt in handwritten files was resolved in this separate formatting PR.

The Infrastructure coverage threshold was added on 2026-09-26.

See `.github/workflows/ci.yml`, `scripts/verify.sh` and `scripts/validate-openapi.sh`.

## Alternatives considered

- Use one aggregate test result or coverage percentage for all layers: a passing layer could hide an untested failing layer.
- Treat every compiler warning as an error: ordinary diagnostics would block the build outside the agreed vulnerability gate.
- Publish an image before the required checks and vulnerability scan: an unverified artifact could be distributed.
