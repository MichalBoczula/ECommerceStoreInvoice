# Health checks

| Route | Healthy | Unhealthy / scope |
| --- | --- | --- |
| `GET /health/live` | `200` when the process handles requests. | Self check only; MongoDB, ProductsCatalog and PDF storage are not probed. |
| `GET /health` | `200`; compatibility alias for liveness. | Same self check as `/health/live`. |
| `GET /health/ready` | `200` when MongoDB `hello` reports a named replica set or `isdbgrid` router, writable primary and positive numeric session timeout, followed by a database ping. | `503` for unavailable primary/router, sessions or ping. The check has a five-second timeout. ProductsCatalog and PDF storage are not probed. |

Probes use ASP.NET Core health-check responses, not the business problem contract. A process that failed startup cannot answer probes: inspect startup logs and MongoDB/index configuration. If readiness is green but checkout fails, inspect ProductsCatalog and actual transaction behavior separately. Production invoice PDFs use private Blob storage; verify upload/download separately. Local-mode `file://` PDFs remain instance-local.

## Verification and limits

[`HealthEndpointTests.cs`](../tests/ECommerceStoreInvoice.Acceptance.Tests/HealthEndpointTests.cs) retains the three original healthy HTTP checks. [Task 12](bdd-12-technical-scenarios.md) adds complete BDD health responses over populated collections, an owned real Mongo stop/restart with readiness 503/recovery and continuing liveness/documentation, and actual standalone Mongo rejection without a readiness ping. A test-only readiness client uses bounded selection/heartbeat settings; production keeps the existing five-second registration timeout and client settings.

[`MongoReadinessHealthCheckTests.cs`](../tests/ECommerceStoreInvoice.Infrastructure.UnitTests/Health/MongoReadinessHealthCheckTests.cs) retains topology/unavailability checks and verifies caller cancellation is rethrown at either hello or ping instead of being converted into an Unhealthy result. Real PDF HTTP cancellation and stream disposal are separate BDD cases. Probes remain the framework's plain Healthy/Unhealthy body rather than business JSON problems. These tests use native Mongo containers and do not prove deployed Azure database or ingress behavior.

Sources: [`Program.cs`](../src/ECommerceStoreInvoice.API/Program.cs), [`MongoReadinessHealthCheck.cs`](../src/ECommerceStoreInvoice.Infrastructure/Health/MongoReadinessHealthCheck.cs), [`MongoInitializer.cs`](../src/ECommerceStoreInvoice.Infrastructure/Configuration/MongoInitializer.cs), [`docker-compose.yml`](../docker-compose.yml).
