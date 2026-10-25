# ADR-0009: Publish Orders/Invoices source bundles for LiveDocs

- Status: Accepted; extends ADR-0005 generated sources and ADR-0008 publication
- Date: 2026-10-25

## Context

Products and Users already publish validated BDD source bundles to the shared
LiveDocs archive. Orders/Invoices has Reqnroll/Allure, executable descriptions and
an operation-link projection, but CI does not yet archive those sources for the portal.

## Decision

Use the same producer pattern with project slug `invoices`. Emit operation links
through the existing checker, export flow and validation responses with OpenAPI
through the existing read-only export test, and collect actual acceptance results
in a stable ignored Allure directory cleared before each suite.

Call the commit-pinned LiveDocs packaging workflow with generated sources,
Reqnroll feature files, raw results and attachments. The quality gate requires
successful bundle validation in addition to all five existing test suites and
unchanged coverage/security checks.

Only after a successful master image publication, upload the ZIP to
`reports/invoices/<commit>/<sha256>.zip` and its discovery receipt to
`candidates/<version>/invoices/<run-id>-<sha256>.json` in the existing private
`stecproductsdevmb261013/livedocs` container. Authenticate with repository secret
`LIVEDOCS_STORAGE_SAS_TOKEN`, a container SAS with read/create/write permissions.
Verify existing bytes on retries and refuse replacement. PRs never upload; a
missing secret or failed upload fails the archive job.

LiveDocs selects the exact archived reference in a manifest PR, retains Products
and Users, and generates Allure before building its static Nginx image. Deployment
stays a digest update and apply in the shared ECommerceStore.Terraform root.
The application runtime and its private invoice-PDF storage settings are unchanged.

## Consequences

Source SHA, workflow run and bundle digest identify the BDD evidence. Generated
reports, attachments and credentials stay out of Git; no new Azure resources,
packages, runtime documentation connections or deployment scripts are required.
The producer SAS must be configured before the first upload. A Blob upload alone
does not update the running portal: selected manifest, verified image and deployment
complete the handoff. Preserve referenced archives for retained versions.

Only synthetic acceptance data belongs in attachments. The archive is private,
while documentation baked into public images or served by public ACA is public.
The optional automated importer and RAG ingestion remain outside this change.

## Alternatives considered

- Commit generated HTML: duplicates rebuildable reports in Git.
- Download documentation in running replicas: adds credentials and mutable runtime content.
- Add dedicated storage or identities: unnecessary for the educational store's existing archive.
- Hand-maintain API/flow JSON: duplicates executable documentation sources.
