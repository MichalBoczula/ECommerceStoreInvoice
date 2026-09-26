# ADR 0004: Public HTTP error contract

Status: Accepted (2026-09-26)

## Context

Consumers need distinguishable failure statuses without leaking server exception messages, and OpenAPI metadata must match observable HTTP responses.

## Decision

The API's centralized exception handler maps validation and malformed JSON to `400`, missing resources to `404`, duplicate resources and stale order writes to `409`, and other exceptions to `500`. Errors are serialized as `application/problem+json`. Validation errors carry their rule descriptors; unexpected errors use a generic title/detail and include a request trace ID. The server logs the exception for diagnosis. Endpoint `.Produces` declarations expose the relevant statuses and media type in generated OpenAPI.

Route constraints such as `{orderId:guid}` are part of ASP.NET routing and can reject a malformed path before the domain handler is invoked. Health probes use ASP.NET health responses, outside this business error contract.

## Consequences

Clients may branch on status and documented validation data; human-readable `Detail` text should not become a machine protocol. A repeat `Paid` is a validation `400`, while a concurrent replacement of an order that was `Created` on read is `409`. An unhandled server exception never sends its stack trace or internal message in the response. Acceptance tests verify the status, media type and body for exercised paths.

See `src/ECommerceStoreInvoice.API/Configuration/ExceptionHandler.cs` and the handlers in `Configuration/Extensions`.
