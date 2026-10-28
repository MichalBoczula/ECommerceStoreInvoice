Feature: Download invoice PDF

  @invoice-read-evidence
  Scenario Outline: Only completed invoices with an available PDF can be downloaded
    A valid one-page binary PDF fixture includes non-UTF8 bytes and belongs to an explicit completed or incomplete invoice. Successful download and repetition must match every stored byte, filename, media and private/no-store header; other states return complete safe problems without changing populated MongoDB or storage.
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
      | Route    | Cause           | StatusCode | ContentType              | Title               | Type                                                        | Detail                                                                                                 | Instance     | TraceId   | ErrorsCount | ErrorName                     | ErrorEntity  | ErrorMessage                   | CacheControl       |
      | by-id    | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-id    | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceById.      | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | by-order | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-order | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceByOrderId. | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | pdf      | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoice.          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | MissingPdf      | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource InvoicePdf identified by id {id} cannot be found in database during action OpenPdf.           | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | StorageFailure  | 500        | application/problem+json | Server error.       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 | An unexpected error occurred.                                                                          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
    When I send this invoice read request
      | Field       | Value                     |
      | Method      | GET                       |
      | Route       | pdf                       |
      | Endpoint    | /invoices/{invoiceId}/pdf |
      | Accept      | application/pdf           |
      | ReaderFault | none                      |
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
      | Field             | Value                        |
      | MetadataRoutes    | both-complete-and-hidden     |
      | Repeat            | same-complete-outcome        |
      | GeneratedSetupPdf | exact-real-rendered-download |

    Examples:
      | state        | status | scenarioId                  |
      | Completed    | 200    | INV01_DownloadInvoicePdf_01 |
      | Generating   | 404    | INV01_DownloadInvoicePdf_02 |
      | Failed       | 404    | INV01_DownloadInvoicePdf_03 |
      | missing      | 404    | INV01_DownloadInvoicePdf_04 |
      | missing-file | 404    | INV01_DownloadInvoicePdf_05 |
      | empty        | 400    | INV01_DownloadInvoicePdf_06 |

  @invoice-read-evidence @pdf-download-fails @allure.label.scenarioId:INV01_DownloadInvoicePdf_07
  Scenario: Storage failure produces a safe error
    The existing scenario-local PDF reader wrapper throws before local file access. Both downloads return the exact generic server problem, with no PDF headers or private exception detail; the original file bytes, metadata and all five populated collections stay unchanged.
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
    And invoice read state "Completed" selects this complete record and artifact contract
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
      | Route    | Cause           | StatusCode | ContentType              | Title               | Type                                                        | Detail                                                                                                 | Instance     | TraceId   | ErrorsCount | ErrorName                     | ErrorEntity  | ErrorMessage                   | CacheControl       |
      | by-id    | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-id    | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceById.      | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | by-order | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-order | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceByOrderId. | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | pdf      | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoice.          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | MissingPdf      | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource InvoicePdf identified by id {id} cannot be found in database during action OpenPdf.           | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | StorageFailure  | 500        | application/problem+json | Server error.       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 | An unexpected error occurred.                                                                          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
    When I send this invoice read request
      | Field       | Value                     |
      | Method      | GET                       |
      | Route       | pdf                       |
      | Endpoint    | /invoices/{invoiceId}/pdf |
      | Accept      | application/pdf           |
      | ReaderFault | simulated-reader          |
    Then the invoice read returns this complete outcome
      | Field              | Value                               |
      | StatusCode         | 500                                 |
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
      | Field             | Value                        |
      | MetadataRoutes    | both-complete-and-hidden     |
      | Repeat            | same-complete-outcome        |
      | GeneratedSetupPdf | exact-real-rendered-download |

  @invoice-read-evidence  @allure.label.scenarioId:INV08_DownloadInvoicePdf_Legacy
  Scenario: A legacy completed invoice streams its exact binary PDF
    The stored invoice has no generation fields. Both metadata routes still return its exact historical reference, PDF downloads return the complete binary fixture and headers, and no read writes generation defaults back to MongoDB.
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
    And invoice read state "legacy" selects this complete record and artifact contract
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
      | Route    | Cause           | StatusCode | ContentType              | Title               | Type                                                        | Detail                                                                                                 | Instance     | TraceId   | ErrorsCount | ErrorName                     | ErrorEntity  | ErrorMessage                   | CacheControl       |
      | by-id    | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-id    | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceById.      | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | by-order | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-order | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceByOrderId. | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | pdf      | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoice.          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | MissingPdf      | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource InvoicePdf identified by id {id} cannot be found in database during action OpenPdf.           | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | StorageFailure  | 500        | application/problem+json | Server error.       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 | An unexpected error occurred.                                                                          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
    When I send this invoice read request
      | Field       | Value                     |
      | Method      | GET                       |
      | Route       | pdf                       |
      | Endpoint    | /invoices/{invoiceId}/pdf |
      | Accept      | application/pdf           |
      | ReaderFault | none                      |
    Then the invoice read returns this complete outcome
      | Field              | Value                               |
      | StatusCode         | 200                                 |
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
      | Field             | Value                        |
      | MetadataRoutes    | both-complete-and-hidden     |
      | Repeat            | same-complete-outcome        |
      | GeneratedSetupPdf | exact-real-rendered-download |

  @invoice-read-evidence @invoice-blob-read @allure.label.scenarioId:INV08_DownloadInvoicePdf_BlobSuccess
  Scenario: A private Blob invoice streams exact bytes through the API
    An owned private Azurite container holds explicit binary PDFs using the actual Blob SDK. The API uses the real Blob adapter and streams the target bytes with the public download headers while both stored objects, Blob metadata and every Mongo document remain unchanged.
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
    And invoice read state "Completed" selects this complete record and artifact contract
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
      | Route    | Cause           | StatusCode | ContentType              | Title               | Type                                                        | Detail                                                                                                 | Instance     | TraceId   | ErrorsCount | ErrorName                     | ErrorEntity  | ErrorMessage                   | CacheControl       |
      | by-id    | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-id    | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceById.      | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | by-order | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-order | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceByOrderId. | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | pdf      | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoice.          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | MissingPdf      | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource InvoicePdf identified by id {id} cannot be found in database during action OpenPdf.           | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | StorageFailure  | 500        | application/problem+json | Server error.       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 | An unexpected error occurred.                                                                          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
    When I send this invoice read request
      | Field       | Value                     |
      | Method      | GET                       |
      | Route       | pdf                       |
      | Endpoint    | /invoices/{invoiceId}/pdf |
      | Accept      | application/pdf           |
      | ReaderFault | none                      |
    Then the invoice read returns this complete outcome
      | Field              | Value                               |
      | StatusCode         | 200                                 |
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
      | Field             | Value                        |
      | MetadataRoutes    | both-complete-and-hidden     |
      | Repeat            | same-complete-outcome        |
      | GeneratedSetupPdf | exact-real-rendered-download |

  @invoice-read-evidence @invoice-blob-read @allure.label.scenarioId:INV08_DownloadInvoicePdf_BlobMissing
  Scenario: A missing Blob PDF returns the complete safe missing-file problem
    A completed invoice points to a valid attempt name in its private Azurite container, but that object is absent. The real adapter handles the storage miss; metadata remains available and both downloads return the exact safe PDF not-found problem without writes.
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
    And invoice read state "missing-file" selects this complete record and artifact contract
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
      | Route    | Cause           | StatusCode | ContentType              | Title               | Type                                                        | Detail                                                                                                 | Instance     | TraceId   | ErrorsCount | ErrorName                     | ErrorEntity  | ErrorMessage                   | CacheControl       |
      | by-id    | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-id    | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceById.      | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | by-order | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-order | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceByOrderId. | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | pdf      | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoice.          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | MissingPdf      | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource InvoicePdf identified by id {id} cannot be found in database during action OpenPdf.           | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | StorageFailure  | 500        | application/problem+json | Server error.       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 | An unexpected error occurred.                                                                          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
    When I send this invoice read request
      | Field       | Value                     |
      | Method      | GET                       |
      | Route       | pdf                       |
      | Endpoint    | /invoices/{invoiceId}/pdf |
      | Accept      | application/pdf           |
      | ReaderFault | none                      |
    Then the invoice read returns this complete outcome
      | Field              | Value                               |
      | StatusCode         | 404                                 |
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
      | Field             | Value                        |
      | MetadataRoutes    | both-complete-and-hidden     |
      | Repeat            | same-complete-outcome        |
      | GeneratedSetupPdf | exact-real-rendered-download |

  @invoice-read-evidence @invoice-blob-read @invoice-blob-denied @allure.label.scenarioId:INV08_DownloadInvoicePdf_BlobDenied
  Scenario: Blob authentication failure returns a safe server problem without storage changes
    Correct fixture credentials upload private Azurite objects, while the API is deliberately configured with a different valid-shaped test key. The actual Blob adapter receives authentication 403; the API returns only its exact safe 500 problem, preserving every object and document.
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
    And invoice read state "Completed" selects this complete record and artifact contract
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
      | Route    | Cause           | StatusCode | ContentType              | Title               | Type                                                        | Detail                                                                                                 | Instance     | TraceId   | ErrorsCount | ErrorName                     | ErrorEntity  | ErrorMessage                   | CacheControl       |
      | by-id    | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-id    | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceById.      | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | by-order | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | by-order | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoiceByOrderId. | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | InvalidId       | 400        | application/problem+json | Validation failed.  | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 | One or more validation errors occurred.                                                                | request-path | generated | 1           | ClientIdIsEmptyValidationRule | ShoppingCart | ClientId cannot be empty Guid. | no-cache, no-store |
      | pdf      | HiddenOrMissing | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource Invoice identified by id {id} cannot be found in database during action LoadInvoice.          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | MissingPdf      | 404        | application/problem+json | Resource not found. | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 | Resource InvoicePdf identified by id {id} cannot be found in database during action OpenPdf.           | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
      | pdf      | StorageFailure  | 500        | application/problem+json | Server error.       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 | An unexpected error occurred.                                                                          | request-path | generated | absent      | <absent>                      | <absent>     | <absent>                       | no-cache, no-store |
    When I send this invoice read request
      | Field       | Value                     |
      | Method      | GET                       |
      | Route       | pdf                       |
      | Endpoint    | /invoices/{invoiceId}/pdf |
      | Accept      | application/pdf           |
      | ReaderFault | blob-authentication       |
    Then the invoice read returns this complete outcome
      | Field              | Value                               |
      | StatusCode         | 500                                 |
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
      | Field             | Value                        |
      | MetadataRoutes    | both-complete-and-hidden     |
      | Repeat            | same-complete-outcome        |
      | GeneratedSetupPdf | exact-real-rendered-download |
