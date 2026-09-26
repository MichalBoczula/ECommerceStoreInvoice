# ADR 0006: CI gates and local verification

Status: Accepted with follow-up (2026-09-26)

## Context

A green pull request must mean every required layer test and contract check passed. A successful build alone does not establish that MongoDB transactions, ProductsCatalog integration or invoice recovery work.

## Decision

The workflow runs on PRs into and pushes to `master`. Build verifies architecture and operation links, restore, changed-file formatting, Release compilation and generated OpenAPI. Separate jobs run Domain, Application, Infrastructure, ExternalProviders and Acceptance tests with TRX artifacts. Gitleaks scans secrets; Dependency Review checks PR dependency changes. The quality gate requires every mandatory job to succeed. Only afterward does the workflow build an unpublished Docker image and scan high/critical vulnerabilities with Trivy. Compiler warnings remain visible and are not an automatic build blocker.

`scripts/verify.sh` is the broader local check: it verifies whole-solution formatting, runs all suites, exports/lints OpenAPI and builds the Docker image. Tests using Testcontainers require a Docker daemon. The checkout acceptance fixture launches MongoDB, SQL Server and `mb0101/product-catalog-api:latest`; a public Docker Hub image can be pulled without a personal access token.

## Consequences and follow-up

The five test jobs are mandatory; a failed Infrastructure job blocks the quality gate. **There is currently no coverage collection or 70% threshold in the workflow on `master`, including Infrastructure.** Add coverage reporting and decide/enforce the intended Domain/Application thresholds in the CI follow-up; do not claim a threshold from an unmerged or superseded branch. The local script also does not currently enforce a numeric threshold. CI's Docker image is scanned but not pushed to a registry.

See `.github/workflows/ci.yml`, `scripts/verify.sh` and `scripts/validate-openapi.sh`.
