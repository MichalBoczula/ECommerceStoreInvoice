Feature: Create order

  @products-api
  Scenario: Create order returns created order
    Given I have a valid shopping cart for order creation
      | Field                | Value  |
      | Name                 | Xiaomi POCO F7 12/512GB Black |
      | Brand                | Xiaomi |
      | UnitPriceAmount      | 2499.00 |
      | UnitPriceCurrency    | PLN    |
      | Quantity             | 2      |
    When I submit the create order request
    Then the order is created successfully
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
