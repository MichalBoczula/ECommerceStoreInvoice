@allure.description:Ensures_getting_a_non_existing_order_by_id_returns_RFC7231_not_found_problem_details_with_request_context.
Feature: Get order by id not found

  Scenario: Get order by id returns problem details when order does not exist
    Given I have a non-existing order id for orders retrieval
    When I request order by id for non-existing order
    Then problem details are returned for get order by id not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /orders/{orderId}                                            |
      | HasTraceId | true                                                         |
