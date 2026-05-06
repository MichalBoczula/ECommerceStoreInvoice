@allure.description:Ensures_creating_order_for_non_existing_client_returns_RFC7231_not_found_problem_details_with_request_context.
Feature: Create order not found

  Scenario: Create order returns problem details when client does not exist
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
