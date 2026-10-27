Feature: Get orders by id validation error

  @allure.label.scenarioId:INV01_GetOrderById_04
  Scenario: Get order by id returns problem details when validation fails
    The order lookup uses an all-zero identifier. It returns the expected identifier validation problem.
    Given I have an invalid get order by id request
      | Field   | Value                                |
      | OrderId | 00000000-0000-0000-0000-000000000000 |
    When I request order by invalid order id
    Then problem details are returned for get order by id validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /orders/00000000-0000-0000-0000-000000000000                |
      | ErrorsCount       | 1                                                            |
      | FirstErrorMessage | ClientId cannot be empty Guid.                               |
