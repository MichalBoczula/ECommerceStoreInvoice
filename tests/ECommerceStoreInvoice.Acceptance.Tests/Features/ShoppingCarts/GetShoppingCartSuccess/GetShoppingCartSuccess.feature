Feature: Get shopping cart

  Scenario: Get shopping cart by client id returns shopping cart
    Given I have an existing shopping cart for retrieval
    When I request the shopping cart by client id
    Then the shopping cart is returned successfully
      | Field         | Value |
      | StatusCode    | 200   |
      | HasId         | true  |
      | HasClientId   | true  |
      | TotalAmount   | 0     |
      | TotalCurrency | USD   |
      | LinesCount    | 0     |
