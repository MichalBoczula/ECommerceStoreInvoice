Feature: Get order by id

  @products-api
  Scenario: Get order by id returns an existing order
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
