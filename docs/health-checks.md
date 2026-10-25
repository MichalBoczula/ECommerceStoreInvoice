# Health checks

| Route | Healthy | Unhealthy / scope |
| --- | --- | --- |
| `GET /health/live` | `200` when the process handles requests. | Self check only; MongoDB, ProductsCatalog and PDF storage are not probed. |
| `GET /health` | `200`; compatibility alias for liveness. | Same self check as `/health/live`. |
| `GET /health/ready` | `200` when MongoDB `hello` reports a named replica set or `isdbgrid` router, writable primary and positive numeric session timeout, followed by a database ping. | `503` for unavailable primary/router, sessions or ping. The check has a five-second timeout. ProductsCatalog and PDF storage are not probed. |

Probes use ASP.NET Core health-check responses, not the business problem contract. A process that failed startup cannot answer probes: inspect startup logs and MongoDB/index configuration. If readiness is green but checkout fails, inspect ProductsCatalog and actual transaction behavior separately. Production invoice PDFs use private Blob storage; verify upload/download separately. Local-mode `file://` PDFs remain instance-local.

## Verification and limits

[`HealthEndpointTests.cs`](../tests/ECommerceStoreInvoice.Acceptance.Tests/HealthEndpointTests.cs) checks successful `/health`, `/health/live` and `/health/ready` HTTP responses with MongoDB available. [`MongoReadinessHealthCheckTests.cs`](../tests/ECommerceStoreInvoice.Infrastructure.UnitTests/Health/MongoReadinessHealthCheckTests.cs) exercises the unavailable-MongoDB branch at the health-check level. There is no HTTP acceptance test that forces `/health/ready` to `503` or proves recovery after failed production startup.

Sources: [`Program.cs`](../src/ECommerceStoreInvoice.API/Program.cs), [`MongoReadinessHealthCheck.cs`](../src/ECommerceStoreInvoice.Infrastructure/Health/MongoReadinessHealthCheck.cs), [`MongoInitializer.cs`](../src/ECommerceStoreInvoice.Infrastructure/Configuration/MongoInitializer.cs), [`docker-compose.yml`](../docker-compose.yml).
