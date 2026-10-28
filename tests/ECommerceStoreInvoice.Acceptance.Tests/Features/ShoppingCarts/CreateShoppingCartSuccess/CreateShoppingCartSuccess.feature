Feature: Create shopping cart

  @allure.label.scenarioId:INV01_CreateShoppingCart_02
  Scenario: Create shopping cart returns created shopping cart
    A new client creates an empty cart while another client already has populated contents.
    The complete response and real MongoDB document retain the requested identity and generated UTC timestamps; the other cart remains unchanged.
    Given I prepare the create shopping cart request
      | Field    | Value |
      | ClientId | auto  |
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
    When I submit the create shopping cart request
    Then the shopping cart response should match
      | Field        | Value |
      | StatusCode   | 200   |
      | HasId        | true  |
      | HasClientId  | true  |
      | HasCreatedAt | true  |
      | HasUpdatedAt | true  |
      | LinesCount   | 0     |
    And the shopping cart database and supporting reads match
      | Field        | Value     |
      | CartsBefore  | 1         |
      | CartsAfter   | 2         |
      | TargetBefore | 0         |
      | TargetAfter  | 1         |
      | OtherCarts   | 1         |
      | Id           | generated |
      | CreatedAt    | generated |
      | UpdatedAt    | generated |
      | OtherClients | unchanged |
