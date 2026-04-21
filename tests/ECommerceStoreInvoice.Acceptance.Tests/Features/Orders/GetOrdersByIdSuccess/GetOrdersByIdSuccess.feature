Feature: Get order by id

  Scenario: Get order by id returns an existing order
    Given I have an existing order id
    When I request order by id
    Then the order is returned successfully by id
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
