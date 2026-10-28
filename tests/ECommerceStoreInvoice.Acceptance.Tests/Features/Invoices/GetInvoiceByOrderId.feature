Feature: Get completed invoice by order id

  @invoice-read-evidence
  Scenario Outline: Order lookup returns only completed invoices
    Explicit complete records and binary artifacts belong to two clients. Both metadata routes expose only completed or legacy records, preserve the stored historical client reference, and leave every populated document and artifact unchanged; absent or invalid order identifiers return exact safe problems.
    Given these complete client versions support invoice reading
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail         |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com    |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Latest   | 50-001     | Wroclaw | River.St | 20             | 8               | 987654321   | 48          | anna.latest@test.com |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com    |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Previous | 50-002     | Wroclaw | Old.St   | 2              | 3               | 555666777   | 48          | anna.old@test.com    |
    And these exact paid order lines and populated carts support invoice reading
      | Order  | Client | Status | OrderCreatedAt       | OrderUpdatedAt       | ProductId                            | IsActive | DeactivatedAt | ProductName                   | Brand  | PriceAmount | Currency | Quantity | CartCreatedAt        | CartUpdatedAt        | CartQuantity |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN      | 2        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z | 5            |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z | 33333333-3333-4333-8333-333333333333 | true     | <null>        | Other.Client.Phone            | Beta   | 99.95       | USD      | 3        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z | 7            |
    And invoice read state "<state>" selects this complete record and artifact contract
      | State        | Document     | GenerationStatus | Pdf          | ClientVersion | CreatedAt            | OtherCreatedAt       |
      | Completed    | present      | Completed        | present      | a-old         | 2024-01-06T10:00:00Z | 2024-01-07T10:00:00Z |
      | legacy       | present      | absent           | present      | a-old         | 2024-01-06T10:00:00Z | 2024-01-07T10:00:00Z |
      | Generating   | present      | Generating       | present      | a-old         | 2024-01-06T10:00:00Z | 2024-01-07T10:00:00Z |
      | Failed       | present      | Failed           | present      | a-old         | 2024-01-06T10:00:00Z | 2024-01-07T10:00:00Z |
      | missing      | absent       | absent           | absent       | a-old         | 2024-01-06T10:00:00Z | 2024-01-07T10:00:00Z |
      | empty        | absent       | absent           | absent       | a-old         | 2024-01-06T10:00:00Z | 2024-01-07T10:00:00Z |
      | missing-file | present      | Completed        | absent       | a-old         | 2024-01-06T10:00:00Z | 2024-01-07T10:00:00Z |
      | real-created | real-created | Completed        | real-created | a-latest      | generated            | 2024-01-07T10:00:00Z |
    And invoice read problems have these complete signatures
      | Route    | Cause           | StatusCode | ContentType              | Title               | Type                                                        | Detail                                                                                                 | Instance     | TraceId   | ErrorsCount | ErrorName                     | ErrorEntity  | ErrorMessage                   |
      | by-id    | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. |
      | by-id    | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceById.      | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       |
      | by-order | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. |
      | by-order | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceByOrderId. | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       |
      | pdf      | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. |
      | pdf      | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoice.          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       |
      | pdf      | MissingPdf      | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource InvoicePdf identified by id {id} cannot be found in database during action OpenPdf.           | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       |
      | pdf      | StorageFailure  | 500        | application/problem+json | Server error.       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 | An unexpected error occurred.                                                                          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       |
    When I send this invoice read request
      | Field       | Value                        |
      | Method      | GET                          |
      | Route       | by-order                     |
      | Endpoint    | /invoices/by-order/{orderId} |
      | Accept      | application/json             |
      | ReaderFault | none                         |
    Then the invoice read returns this complete outcome
      | Field              | Value                               |
      | StatusCode         | <status>                            |
      | Metadata           | all-five-fields-exact               |
      | ClientVersion      | declared-stored-reference           |
      | PdfBytes           | exact-declared-artifact             |
      | ContentType        | application/pdf                     |
      | ContentDisposition | attachment; invoice-{invoiceId}.pdf |
      | CacheControl       | private, no-store                   |
      | ContentLength      | exact-if-present                    |
    And invoice reading preserves every populated document and storage artifact
      | Field           | Value                        |
      | Collections     | all-five-fields-unchanged    |
      | Artifacts       | bytes-and-metadata-unchanged |
      | CatalogRequests | 0                            |
    And both invoice metadata routes and a repeated primary read preserve the same state
      | Field          | Value                    |
      | MetadataRoutes | both-complete-and-hidden |
      | Repeat         | same-complete-outcome    |
      | GeneratedSetupPdf | exact-real-rendered-download |

    Examples:
      | state      | status | scenarioId                   |
      | Completed  | 200    | INV01_GetInvoiceByOrderId_01 |
      | legacy     | 200    | INV01_GetInvoiceByOrderId_02 |
      | Generating | 404    | INV01_GetInvoiceByOrderId_03 |
      | Failed     | 404    | INV01_GetInvoiceByOrderId_04 |
      | missing    | 404    | INV01_GetInvoiceByOrderId_05 |
      | empty      | 400    | INV01_GetInvoiceByOrderId_06 |
