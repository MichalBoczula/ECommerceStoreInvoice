# ADR-0007: Isolate acceptance scenarios on a shared MongoDB container

- Status: Accepted
- Date: 2026-09-26

## Context

HTTP acceptance scenarios exercise MongoDB indexes and transactions, and checkout scenarios call a running ProductsCatalog API backed by SQL Server. State left by one scenario must not change another scenario's outcome. Starting all three containers for every scenario would add significant setup time.

## Decision

The acceptance suite starts one MongoDB replica-set Testcontainer lazily and gives each `ApplicationFactory` a unique MongoDB database name. A Reqnroll `BeforeScenario` hook creates a new factory and HTTP client for the scenario; `AfterScenario` disposes them and drops that database. The suite disables test parallelization. Each factory initializes its MongoDB indexes and serves generated OpenAPI for a response handler that compares exercised HTTP outcomes with the contract.

Only scenarios tagged `@products-api` start the shared ProductsCatalog fixture. That fixture starts `mb0101/product-catalog-api:latest` and SQL Server on a dedicated Docker network; catalog migrations seed product data used by checkout scenarios. Catalog and SQL Server containers are reused for those scenarios and disposed after the test run. The scenarios read seeded catalog products; their scenario-specific orders, carts, snapshots and invoices stay in their own MongoDB database. Tags `@pdf-fails-once` and `@completion-ack-lost` install scenario-local failure wrappers for recovery cases. Other acceptance tests can use their own `ApplicationFactory` instances and still get a distinct MongoDB database.

## Consequences

Scenarios use isolated invoice data while reusing expensive containers. A failed database drop surfaces as a test failure. Isolation depends on catalog scenarios continuing to treat the shared seeded SQL data as read-only; if tests later mutate catalog data, they need their own SQL reset or database boundary. The mutable `latest` catalog image and its seed IDs must remain compatible with the tests. The per-scenario MongoDB boundary does not assert that local PDF files are shared between API instances or that a production database can be upgraded safely. A Docker daemon and Playwright dependencies are required for the complete acceptance suite.

See `tests/ECommerceStoreInvoice.Acceptance.Tests/ApplicationFactory.cs`, `ProductCatalogContainerFixture.cs`, `AssemblyInfo.cs`, `Features/Common/TestHooks.cs` and the tagged `.feature` scenarios.

## Alternatives considered

- Reuse one MongoDB database across scenarios: leftover orders, indexes and invoice claims could make tests order dependent.
- Start MongoDB, ProductsCatalog and SQL Server for every scenario: data would be isolated but container startup and migrations would be repeated for each checkout case.
- Replace the catalog with a mock for all acceptance tests: checkout would no longer verify the actual HTTP integration and seeded product snapshots.
