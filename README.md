# ECommerceStoreInvoice

## Purpose

.NET 10 Minimal API for shopping carts, order checkout, client data versions and invoice generation. MongoDB stores the business data; ProductsCatalog supplies product details when an order is created. Order reads and invoice generation use the saved product snapshots, without calling ProductsCatalog again.

The service owns the order and invoice lifecycle independently of the product catalog. Its MongoDB transactions protect checkout state, while immutable product snapshots preserve what the client ordered even if catalog data later changes.

## Engineering approach

### Designed contracts, executable sources

Define routes, DTOs, validation, HTTP outcomes and acceptance scenarios together. Minimal API metadata and DTOs generate the OpenAPI document; the repository does not maintain a second handwritten specification. Reqnroll `.feature` files describe observable behavior, and acceptance tests compare exercised HTTP responses with generated OpenAPI. These checks cover the scenarios they execute, not every possible runtime response.

### Flow and policy descriptions

Application services execute flow descriptors, and Domain policies describe validation rules and possible errors. The API publishes them through `/orders-documentation/flows` and `/orders-documentation/validations`. The operation-link check connects routes to executed flows, policies and acceptance scenarios. Generated OpenAPI and link reports are verification artifacts; see [ADR-0005](docs/adr/0005-generated-contract-documentation.md).

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

An invoice requires a paid order owned by the client and a client data version. Generation reserves a unique `OrderId` in MongoDB, writes a PDF, then marks the reservation completed. The PDF currently lives on the API instance's local filesystem and its response URL is `file://`; use shared durable storage before running multiple API instances. See [the ADR index](docs/adr/README.md) for the implemented decisions and limitations.

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
- For local PDF generation outside the Docker image: PowerShell (`pwsh`) and a working Playwright Chromium installation path; the service invokes `playwright.ps1 install` on first use. The API Dockerfile installs these dependencies.
- ProductsCatalog is needed to create an order. Acceptance tests start `mb0101/product-catalog-api:latest` and SQL Server 2022 with Testcontainers; the seeded catalog IDs are used by those tests. Shopping cart operations and order/invoice reads do not need a live ProductsCatalog request.

### Run locally

The checked-in Compose stack initializes a single-node MongoDB replica set and runs the API. To start just MongoDB for an API process on your host:

```bash
docker compose -f docker-compose.yml up -d mongo-init
export MongoDbSettings__ConnectionString='mongodb://admin:admin123@localhost:27017/?authSource=admin&directConnection=true'
dotnet run --project src/ECommerceStoreInvoice.API --launch-profile http
```

The password in `docker-compose.yml` is a local development example; supply credentials from environment variables or a secret store for another deployment. The API uses database and collection names from [`appsettings.json`](src/ECommerceStoreInvoice.API/appsettings.json), creates indexes at startup and fails startup if MongoDB initialization fails. The host command expects port `27017` to be free. [`.env.example`](.env.example) shows the environment variable for an unauthenticated local MongoDB, but that server must still be a replica set for checkout transactions and readiness. If a replica set advertises an address not reachable from the host, configure its advertised address or run the API in the same Docker network.

The host API listens at <http://localhost:5039> with the `http` profile. `ExternalServices:ProductCatalog:BaseUrl` defaults to `http://localhost:5000` in application settings; override it with `ExternalServices__ProductCatalog__BaseUrl` if ProductsCatalog listens elsewhere. The ProductsCatalog container and SQL Server are managed automatically by the acceptance tests, not by this Compose file. If running the API container from Compose, set the ProductsCatalog URL to a network-reachable address; `localhost` inside the API container points back to the API container.

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

The public route groups are `/shopping-carts`, `/orders`, `/invoices` and `/client-data-versions`. `POST /orders/client/{clientId}` creates an order from a cart; the older `POST /orders/{clientId}` alias remains callable but is excluded from OpenAPI. `GET /orders/{orderId}` includes status, total amount, currency and snapshot-backed lines. Its total is a decimal amount in the product currency; do not treat it as integer minor units. The response mapping currently takes the first line's currency, so mixed-currency orders require a separate domain decision before a payment integration assumes a single currency.

