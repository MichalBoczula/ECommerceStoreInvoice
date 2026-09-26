# ECommerceStoreInvoice

.NET 10 Minimal API for shopping carts, order checkout, client data versions and invoice generation. MongoDB stores the business data; ProductsCatalog supplies product details when an order is created. Order reads and invoice generation use the saved product snapshots, without calling ProductsCatalog again.

## Architecture

| Project | Responsibility |
| --- | --- |
| `src/ECommerceStoreInvoice.API` | Routes, HTTP problem responses, health checks, Swagger and composition. |
| `src/ECommerceStoreInvoice.Application` | Use cases, DTO mapping, invoice PDF generation and executable flow descriptors. |
| `src/ECommerceStoreInvoice.Domain` | Aggregates, repository contracts and validation policies. |
| `src/ECommerceStoreInvoice.Infrastructure` | MongoDB documents, repositories, transactions, indexes, readiness and ProductsCatalog client. |

An order checkout loads the client's cart, fetches the selected products from ProductsCatalog, creates product version snapshots, then writes snapshots, the order and the cleared cart in one MongoDB transaction. A MongoDB replica set with a writable primary and sessions is required. The order status can move from `Created` to `Paid` or `Cancelled` once. Repeating `Paid` yields a validation `400`; a concurrent write against a stale `Created` order yields `409`.

An invoice requires a paid order owned by the client and a client data version. Generation reserves a unique `OrderId` in MongoDB, writes a PDF, then marks the reservation completed. The PDF currently lives on the API instance's local filesystem and its response URL is `file://`; use shared durable storage before running multiple API instances. See [the ADR index](docs/adr/README.md) for the implemented decisions and limitations.

## Prerequisites

- .NET SDK 10, Docker with a running daemon and Docker Compose.
- For full local verification: Bash, Python 3 and Node.js (the OpenAPI lint uses Redocly CLI through `npx`).
- For local PDF generation outside the Docker image: PowerShell (`pwsh`) and a working Playwright Chromium installation path; the service invokes `playwright.ps1 install` on first use. The API Dockerfile installs these dependencies.
- ProductsCatalog is needed to create an order. Acceptance tests start `mb0101/product-catalog-api:latest` and SQL Server 2022 with Testcontainers; the seeded catalog IDs are used by those tests. Shopping cart operations and order/invoice reads do not need a live ProductsCatalog request.

## Local startup

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

`/health` is a compatibility alias for liveness. Readiness checks a writable replica-set primary, sessions and a database ping; it returns `503` when MongoDB cannot serve transactional writes. The probes do not test ProductsCatalog availability or PDF storage. Stop the local stack with `docker compose -f docker-compose.yml down`; add `--volumes` only when you intend to delete local MongoDB data.

## API contract

- Swagger UI: <http://localhost:5039/swagger>
- Generated OpenAPI: <http://localhost:5039/swagger/v1/swagger.json>
- Flow and policy descriptions: `/orders-documentation/flows` and `/orders-documentation/validations`.

The public route groups are `/shopping-carts`, `/orders`, `/invoices` and `/client-data-versions`. `POST /orders/client/{clientId}` creates an order from a cart; the older `POST /orders/{clientId}` alias remains callable but is excluded from OpenAPI. `GET /orders/{orderId}` includes status, total amount, currency and snapshot-backed lines. Its total is a decimal amount in the product currency; do not treat it as integer minor units. The response mapping currently takes the first line's currency, so mixed-currency orders require a separate domain decision before a payment integration assumes a single currency.

Validation and malformed JSON return `400`, missing resources `404`, duplicate invoices/carts and stale order writes `409`, and unexpected errors `500`. Business errors use `application/problem+json`; unexpected errors have a generic detail and a trace ID. Probe responses use the ASP.NET health-check format rather than the business problem contract. For the exact request/response DTOs and status declarations, use generated OpenAPI. Acceptance tests assert observed HTTP responses against the generated specification; this validates exercised responses and is not a proof of all possible runtime paths.

## Verification

Run from the repository root:

```bash
bash scripts/verify.sh
```

The script checks architecture and operation links, restores, builds, verifies solution formatting, runs Domain, Application, Infrastructure, ExternalProviders and Acceptance suites, exports and lints generated OpenAPI, then builds the Docker image. Testcontainers require Docker; order scenarios start MongoDB, ProductsCatalog and SQL Server. Output is written under `artifacts/verification/`.

For a focused Infrastructure run:

```bash
dotnet test tests/ECommerceStoreInvoice.Infrastructure.UnitTests/ECommerceStoreInvoice.Infrastructure.UnitTests.csproj --configuration Release
```

CI on `master` and pull requests runs build/OpenAPI, five separate test suites, secret scanning, PR dependency review, a quality gate and an image build with Trivy scanning. Every required job must pass before the image job starts; the image is not published. **The current workflow does not enforce a numeric coverage threshold**, including for Infrastructure. Coverage reporting and thresholds are a follow-up to the current CI configuration. See [CI ADR](docs/adr/0006-ci-and-verification.md) and [Definition of Done](docs/definition-of-done.md).

## Repository guidance

Contributors and coding agents should read [`AGENTS.md`](AGENTS.md) and [Definition of Done](docs/definition-of-done.md). Flow descriptors and validation policies are executable documentation; generated `.feature.cs` files and exported OpenAPI are outputs, not handwritten sources.
