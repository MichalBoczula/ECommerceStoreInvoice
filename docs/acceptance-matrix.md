# HTTP cause-to-response-to-scenario matrix

This matrix tracks distinct **exercised causes** against business operations. Links point to `.feature` source or a direct xUnit HTTP test. A listed status does not mean every possible cause of that status is covered. For payload fields see the [public error contract](api-problem-contract.md). `POST /orders/client/{clientId}` is canonical; some scenarios call the supported `/orders/{clientId}` alias, which is excluded from generated OpenAPI.

The machine-checked [case matrix](acceptance-matrix.tsv) links every current
expanded BDD case, including shared failure and recovery features. The
[baseline](bdd-01-baseline.md) preserves original behavioral examples. Explicit
[coverage gaps](acceptance-gaps.tsv) distinguish absent or conditional evidence
from guaranteed scenario intent; they must not be silently counted as coverage.

## Shopping carts

| Operation | Cause → response | Acceptance evidence |
| --- | --- | --- |
| `GET /shopping-carts/client/{clientId}` | Existing cart → `200`; empty GUID → `400`; no cart → `404` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/GetShoppingCartSuccess/GetShoppingCartSuccess.feature), [empty ID](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/ShoppingCartFlowFailures.feature), [missing](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/GetShoppingCartNotFound/GetShoppingCartNotFound.feature) |
| `POST /shopping-carts/{clientId}` | New cart → `200`; empty GUID → `400`; duplicate cart → `409` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/CreateShoppingCartSuccess/CreateShoppingCartSuccess.feature), [validation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/CreateShoppingCartValidationError/CreateShoppingCartValidationError.feature), [duplicate](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/ShoppingCartFlowFailures.feature) |
| `PUT /shopping-carts/{clientId}` | Valid lines → `200`; zero quantity → `400`; no cart → `404`; malformed or missing body → `400` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/UpdateShoppingCartSuccess/UpdateShoppingCartSuccess.feature), [validation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/UpdateShoppingCartValidationError/UpdateShoppingCartValidationError.feature), [missing](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ShoppingCarts/UpdateShoppingCartNotFound/UpdateShoppingCartNotFound.feature), [body tests](../tests/ECommerceStoreInvoice.Acceptance.Tests/RequestBodyErrorTests.cs) |

## Orders

| Operation | Cause → response | Acceptance evidence |
| --- | --- | --- |
| `POST /orders/client/{clientId}` | Cart with catalog product → `200`; empty cart → `400`; missing cart → `404`; missing catalog product → `404`, with cart/order state checked | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/CreateOrderSuccess/CreateOrderSuccess.feature), [empty cart](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/CreateOrderValidationError/CreateOrderValidationError.feature), [missing cart](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/CreateOrderNotFound/CreateOrderNotFound.feature), [missing product](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/CreateOrderMissingProduct.feature) |
| `GET /orders/{orderId}` | Existing order → `200`; empty GUID → `400`; missing order → `404` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/GetOrdersByIdSuccess/GetOrdersByIdSuccess.feature), [validation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/GetOrdersByIdValidationError/GetOrdersByIdValidationError.feature), [missing](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/GetOrdersByIdNotFound/GetOrdersByIdNotFound.feature) |
| `GET /orders/client/{clientId}` | Orders or empty list → `200`; empty GUID → `400` | [Orders](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/GetOrdersByClientIdSuccess/GetOrdersByClientIdSuccess.feature), [empty list](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/GetOrdersByClientIdEmptySuccess/GetOrdersByClientIdEmptySuccess.feature), [validation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/GetOrdersByClientIdValidationError/GetOrdersByClientIdValidationError.feature) |
| `PATCH /orders/{orderId}/status` | `Created → Paid/Cancelled` → `200`; unknown/unchanged/terminal status or empty GUID → `400`; missing order → `404`; concurrent changes → one `200`, the other `400` or `409` depending on when it reads the order; deterministic held writes guarantee the stale `409` | [Status scenarios](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Orders/UpdateOrderStatus.feature), including repeat `Paid` → `400` |

## Invoices

`GET /invoices/{invoiceId}/pdf` covers completed PDF download (`200`, binary PDF with attachment/private caching), empty ID (`400`), missing/Generating/Failed invoice or missing file (`404`), and a storage failure (`500`, safe problem response). See [download scenarios](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/DownloadInvoicePdf.feature). Real Blob protocol, private access, immutable attempt uploads, cleanup and storage-adapter replacement are exercised with Azurite in Infrastructure tests; deployed Azure verification remains separate.

| Operation | Cause → response | Acceptance evidence |
| --- | --- | --- |
| `POST /invoices/{clientId}/{orderId}` | Paid order with client data → `200`; empty ID or unpaid order → `400`; missing order, another client's order or missing client data → `404`; duplicate invoice → `409` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/CreateInvoiceForOrderSuccess/CreateInvoiceForOrderSuccess.feature), [invalid ID](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/CreateInvoiceForOrderValidationError/CreateInvoiceForOrderValidationError.feature), [rules](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/InvoiceFlowFailures.feature), [missing order](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/CreateInvoiceForOrderNotFound/CreateInvoiceForOrderNotFound.feature), [duplicate](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/CreateInvoiceForOrderConflict/CreateInvoiceForOrderConflict.feature) |
| `POST /invoices/{clientId}/{orderId}` | PDF fails once → safe `500`, then retry succeeds; concurrent requests → one `200`, one `409` and one completed invoice; lost completion acknowledgment → completed invoice returned | [Recovery and concurrency](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/InvoiceGenerationRecovery.feature). The accessible PDF is local to the test instance. |
| `GET /invoices/{invoiceId}` | Completed invoice → `200`; empty GUID → `400`; missing invoice → `404` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/GetInvoiceByIdSuccess/GetInvoiceByIdSuccess.feature), [validation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/GetInvoiceByIdValidationError/GetInvoiceByIdValidationError.feature), [missing](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/GetInvoiceByIdNotFound/GetInvoiceByIdNotFound.feature) |

