Feature: Get orders by client id

  @allure.label.scenarioId:INV01_GetOrdersByClientId_02
  Scenario: Get orders by client id returns empty list when client has no orders
    The client identifier is valid but has no orders. Listing orders succeeds with an empty collection.
    Given I have a client without orders
      | Field       | Value                  |
      | ClientId    | AUTO                   |
      | Method      | GET                    |
      | Endpoint    | /orders/client/{id}    |
      | Description | Client has no orders   |
    When I request orders by client id for the client without orders
    Then an empty list of orders is returned successfully
      | Field       | Value |
      | StatusCode  | 200   |
      | OrdersCount | 0     |
      | IsEmpty     | true  |
      | OrdersJson  | []    |
