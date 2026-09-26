# Local startup and failure behavior

## Startup sequence

1. Set `ExternalServices:ProductCatalog:BaseUrl` to a non-empty URL. The ProductsCatalog HTTP client uses this URL with a 15-second timeout. Startup validates the setting's presence, but does not call the catalog.
2. Configure `MongoDbSettings:ConnectionString` as a `mongodb://` or `mongodb+srv://` URI. Before serving requests, initialize the unique cart `ClientId` and invoice `OrderId` indexes and the `(ClientId asc, CreatedAt desc)` client-data-version index. An unavailable database or index error fails startup. The `OpenApiExport` environment skips this initialization to export the spec without MongoDB; it is not a serving configuration.
3. Serve the routes. Checkout requires sessions and transactions on a writable MongoDB replica-set primary. A standalone MongoDB may permit index creation yet fail readiness and checkout.

The checked-in Compose stack initializes replica set `rs0`, waits for a writable primary, then starts the API. Its credentials are local examples. A host process must reach the replica set's advertised member address. Configure the ProductsCatalog URL to be reachable **from the API process** before creating orders; Compose does not start ProductsCatalog or SQL Server. See [local startup commands](../README.md#local-startup).

Use [`/health/ready`](health-checks.md) to route traffic after startup. Startup/index failures require correcting the MongoDB connection, permissions or conflicting index definitions and restarting the process. A later MongoDB outage is reflected by readiness while liveness remains process-only.

[`MongoInitializerTests.cs`](../tests/ECommerceStoreInvoice.Infrastructure.UnitTests/Integration/Tests/MongoInitializerTests.cs) verifies index creation. There is no test proving recovery after a failed production startup. See [probe behavior and its verification](health-checks.md).
