Feature: Create order not found

  @allure.label.scenarioId:INV01_CreateOrder_03
  Scenario: Create order returns problem details when client does not exist
    The requested client has no shopping cart. Checkout returns the expected missing-cart problem.
    Given I have a non-existing client id for order creation
      | Field    | Value        |
      | ClientId | <generatedId> |
    When I submit create order request for a non-existing client
      | Field    | Value              |
      | Method   | POST               |
      | Endpoint | /orders/{clientId} |
      | Body     | null               |
    Then problem details are returned for create order not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /orders/{clientId}                                           |
      | HasTraceId | true                                                         |
