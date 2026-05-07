Feature: Create shopping cart

  Scenario: Create shopping cart returns created shopping cart
    Given I prepare the create shopping cart request
      | Field    | Value |
      | ClientId | auto  |
    When I submit the create shopping cart request
    Then the shopping cart response should match
      | Field         | Value |
      | StatusCode    | 200   |
      | HasId         | true  |
      | HasClientId   | true  |
      | TotalAmount   | 0     |
      | TotalCurrency | USD   |
      | LinesCount    | 0     |
