# Health checks

| Route | Healthy | Unhealthy / scope |
| --- | --- | --- |
| `GET /health/live` | `200` when the process handles requests. | Self check only; MongoDB, ProductsCatalog and PDF storage are not probed. |
| `GET /health` | `200`; compatibility alias for liveness. | Same self check as `/health/live`. |
| `GET /health/ready` | `200` when MongoDB `hello` reports a named replica set, writable primary and logical sessions, followed by a ping of the configured database. | `503` for an unhealthy check, including unavailable primary, sessions or ping. The Mongo check has a five-second timeout. ProductsCatalog and PDF storage are not probed. |

Probes use ASP.NET Core health-check responses, not the business `application/problem+json` error contract. A process that failed startup cannot answer probes: inspect startup logs and MongoDB/index configuration. If liveness is green but readiness is `503`, inspect replica-set status, primary election, sessions and connectivity. If readiness is green but checkout fails, inspect ProductsCatalog separately. An invoice's `file://` PDF remains local to one API instance.

## Verification and limits

[`HealthEndpointTests.cs`](../tests/ECommerceStoreInvoice.Acceptance.Tests/HealthEndpointTests.cs) checks successful `/health`, `/health/live` and `/health/ready` HTTP responses with MongoDB available. [`MongoReadinessHealthCheckTests.cs`](../tests/ECommerceStoreInvoice.Infrastructure.UnitTests/Health/MongoReadinessHealthCheckTests.cs) exercises the unavailable-MongoDB branch at the health-check level. There is no HTTP acceptance test that forces `/health/ready` to `503` or proves recovery after failed production startup.

Sources: [`Program.cs`](../src/ECommerceStoreInvoice.API/Program.cs), [`MongoReadinessHealthCheck.cs`](../src/ECommerceStoreInvoice.Infrastructure/Health/MongoReadinessHealthCheck.cs), [`MongoInitializer.cs`](../src/ECommerceStoreInvoice.Infrastructure/Configuration/MongoInitializer.cs), [`docker-compose.yml`](../docker-compose.yml).
