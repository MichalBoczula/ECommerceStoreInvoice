@allure.description:Ensures updating a non-existing shopping cart returns RFC7231 not-found problem details with request context.
Feature: Update shopping cart not found

  Scenario: Update shopping cart returns problem details when shopping cart does not exist
    Given I have a non-existing client id for shopping cart update
    And I have an update shopping cart request for a non-existing shopping cart
      | ProductId                            | Name   | Brand | UnitPriceAmount | UnitPriceCurrency | Quantity |
      | 11111111-1111-1111-1111-111111111111 | Phone  | Apple | 999.99          | usd               | 1        |
      | 22222222-2222-2222-2222-222222222222 | Watch  | Apple | 399.99          | usd               | 2        |
    When I submit the update shopping cart request for a non-existing shopping cart
    Then problem details are returned for update shopping cart not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /shopping-carts/{clientId}                                   |
      | HasTraceId | true                                                         |
