Feature: Get orders by client id

  @products-api
  Scenario: Get orders by client id returns existing orders
    Given I have existing orders for a client
      | Field             | Value  |
      | ProductName       | Xiaomi POCO F7 12/512GB Black |
      | ProductBrand      | Xiaomi |
      | UnitPriceAmount   | 2499.00 |
      | UnitPriceCurrency | PLN    |
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
      | FirstOrderTotalAmount    | 4998.00   |
      | FirstOrderTotalCurrency  | PLN       |
      | FirstOrderLinesCount     | 1         |
      | FirstLineName            | Xiaomi POCO F7 12/512GB Black    |
      | FirstLineBrand           | Xiaomi    |
      | FirstLineQuantity        | 2         |
      | FirstLineUnitPriceAmount | 2499.00    |
      | FirstLineUnitPriceCurrency | PLN     |
      | FirstLineTotalAmount     | 4998.00   |
      | FirstLineTotalCurrency   | PLN       |
      | ResponseJson             | [{"id":"<generated-guid>","clientId":"<scenario-client-id>","status":"Created","totalAmount":4998.00,"totalCurrency":"PLN","lines":[{"name":"Xiaomi POCO F7 12/512GB Black","brand":"Xiaomi","quantity":2,"unitPriceAmount":2499.00,"unitPriceCurrency":"PLN","totalAmount":4998.00,"totalCurrency":"PLN"}]},{"id":"<generated-guid>","clientId":"<scenario-client-id>","status":"Created","totalAmount":4998.00,"totalCurrency":"PLN","lines":[{"name":"Xiaomi POCO F7 12/512GB Black","brand":"Xiaomi","quantity":2,"unitPriceAmount":2499.00,"unitPriceCurrency":"PLN","totalAmount":4998.00,"totalCurrency":"PLN"}]}] |
