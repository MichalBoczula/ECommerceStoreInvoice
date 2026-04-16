Feature: Create shopping cart

  Scenario: Create shopping cart returns created shopping cart
    Given I have a valid client id for shopping cart creation
    When I submit the create shopping cart request
    Then the shopping cart is created successfully
      | Field         | Value |
      | StatusCode    | 200   |
      | HasId         | true  |
      | HasClientId   | true  |
      | TotalAmount   | 0     |
      | TotalCurrency | EUR   |
      | LinesCount    | 0     |
