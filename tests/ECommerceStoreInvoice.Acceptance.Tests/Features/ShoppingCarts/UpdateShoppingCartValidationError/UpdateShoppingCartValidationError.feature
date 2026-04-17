@allure.description:Ensures_updating_an_existing_shopping_cart_with_invalid_payload_returns_RFC7231_bad_request_problem_details_with_validation_errors.
Feature: Update shopping cart validation error

  Scenario: Update shopping cart returns problem details when validation fails
    Given I have an existing shopping cart for invalid update
    And I have an invalid update shopping cart request
      | ProductId                            | Name  | Brand | UnitPriceAmount | UnitPriceCurrency | Quantity |
      | 33333333-3333-3333-3333-333333333333 | Phone | Apple | 999.99          | usd               | 0        |
    When I submit the invalid update shopping cart request
    Then problem details are returned for update shopping cart validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /shopping-carts/{clientId}                                   |
      | ErrorsCount       | 1                                                            |
      | FirstErrorMessage | Quantity must be greater than zero.                          |
