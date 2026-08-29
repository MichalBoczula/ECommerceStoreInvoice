Feature: Get shopping cart

  Scenario: Get shopping cart by client id returns shopping cart
    Given I have an existing shopping cart for retrieval
    And the get shopping cart request data is
      | Field    | Value                  |
      | Method   | GET                    |
      | Endpoint | /shopping-carts/client |
      | ClientId | <existing shopping cart client id> |
    When I request the shopping cart by client id
    Then the shopping cart is returned successfully
      | Field         | Value |
      | StatusCode    | 200   |
      | HasId         | true  |
      | HasClientId   | true  |
      | HasCreatedAt  | true  |
      | HasUpdatedAt  | true  |
      | LinesCount    | 0     |
    And the shopping cart response payload is
      | Field         | Value |
      | Id            | <generated guid> |
      | ClientId      | <existing shopping cart client id> |
      | CreatedAt     | <generated timestamp> |
      | UpdatedAt     | <generated timestamp> |
      | Lines         | []    |
