# ECommerceStoreInvoice

## Purpose

.NET 10 Minimal API for shopping carts, order checkout, client data versions and invoice generation. MongoDB stores the business data; ProductsCatalog supplies product details when an order is created. Order reads and invoice generation use the saved product snapshots, without calling ProductsCatalog again.

The service owns the order and invoice lifecycle independently of the product catalog. Its MongoDB transactions protect checkout state, while immutable product snapshots preserve what the client ordered even if catalog data later changes.

## Engineering approach

### Designed contracts, executable sources

Define routes, DTOs, validation, HTTP outcomes and acceptance scenarios together. Minimal API metadata and DTOs generate the OpenAPI document; the repository does not maintain a second handwritten specification. Reqnroll `.feature` files describe observable behavior, and acceptance tests compare exercised HTTP responses with generated OpenAPI. These checks cover the scenarios they execute, not every possible runtime response.

### Flow and policy descriptions

Application services execute flow descriptors, and Domain policies describe validation rules and possible errors. The API publishes them through `/orders-documentation/flows` and `/orders-documentation/validations`. The operation-link check connects routes to executed flows, policies and acceptance scenarios. Generated OpenAPI and link reports are verification artifacts; see [ADR-0005](docs/adr/0005-generated-contract-documentation.md).

CI packages these generated sources with real Reqnroll/Allure results for the
shared LiveDocs portal; see [BDD report publication](#bdd-reports-in-livedocs).

## Architecture

```mermaid
flowchart TD
    Client[API consumers] --> API[ECommerceStoreInvoice.API]
    API --> Application[ECommerceStoreInvoice.Application]
    Application --> Domain[ECommerceStoreInvoice.Domain]
    API --> Infrastructure[ECommerceStoreInvoice.Infrastructure]
    Infrastructure --> Domain
    Infrastructure --> Mongo[(MongoDB replica set)]
    Infrastructure --> Products[ProductsCatalog API]
```

| Project | Responsibility |
| --- | --- |
| `src/ECommerceStoreInvoice.API` | Routes, HTTP problem responses, health checks, Swagger and composition. |
| `src/ECommerceStoreInvoice.Application` | Use cases, DTO mapping, invoice PDF generation and executable flow descriptors. |
| `src/ECommerceStoreInvoice.Domain` | Aggregates, repository contracts and validation policies. |
| `src/ECommerceStoreInvoice.Infrastructure` | MongoDB documents, repositories, transactions, indexes, readiness and ProductsCatalog client. |

An order checkout loads the client's cart, fetches the selected products from ProductsCatalog, creates product version snapshots, then writes snapshots, the order and the cleared cart in one MongoDB transaction. A MongoDB replica set with a writable primary and sessions is required. The order status can move from `Created` to `Paid` or `Cancelled` once. Repeating `Paid` yields a validation `400`; a concurrent write against a stale `Created` order yields `409`.

An invoice requires a paid order owned by the client and a client data version. Generation reserves a unique `OrderId` in MongoDB, uploads its attempt PDF to private Azure Blob Storage, then marks the reservation completed. `GET /invoices/{invoiceId}/pdf` streams only completed invoice PDFs through the API. Local Development/Testing can retain instance-local `file://` storage when Blob settings are absent; Production requires Blob configuration. See [the ADR index](docs/adr/README.md) for recovery behavior and limitations.

## Technology stack

| Area | Technology |
| --- | --- |
| Runtime and API | .NET 10, ASP.NET Core Minimal APIs |
| Application flow and PDFs | Application services, source flow descriptors, Playwright |
| Database | MongoDB replica set, MongoDB .NET Driver |
| Product integration | Kiota client for ProductsCatalog |
| API contract | Swashbuckle OpenAPI, Redocly CLI validation |
| Tests | xUnit, Shouldly, Moq, Reqnroll, Testcontainers |
| Coverage | Coverlet collector, ReportGenerator |
| Containers | Docker, Docker Compose |
| CI | GitHub Actions, NuGet Audit, Dependency Review, Gitleaks, Trivy |

### Repository structure

```text
src/
  ECommerceStoreInvoice.API/
  ECommerceStoreInvoice.Application/
  ECommerceStoreInvoice.Domain/
  ECommerceStoreInvoice.Infrastructure/
tests/
  ECommerceStoreInvoice.Domain.UnitTests/
  ECommerceStoreInvoice.Application.UnitTests/
  ECommerceStoreInvoice.Infrastructure.UnitTests/
  ECommerceStoreInvoice.ExternalProviders.IntegrationTests/
  ECommerceStoreInvoice.Acceptance.Tests/
  ECommerceStoreInvoice.Performance.Benchmarks/
docs/
  adr/
```

## Local startup

### Prerequisites

- .NET SDK 10.0.100 or a newer .NET 10 feature band (selected by [`global.json`](global.json)), Docker with a running daemon and Docker Compose.
- For full local verification: Bash, Python 3 and Node.js (the OpenAPI lint uses Redocly CLI through `npx`).
- For local PDF generation outside the Docker image: install Chromium once after building, using `pwsh src/ECommerceStoreInvoice.API/bin/Debug/net10.0/playwright.ps1 install --with-deps chromium`. The API Dockerfile already installs Chromium and its dependencies. Invoice requests never install or download browsers.
- ProductsCatalog is needed to create an order. Acceptance tests start the digest-pinned Products image in `ProductCatalogContainerFixture.cs` and SQL Server 2022 with Testcontainers; the seeded catalog IDs are used by those tests. Shopping cart operations and order/invoice reads do not need a live ProductsCatalog request.

### Run locally

The checked-in Compose stack initializes a single-node MongoDB replica set and runs the API. To start just MongoDB for an API process on your host:

```bash
docker compose -f docker-compose.yml up -d mongo-init
export MongoDbSettings__ConnectionString='mongodb://admin:admin123@localhost:27017/?authSource=admin&directConnection=true'
dotnet run --project src/ECommerceStoreInvoice.API --launch-profile http
```

The password in `docker-compose.yml` is a local development example; supply credentials from environment variables or a secret store for another deployment. The API uses database and collection names from [`appsettings.json`](src/ECommerceStoreInvoice.API/appsettings.json), creates indexes at startup and fails startup if MongoDB initialization fails. The host command expects port `27017` to be free. [`.env.example`](.env.example) shows the environment variable for an unauthenticated local MongoDB, but that server must still be a replica set for checkout transactions and readiness. If a replica set advertises an address not reachable from the host, configure its advertised address or run the API in the same Docker network.

The host API listens at <http://localhost:5039> with the `http` profile. `ExternalServices:ProductCatalog:BaseUrl` defaults to `http://localhost:5000` in application settings; override it with `ExternalServices__ProductCatalog__BaseUrl` if ProductsCatalog listens elsewhere. The ProductsCatalog container and SQL Server are managed automatically by the acceptance tests, not by this Compose file. If running the API container from Compose, set the ProductsCatalog URL to a network-reachable address; `localhost` inside the API container points back to the API container.

See [startup and failure behavior](docs/local-startup.md) for index initialization and the boundary between startup failure and readiness.

```bash
curl -i http://localhost:5039/health/live
curl -i http://localhost:5039/health/ready
```

### Run with Docker Compose

To build and start the API with its MongoDB replica set:

```bash
docker compose -f docker-compose.yml up -d --build
curl -i http://localhost:8080/health/ready
```

Compose exposes the API at <http://localhost:8080>. Its example MongoDB password is for local development only. Before creating orders from the API container, configure `ExternalServices__ProductCatalog__BaseUrl` with an address reachable from the Compose network; the default `localhost:5000` points to the API container itself.

Stop the stack with `docker compose -f docker-compose.yml down`; add `--volumes` only when you intend to delete local MongoDB data.

## API contract

- Swagger UI: <http://localhost:5039/swagger> (or port `8080` with Compose).
- Generated OpenAPI: <http://localhost:5039/swagger/v1/swagger.json> (or port `8080` with Compose).
- Flow and policy descriptions: `/orders-documentation/flows` and `/orders-documentation/validations`.

`GET /invoices/by-order/{orderId}` returns only a completed invoice (including legacy completed documents). Payments uses it to reconcile a lost creation response; incomplete reservations return `404`. Creation and lease behavior are unchanged.

`GET /invoices/{invoiceId}/pdf` returns `application/pdf` with an attachment filename and `Cache-Control: private, no-store`. Empty IDs return `400`; missing or incomplete invoices and missing PDFs return `404`; storage failures return the existing safe `500` problem contract. Consumers should use this endpoint instead of opening `StorageUrl` directly. Blob URLs in metadata contain no SAS token and the container remains private.

The public route groups are `/shopping-carts`, `/orders`, `/invoices` and `/client-data-versions`. `POST /orders/client/{clientId}` creates an order from a cart; the older `POST /orders/{clientId}` alias remains callable but is excluded from OpenAPI. `GET /orders/{orderId}` includes status, total amount, currency and snapshot-backed lines. Its total is a decimal amount in the product currency; do not treat it as integer minor units. The response mapping currently takes the first line's currency, so mixed-currency orders require a separate domain decision before a payment integration assumes a single currency.

Validation and malformed JSON return `400`, missing resources `404`, duplicate invoices/carts and stale order writes `409`, and unexpected errors `500`. Business errors use `application/problem+json`; unexpected errors have a generic detail and a trace ID. Probe responses use the ASP.NET health-check format rather than the business problem contract. For the exact request/response DTOs and status declarations, use generated OpenAPI. Acceptance tests assert observed HTTP responses against the generated specification; this validates exercised responses and is not a proof of all possible runtime paths.

See the [public error contract](docs/api-problem-contract.md) and [response scenario matrix](docs/acceptance-matrix.md) for mapped causes and actual acceptance evidence.

## Health checks

| Route | Meaning |
| --- | --- |
| `/health/live` | Process liveness, independent of MongoDB. |
| `/health/ready` | Writable MongoDB replica set or router, positive session support and database ping; returns `503` when unavailable. |
| `/health` | Compatibility alias for liveness. |

Use readiness for traffic routing and liveness for process checks. These probes do not test ProductsCatalog availability or PDF storage.

See [health-check details](docs/health-checks.md) for failure behavior and the currently tested paths.

## Tests

The [BDD backlog](docs/bdd-backlog.md) groups the Orders/Invoices documentation
work into small packages. [Task 1's baseline](docs/bdd-01-baseline.md) preserves
38 feature files, 52 declarations and all 77 original expanded BDD cases, plus
eight standalone acceptance checks. The source gate now checks every matrix
example and all 56 declared operation/status pairs: 54 have guaranteed scenario
intent and two explicit gaps remain. These are source coverage links, not
a claim of complete runtime cause coverage. [Task 2](docs/bdd-02-http-evidence.md)
adds actual paired HTTP evidence, explicit IDs and readable descriptions for all
77 BDD cases and 18 recorder regressions. [Task 3](docs/bdd-03-shopping-carts.md)
adds complete cart response/persistence evidence and four populated/empty/replacement
cases. [Task 4](docs/bdd-04-checkout-snapshots.md) adds complete checkout/snapshot
state, actual catalog exchanges and isolated catalog-change/outage evidence.
[Task 5](docs/bdd-05-order-reads-status.md) adds complete order reads, client
filtering and exact populated state for accepted/rejected status changes.
[Task 6](docs/bdd-06-client-data-versions.md) adds immutable client-data histories,
timestamp-based latest selection, isolation and historical invoice-reference evidence.
[Task 7](docs/bdd-07-invoice-generation-recovery.md) adds complete invoice creation,
reservation, attempt cleanup, retry and lost-acknowledgment evidence.
[Task 8](docs/bdd-08-invoice-reads-pdfs.md) adds complete completed-only/legacy
metadata, exact binary downloads and real private Blob adapter HTTP evidence.
The current acceptance inventory is **106 BDD + 27 standalone = 133**; recorder
regressions remain standalone checks. Generated `.feature.cs` files are ignored
build outputs, not maintained source.

Run from the repository root:

```bash
bash scripts/verify.sh
```

The script checks architecture and operation links, restores, builds, verifies formatting of all handwritten C# files (generated Reqnroll `.feature.cs` files are excluded), runs Domain, Application, Infrastructure, ExternalProviders and Acceptance suites, exports and lints generated OpenAPI, then builds the Docker image. Testcontainers require Docker; order scenarios start MongoDB, ProductsCatalog and SQL Server. TRX files, coverage reports, the combined `summary.md` and generated OpenAPI use the shared layout in [local verification](docs/local-verification.md).

For a focused Infrastructure run:

```bash
dotnet test tests/ECommerceStoreInvoice.Infrastructure.UnitTests/ECommerceStoreInvoice.Infrastructure.UnitTests.csproj --configuration Release
```

Infrastructure tests check MongoDB indexes, mapping, transactions and readiness. ExternalProviders tests exercise the ProductsCatalog adapter; checkout acceptance scenarios use MongoDB, ProductsCatalog and SQL Server Testcontainers. Performance benchmarks in `tests/ECommerceStoreInvoice.Performance.Benchmarks/` are outside the five required CI suites. Generated `.feature.cs` files are build outputs and should not be edited manually.

## CI

The workflow calls `scripts/ci.sh` for source checks, audited restore and
build, formatting, each test suite and coverage, and the generated OpenAPI
contract. `scripts/verify.sh` uses the same entry points locally. GitHub
Actions owns job dependencies, artifact upload, secrets and image scanning.

CI on `master` and pull requests runs build/OpenAPI, five separate test suites with TRX summaries, secret scanning, PR dependency review, a quality gate and an image build with Trivy scanning. Restore audits direct and transitive NuGet dependencies; high/critical findings (`NU1903`/`NU1904`) fail the build while ordinary compiler warnings remain visible. Domain, Application and Infrastructure each require at least 70% line coverage. Infrastructure excludes only the generated Kiota Products client through its [coverage settings](tests/ECommerceStoreInvoice.Infrastructure.UnitTests/coverage.runsettings); repository, mapping, health, configuration, DI and handwritten Products adapter code remain measured. The same checks run through `scripts/verify.sh`. Every required job must pass before the image job starts. Pull requests build and scan without publishing; after a successful quality gate and Trivy scan on a `master` push, CI publishes the same scanned image to `mb0101/ecommerce-store-invoice-api` with `latest` and full commit SHA tags. Docker Hub credentials come from the `DOCKERHUB_USERNAME` and `DOCKERHUB_TOKEN` GitHub Actions secrets. See [CI ADR](docs/adr/0006-ci-and-verification.md), [image publication ADR](docs/adr/0008-publish-scanned-image.md) and [Definition of Done](docs/definition-of-done.md).

### BDD reports in LiveDocs

The pipeline follows the Products and Users pattern. Both PRs and master builds
package actual Allure results/attachments, feature sources, generated OpenAPI,
flows, validation policies and operation links using the commit-pinned LiveDocs
workflow. Successful bundle validation is required by the quality gate. All five
test suites, separate coverage floors and existing security/image gates remain.

After successful master image publication, CI uploads the source ZIP and receipt
to the existing private **`stecproductsdevmb261013/livedocs`** container:

- `reports/invoices/<source-commit>/<bundle-sha256>.zip`
- `candidates/v1/invoices/<workflow-run-id>-<bundle-sha256>.json`

Retries download existing objects and compare bytes; different content is refused.
The `documentation-reference` Actions artifact contains the exact reference for
LiveDocs. URLs contain no SAS. PRs never upload documentation to Azure.

Add **`LIVEDOCS_STORAGE_SAS_TOKEN`** as a repository Actions secret in
**ECommerceStoreInvoice**. Use the complete HTTPS-only container SAS query string,
including every field and `sig`, with **read/create/write** permissions on
`livedocs`. The same SAS used by Products and Users works. Keep it out of source,
issues and image arguments. Optional variables are `LIVEDOCS_STORAGE_ACCOUNT`
(default `stecproductsdevmb261013`) and `LIVEDOCS_VERSION` (default `v1`). There is
no enable flag: missing credentials or upload failures fail the archive job.

LiveDocs selects the archived receipt's `reference` under project `invoices` in its
development v1 manifest. Its builder downloads and validates the bundle outside
Docker and generates the report at `/livedoc/v1/invoices/allure/`, with BDD/API/flow
sources at `/livedoc/v1/invoices/`. The report covers this service's shopping-cart,
order and invoice scenarios alongside the existing Products and Users reports.

A new LiveDocs image and Terraform digest update publish the snapshot. Uploads
alone do not change running reports. Infrastructure stays in ECommerceStore.Terraform;
no additional Azure resources, API runtime secrets or BFF changes are needed for
documentation. The existing `invoices` Blob container continues storing business
PDFs; `livedocs` stores CI documentation sources. See
[ADR-0009](docs/adr/0009-livedocs-source-publication.md).

## Operations

Checkout requires a reachable ProductsCatalog API and a MongoDB deployment capable of transactions. Readiness accepts a writable replica set or the `isdbgrid` router topology used by Azure DocumentDB. Startup creates checkout collections outside transactions, initializes indexes and fails if initialization fails. Hello/ping and native MongoDB tests do not prove every hosted-database transaction behavior; verify real checkout and rollback after deployment.

For the Azure deployment, supply these existing-container settings as environment variables:

```text
MongoDbSettings__ConnectionString=<Mongo connection secret>
MongoDbSettings__DatabaseName=ecommerce-store-invoice-db
ExternalServices__ProductCatalog__BaseUrl=<Products API URL>
InvoiceStorage__ConnectionString=<storage account connection secret>
InvoiceStorage__ContainerName=invoices
```

Production fails startup without the Blob connection string. Terraform creates the private container; the application does not create or make containers public. PDFs use immutable invoice/attempt filenames and `application/pdf` content type. A failed upload prevents invoice completion, and failed attempts delete only their own Blob. After database completion is attempted, recovery preserves the Blob because MongoDB may have committed despite a lost acknowledgement. Orphan cleanup for abandoned attempts remains future work; no background cleanup job is introduced.

The browser download path through the storefront is `/backend/invoices/{invoiceId}/pdf`; BFF already forwards `/invoices`. Web still needs a download button if one is desired in its existing invoice metadata screen. Local-mode `file://` PDFs are not shared or migrated automatically; a missing legacy file returns `404`.

Before enabling real traffic, verify paid-order generation, private Blob upload and API download, then restart the ACA and download the same invoice again. Readiness checks MongoDB, not Blob permissions or Chromium rendering. The currency returned for an order is taken from the first snapshot line; mixed-currency totals remain a separate domain decision before payment integration.

Operational references: [startup](docs/local-startup.md), [health probes](docs/health-checks.md), [public error contract](docs/api-problem-contract.md), and the [cause-to-response-to-scenario matrix](docs/acceptance-matrix.md). The matrix marks exercised responses and explicit gaps; keep it aligned with endpoint metadata and acceptance scenarios.

## Architecture decisions

The [ADR index](docs/adr/README.md) covers MongoDB snapshots, checkout and status writes, PDF recovery, public errors, generated contracts, CI and acceptance isolation. Contributors and coding agents should also read [`AGENTS.md`](AGENTS.md) and [Definition of Done](docs/definition-of-done.md).

