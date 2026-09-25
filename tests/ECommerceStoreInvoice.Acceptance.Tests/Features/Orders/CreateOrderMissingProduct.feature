Feature: Create order with a missing catalog product
  Order creation must fetch the cart's product from Products Catalog.

  @products-api
  Scenario: A missing product does not create an order or clear the cart
    Given I have a shopping cart containing a product missing from the catalog
      | Field    | Value |
      | Quantity | 2     |
    When I submit the create order request
    Then order creation reports the missing product and leaves the cart untouched
