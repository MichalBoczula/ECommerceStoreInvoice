Feature: Update shopping cart

  Scenario: Update shopping cart returns updated shopping cart
    Given I have an existing shopping cart for update
    And I have a valid update shopping cart request
    When I submit the update shopping cart request
    Then the shopping cart is updated successfully
      | Field                  | Value   |
      | StatusCode             | 200     |
      | HasId                  | true    |
      | HasClientId            | true    |
      | TotalAmount            | 2399.97 |
      | TotalCurrency          | USD     |
      | LinesCount             | 2       |
      | FirstLineName          | Phone   |
      | FirstLineBrand         | Apple   |
      | FirstLineQuantity      | 2       |
      | FirstLineTotalAmount   | 1999.98 |
      | FirstLineTotalCurrency | USD     |
