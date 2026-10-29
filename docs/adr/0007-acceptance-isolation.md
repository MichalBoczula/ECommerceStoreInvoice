# ADR-0007: Isolate acceptance scenarios on a shared MongoDB container

- Status: Accepted
- Date: 2026-09-26

## Context

HTTP acceptance scenarios exercise MongoDB indexes and transactions, and checkout scenarios call a running ProductsCatalog API backed by SQL Server. State left by one scenario must not change another scenario's outcome. Starting all three containers for every scenario would add significant setup time.

## Decision

The acceptance suite starts one MongoDB replica-set Testcontainer lazily and gives each `ApplicationFactory` a unique MongoDB database name. A Reqnroll `BeforeScenario` hook creates a new factory and HTTP client for the scenario; `AfterScenario` disposes them and drops that database. The suite disables test parallelization. Each factory initializes its MongoDB indexes and serves generated OpenAPI for a response handler that compares exercised HTTP outcomes with the contract.

Only scenarios tagged `@products-api` start the shared ProductsCatalog fixture. That fixture starts the digest-pinned Products image declared in `ProductCatalogContainerFixture.cs` and SQL Server on a dedicated Docker network; catalog migrations seed product data used by checkout scenarios. Catalog and SQL Server containers are reused for those scenarios and disposed after the test run. The scenarios read seeded catalog products; their scenario-specific orders, carts, snapshots and invoices stay in their own MongoDB database. Tags `@pdf-fails-once` and `@completion-ack-lost` install scenario-local failure wrappers for recovery cases. Other acceptance tests can use their own `ApplicationFactory` instances and still get a distinct MongoDB database.

INV-BDD-04 adds an explicit `isolated-products` tag for the scenario that changes
catalog fixture values and stops its API. That scenario owns a separate Products
API, SQL Server and Docker network with the same pinned image; all are disposed
with its factory. Direct parameterized SQL changes are labelled as fixture setup
and independently confirmed over Products HTTP. The shared seeded catalog stays
read-only. `checkout-evidence` adds the shared byte recorder to the existing
Products HttpClient and counts actual application fetches separately from explicit
fixture probes; it does not replace the real Kiota adapter or Products service.

## Consequences

Scenarios use isolated invoice data while reusing expensive containers. A failed database drop surfaces as a test failure. Isolation depends on catalog scenarios continuing to treat the shared seeded SQL data as read-only; if tests later mutate catalog data, they need their own SQL reset or database boundary. INV-BDD-01 replaces the mutable `latest` input with the published digest from Products commit `59e1d8f827fac1d1c87cd67eaef884d92a06d1a3` (CI 40398686283). Future pin updates must verify catalog behavior and seed IDs against the checkout tests; an unrelated Products publication no longer silently changes their input. The per-scenario MongoDB boundary does not assert that local PDF files are shared between API instances or that a production database can be upgraded safely. A Docker daemon and Playwright dependencies are required for the complete acceptance suite.

See `tests/ECommerceStoreInvoice.Acceptance.Tests/ApplicationFactory.cs`, `ProductCatalogContainerFixture.cs`, `AssemblyInfo.cs`, `Features/Common/TestHooks.cs` and the tagged `.feature` scenarios.

## Alternatives considered

- Reuse one MongoDB database across scenarios: leftover orders, indexes and invoice claims could make tests order dependent.
- Start MongoDB, ProductsCatalog and SQL Server for every scenario: data would be isolated but container startup and migrations would be repeated for each checkout case.
- Replace the catalog with a mock for all acceptance tests: checkout would no longer verify the actual HTTP integration and seeded product snapshots.


INV-BDD-11 adds scenario-local, bounded test gates around the real scoped Mongo
repositories and transaction. Both status requests validate before a selected
write; both checkouts stage writes before the selected cart update. Session-aware
reads inspect staged documents while independent clients verify invisibility.
Failure injection occurs after actual successful writes; production services
perform their normal commit/abort. An Infrastructure friend assembly allows the
Acceptance tests to observe the internal session, following the existing test
friend pattern. This adds no runtime coordination or transaction behavior.


INV-BDD-12 uses owned Mongo containers only for deliberate outage or standalone
topology tests; shared Mongo is never stopped. Test-only readiness monitoring
records actual hello/ping commands with bounded client timeouts. Nested peer hosts
have distinct databases despite identical business IDs, and registered async
cleanup drops only their owned database. PDF cancellation gates one real local
stream before headers or during copying; RequestAborted supplies cancellation and
a healthy retry verifies unchanged bytes. These are native test fixtures, not
production coordination, shared local storage or deployed outage tests.
