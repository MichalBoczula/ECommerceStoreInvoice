Feature: Get orders by client id

  Scenario: Get orders by client id returns existing orders
    Given I have existing orders for a client
      | Field             | Value  |
      | ProductName       | Laptop |
      | ProductBrand      | Lenovo |
      | UnitPriceAmount   | 999.99 |
      | UnitPriceCurrency | usd    |
      | Quantity          | 2      |
      | OrdersToCreate    | 2      |
    When I request orders by client id
    Then the orders are returned successfully
      | Field                    | Value     |
      | StatusCode               | 200       |
      | OrdersCount              | 2         |
      | FirstOrderHasId          | true      |
      | FirstOrderHasClientId    | true      |
      | FirstOrderStatus         | Created   |
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
      | ResponseJson             | [{"id":"<generated-guid>","clientId":"<scenario-client-id>","status":"Created","totalAmount":1999.98,"totalCurrency":"USD","lines":[{"name":"Laptop","brand":"Lenovo","quantity":2,"unitPriceAmount":999.99,"unitPriceCurrency":"USD","totalAmount":1999.98,"totalCurrency":"USD"}]},{"id":"<generated-guid>","clientId":"<scenario-client-id>","status":"Created","totalAmount":1999.98,"totalCurrency":"USD","lines":[{"name":"Laptop","brand":"Lenovo","quantity":2,"unitPriceAmount":999.99,"unitPriceCurrency":"USD","totalAmount":1999.98,"totalCurrency":"USD"}]}] |
