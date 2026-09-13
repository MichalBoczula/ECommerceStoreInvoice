@allure.description:Ensures_updating_a_non_existing_shopping_cart_returns_RFC7231_not_found_problem_details_with_request_context.
Feature: Update shopping cart not found

  Scenario: Update shopping cart returns problem details when shopping cart does not exist
    Given I have a non-existing client id for shopping cart update
      | Field    | Value         |
      | ClientId | <generatedId> |
    And I have an update shopping cart request for a non-existing shopping cart
      | ProductId                            | Quantity |
      | 11111111-1111-1111-1111-111111111111 | 1        |
      | 22222222-2222-2222-2222-222222222222 | 2        |
    When I submit the update shopping cart request for a non-existing shopping cart
      | Field    | Value                       |
      | Method   | PUT                         |
      | Endpoint | /shopping-carts/{clientId}  |
      | Lines    | 2                           |
    Then problem details are returned for update shopping cart not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /shopping-carts/{clientId}                                   |
      | HasTraceId | true                                                         |
    And the update shopping cart not found response data is
      | Field            | Value                                    |
      | DetailContainsId | true                                     |
      | DetailContains   | ShoppingCart                             |
      | TraceId          | <generated>                              |
