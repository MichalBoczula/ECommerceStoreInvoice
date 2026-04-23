Feature: Create order

  Scenario: Create order returns created order
    Given I have a valid shopping cart for order creation
    When I submit the create order request
    Then the order is created successfully
      | Field                      | Value   |
      | StatusCode                 | 200     |
      | HasId                      | true    |
      | HasClientId                | true    |
      | Status                     | Created |
      | TotalAmount                | 1999.98 |
      | TotalCurrency              | USD     |
      | LinesCount                 | 1       |
      | FirstLineHasProductVersionId | true  |
      | FirstLineName              | Laptop  |
      | FirstLineBrand             | Lenovo  |
      | FirstLineQuantity          | 2       |
      | FirstLineUnitPriceAmount   | 999.99  |
      | FirstLineUnitPriceCurrency | USD     |
      | FirstLineTotalAmount       | 1999.98 |
      | FirstLineTotalCurrency     | USD     |
