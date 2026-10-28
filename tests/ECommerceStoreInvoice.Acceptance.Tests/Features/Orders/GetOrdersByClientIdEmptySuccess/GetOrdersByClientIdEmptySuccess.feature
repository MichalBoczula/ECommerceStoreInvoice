Feature: Get orders by client id

  @order-state-evidence
  @allure.label.scenarioId:INV01_GetOrdersByClientId_02
  Scenario: Get orders by client id returns empty list when client has no orders
    The requested client has no orders while another client owns a populated paid order and invoice. The response is exactly an empty array and all five populated collections, including both client carts, stay unchanged.
    Given these complete stored orders exist
      | Order      | Client | Status | CreatedAt                | UpdatedAt                |
      | other-paid | B      | Paid   | 2024-01-02T03:04:05.123Z | 2024-02-03T04:05:06.456Z |
    And the stored orders reference these complete product snapshots
      | Order      | Snapshot       | ProductId                            | IsActive | CreatedAt                | DeactivatedAt | Name               | Brand       | PriceAmount | PriceCurrency | Quantity |
      | other-paid | other-snapshot | 88888888-8888-8888-8888-888888888888 | true     | 2023-11-01T02:03:04.123Z | <null>        | Other client phone | Other Brand | 9999.99     | PLN           | 1        |
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
    Then the order response is this exact empty collection
      | Field       | Value |
      | StatusCode  | 200   |
      | OrdersCount | 0     |
      | IsEmpty     | true  |
      | Body        | []    |
    And the order operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | ChangedOrder    | none      |
      | Orders          | 1         |
      | Snapshots       | 1         |
      | Carts           | 2         |
      | Invoices        | 1         |
      | ClientVersions  | 1         |
      | CatalogRequests | 0         |
    And complete supporting order and cart reads preserve all stored state
