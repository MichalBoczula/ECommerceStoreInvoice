Feature: Shopping cart flow failures
  The read flow rejects an empty client id and the create flow rejects an existing cart.

  @business-validation-evidence @allure.label.scenarioId:INV01_GetShoppingCartByClientId_04
  Scenario: An empty client identifier is invalid when reading the cart
    The original empty client lookup checks the full exact GUID problem and every unchanged populated document. Both real client carts and all other supporting reads still return complete saved profiles.
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
    And business requests have these complete defaults
      | Operation                 | Method | Path                              | BodyJson                                                                                                                                                                                                         |
      | CreateClientDataVersion   | POST   | /client-data-versions/{clientId}  | {"clientName":"John Doe","postalCode":"00-001","city":"NewYork","street":"Main.St","buildingNumber":"10A","apartmentNumber":"5","phoneNumber":"123456789","phonePrefix":"48","addressEmail":"john.doe@test.com"} |
      | UpdateShoppingCart        | PUT    | /shopping-carts/{clientId}        | {"lines":[{"productId":"33333333-3333-3333-3333-333333333333","quantity":2},{"productId":"44444444-4444-4444-4444-444444444444","quantity":4}]}                                                                  |
      | UpdateOrderStatus         | PATCH  | /orders/{orderId}/status          | {"status":"Paid"}                                                                                                                                                                                                |
      | CreateShoppingCart        | POST   | /shopping-carts/{clientId}        | null                                                                                                                                                                                                             |
      | GetShoppingCartByClientId | GET    | /shopping-carts/client/{clientId} | null                                                                                                                                                                                                             |
      | CreateOrder               | POST   | /orders/client/{clientId}         | null                                                                                                                                                                                                             |
    And business validation problems have this complete signature
      | Field        | Value                                                       |
      | StatusCode   | 400                                                         |
      | ContentType  | application/problem+json                                    |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Title        | Validation failed.                                          |
      | Detail       | One or more validation errors occurred.                     |
      | Instance     | request-path                                                |
      | TraceId      | generated                                                   |
      | CacheControl | no-cache,no-store                                           |
    Given the selected business request is "GetShoppingCartByClientId"
    And business route parameter "clientId" is the empty identifier
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name                          | Message                        | Entity       |
      | ClientIdIsEmptyValidationRule | ClientId cannot be empty Guid. | ShoppingCart |
    And business rejection preserves every populated document and supporting read

  @allure.label.scenarioId:INV01_CreateShoppingCart_04
  Scenario: An existing shopping cart cannot be created twice
    A client already has a shopping cart. Creating another cart for that same client returns a conflict.
    Given a shopping cart has already been created
      | Field           | Value                       |
      | Method          | POST                        |
      | Endpoint        | /shopping-carts/{clientId}  |
      | HasBody         | false                       |
      | SetupStatus     | 200                         |
      | DuplicateStatus | 409                         |
    When I create the same shopping cart again
    Then the cart response is problem 409
