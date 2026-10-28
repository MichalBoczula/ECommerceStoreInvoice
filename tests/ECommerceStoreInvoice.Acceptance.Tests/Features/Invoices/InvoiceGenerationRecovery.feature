Feature: Invoice generation is single-writer and retryable
  The Order already contains product snapshots; invoice requests use those saved versions.

  @invoice-generation-evidence @pdf-fails-once @allure.label.scenarioId:INV01_CreateInvoiceForOrder_10
  Scenario: A failed PDF generation can be retried for the same order
    The injected first PDF failure leaves a real Failed reservation retaining its attempt identity and creation timestamp. The owner then creates a new client version over HTTP; retry claims the same invoice with a new attempt, uses the newest version and completes a real local PDF without changing older data.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>        | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId | GenerationLeaseUntil |
      | other | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>              | <null>               |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this exact safe problem
      | Field       | Value                                                       |
      | StatusCode  | 500                                                         |
      | ContentType | application/problem+json                                    |
      | Title       | Server error.                                               |
      | Detail      | An unexpected error occurred.                               |
      | Type        | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 |
      | Instance    | request-path                                                |
      | TraceId     | generated                                                   |
      | ErrorsCount | absent                                                      |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | failed               |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 1                    |
      | Deletes                        | 1                    |
      | FailedPdf                      | not-written          |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |
    When the invoice owner appends this new client version before retrying
      | Field           | Value             |
      | Client          | A                 |
      | Version         | a-retry           |
      | ClientName      | John Retry        |
      | PostalCode      | 80-001            |
      | City            | Gdansk            |
      | Street          | New.St            |
      | BuildingNumber  | 31C               |
      | ApartmentNumber | 9                 |
      | PhoneNumber     | 222333444         |
      | PhonePrefix     | 48                |
      | AddressEmail    | retry@example.com |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this complete metadata
      | Field         | Value                     |
      | StatusCode    | 200                       |
      | ContentType   | application/json          |
      | Id            | claimed-invoice           |
      | Order         | target                    |
      | ClientVersion | a-retry                   |
      | StorageUrl    | current-attempt-local-PDF |
      | CreatedAt     | reservation-timestamp     |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | completed            |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 1                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence @invoice-hold-pdf @allure.label.scenarioId:INV01_CreateInvoiceForOrder_11
  Scenario: Concurrent invoice requests create one accessible PDF
    The first request is held only after its real Mongo claim. While that request is in progress, both invoice lookup routes hide its reservation and a second request receives an exact active-claim conflict. Releasing the gate completes exactly one real PDF and one invoice, preserving every unrelated field.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>        | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId | GenerationLeaseUntil |
      | other | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>              | <null>               |
    When two invoice requests overlap while the winning reservation is held
      | Field    | Value                       |
      | Requests | 2                           |
      | Client   | A                           |
      | Order    | target                      |
      | HoldAt   | after-real-claim-before-PDF |
    Then invoice generation returns this complete metadata
      | Field         | Value                     |
      | StatusCode    | 200                       |
      | ContentType   | application/json          |
      | Id            | claimed-invoice           |
      | Order         | target                    |
      | ClientVersion | a-latest                  |
      | StorageUrl    | current-attempt-local-PDF |
      | CreatedAt     | reservation-timestamp     |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | completed            |
      | ClaimsAttempted                | 2                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 1                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence @completion-ack-lost @allure.label.scenarioId:INV01_CreateInvoiceForOrder_12
  Scenario: Lost completion acknowledgement is recovered from Mongo
    Actual Mongo completion commits before the existing wrapper throws its acknowledgement failure. The observed Completed document is reconciled to a successful response, with no release or deletion and with its exact generated PDF preserved.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>        | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId | GenerationLeaseUntil |
      | other | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>              | <null>               |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this complete metadata
      | Field         | Value                     |
      | StatusCode    | 200                       |
      | ContentType   | application/json          |
      | Id            | claimed-invoice           |
      | Order         | target                    |
      | ClientVersion | a-latest                  |
      | StorageUrl    | current-attempt-local-PDF |
      | CreatedAt     | reservation-timestamp     |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | completed            |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 1                    |
      | CompletionAcknowledgementsLost | 1                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence  @allure.label.scenarioId:INV07_CreateInvoice_ActiveReservation
  Scenario: An active invoice reservation rejects a new generation attempt
    A historical live reservation is hidden by both lookup routes. A new request loses the real claim, receives the complete active-generation conflict, makes no PDF attempt and preserves every reservation field and populated collection.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>        | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order  | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId              | GenerationLeaseUntil |
      | other  | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>                           | <null>               |
      | target | a-old         | <empty>                                   | 2024-01-06T10:00:00Z | Generating       | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | future:+5-minutes    |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this exact safe problem
      | Field       | Value                                                                                                                                                            |
      | StatusCode  | 409                                                                                                                                                              |
      | ContentType | application/problem+json                                                                                                                                         |
      | Title       | Conflict.                                                                                                                                                        |
      | Detail      | Resource Invoice generation for order '{orderId}' is already in progress or completed. identified by id {orderId} already exists in db. Error in action Invoice. |
      | Type        | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8                                                                                                      |
      | Instance    | request-path                                                                                                                                                     |
      | TraceId     | generated                                                                                                                                                        |
      | ErrorsCount | absent                                                                                                                                                           |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | unchanged            |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 0                    |
      | PdfAttempts                    | 0                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence  @allure.label.scenarioId:INV07_CreateInvoice_FailedReservation
  Scenario: A failed reservation is reclaimed with latest client data and saved product lines
    A Failed reservation references older client data. A real reclaim keeps its invoice identity and original creation time, changes to a new attempt and the owner latest version, then renders exact multiple saved product inputs including an inactive snapshot and completes the same invoice.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt        | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>               | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | target | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | false    | 2024-01-06T08:00:00Z | 2024-01-08T08:00:00Z | Historical.Second.Product     | Alpha  | 19.95       | PLN           | 3        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>               | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order  | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId              | GenerationLeaseUntil |
      | other  | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>                           | <null>               |
      | target | a-old         | <empty>                                   | 2024-01-06T10:00:00Z | Failed           | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | <null>               |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this complete metadata
      | Field         | Value                     |
      | StatusCode    | 200                       |
      | ContentType   | application/json          |
      | Id            | claimed-invoice           |
      | Order         | target                    |
      | ClientVersion | a-latest                  |
      | StorageUrl    | current-attempt-local-PDF |
      | CreatedAt     | reservation-timestamp     |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | completed            |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 1                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence  @allure.label.scenarioId:INV07_CreateInvoice_ExpiredReservation
  Scenario: An expired generation lease can be reclaimed and completed
    A Generating reservation has an explicitly expired lease and old attempt. Real Mongo reclaim must keep the reservation identity and creation timestamp, replace only the eligible generation fields and use the latest owner data and exact immutable multi-product inputs.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt        | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>               | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | target | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | false    | 2024-01-06T08:00:00Z | 2024-01-08T08:00:00Z | Historical.Second.Product     | Alpha  | 19.95       | PLN           | 3        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>               | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order  | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId              | GenerationLeaseUntil |
      | other  | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>                           | <null>               |
      | target | a-old         | <empty>                                   | 2024-01-06T10:00:00Z | Generating       | aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa | 2024-01-08T08:00:00Z |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this complete metadata
      | Field         | Value                     |
      | StatusCode    | 200                       |
      | ContentType   | application/json          |
      | Id            | claimed-invoice           |
      | Order         | target                    |
      | ClientVersion | a-latest                  |
      | StorageUrl    | current-attempt-local-PDF |
      | CreatedAt     | reservation-timestamp     |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | completed            |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 1                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence @invoice-fails-after-write @allure.label.scenarioId:INV07_CreateInvoice_WrittenPdfFailure
  Scenario: A failed attempt deletes its written PDF and retries with a different attempt
    A scenario-local wrapper throws only after real Chromium writes the first attempt PDF. Recovery releases the reservation and deletes that exact artifact; retry reuses the invoice identity with a new attempt and completes a distinct available PDF while the failed file stays absent.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt        | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>               | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | target | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | false    | 2024-01-06T08:00:00Z | 2024-01-08T08:00:00Z | Historical.Second.Product     | Alpha  | 19.95       | PLN           | 3        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>               | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId | GenerationLeaseUntil |
      | other | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>              | <null>               |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this exact safe problem
      | Field       | Value                                                       |
      | StatusCode  | 500                                                         |
      | ContentType | application/problem+json                                    |
      | Title       | Server error.                                               |
      | Detail      | An unexpected error occurred.                               |
      | Type        | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1 |
      | Instance    | request-path                                                |
      | TraceId     | generated                                                   |
      | ErrorsCount | absent                                                      |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | failed               |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 1                    |
      | Deletes                        | 1                    |
      | FailedPdf                      | written              |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |
    When I send this invoice generation request
      | Field    | Value                          |
      | Method   | POST                           |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | HasBody  | false                          |
      | Client   | A                              |
      | Order    | target                         |
    Then invoice generation returns this complete metadata
      | Field         | Value                     |
      | StatusCode    | 200                       |
      | ContentType   | application/json          |
      | Id            | claimed-invoice           |
      | Order         | target                    |
      | ClientVersion | a-latest                  |
      | StorageUrl    | current-attempt-local-PDF |
      | CreatedAt     | reservation-timestamp     |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | completed            |
      | ClaimsAttempted                | 1                    |
      | ClaimsWon                      | 1                    |
      | PdfAttempts                    | 1                    |
      | Completions                    | 1                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 2                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |
