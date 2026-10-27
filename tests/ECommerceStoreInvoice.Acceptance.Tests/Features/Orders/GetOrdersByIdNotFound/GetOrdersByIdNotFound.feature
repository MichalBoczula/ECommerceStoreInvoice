Feature: Get order by id not found

  @allure.label.scenarioId:INV01_GetOrderById_02
  Scenario: Get order by id returns problem details when order does not exist
    The order identifier has no saved order. Retrieval returns the expected not-found problem.
    Given I have a non-existing order id for orders retrieval
      | Field   | Value                  |
      | OrderId | generated-non-existing |
    When I request order by id for non-existing order
    Then problem details are returned for get order by id not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /orders/{orderId}                                            |
      | HasTraceId | true                                                         |
