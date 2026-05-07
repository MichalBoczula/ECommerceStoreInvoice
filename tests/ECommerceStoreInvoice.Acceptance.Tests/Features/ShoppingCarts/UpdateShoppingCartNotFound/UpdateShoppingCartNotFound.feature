@allure.description:Ensures_updating_a_non_existing_shopping_cart_returns_RFC7231_not_found_problem_details_with_request_context.
Feature: Update shopping cart not found

  Scenario: Update shopping cart returns problem details when shopping cart does not exist
    Given I have a non-existing client id for shopping cart update
    And I have an update shopping cart request for a non-existing shopping cart
      | ProductId                            | Name   | Brand | UnitPriceAmount | UnitPriceCurrency | Quantity |
      | 11111111-1111-1111-1111-111111111111 | Phone  | Apple | 999.99          | usd               | 1        |
      | 22222222-2222-2222-2222-222222222222 | Watch  | Apple | 399.99          | usd               | 2        |
    And the expected update shopping cart request json is
      """
      {
        "lines": [
          {
            "productId": "11111111-1111-1111-1111-111111111111",
            "name": "Phone",
            "brand": "Apple",
            "unitPriceAmount": 999.99,
            "unitPriceCurrency": "usd",
            "quantity": 1
          },
          {
            "productId": "22222222-2222-2222-2222-222222222222",
            "name": "Watch",
            "brand": "Apple",
            "unitPriceAmount": 399.99,
            "unitPriceCurrency": "usd",
            "quantity": 2
          }
        ]
      }
      """
    When I submit the update shopping cart request for a non-existing shopping cart
    Then problem details are returned for update shopping cart not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /shopping-carts/{clientId}                                   |
      | HasTraceId | true                                                         |
    And the expected update shopping cart not found response json is
      """
      {
        "title": "Resource not found.",
        "type": "https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4",
        "status": 404,
        "detail": "ShoppingCart with id '<clientId>' was not found.",
        "instance": "/shopping-carts/<clientId>",
        "traceId": "<generated>"
      }
      """
