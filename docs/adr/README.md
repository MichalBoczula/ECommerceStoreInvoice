# Architecture Decision Records

These records describe decisions implemented at the time they were written. A later ADR can supersede a decision without rewriting history.

Use repository-local, sequential ADR numbers. Each record has a title, `Status` and `Date` metadata, then `Context`, `Decision`, `Consequences` and `Alternatives considered` sections in that order. Keep accepted records when later decisions supersede them, and add the new record to this index.

| ADR | Status | Decision |
| --- | --- | --- |
| [0001](0001-mongodb-documents-and-snapshots.md) | Accepted | MongoDB documents, product snapshots, client versions and indexes. |
| [0002](0002-checkout-and-status-writes.md) | Accepted | Transactional checkout and conditional status update. |
| [0003](0003-invoice-pdf-and-recovery.md) | Accepted with limitation | Claim-based invoice generation and local PDF storage. |
| [0004](0004-public-error-contract.md) | Accepted | Safe problem responses and status mapping. |
| [0005](0005-generated-contract-documentation.md) | Accepted | Generated OpenAPI and source-linked flows/policies/scenarios. |
| [0006](0006-ci-and-verification.md) | Accepted | Required CI jobs, coverage and verification. |
| [0007](0007-acceptance-isolation.md) | Accepted | Isolate MongoDB per scenario while reusing the MongoDB, ProductsCatalog and SQL Server containers. |
