Feature: Update order status
  Status changes are validated before persistence; unrelated populated state remains exact.

  @order-state-evidence
  @allure.label.scenarioId:INV01_UpdateOrderStatus_02
  Scenario: A created order can become paid
    A valid transition returns the full original quantity-2 POCO order and generates UpdatedAt. Only the target status and timestamp may change; snapshots, carts, other orders, client versions and invoices remain exact.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone            | Other Brand | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | target                   |
      | Client   | <none>                   |
      | Status   | Paid                     |
    Then the order response contains these complete profiles
      | Order  | Client | Status | CreatedAt                | UpdatedAt | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | Paid   | 2024-01-02T03:04:05.123Z | generated | 4998.00     | PLN           | 1          |
    And the order response contains these complete saved lines
      | Order  | Snapshot     | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-phone | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
    And the order operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | status-updated |
      | ChangedOrder    | target         |
      | Orders          | 2              |
      | Snapshots       | 2              |
      | Carts           | 2              |
      | Invoices        | 1              |
      | ClientVersions  | 1              |
      | CatalogRequests | 0              |
    And complete supporting order and cart reads preserve all stored state

  @order-state-evidence
  @allure.label.scenarioId:INV01_UpdateOrderStatus_03
  Scenario: An unknown status is rejected
    The rejected status request has a complete exact problem response. Both clients have populated state; every document and timestamp remains unchanged and supporting reads still return complete profiles without catalog calls.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone            | Other Brand | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | target                   |
      | Client   | <none>                   |
      | Status   | Unknown                  |
    Then the order request returns this complete problem
      | Field        | Value                                                                        |
      | StatusCode   | 400                                                                          |
      | ContentType  | application/problem+json                                                     |
      | Title        | Validation failed.                                                           |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1                  |
      | Detail       | One or more validation errors occurred.                                      |
      | Instance     | request-path                                                                 |
      | TraceId      | generated                                                                    |
      | ErrorsCount  | 1                                                                            |
      | ErrorName    | status                                                                       |
      | ErrorEntity  | Order                                                                        |
      | ErrorMessage | Order status 'Unknown' is invalid. Allowed values: Paid, Cancelled, Created. |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 2         |
      | Snapshots       | 2         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state

  @order-state-evidence
  @allure.label.scenarioId:INV01_UpdateOrderStatus_04
  Scenario: A missing order cannot be updated
    The rejected status request has a complete exact problem response. Both clients have populated state; every document and timestamp remains unchanged and supporting reads still return complete profiles without catalog calls.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone            | Other Brand | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | missing                  |
      | Client   | <none>                   |
      | Status   | Paid                     |
    Then the order request returns this complete problem
      | Field       | Value                                                                                                                         |
      | StatusCode  | 404                                                                                                                           |
      | ContentType | application/problem+json                                                                                                      |
      | Title       | Resource not found.                                                                                                           |
      | Type        | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4                                                                   |
      | Detail      | Resource Order with id '{orderId}' was not found. identified by id {orderId} cannot be found in database during action Order. |
      | Instance    | request-path                                                                                                                  |
      | TraceId     | generated                                                                                                                     |
      | ErrorsCount | absent                                                                                                                        |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 2         |
      | Snapshots       | 2         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state

  @order-state-evidence
  @allure.label.scenarioId:INV01_UpdateOrderStatus_05
  Scenario: An empty order id is invalid
    The rejected status request has a complete exact problem response. Both clients have populated state; every document and timestamp remains unchanged and supporting reads still return complete profiles without catalog calls.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone            | Other Brand | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | empty                    |
      | Client   | <none>                   |
      | Status   | Paid                     |
    Then the order request returns this complete problem
      | Field        | Value                                                       |
      | StatusCode   | 400                                                         |
      | ContentType  | application/problem+json                                    |
      | Title        | Validation failed.                                          |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Detail       | One or more validation errors occurred.                     |
      | Instance     | request-path                                                |
      | TraceId      | generated                                                   |
      | ErrorsCount  | 1                                                           |
      | ErrorName    | ClientIdIsEmptyValidationRule                               |
      | ErrorEntity  | ShoppingCart                                                |
      | ErrorMessage | ClientId cannot be empty Guid.                              |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 2         |
      | Snapshots       | 2         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state

  @order-state-evidence
  @allure.label.scenarioId:INV01_UpdateOrderStatus_06
  Scenario: A created order can become cancelled
    A valid transition returns the full original quantity-2 POCO order and generates UpdatedAt. Only the target status and timestamp may change; snapshots, carts, other orders, client versions and invoices remain exact.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone            | Other Brand | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | target                   |
      | Client   | <none>                   |
      | Status   | Cancelled                |
    Then the order response contains these complete profiles
      | Order  | Client | Status    | CreatedAt                | UpdatedAt | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | Cancelled | 2024-01-02T03:04:05.123Z | generated | 4998.00     | PLN           | 1          |
    And the order response contains these complete saved lines
      | Order  | Snapshot     | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-phone | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
    And the order operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | status-updated |
      | ChangedOrder    | target         |
      | Orders          | 2              |
      | Snapshots       | 2              |
      | Carts           | 2              |
      | Invoices        | 1              |
      | ClientVersions  | 1              |
      | CatalogRequests | 0              |
    And complete supporting order and cart reads preserve all stored state

  @order-state-evidence
  @allure.label.scenarioId:INV01_UpdateOrderStatus_07
  Scenario: A created order cannot be changed back to created
    The rejected status request has a complete exact problem response. Both clients have populated state; every document and timestamp remains unchanged and supporting reads still return complete profiles without catalog calls.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone            | Other Brand | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | target                   |
      | Client   | <none>                   |
      | Status   | Created                  |
    Then the order request returns this complete problem
      | Field        | Value                                                           |
      | StatusCode   | 400                                                             |
      | ContentType  | application/problem+json                                        |
      | Title        | Validation failed.                                              |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1     |
      | Detail       | One or more validation errors occurred.                         |
      | Instance     | request-path                                                    |
      | TraceId      | generated                                                       |
      | ErrorsCount  | 1                                                               |
      | ErrorName    | UpdateOrderStatusTransitionValidationRule                       |
      | ErrorEntity  | Order                                                           |
      | ErrorMessage | Only transitions from Created to Paid or Cancelled are allowed. |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 2         |
      | Snapshots       | 2         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state

  @order-state-evidence
  Scenario Outline: A terminal status cannot be changed
    The first request changes a Created order to the example terminal status and verifies its full response and sole stored change. The second request, including repeated Paid and returning to Created, returns the exact transition error and preserves the complete terminal order and all populated collections.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone            | Other Brand | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | target                   |
      | Client   | <none>                   |
      | Status   | <current>                |
    Then the order response contains these complete profiles
      | Order  | Client | Status    | CreatedAt                | UpdatedAt | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | <current> | 2024-01-02T03:04:05.123Z | generated | 4998.00     | PLN           | 1          |
    And the order response contains these complete saved lines
      | Order  | Snapshot     | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-phone | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
    And the order operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | status-updated |
      | ChangedOrder    | target         |
      | Orders          | 2              |
      | Snapshots       | 2              |
      | Carts           | 2              |
      | Invoices        | 1              |
      | ClientVersions  | 1              |
      | CatalogRequests | 0              |
    And complete supporting order and cart reads preserve all stored state
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | target                   |
      | Client   | <none>                   |
      | Status   | <requested>              |
    Then the order request returns this complete problem
      | Field        | Value                                                           |
      | StatusCode   | 400                                                             |
      | ContentType  | application/problem+json                                        |
      | Title        | Validation failed.                                              |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1     |
      | Detail       | One or more validation errors occurred.                         |
      | Instance     | request-path                                                    |
      | TraceId      | generated                                                       |
      | ErrorsCount  | 1                                                               |
      | ErrorName    | UpdateOrderStatusTransitionValidationRule                       |
      | ErrorEntity  | Order                                                           |
      | ErrorMessage | Only transitions from Created to Paid or Cancelled are allowed. |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 2         |
      | Snapshots       | 2         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state

    Examples:
      | current   | requested | scenarioId                               |
      | Paid      | Paid      | INV01_UpdateOrderStatus_08               |
      | Paid      | Cancelled | INV01_UpdateOrderStatus_09               |
      | Cancelled | Paid      | INV01_UpdateOrderStatus_10               |
      | Cancelled | Cancelled | INV01_UpdateOrderStatus_11               |
      | Paid      | Created   | INV05_UpdateOrderStatus_PaidCreated      |
      | Cancelled | Created   | INV05_UpdateOrderStatus_CancelledCreated |

  @order-state-evidence
  Scenario Outline: A valid transition preserves every multi-line order field
    The target has fractional-price snapshots, including an inactive version with a saved deactivation timestamp. Paid and Cancelled transitions preserve all identities, quantities, prices, totals, other orders and populated collections, changing only Status and UpdatedAt.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt            | Name               | Brand         | PriceAmount | PriceCurrency | Quantity |
      | target     | target-a       | 11111111-1111-1111-1111-111111111111 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Saved phone A      | Saved Brand A | 123.45      | PLN           | 3        |
      | target     | target-b       | 22222222-2222-2222-2222-222222222222 | false    | 2023-11-01T02:03:04.123Z | 2024-03-04T05:06:07.123Z | Saved phone B      | Saved Brand B | 19.95       | PLN           | 2        |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Other client phone | Other Brand   | 9999.99     | PLN           | 1        |
    And both clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt                | UpdatedAt                |
      | A      | 33333333-3333-3333-3333-333333333333 | 5        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
      | B      | 44444444-4444-4444-4444-444444444444 | 7        | 2020-01-02T03:04:05.123Z | 2025-02-03T04:05:06.456Z |
    And a completed invoice and client version populate the remaining collections
      | Field                  | Value                                   |
      | Order                  | other-paid                              |
      | Client                 | B                                       |
      | ClientName             | Other Customer                          |
      | PostalCode             | 80-001                                  |
      | City                   | Gdansk                                  |
      | Street                 | Portowa                                 |
      | BuildingNumber         | 4A                                      |
      | ApartmentNumber        | 2                                       |
      | PhoneNumber            | 123456789                               |
      | PhonePrefix            | 48                                      |
      | AddressEmail           | other@example.com                       |
      | ClientVersionCreatedAt | 2024-02-03T05:06:07.123Z                |
      | InvoiceCreatedAt       | 2024-02-04T05:06:07.123Z                |
      | StorageUrl             | file:///fixtures/order-read-invoice.pdf |
      | GenerationStatus       | Completed                               |
      | GenerationAttemptId    | <null>                                  |
      | GenerationLeaseUntil   | <null>                                  |
    When I execute this order read or status request
      | Field    | Value                    |
      | Method   | PATCH                    |
      | Endpoint | /orders/{orderId}/status |
      | Order    | target                   |
      | Client   | <none>                   |
      | Status   | <requested>              |
    Then the order response contains these complete profiles
      | Order  | Client | Status      | CreatedAt                | UpdatedAt | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | <requested> | 2024-01-02T03:04:05.123Z | generated | 410.25      | PLN           | 2          |
    And the order response contains these complete saved lines
      | Order  | Snapshot | ProductId                            | IsActive | CreatedAt                | DeactivatedAt            | Name          | Brand         | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-a | 11111111-1111-1111-1111-111111111111 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Saved phone A | Saved Brand A | 123.45      | PLN           | 3        | 370.35          |
      | target | target-b | 22222222-2222-2222-2222-222222222222 | false    | 2023-11-01T02:03:04.123Z | 2024-03-04T05:06:07.123Z | Saved phone B | Saved Brand B | 19.95       | PLN           | 2        | 39.90           |
    And the order operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | status-updated |
      | ChangedOrder    | target         |
      | Orders          | 2              |
      | Snapshots       | 3              |
      | Carts           | 2              |
      | Invoices        | 1              |
      | ClientVersions  | 1              |
      | CatalogRequests | 0              |
    And complete supporting order and cart reads preserve all stored state

    Examples:
      | requested | scenarioId                                      |
      | Paid      | INV05_UpdateOrderStatus_MultipleLines_Paid      |
      | Cancelled | INV05_UpdateOrderStatus_MultipleLines_Cancelled |

  @products-api
  @allure.label.scenarioId:INV01_UpdateOrderStatus_12
  Scenario: Concurrent status changes cannot both succeed
    Paid and Cancelled requests compete for the same Created order. Exactly one succeeds; the loser is a terminal-state 400 or a stale-write 409 depending on read timing, and the saved order matches the winner.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I concurrently change the order status to Paid and Cancelled
    Then exactly one status change succeeds and the stored order matches it
