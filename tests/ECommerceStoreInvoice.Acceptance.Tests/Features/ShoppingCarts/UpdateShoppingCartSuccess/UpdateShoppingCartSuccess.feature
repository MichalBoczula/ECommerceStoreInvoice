Feature: Update shopping cart

  @allure.label.scenarioId:INV01_UpdateShoppingCart_03
  Scenario: Update shopping cart returns updated shopping cart
    A populated cart receives the two original requested product IDs and quantities. The operation replaces the old collection, removes the unrequested product, retains cart identity and CreatedAt, advances UpdatedAt, and preserves another client.
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
    And I have a valid update shopping cart request
      | Field          | Value                                |
      | LinesCount     | 2                                    |
      | Line1ProductId | 11111111-1111-1111-1111-111111111111 |
      | Line1Quantity  | 2                                    |
      | Line2ProductId | 22222222-2222-2222-2222-222222222222 |
      | Line2Quantity  | 1                                    |
    When I submit the update shopping cart request
    Then the shopping cart is updated successfully
      | Field          | Value                                |
      | StatusCode     | 200                                  |
      | HasId          | true                                 |
      | HasClientId    | true                                 |
      | HasCreatedAt   | true                                 |
      | HasUpdatedAt   | true                                 |
      | LinesCount     | 2                                    |
      | Line1ProductId | 11111111-1111-1111-1111-111111111111 |
      | Line1Quantity  | 2                                    |
      | Line2ProductId | 22222222-2222-2222-2222-222222222222 |
      | Line2Quantity  | 1                                    |
    And the shopping cart database and supporting reads match
      | Field        | Value     |
      | CartsBefore  | 2         |
      | CartsAfter   | 2         |
      | TargetBefore | 1         |
      | TargetAfter  | 1         |
      | OtherCarts   | 1         |
      | Id           | retained  |
      | CreatedAt    | retained  |
      | UpdatedAt    | advanced  |
      | OtherClients | unchanged |

  @allure.label.scenarioId:INV03_UpdateShoppingCart_Replace
  Scenario: Replacing cart contents changes quantity and removes unrequested products
    The request retains one existing product at a new quantity and omits the other. The complete response, persisted lines and follow-up reads must contain exactly the requested replacement while another client remains unchanged.
    Given a stored shopping cart for the target client
      | Field          | Value                                |
      | Id             | generated                            |
      | CreatedAt      | 2020-01-02T03:04:05.123Z             |
      | UpdatedAt      | 2020-02-03T04:05:06.456Z             |
      | LinesCount     | 2                                    |
      | Line1ProductId | 11111111-1111-1111-1111-111111111111 |
      | Line1Quantity  | 7                                    |
      | Line2ProductId | 22222222-2222-2222-2222-222222222222 |
      | Line2Quantity  | 8                                    |
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
    And I have a valid update shopping cart request
      | Field          | Value                                |
      | LinesCount     | 1                                    |
      | Line1ProductId | 22222222-2222-2222-2222-222222222222 |
      | Line1Quantity  | 3                                    |
    When I submit the update shopping cart request
    Then the shopping cart is updated successfully
      | Field          | Value                                |
      | StatusCode     | 200                                  |
      | HasId          | true                                 |
      | HasClientId    | true                                 |
      | HasCreatedAt   | true                                 |
      | HasUpdatedAt   | true                                 |
      | LinesCount     | 1                                    |
      | Line1ProductId | 22222222-2222-2222-2222-222222222222 |
      | Line1Quantity  | 3                                    |
    And the shopping cart database and supporting reads match
      | Field        | Value     |
      | CartsBefore  | 2         |
      | CartsAfter   | 2         |
      | TargetBefore | 1         |
      | TargetAfter  | 1         |
      | OtherCarts   | 1         |
      | Id           | retained  |
      | CreatedAt    | retained  |
      | UpdatedAt    | advanced  |
      | OtherClients | unchanged |

  @allure.label.scenarioId:INV03_UpdateShoppingCart_Clear
  Scenario: An empty replacement clears a populated cart
    An explicit empty Lines collection removes every previous product. The cart keeps its identity and CreatedAt, advances UpdatedAt, remains readable as empty and preserves another populated client cart.
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
    And I have a valid update shopping cart request
      | Field      | Value |
      | LinesCount | 0     |
    When I submit the update shopping cart request
    Then the shopping cart is updated successfully
      | Field        | Value |
      | StatusCode   | 200   |
      | HasId        | true  |
      | HasClientId  | true  |
      | HasCreatedAt | true  |
      | HasUpdatedAt | true  |
      | LinesCount   | 0     |
    And the shopping cart database and supporting reads match
      | Field        | Value     |
      | CartsBefore  | 2         |
      | CartsAfter   | 2         |
      | TargetBefore | 1         |
      | TargetAfter  | 1         |
      | OtherCarts   | 1         |
      | Id           | retained  |
      | CreatedAt    | retained  |
      | UpdatedAt    | advanced  |
      | OtherClients | unchanged |

  @allure.label.scenarioId:INV03_UpdateShoppingCart_Empty
  Scenario: An empty cart accepts an empty replacement
    An already empty cart accepts an explicit empty Lines collection. The stored cart stays empty with retained identity and CreatedAt and a new UpdatedAt; another populated client cart remains unchanged.
    Given a stored shopping cart for the target client
      | Field      | Value                    |
      | Id         | generated                |
      | CreatedAt  | 2020-01-02T03:04:05.123Z |
      | UpdatedAt  | 2020-02-03T04:05:06.456Z |
      | LinesCount | 0                        |
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
    And I have a valid update shopping cart request
      | Field      | Value |
      | LinesCount | 0     |
    When I submit the update shopping cart request
    Then the shopping cart is updated successfully
      | Field        | Value |
      | StatusCode   | 200   |
      | HasId        | true  |
      | HasClientId  | true  |
      | HasCreatedAt | true  |
      | HasUpdatedAt | true  |
      | LinesCount   | 0     |
    And the shopping cart database and supporting reads match
      | Field        | Value     |
      | CartsBefore  | 2         |
      | CartsAfter   | 2         |
      | TargetBefore | 1         |
      | TargetAfter  | 1         |
      | OtherCarts   | 1         |
      | Id           | retained  |
      | CreatedAt    | retained  |
      | UpdatedAt    | advanced  |
      | OtherClients | unchanged |
