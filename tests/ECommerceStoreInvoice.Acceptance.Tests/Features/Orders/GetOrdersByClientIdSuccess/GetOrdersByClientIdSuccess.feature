Feature: Get orders by client id

  Scenario: Get orders by client id returns existing orders
    Given I have existing orders for a client
    When I request orders by client id
    Then the orders are returned successfully
      | Field                    | Value     |
      | StatusCode               | 200       |
      | OrdersCount              | 2         |
      | FirstOrderHasId          | true      |
      | FirstOrderHasClientId    | true      |
      | FirstOrderStatus         | Pending   |
      | FirstOrderTotalAmount    | 1999.98   |
      | FirstOrderTotalCurrency  | USD       |
      | FirstOrderLinesCount     | 1         |
      | FirstLineName            | Laptop    |
      | FirstLineBrand           | Lenovo    |
      | FirstLineQuantity        | 2         |
      | FirstLineUnitPriceAmount | 999.99    |
      | FirstLineUnitPriceCurrency | USD     |
      | FirstLineTotalAmount     | 1999.98   |
      | FirstLineTotalCurrency   | USD       |
