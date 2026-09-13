Feature: Update shopping cart

  Scenario: Update shopping cart returns updated shopping cart
    Given I have an existing shopping cart for update
    And I have a valid update shopping cart request
      | Field                     | Value                                |
      | Line1ProductId            | 11111111-1111-1111-1111-111111111111 |
      | Line1Quantity             | 2                                    |
      | Line2ProductId            | 22222222-2222-2222-2222-222222222222 |
      | Line2Quantity             | 1                                    |
    When I submit the update shopping cart request
    Then the shopping cart is updated successfully
      | Field                      | Value                                |
      | StatusCode                 | 200                                  |
      | LinesCount                 | 2                                    |
      | Line1ProductId             | 11111111-1111-1111-1111-111111111111 |
      | Line1Quantity              | 2                                    |
      | Line2ProductId             | 22222222-2222-2222-2222-222222222222 |
      | Line2Quantity              | 1                                    |
