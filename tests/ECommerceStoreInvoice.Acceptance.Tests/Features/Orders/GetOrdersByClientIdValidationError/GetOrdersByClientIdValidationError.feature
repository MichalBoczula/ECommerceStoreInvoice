Feature: Get orders by client id validation error

  @allure.label.scenarioId:INV01_GetOrdersByClientId_04
  Scenario: Get orders by client id returns problem details when validation fails
    The client order-list request uses an all-zero identifier. It returns the expected identifier validation problem.
    Given I have an invalid client id for orders retrieval
      | ClientId                             |
      | 00000000-0000-0000-0000-000000000000 |
    When I request orders by invalid client id
    Then problem details are returned for get orders by client id validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /orders/client/00000000-0000-0000-0000-000000000000         |
      | ErrorsCount       | 1                                                            |
      | FirstErrorMessage | ClientId cannot be empty Guid.                               |
    And the validation error response JSON contains
      | Field    | Message                       |
      | clientId | ClientId cannot be empty Guid. |
