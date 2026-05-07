@allure.description:Ensures_updating_an_existing_shopping_cart_with_invalid_payload_returns_RFC7231_bad_request_problem_details_with_validation_errors.
Feature: Update shopping cart validation error

  Scenario: Update shopping cart returns problem details when validation fails
    Given I have an existing shopping cart for invalid update
    And update shopping cart request fields are documented
      | Field                      | Value                                |
      | ClientId                   | {clientId}                           |
      | Lines[0].ProductId         | 33333333-3333-3333-3333-333333333333 |
      | Lines[0].Name              | Phone                                |
      | Lines[0].Brand             | Apple                                |
      | Lines[0].UnitPrice.Amount  | 999.99                               |
      | Lines[0].UnitPrice.Currency| usd                                  |
      | Lines[0].Quantity          | 0                                    |
    And I have an invalid update shopping cart request
      | ProductId                            | Name  | Brand | UnitPriceAmount | UnitPriceCurrency | Quantity |
      | 33333333-3333-3333-3333-333333333333 | Phone | Apple | 999.99          | usd               | 0        |
    And update shopping cart request json is documented
      """
      {
        "lines": [
          {
            "productId": "33333333-3333-3333-3333-333333333333",
            "name": "Phone",
            "brand": "Apple",
            "unitPrice": {
              "amount": 999.99,
              "currency": "usd"
            },
            "quantity": 0
          }
        ]
      }
      """
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
    And update shopping cart validation error fields are documented
      | Field                   | Value                              |
      | statusCode              | 400                                |
      | title                   | Validation failed.                 |
      | detail                  | One or more validation errors occurred. |
      | type                    | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | instance                | /shopping-carts/{clientId}         |
      | errors[0].message       | Quantity must be greater than zero. |
    And update shopping cart validation error json is documented
      """
      {
        "statusCode": 400,
        "title": "Validation failed.",
        "detail": "One or more validation errors occurred.",
        "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1",
        "instance": "/shopping-carts/{clientId}",
        "errors": [
          {
            "message": "Quantity must be greater than zero."
          }
        ]
      }
      """
