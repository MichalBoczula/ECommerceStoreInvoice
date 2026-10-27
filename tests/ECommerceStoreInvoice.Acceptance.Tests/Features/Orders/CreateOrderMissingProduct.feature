Feature: Create order with a missing catalog product
  Order creation must fetch the cart's product from Products Catalog.

  @products-api
  @allure.label.scenarioId:INV01_CreateOrder_02
  Scenario: A missing product does not create an order or clear the cart
    A cart contains a product identifier that ProductsCatalog does not return. Checkout reports the missing product, preserves the cart line and leaves the client without a created order.
    Given I have a shopping cart containing a product missing from the catalog
      | Field    | Value |
      | Quantity | 2     |
    When I submit the create order request
    Then order creation reports the missing product and leaves the cart untouched
