Feature: Get orders by client id

  @order-state-evidence
  @allure.label.scenarioId:INV01_GetOrdersByClientId_03
  Scenario: Get orders by client id returns existing orders
    The requested client has the original two Created POCO orders, each quantity 2 at PLN 2499.00. The complete exact ID set, order profiles and saved lines exclude another client paid order and preserve all five collections.
    Given these complete stored orders exist
      | Order      | Client | Status  | CreatedAt                | UpdatedAt                |
      | target     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | second     | A      | Created | 2024-01-02T03:04:05.123Z | <null>                   |
      | other-paid | B      | Paid    | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand       | PriceAmount | PriceCurrency | Quantity |
      | target     | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
      | second     | second-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi      | 2499.00     | PLN           | 2        |
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
      | Field    | Value                     |
      | Method   | GET                       |
      | Endpoint | /orders/client/{clientId} |
      | Order    | <none>                    |
      | Client   | A                         |
      | Status   | <none>                    |
    Then the order response contains these complete profiles
      | Order  | Client | Status  | CreatedAt                | UpdatedAt | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | Created | 2024-01-02T03:04:05.123Z | <null>    | 4998.00     | PLN           | 1          |
      | second | A      | Created | 2024-01-02T03:04:05.123Z | <null>    | 4998.00     | PLN           | 1          |
    And the order response contains these complete saved lines
      | Order  | Snapshot     | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-phone | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
      | second | second-phone | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 3         |
      | Snapshots       | 3         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state

  @order-state-evidence
  @allure.label.scenarioId:INV05_GetOrdersByClientId_Mixed
  Scenario: Client order listing filters exact mixed-status profiles and snapshots
    Created, Paid and Cancelled target orders have different totals and line counts. Another client has two different orders, including USD snapshots; only the requested client exact full profiles are returned, with every collection unchanged.
    Given these complete stored orders exist
      | Order         | Client | Status    | CreatedAt                | UpdatedAt                |
      | target        | A      | Created   | 2024-01-02T03:04:05.123Z | <null>                   |
      | second        | A      | Paid      | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
      | third         | A      | Cancelled | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
      | other-paid    | B      | Paid      | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
      | other-created | B      | Created   | 2024-01-02T03:04:05.123Z | <null>                   |
    And the stored orders reference these complete product snapshots
      | Order         | Snapshot        | ProductId                            | IsActive | CreatedAt                | DeactivatedAt            | Name                          | Brand         | PriceAmount | PriceCurrency | Quantity |
      | target        | target-phone    | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Xiaomi POCO F7 12/512GB Black | Xiaomi        | 2499.00     | PLN           | 2        |
      | second        | second-a        | 11111111-1111-1111-1111-111111111111 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Saved phone A                 | Saved Brand A | 123.45      | PLN           | 3        |
      | second        | second-b        | 22222222-2222-2222-2222-222222222222 | false    | 2023-11-01T02:03:04.123Z | 2024-03-04T05:06:07.123Z | Saved phone B                 | Saved Brand B | 19.95       | PLN           | 2        |
      | third         | third-snapshot  | 55555555-5555-5555-5555-555555555555 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Saved cancelled phone         | Saved Brand C | 99.50       | PLN           | 2        |
      | other-paid    | other-snapshot  | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Other client phone            | Other Brand   | 9999.99     | PLN           | 1        |
      | other-created | other-created-a | 66666666-6666-6666-6666-666666666666 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Other created A               | Other Brand   | 17.50       | USD           | 4        |
      | other-created | other-created-b | 77777777-7777-7777-7777-777777777777 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Other created B               | Other Brand   | 5.25        | USD           | 2        |
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
      | Field    | Value                     |
      | Method   | GET                       |
      | Endpoint | /orders/client/{clientId} |
      | Order    | <none>                    |
      | Client   | A                         |
      | Status   | <none>                    |
    Then the order response contains these complete profiles
      | Order  | Client | Status    | CreatedAt                | UpdatedAt                | TotalAmount | TotalCurrency | LinesCount |
      | target | A      | Created   | 2024-01-02T03:04:05.123Z | <null>                   | 4998.00     | PLN           | 1          |
      | second | A      | Paid      | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z | 410.25      | PLN           | 2          |
      | third  | A      | Cancelled | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z | 199.00      | PLN           | 1          |
    And the order response contains these complete saved lines
      | Order  | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt            | Name                          | Brand         | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | target | target-phone   | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Xiaomi POCO F7 12/512GB Black | Xiaomi        | 2499.00     | PLN           | 2        | 4998.00         |
      | second | second-a       | 11111111-1111-1111-1111-111111111111 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Saved phone A                 | Saved Brand A | 123.45      | PLN           | 3        | 370.35          |
      | second | second-b       | 22222222-2222-2222-2222-222222222222 | false    | 2023-11-01T02:03:04.123Z | 2024-03-04T05:06:07.123Z | Saved phone B                 | Saved Brand B | 19.95       | PLN           | 2        | 39.90           |
      | third  | third-snapshot | 55555555-5555-5555-5555-555555555555 | true     | 2023-11-01T02:03:04.123Z | <null>                   | Saved cancelled phone         | Saved Brand C | 99.50       | PLN           | 2        | 199.00          |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 5         |
      | Snapshots       | 7         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state
