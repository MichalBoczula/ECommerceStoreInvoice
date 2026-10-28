Feature: Get shopping cart

  @allure.label.scenarioId:INV01_GetShoppingCartByClientId_03
  Scenario: Get shopping cart by client id returns shopping cart
    A client reads its empty cart while another client has populated contents.
    The complete response matches the saved identity and timestamps; both stored documents remain unchanged through the primary and supporting reads.
    Given I have an existing shopping cart for retrieval
    And a stored shopping cart for another client
      | Field          | Value                                |
      | Id             | generated                            |
      | CreatedAt      | 2020-01-02T03:04:05.123Z             |
      | UpdatedAt      | 2020-02-03T04:05:06.456Z             |
      | LinesCount     | 2                                    |
      | Line1ProductId | 44444444-4444-4444-4444-444444444444 |
      | Line1Quantity  | 4                                    |
      | Line2ProductId | 55555555-5555-5555-5555-555555555555 |
      | Line2Quantity  | 5                                    |
    And the get shopping cart request data is
      | Field    | Value                              |
      | Method   | GET                                |
      | Endpoint | /shopping-carts/client             |
      | ClientId | <existing shopping cart client id> |
    When I request the shopping cart by client id
    Then the shopping cart is returned successfully
      | Field        | Value |
      | StatusCode   | 200   |
      | HasId        | true  |
      | HasClientId  | true  |
      | HasCreatedAt | true  |
      | HasUpdatedAt | true  |
      | LinesCount   | 0     |
    And the shopping cart response payload is
      | Field     | Value                              |
      | Id        | <generated guid>                   |
      | ClientId  | <existing shopping cart client id> |
      | CreatedAt | <generated timestamp>              |
      | UpdatedAt | <generated timestamp>              |
      | Lines     | []                                 |
    And the shopping cart database and supporting reads match
      | Field        | Value     |
      | CartsBefore  | 2         |
      | CartsAfter   | 2         |
      | TargetBefore | 1         |
      | TargetAfter  | 1         |
      | OtherCarts   | 1         |
      | Id           | retained  |
      | CreatedAt    | retained  |
      | UpdatedAt    | retained  |
      | OtherClients | unchanged |

  @allure.label.scenarioId:INV03_GetShoppingCart_Populated
  Scenario: Reading a populated cart returns only the requested client contents
    Two clients have distinct populated carts with known historical UTC timestamps.
    Reading the target returns every saved line and timestamp and leaves both complete MongoDB documents unchanged.
    Given a stored shopping cart for the target client
      | Field          | Value                                |
      | Id             | generated                            |
      | CreatedAt      | 2020-01-02T03:04:05.123Z             |
      | UpdatedAt      | 2020-02-03T04:05:06.456Z             |
      | LinesCount     | 2                                    |
      | Line1ProductId | 11111111-1111-1111-1111-111111111111 |
      | Line1Quantity  | 7                                    |
      | Line2ProductId | 33333333-3333-3333-3333-333333333333 |
      | Line2Quantity  | 9                                    |
    And a stored shopping cart for another client
      | Field          | Value                                |
      | Id             | generated                            |
      | CreatedAt      | 2020-01-02T03:04:05.123Z             |
      | UpdatedAt      | 2020-02-03T04:05:06.456Z             |
      | LinesCount     | 2                                    |
      | Line1ProductId | 44444444-4444-4444-4444-444444444444 |
      | Line1Quantity  | 4                                    |
      | Line2ProductId | 55555555-5555-5555-5555-555555555555 |
      | Line2Quantity  | 5                                    |
    And the get shopping cart request data is
      | Field    | Value                              |
      | Method   | GET                                |
      | Endpoint | /shopping-carts/client             |
      | ClientId | <existing shopping cart client id> |
    When I request the shopping cart by client id
    Then the shopping cart is returned successfully
      | Field          | Value                                |
      | StatusCode     | 200                                  |
      | HasId          | true                                 |
      | HasClientId    | true                                 |
      | HasCreatedAt   | true                                 |
      | HasUpdatedAt   | true                                 |
      | LinesCount     | 2                                    |
      | Line1ProductId | 11111111-1111-1111-1111-111111111111 |
      | Line1Quantity  | 7                                    |
      | Line2ProductId | 33333333-3333-3333-3333-333333333333 |
      | Line2Quantity  | 9                                    |
    And the shopping cart database and supporting reads match
      | Field        | Value     |
      | CartsBefore  | 2         |
      | CartsAfter   | 2         |
      | TargetBefore | 1         |
      | TargetAfter  | 1         |
      | OtherCarts   | 1         |
      | Id           | retained  |
      | CreatedAt    | retained  |
      | UpdatedAt    | retained  |
      | OtherClients | unchanged |
