Feature: Shopping cart flow failures
  The read flow rejects an empty client id and the create flow rejects an existing cart.

  Scenario: An empty client identifier is invalid when reading the cart
    When I read a shopping cart with an empty client id
    Then the cart response is problem 400

  Scenario: An existing shopping cart cannot be created twice
    Given a shopping cart has already been created
    When I create the same shopping cart again
    Then the cart response is problem 409
