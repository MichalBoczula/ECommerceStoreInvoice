Feature: Get order by id

  @products-api
  @allure.label.scenarioId:INV01_GetOrderById_03
  Scenario: Get order by id returns an existing order
    Setup checks out a cart using the catalog product fixture. Retrieval returns the saved order identity, status, totals and snapshot-backed line.
    Given I have an existing order id with setup data
      | Field            | Value  |
      | ProductName      | Xiaomi POCO F7 12/512GB Black |
      | ProductBrand     | Xiaomi |
      | UnitPriceAmount  | 2499.00 |
      | UnitPriceCurrency| PLN    |
      | Quantity         | 2      |
    When I request order by id
    Then the order is returned successfully by id
      | Field                      | Value   |
      | StatusCode                 | 200     |
      | HasId                      | true    |
      | HasClientId                | true    |
      | Status                     | Created |
      | TotalAmount                | 4998.00 |
      | TotalCurrency              | PLN     |
      | LinesCount                 | 1       |
      | FirstLineHasProductVersionId | true  |
      | FirstLineName              | Xiaomi POCO F7 12/512GB Black  |
      | FirstLineBrand             | Xiaomi  |
      | FirstLineQuantity          | 2       |
      | FirstLineUnitPriceAmount   | 2499.00  |
      | FirstLineUnitPriceCurrency | PLN     |
      | FirstLineTotalAmount       | 4998.00 |
      | FirstLineTotalCurrency     | PLN     |
