Feature: Create order

  @products-api @checkout-evidence
  @allure.label.scenarioId:INV01_CreateOrder_04
  Scenario: Create order returns created order
    The original seeded product and quantity produce the complete order and immutable product snapshot. Exactly one actual catalog fetch precedes the coordinated MongoDB writes; the cart is cleared and another client stays unchanged.
    Given the checkout cart uses these identities and dates
      | Field     | Value                    |
      | CartId    | generated                |
      | ClientId  | generated                |
      | CreatedAt | 2020-01-02T03:04:05.123Z |
      | UpdatedAt | 2020-02-03T04:05:06.456Z |
    And the checkout cart contains these catalog products
      | ProductId                            | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
    And another checkout client has these stored cart lines
      | ProductId                            | Quantity |
      | 33333333-3333-3333-3333-333333333333 | 9        |
      | 44444444-4444-4444-4444-444444444444 | 4        |
    When the client checks out using this request
      | Field    | Value                     |
      | Method   | POST                      |
      | Endpoint | /orders/client/{clientId} |
      | HasBody  | false                     |
    Then checkout returns this complete order profile
      | Field         | Value     |
      | StatusCode    | 200       |
      | Id            | generated |
      | ClientId      | requested |
      | CreatedAt     | generated |
      | UpdatedAt     | <null>    |
      | Status        | Created   |
      | TotalAmount   | 4998.00   |
      | TotalCurrency | PLN       |
      | LinesCount    | 1         |
    And checkout returns these complete snapshot lines
      | ProductId                            | ProductVersionId | IsActive | CreatedAt | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | generated        | true     | generated | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
    And checkout commits these coordinated database changes
      | Field                 | Value     |
      | OrdersBefore          | 0         |
      | OrdersAfter           | 1         |
      | SnapshotsBefore       | 0         |
      | SnapshotsAfter        | 1         |
      | CartsBefore           | 2         |
      | CartsAfter            | 2         |
      | CartLinesBefore       | 1         |
      | CartLinesAfter        | 0         |
      | CatalogRequestsBefore | 0         |
      | CatalogRequestsAfter  | 1         |
      | CartId                | retained  |
      | CartCreatedAt         | retained  |
      | CartUpdatedAt         | advanced  |
      | OtherClients          | unchanged |
    And snapshot reads retain this committed state without catalog calls
      | Field           | Value     |
      | Orders          | 1         |
      | Snapshots       | 1         |
      | CatalogRequests | 1         |
      | CartLines       | 0         |
      | StoredState     | unchanged |
      | OtherClients    | unchanged |

  @products-api @checkout-evidence
  @allure.label.scenarioId:INV04_Checkout_MultipleProducts
  Scenario: Checkout snapshots multiple products with exact quantities and totals
    Two distinct live catalog products have different quantities and unit prices. One catalog POST supplies both snapshots; every response and stored field, total, cleared cart and other client are verified.
    Given the checkout cart uses these identities and dates
      | Field     | Value                    |
      | CartId    | generated                |
      | ClientId  | generated                |
      | CreatedAt | 2020-01-02T03:04:05.123Z |
      | UpdatedAt | 2020-02-03T04:05:06.456Z |
    And the checkout cart contains these catalog products
      | ProductId                            | Name                                      | Brand  | PriceAmount | PriceCurrency | Quantity |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | Xiaomi POCO F7 12/512GB Black             | Xiaomi | 2499.00     | PLN           | 2        |
      | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | Xiaomi Redmi 15 5G 4/128GB Midnight Black | Xiaomi | 999.00      | PLN           | 3        |
    And another checkout client has these stored cart lines
      | ProductId                            | Quantity |
      | 33333333-3333-3333-3333-333333333333 | 9        |
      | 44444444-4444-4444-4444-444444444444 | 4        |
    When the client checks out using this request
      | Field    | Value                     |
      | Method   | POST                      |
      | Endpoint | /orders/client/{clientId} |
      | HasBody  | false                     |
    Then checkout returns this complete order profile
      | Field         | Value     |
      | StatusCode    | 200       |
      | Id            | generated |
      | ClientId      | requested |
      | CreatedAt     | generated |
      | UpdatedAt     | <null>    |
      | Status        | Created   |
      | TotalAmount   | 7995.00   |
      | TotalCurrency | PLN       |
      | LinesCount    | 2         |
    And checkout returns these complete snapshot lines
      | ProductId                            | ProductVersionId | IsActive | CreatedAt | DeactivatedAt | Name                                      | Brand  | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | generated        | true     | generated | <null>        | Xiaomi POCO F7 12/512GB Black             | Xiaomi | 2499.00     | PLN           | 2        | 4998.00         |
      | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | generated        | true     | generated | <null>        | Xiaomi Redmi 15 5G 4/128GB Midnight Black | Xiaomi | 999.00      | PLN           | 3        | 2997.00         |
    And checkout commits these coordinated database changes
      | Field                 | Value     |
      | OrdersBefore          | 0         |
      | OrdersAfter           | 1         |
      | SnapshotsBefore       | 0         |
      | SnapshotsAfter        | 2         |
      | CartsBefore           | 2         |
      | CartsAfter            | 2         |
      | CartLinesBefore       | 2         |
      | CartLinesAfter        | 0         |
      | CatalogRequestsBefore | 0         |
      | CatalogRequestsAfter  | 1         |
      | CartId                | retained  |
      | CartCreatedAt         | retained  |
      | CartUpdatedAt         | advanced  |
      | OtherClients          | unchanged |
    And snapshot reads retain this committed state without catalog calls
      | Field           | Value     |
      | Orders          | 1         |
      | Snapshots       | 2         |
      | CatalogRequests | 1         |
      | CartLines       | 0         |
      | StoredState     | unchanged |
      | OtherClients    | unchanged |

  @products-api @checkout-evidence @isolated-products
  @allure.label.scenarioId:INV04_Checkout_ImmutableSnapshots
  Scenario: Catalog changes and unavailability preserve completed order snapshots
    A private Products API and SQL Server provide fractional prices for two products. After real checkout, direct isolated SQL fixture changes are confirmed over HTTP; saved order reads retain every original snapshot value. Stopping that catalog still leaves complete reads and all five MongoDB collections unchanged.
    Given the isolated catalog contains these product values
      | ProductId                            | Name             | Brand            | PriceAmount | PriceCurrency |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | Snapshot phone A | Original Brand A | 123.45      | PLN           |
      | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | Snapshot phone B | Original Brand B | 19.95       | PLN           |
    And the checkout cart uses these identities and dates
      | Field     | Value                    |
      | CartId    | generated                |
      | ClientId  | generated                |
      | CreatedAt | 2020-01-02T03:04:05.123Z |
      | UpdatedAt | 2020-02-03T04:05:06.456Z |
    And the checkout cart contains these catalog products
      | ProductId                            | Name             | Brand            | PriceAmount | PriceCurrency | Quantity |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | Snapshot phone A | Original Brand A | 123.45      | PLN           | 3        |
      | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | Snapshot phone B | Original Brand B | 19.95       | PLN           | 2        |
    And another checkout client has these stored cart lines
      | ProductId                            | Quantity |
      | 33333333-3333-3333-3333-333333333333 | 9        |
      | 44444444-4444-4444-4444-444444444444 | 4        |
    When the client checks out using this request
      | Field    | Value                     |
      | Method   | POST                      |
      | Endpoint | /orders/client/{clientId} |
      | HasBody  | false                     |
    Then checkout returns this complete order profile
      | Field         | Value     |
      | StatusCode    | 200       |
      | Id            | generated |
      | ClientId      | requested |
      | CreatedAt     | generated |
      | UpdatedAt     | <null>    |
      | Status        | Created   |
      | TotalAmount   | 410.25    |
      | TotalCurrency | PLN       |
      | LinesCount    | 2         |
    And checkout returns these complete snapshot lines
      | ProductId                            | ProductVersionId | IsActive | CreatedAt | DeactivatedAt | Name             | Brand            | PriceAmount | PriceCurrency | Quantity | LineTotalAmount |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | generated        | true     | generated | <null>        | Snapshot phone A | Original Brand A | 123.45      | PLN           | 3        | 370.35          |
      | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | generated        | true     | generated | <null>        | Snapshot phone B | Original Brand B | 19.95       | PLN           | 2        | 39.90           |
    And checkout commits these coordinated database changes
      | Field                 | Value     |
      | OrdersBefore          | 0         |
      | OrdersAfter           | 1         |
      | SnapshotsBefore       | 0         |
      | SnapshotsAfter        | 2         |
      | CartsBefore           | 2         |
      | CartsAfter            | 2         |
      | CartLinesBefore       | 2         |
      | CartLinesAfter        | 0         |
      | CatalogRequestsBefore | 0         |
      | CatalogRequestsAfter  | 1         |
      | CartId                | retained  |
      | CartCreatedAt         | retained  |
      | CartUpdatedAt         | advanced  |
      | OtherClients          | unchanged |
    And snapshot reads retain this committed state without catalog calls
      | Field           | Value     |
      | Orders          | 1         |
      | Snapshots       | 2         |
      | CatalogRequests | 1         |
      | CartLines       | 0         |
      | StoredState     | unchanged |
      | OtherClients    | unchanged |
    And the isolated catalog changes to these product values
      | ProductId                            | Name            | Brand           | PriceAmount | PriceCurrency |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | Updated phone A | Changed Brand A | 777.77      | USD           |
      | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | Updated phone B | Changed Brand B | 0.99        | USD           |
    And snapshot reads retain this committed state without catalog calls
      | Field           | Value     |
      | Orders          | 1         |
      | Snapshots       | 2         |
      | CatalogRequests | 1         |
      | CartLines       | 0         |
      | StoredState     | unchanged |
      | OtherClients    | unchanged |
    And the isolated Products API is stopped
    And snapshot reads retain this committed state without catalog calls
      | Field           | Value     |
      | Orders          | 1         |
      | Snapshots       | 2         |
      | CatalogRequests | 1         |
      | CartLines       | 0         |
      | StoredState     | unchanged |
      | OtherClients    | unchanged |
