Feature: Safe unexpected errors for every API operation
  Every endpoint must return a stable problem response when its dependency fails.

  Background:
    Given these complete client histories support business validation
      | Version  | Client | CreatedAt            | ClientName  | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail            |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Doe    | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com       |
      | b-latest | B      | 2024-01-04T08:00:00Z | Anna Latest | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna.latest@example.com |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Latest | 00-001     | Warsaw  | Main     | 10             | 5               | 123456789   | 48          | john.latest@example.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Old    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna.old@example.com    |
    And these exact stored order snapshots support business validation
      | Order  | Client | Status  | CreatedAt            | UpdatedAt            | ProductId                            | Name              | Brand | PriceAmount | PriceCurrency | Quantity |
      | target | A      | Created | 2024-01-06T08:00:00Z | <null>               | 33333333-3333-3333-3333-333333333333 | Target.Phone      | Alpha | 12.50       | PLN           | 2        |
      | paid-a | A      | Paid    | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z | 44444444-4444-4444-4444-444444444444 | Historical.Tablet | Beta  | 19.95       | USD           | 3        |
      | paid-b | B      | Paid    | 2024-01-08T08:00:00Z | 2024-01-08T09:00:00Z | 55555555-5555-4555-8555-555555555555 | Other.Phone       | Gamma | 4.25        | PLN           | 7        |
    And these populated carts support business validation
      | Client | CreatedAt            | UpdatedAt            | ProductId                            | Quantity |
      | A      | 2024-01-01T10:00:00Z | 2024-01-02T10:00:00Z | 33333333-3333-3333-3333-333333333333 | 5        |
      | A      | 2024-01-01T10:00:00Z | 2024-01-02T10:00:00Z | 44444444-4444-4444-4444-444444444444 | 3        |
      | B      | 2024-01-01T11:00:00Z | 2024-01-02T11:00:00Z | 55555555-5555-4555-8555-555555555555 | 7        |
    And these completed invoice references finish the populated business fixture
      | Invoice | Order  | Version | CreatedAt            | StorageUrl                          |
      | a       | paid-a | a-old   | 2024-01-07T10:00:00Z | file:///fixtures/business-a-old.pdf |
      | b       | paid-b | b-old   | 2024-01-08T10:00:00Z | file:///fixtures/business-b-old.pdf |
    And framework problems have these complete safe signatures
      | Status | ContentType              | Type                                                         | Title                   | Detail                                               |
      | 400    | application/problem+json | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1  | Invalid JSON payload.   | The request body must contain valid JSON.            |
      | 404    | application/problem+json | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4  | Route not found.        | The requested route does not exist.                  |
      | 405    | application/problem+json | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.5  | Method not allowed.     | The requested method is not supported by this route. |
      | 415    | application/problem+json | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.13 | Unsupported media type. | The request content type is not supported.           |
      | 500    | application/problem+json | https://datatracker.ietf.org/doc/html/rfc7231#section-6.6.1  | Server error.           | An unexpected error occurred.                        |
    And controlled server faults use these complete request contracts
      | Operation                      | Method | Path                                    | ContentType      | Body                                                                                                                                                                                                          |
      | GetShoppingCartByClientId      | GET    | /shopping-carts/client/{clientId}       | <none>           | <no-content>                                                                                                                                                                                                  |
      | CreateShoppingCart             | POST   | /shopping-carts/{clientId}              | <none>           | <no-content>                                                                                                                                                                                                  |
      | UpdateShoppingCart             | PUT    | /shopping-carts/{clientId}              | application/json | {"lines":[]}                                                                                                                                                                                                  |
      | GetOrderById                   | GET    | /orders/{orderId}                       | <none>           | <no-content>                                                                                                                                                                                                  |
      | GetOrdersByClientId            | GET    | /orders/client/{clientId}               | <none>           | <no-content>                                                                                                                                                                                                  |
      | CreateOrder                    | POST   | /orders/{clientId}                      | <none>           | <no-content>                                                                                                                                                                                                  |
      | UpdateOrderStatus              | PATCH  | /orders/{orderId}/status                | application/json | {"status":"Paid"}                                                                                                                                                                                             |
      | GetInvoiceById                 | GET    | /invoices/{invoiceId}                   | <none>           | <no-content>                                                                                                                                                                                                  |
      | CreateInvoiceForOrder          | POST   | /invoices/{clientId}/{paidOrderId}      | <none>           | <no-content>                                                                                                                                                                                                  |
      | GetClientDataVersionByClientId | GET    | /client-data-versions/client/{clientId} | <none>           | <no-content>                                                                                                                                                                                                  |
      | CreateClientDataVersion        | POST   | /client-data-versions/{clientId}        | application/json | {"clientName":"Example","postalCode":"00-001","city":"Warsaw","street":"Street","buildingNumber":"1","apartmentNumber":"1","phoneNumber":"123456789","phonePrefix":"48","addressEmail":"example@example.com"} |
      | GetFlowDocumentation           | GET    | /orders-documentation/flows             | <none>           | <no-content>                                                                                                                                                                                                  |
      | GetValidationDocumentation     | GET    | /orders-documentation/validations       | <none>           | <no-content>                                                                                                                                                                                                  |
      | GetInvoiceByOrderId            | GET    | /invoices/by-order/{paidOrderId}        | <none>           | <no-content>                                                                                                                                                                                                  |

  @framework-evidence
  Scenario Outline: <operationId> returns a safe 500
    A scenario-specific application service or descriptor provider throws an injected internal exception before writes. The complete six-field server problem preserves the existing signature and hides the private marker. All five populated collections and nine complete healthy supporting reads remain exact; this is a controlled dependency fault, not a real repository failure.
    Given the "<operationId>" dependency fails unexpectedly
    When I call the failing "<operationId>" endpoint
    Then the Invoice server error is safe

    Examples:
      | operationId                     | scenarioId |
      | GetShoppingCartByClientId       | INV01_GetShoppingCartByClientId_01 |
      | CreateShoppingCart              | INV01_CreateShoppingCart_01 |
      | UpdateShoppingCart              | INV01_UpdateShoppingCart_01 |
      | GetOrderById                    | INV01_GetOrderById_01 |
      | GetOrdersByClientId             | INV01_GetOrdersByClientId_01 |
      | CreateOrder                     | INV01_CreateOrder_01 |
      | UpdateOrderStatus               | INV01_UpdateOrderStatus_01 |
      | GetInvoiceById                  | INV01_GetInvoiceById_01 |
      | CreateInvoiceForOrder           | INV01_CreateInvoiceForOrder_01 |
      | GetClientDataVersionByClientId  | INV01_GetClientDataVersionByClientId_04 |
      | CreateClientDataVersion         | INV01_CreateClientDataVersion_03 |
      | GetFlowDocumentation            | INV01_GetFlowDocumentation_01 |
      | GetValidationDocumentation      | INV01_GetValidationDocumentation_01 |
      | GetInvoiceByOrderId | INV10_GetInvoiceByOrderId_ServerError |
