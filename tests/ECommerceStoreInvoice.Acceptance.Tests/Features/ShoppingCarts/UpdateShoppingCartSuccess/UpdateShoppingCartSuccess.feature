Feature: Update shopping cart

  Scenario: Update shopping cart returns updated shopping cart
    Given I have an existing shopping cart for update
    And I have a valid update shopping cart request
      | Field                     | Value                                |
      | Line1ProductId            | 11111111-1111-1111-1111-111111111111 |
      | Line1Name                 | Phone                                |
      | Line1Brand                | Apple                                |
      | Line1UnitPriceAmount      | 999.99                               |
      | Line1UnitPriceCurrency    | usd                                  |
      | Line1Quantity             | 2                                    |
      | Line2ProductId            | 22222222-2222-2222-2222-222222222222 |
      | Line2Name                 | Watch                                |
      | Line2Brand                | Apple                                |
      | Line2UnitPriceAmount      | 399.99                               |
      | Line2UnitPriceCurrency    | usd                                  |
      | Line2Quantity             | 1                                    |
    When I submit the update shopping cart request
    Then the shopping cart is updated successfully
      | Field                      | Value                                |
      | StatusCode                 | 200                                  |
      | HasId                      | true                                 |
      | HasClientId                | true                                 |
      | ClientId                   | from-scenario-client-id              |
      | TotalAmount                | 2399.97                              |
      | TotalCurrency              | USD                                  |
      | LinesCount                 | 2                                    |
      | Line1Name                  | Phone                                |
      | Line1Brand                 | Apple                                |
      | Line1Quantity              | 2                                    |
      | Line1TotalAmount           | 1999.98                              |
      | Line1TotalCurrency         | USD                                  |
      | Line2Name                  | Watch                                |
      | Line2Brand                 | Apple                                |
      | Line2Quantity              | 1                                    |
      | Line2TotalAmount           | 399.99                               |
      | Line2TotalCurrency         | USD                                  |
