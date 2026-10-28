Feature: Get order by id

  @order-state-evidence
  @allure.label.scenarioId:INV01_GetOrderById_03
  Scenario: Get order by id returns an existing order
    The complete stored Created order retains the original POCO name, brand, quantity and PLN totals. A second client has a paid order and both clients have populated carts; reads preserve all five collections and make no catalog calls.
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
      | Field    | Value             |
      | Method   | GET               |
      | Endpoint | /orders/{orderId} |
      | Order    | target            |
      | Client   | <none>            |
      | Status   | <none>            |
    Then the order response contains these complete profiles
      | Order  | Client | Status  | CreatedAt                | UpdatedAt | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | Created | 2024-01-02T03:04:05.123Z | <null>    | 4998.00     | PLN           | 1          |
    And the order response contains these complete saved lines
      | Order  | Snapshot     | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-phone | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
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
  Scenario Outline: Reading a <status> order retains the complete multi-line profile
    A terminal order has two distinct saved snapshots, including an inactive version with a deactivation timestamp. The response returns every saved value and leaves populated state unchanged without consulting Products.
    Given these complete stored orders exist
      | Order      | Client | Status   | CreatedAt                | UpdatedAt                |
      | target     | A      | <status> | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
      | other-paid | B      | Paid     | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
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
      | Field    | Value             |
      | Method   | GET               |
      | Endpoint | /orders/{orderId} |
      | Order    | target            |
      | Client   | <none>            |
      | Status   | <none>            |
    Then the order response contains these complete profiles
      | Order  | Client | Status   | CreatedAt                | UpdatedAt                | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | <status> | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z | 410.25      | PLN           | 2          |
    And the order response contains these complete saved lines
      | Order  | Snapshot | ProductId                            | IsActive | CreatedAt                | DeactivatedAt            | Name          | Brand         | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-a | 11111111-1111-1111-1111-111111111111 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Saved phone A | Saved Brand A | 123.45      | PLN           | 3        | 370.35          |
      | target | target-b | 22222222-2222-2222-2222-222222222222 | false    | 2023-11-01T02:03:04.123Z | 2024-03-04T05:06:07.123Z | Saved phone B | Saved Brand B | 19.95       | PLN           | 2        | 39.90           |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 2         |
      | Snapshots       | 3         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state

    Examples:
      | status    | scenarioId                   |
      | Paid      | INV05_GetOrderById_Paid      |
      | Cancelled | INV05_GetOrderById_Cancelled |
