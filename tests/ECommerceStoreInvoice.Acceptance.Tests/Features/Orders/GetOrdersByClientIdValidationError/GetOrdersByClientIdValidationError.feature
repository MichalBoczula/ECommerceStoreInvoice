@allure.description:Ensures_getting_orders_by_client_id_with_invalid_client_id_returns_RFC7231_bad_request_problem_details_with_validation_errors.
Feature: Get orders by client id validation error

  Scenario: Get orders by client id returns problem details when validation fails
    Given I have an invalid client id for orders retrieval
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
