Feature: Shopping cart flow failures
  The read flow rejects an empty client id and the create flow rejects an existing cart.

  @allure.label.scenarioId:INV01_GetShoppingCartByClientId_04
  Scenario: An empty client identifier is invalid when reading the cart
    The Invoice API is available and the request uses an all-zero client identifier. Reading the cart returns a validation problem instead of looking up a valid client cart.
    Given the Invoice API is available
    When I read a shopping cart with an empty client id
    Then the cart response is problem 400

  @allure.label.scenarioId:INV01_CreateShoppingCart_04
  Scenario: An existing shopping cart cannot be created twice
    A client already has a shopping cart. Creating another cart for that same client returns a conflict.
    Given a shopping cart has already been created
    When I create the same shopping cart again
    Then the cart response is problem 409
