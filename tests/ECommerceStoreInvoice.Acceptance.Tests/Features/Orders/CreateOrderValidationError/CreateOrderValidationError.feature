@allure.description:Ensures_creating_order_with_empty_shopping_cart_returns_RFC7231_bad_request_problem_details_with_validation_errors.
Feature: Create order validation error

  Scenario: Create order returns problem details when validation fails
    Given I have a client with an empty shopping cart for order creation
    When I submit the create order request for the empty shopping cart
    Then problem details are returned for create order validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /orders/{clientId}                                           |
      | ErrorsCount       | 1                                                            |
      | FirstErrorMessage | Order lines cannot be empty.                                 |