## Client data versions and documentation

| Operation | Cause → response | Acceptance evidence |
| --- | --- | --- |
| `POST /client-data-versions/{clientId}` | Valid version → `200`; nondigit phone number → `400` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ClientDataVersions/CreateClientDataVersionSuccess/CreateClientDataVersionSuccess.feature), [validation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ClientDataVersions/CreateClientDataVersionValidationError/CreateClientDataVersionValidationError.feature) |
| `GET /client-data-versions/client/{clientId}` | Latest version → `200`; empty GUID → `400`; no version → `404` | [Success](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ClientDataVersions/GetClientDataVersionSuccess/GetClientDataVersionSuccess.feature), [validation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ClientDataVersions/GetClientDataVersionValidationError/GetClientDataVersionValidationError.feature), [missing](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/ClientDataVersions/GetClientDataVersionNotFound/GetClientDataVersionNotFound.feature) |
| `GET /orders-documentation/flows` | Descriptors → `200` | [Flow documentation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Documentation/GetFlowSuccess/GetFlowSuccess.feature) |
| `GET /orders-documentation/validations` | Policies → `200` | [Validation documentation](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Documentation/GetValidationsSuccess/GetValidationsSuccess.feature) |

## Shared failure and probe paths

For the 13 operations listed in the shared failure outline, an **injected dependency exception** returns a generic `500` with trace ID and no internal message: [UnexpectedServerErrors.feature](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Common/UnexpectedServerErrors.feature). These are injected failures, not evidence for every database, catalog or PDF failure mode. DownloadInvoicePdf has its separate storage-failure case; GetInvoiceByOrderId now has its declared safe 500 case (INV-BDD-10). INV-BDD-11 guarantees a real stale-write 409 after both requests validate Created, with either status selected as winner. The original optional matrix annotation remains conservative; the new cases close the final declared status gap. The common `CreateOrder` `500` case calls the legacy order route.

The non-business `/health`, `/health/live` and `/health/ready` probes have successful `200` HTTP checks in [HealthEndpointTests.cs](../tests/ECommerceStoreInvoice.Acceptance.Tests/HealthEndpointTests.cs). MongoDB unavailability is covered at the health-check level in [MongoReadinessHealthCheckTests.cs](../tests/ECommerceStoreInvoice.Infrastructure.UnitTests/Health/MongoReadinessHealthCheckTests.cs), not as an HTTP `503` acceptance response. See [startup and health](health-checks.md).

Current distinctions: Task 10 checks binding/media paths on all three body operations and invalid GUID route constraints. Task 09 covers isolated phone/email/address rule branches, accepted boundaries and repeated errors with populated-state evidence; see [business validation](bdd-09-business-validation.md). An all-zero GUID `400` does not verify how `{id:guid}` routing handles non-GUID path text. Keep this matrix aligned with endpoint metadata and `.feature` source as those paths change.

`GET /invoices/by-order/{orderId}`: completed/legacy → 200; empty ID → 400; missing/Generating/Failed → 404. See [lookup scenarios](../tests/ECommerceStoreInvoice.Acceptance.Tests/Features/Invoices/GetInvoiceByOrderId.feature).

## Framework rejection links

[Task 10](bdd-10-framework-failures.md) covers raw malformed/missing/null/wrong-type
bodies, unsupported media and required cart structure on all three JSON-body
operations. Named binding/media failures remain in acceptance-matrix.tsv.
Unmatched routes, invalid GUID constraints and wrong methods have no selected
operation: [framework-scenarios.tsv](framework-scenarios.tsv) separately links all
28 routing cases without inflating business operation/status coverage.


## Deterministic concurrency and rollback

[Task 11](bdd-11-concurrency-atomicity.md) links both status winners to exact 409
problems and complete winning state. Competing checkout transactions each stage
two actual snapshots and an order before one commits; the other's real cart write
conflict uses the existing safe 500 and rolls back all losing writes. Separate
failures after successful snapshot/order/cart writes abort real sessions, preserve
all five populated collections and allow an HTTP retry. A later empty cart is 400.

An expired held invoice attempt is fenced after a replacement completes: its
completion fails with the existing safe 500, release changes nothing and the
winning PDF remains intact. Explicit old-attempt storage cleanup deletes only the
old file. Production keeps ambiguous completion files; automatic orphan cleanup
remains deferred. Existing active-claim 409 and failed-attempt cleanup cases remain.
All 59 named operation/status pairs now have guaranteed intent, distinct from
complete cause coverage. Routing cases remain separately classified.


## Technical outcomes

[Task 12](bdd-12-technical-scenarios.md) uses [technical-scenarios.tsv](technical-scenarios.tsv)
for health/Swagger, host isolation and cancelled PDF reads. Healthy/Unhealthy
probes are plain text with cache protection. Cancelled exchanges have no invented
HTTP status, in particular no synthetic 499, and cannot inflate named status
coverage. Two populated-data documentation cases remain named operation links;
all current flows/policies are compared completely with their source projections.
Actual bootstrap/index failures remain standalone lifecycle tests because no host
serves an HTTP response. Their counts are reported separately from BDD cases.


INV-BDD-13 adds four named serializer cases and one separate technical contract
read: 281 named, 28 framework and eleven technical cases, totaling 320. All fifteen
operations retain 59/59 declared status pairs. Generated schema metadata, all typed
examples, exact safe problem alternatives and binary PDF headers are verified
against actual serializer/type information; guard mutation tests remain standalone.
See [request/response contracts](bdd-13-request-response-contracts.md).
