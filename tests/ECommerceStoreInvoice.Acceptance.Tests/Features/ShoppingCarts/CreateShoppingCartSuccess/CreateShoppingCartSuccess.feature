Feature: Create shopping cart

  @allure.label.scenarioId:INV01_CreateShoppingCart_02
  Scenario: Create shopping cart returns created shopping cart
    A valid client requests a new cart. The successful response contains generated cart identity, timestamps and the expected empty line collection.
    Given I prepare the create shopping cart request
      | Field    | Value |
      | ClientId | auto  |
    When I submit the create shopping cart request
    Then the shopping cart response should match
      | Field         | Value |
      | StatusCode    | 200   |
      | HasId         | true  |
      | HasClientId   | true  |
      | HasCreatedAt  | true  |
      | HasUpdatedAt  | true  |
      | LinesCount    | 0     |
