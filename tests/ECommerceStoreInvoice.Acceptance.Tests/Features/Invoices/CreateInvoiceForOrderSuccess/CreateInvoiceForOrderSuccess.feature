Feature: Create invoice for order

  @invoice-generation-evidence  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_04
  Scenario: Create invoice for order returns created invoice
    A paid order retains the original POCO product and quantity. Real Chromium rendering and Mongo completion must use the owner latest complete client version, preserve the older versions and unrelated invoice, and return every metadata field with an available attempt-specific local test PDF.
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