Validation and malformed JSON return `400`, missing resources `404`, duplicate invoices/carts and stale order writes `409`, and unexpected errors `500`. Business errors use `application/problem+json`; unexpected errors have a generic detail and a trace ID. Probe responses use the ASP.NET health-check format rather than the business problem contract. For the exact request/response DTOs and status declarations, use generated OpenAPI. Acceptance tests assert observed HTTP responses against the generated specification; this validates exercised responses and is not a proof of all possible runtime paths.

## Health checks

| Route | Meaning |
| --- | --- |
| `/health/live` | Process liveness, independent of MongoDB. |
| `/health/ready` | Writable MongoDB replica-set primary, sessions and database ping; returns `503` when transactional writes are unavailable. |
| `/health` | Compatibility alias for liveness. |

Use readiness for traffic routing and liveness for process checks. These probes do not test ProductsCatalog availability or PDF storage.

## Tests

Run from the repository root:

```bash
bash scripts/verify.sh
```

The script checks architecture and operation links, restores, builds, verifies formatting of all handwritten C# files (generated Reqnroll `.feature.cs` files are excluded), runs Domain, Application, Infrastructure, ExternalProviders and Acceptance suites, exports and lints generated OpenAPI, then builds the Docker image. Testcontainers require Docker; order scenarios start MongoDB, ProductsCatalog and SQL Server. Output is written under `artifacts/verification/`.

For a focused Infrastructure run:

```bash
dotnet test tests/ECommerceStoreInvoice.Infrastructure.UnitTests/ECommerceStoreInvoice.Infrastructure.UnitTests.csproj --configuration Release
```

Infrastructure tests check MongoDB indexes, mapping, transactions and readiness. ExternalProviders tests exercise the ProductsCatalog adapter; checkout acceptance scenarios use MongoDB, ProductsCatalog and SQL Server Testcontainers. Performance benchmarks in `tests/ECommerceStoreInvoice.Performance.Benchmarks/` are outside the five required CI suites. Generated `.feature.cs` files are build outputs and should not be edited manually.

## CI

CI on `master` and pull requests runs build/OpenAPI, five separate test suites with TRX summaries, secret scanning, PR dependency review, a quality gate and an image build with Trivy scanning. Restore audits direct and transitive NuGet dependencies; high/critical findings (`NU1903`/`NU1904`) fail the build while ordinary compiler warnings remain visible. Domain, Application and Infrastructure each require at least 70% line coverage. Infrastructure excludes only the generated Kiota Products client through its [coverage settings](tests/ECommerceStoreInvoice.Infrastructure.UnitTests/coverage.runsettings); repository, mapping, health, configuration, DI and handwritten Products adapter code remain measured. The same checks run through `scripts/verify.sh`. Every required job must pass before the image job starts; the image is not published. See [CI ADR](docs/adr/0006-ci-and-verification.md) and [Definition of Done](docs/definition-of-done.md).

## Operations

Checkout requires a reachable ProductsCatalog API and a MongoDB replica set capable of transactions. A temporary ProductsCatalog outage can prevent creating an order; existing order reads, status changes and invoice generation use persisted data. Startup initializes indexes and fails when MongoDB is unavailable.

Invoice PDFs and their `file://` URLs are local to one API instance. Multiple replicas need shared durable storage and a downloadable public URL before invoice files can be served reliably across instances. The currency returned for an order is taken from the first snapshot line; resolve mixed-currency totals before a payment consumer treats it as one monetary amount.

## Architecture decisions

The [ADR index](docs/adr/README.md) covers MongoDB snapshots, checkout and status writes, PDF recovery, public errors, generated contracts, CI and acceptance isolation. Contributors and coding agents should also read [`AGENTS.md`](AGENTS.md) and [Definition of Done](docs/definition-of-done.md).
