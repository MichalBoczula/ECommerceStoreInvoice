# Public error contract

Business endpoint failures pass through the [central exception handler](../src/ECommerceStoreInvoice.API/Configuration/ExceptionHandler.cs). Use generated [OpenAPI](../README.md#api-contract) for each operation's declared statuses. The handler writes `application/problem+json` for the mapped exceptions below.

| Cause | HTTP | Public response | Log level |
| --- | --- | --- | --- |
| Domain `ValidationException` | `400` | `Validation failed.` with `errors` containing validation-rule descriptors. | Warning |
| `JsonException` or `BadHttpRequestException` with status `400` | `400` | `Invalid JSON payload.` with generic detail and trace ID. | Warning |
| `ResourceNotFoundException` | `404` | `Resource not found.` with resource/action context. | Error |
| `ResourceAlreadyExistsException` | `409` | `Conflict.` with resource/action context. | Warning |
| `OrderWriteConflictException` | `409` | `Conflict.`; reload the order after a concurrent change. | Warning |
| Other unhandled exceptions | `500` | `Server error.` with generic detail and trace ID; exception text stays in logs. | Error |

The problem payload includes `type`, `title`, `status`, `detail`, `instance` and `traceId`. Validation responses additionally carry `errors`; clients should use status and documented validation descriptors rather than match human-readable detail. A repeat `Paid` is a `400`; a stale concurrent order write is `409`. A duplicate invoice or cart is `409`. PDF failure can return a safe `500` with retry behavior described in [ADR-0003](adr/0003-invoice-pdf-and-recovery.md).

`{id:guid}` route constraints can reject malformed path text before the endpoint or exception handler runs. Such routing responses are outside this mapping; a syntactically valid all-zero GUID reaches domain validation and is covered by `400` scenarios. Health probes have their own response format; see [startup and health](health-checks.md).

[`RequestBodyErrorTests.cs`](../tests/ECommerceStoreInvoice.Acceptance.Tests/RequestBodyErrorTests.cs) checks malformed, empty-object and absent bodies on `PUT /shopping-carts/{clientId}`. [`UnexpectedServerErrors.feature`](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Common/UnexpectedServerErrors.feature) injects a dependency failure for each of the 13 named operations and verifies a safe `500`. These checks do not establish that every possible server failure or every endpoint's malformed-body path is tested. Consult the [cause-to-scenario matrix](acceptance-matrix.md) for the exercised paths.
