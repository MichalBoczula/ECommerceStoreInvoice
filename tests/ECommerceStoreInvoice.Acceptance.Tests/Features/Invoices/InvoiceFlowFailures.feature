Feature: Invoice creation rules
  An invoice requires the correct client, a paid order and client data.

  @invoice-generation-evidence  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_06
  Scenario: An order that has not been paid cannot be invoiced
    The original quantity-two order remains Created. The paid-order rule rejects it with its exact signature before any reservation or PDF attempt; existing populated data remains unchanged.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status  | CreatedAt            | UpdatedAt            |
      | target | A      | Created | 2024-01-06T08:00:00Z | <null>               |
      | other  | B      | Paid    | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
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
      | Field        | Value                                                       |
      | StatusCode   | 400                                                         |
      | ContentType  | application/problem+json                                    |
      | Title        | Validation failed.                                          |
      | Detail       | One or more validation errors occurred.                     |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance     | request-path                                                |
      | TraceId      | generated                                                   |
      | ErrorsCount  | 1                                                           |
      | ErrorName    | InvoiceOrderStatusValidationRule                            |
      | ErrorEntity  | Order                                                       |
      | ErrorMessage | Invoice can be created only for paid orders.                |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | unchanged            |
      | ClaimsAttempted                | 0                    |
      | ClaimsWon                      | 0                    |
      | PdfAttempts                    | 0                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 1                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_07
  Scenario: Another client's order cannot be invoiced
    A different client has its own versions, paid order and completed invoice, but requests the target owner order. The safe response matches a missing-order problem without revealing ownership or changing stored state.
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
      | Client   | B                              |
      | Order    | target                         |
    Then invoice generation returns this exact safe problem
      | Field       | Value                                                                                                                         |
      | StatusCode  | 404                                                                                                                           |
      | ContentType | application/problem+json                                                                                                      |
      | Title       | Resource not found.                                                                                                           |
      | Detail      | Resource Order with id '{orderId}' was not found. identified by id {orderId} cannot be found in database during action Order. |
      | Type        | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4                                                                   |
      | Instance    | request-path                                                                                                                  |
      | TraceId     | generated                                                                                                                     |
      | ErrorsCount | absent                                                                                                                        |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | unchanged            |
      | ClaimsAttempted                | 0                    |
      | ClaimsWon                      | 0                    |
      | PdfAttempts                    | 0                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 1                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_08
  Scenario: A paid order without a client data version cannot be invoiced
    The target owner has no client version while the other client has multiple versions and an invoice. Creation returns the exact missing-client-version problem before reserving or rendering; all existing records remain exact.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail     |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com |
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
      | Field       | Value                                                                                                  |
      | StatusCode  | 404                                                                                                    |
      | ContentType | application/problem+json                                                                               |
      | Title       | Resource not found.                                                                                    |
      | Detail      | Resource ClientDataVersion identified by id {clientId} cannot be found in database during action Load. |
      | Type        | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4                                            |
      | Instance    | request-path                                                                                           |
      | TraceId     | generated                                                                                              |
      | ErrorsCount | absent                                                                                                 |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | unchanged            |
      | ClaimsAttempted                | 0                    |
      | ClaimsWon                      | 0                    |
      | PdfAttempts                    | 0                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 1                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_09
  Scenario: An empty client identifier is invalid
    The original empty client identifier is rejected before reading or claiming an existing paid order. The complete validation problem is checked and every populated collection is preserved.
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
      | Client   | empty                          |
      | Order    | target                         |
    Then invoice generation returns this exact safe problem
      | Field        | Value                                                       |
      | StatusCode   | 400                                                         |
      | ContentType  | application/problem+json                                    |
      | Title        | Validation failed.                                          |
      | Detail       | One or more validation errors occurred.                     |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance     | request-path                                                |
      | TraceId      | generated                                                   |
      | ErrorsCount  | 1                                                           |
      | ErrorName    | ClientIdIsEmptyValidationRule                               |
      | ErrorEntity  | ShoppingCart                                                |
      | ErrorMessage | ClientId cannot be empty Guid.                              |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | unchanged            |
      | ClaimsAttempted                | 0                    |
      | ClaimsWon                      | 0                    |
      | PdfAttempts                    | 0                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 1                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |

  @invoice-generation-evidence  @allure.label.scenarioId:INV07_CreateInvoice_Cancelled
  Scenario: A cancelled order cannot be invoiced
    A terminal Cancelled order fails the same paid-order rule without reserving or rendering. Its existing timestamp, all snapshots, client histories, carts and the unrelated completed invoice remain unchanged.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status    | CreatedAt            | UpdatedAt            |
      | target | A      | Cancelled | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid      | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
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
      | Field        | Value                                                       |
      | StatusCode   | 400                                                         |
      | ContentType  | application/problem+json                                    |
      | Title        | Validation failed.                                          |
      | Detail       | One or more validation errors occurred.                     |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance     | request-path                                                |
      | TraceId      | generated                                                   |
      | ErrorsCount  | 1                                                           |
      | ErrorName    | InvoiceOrderStatusValidationRule                            |
      | ErrorEntity  | Order                                                       |
      | ErrorMessage | Invoice can be created only for paid orders.                |
    And invoice generation has exactly this database and attempt effect
      | Field                          | Value                |
      | Effect                         | unchanged            |
      | ClaimsAttempted                | 0                    |
      | ClaimsWon                      | 0                    |
      | PdfAttempts                    | 0                    |
      | Completions                    | 0                    |
      | CompletionAcknowledgementsLost | 0                    |
      | Releases                       | 0                    |
      | Deletes                        | 0                    |
      | FailedPdf                      | none                 |
      | Invoices                       | 1                    |
      | OtherCollections               | all-fields-unchanged |
      | CatalogRequests                | 0                    |
    And all invoice visibility and client version reads preserve stored state
      | Field         | Value              |
      | InvoiceRoutes | by-id-and-by-order |
      | Incomplete    | hidden             |
      | Collections   | all-five-unchanged |
