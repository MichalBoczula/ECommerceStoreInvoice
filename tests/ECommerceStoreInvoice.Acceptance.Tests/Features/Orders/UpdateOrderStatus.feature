Feature: Update order status
  Status changes are validated by the Order flow before they are persisted.

  @products-api
  @allure.label.scenarioId:INV01_UpdateOrderStatus_02
  Scenario: A created order can become paid
    An existing order has Created status and saved product snapshots. Changing it to Paid returns a successful order response and the persisted status becomes Paid.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Paid"
    Then the order status response is 200 with status "Paid"

  @products-api
  @allure.label.scenarioId:INV01_UpdateOrderStatus_03
  Scenario: An unknown status is rejected
    An existing order has Created status. An unrecognized status value returns a validation problem and the saved order remains Created.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Unknown"
    Then the order status response is 400 and the stored status is "Created"

  @allure.label.scenarioId:INV01_UpdateOrderStatus_04
  Scenario: A missing order cannot be updated
    The requested order identifier has no stored order. Updating its status returns a not-found problem.
    Given I have an order id that does not exist
    When I change the order status to "Paid"
    Then the order status response is 404

  @allure.label.scenarioId:INV01_UpdateOrderStatus_05
  Scenario: An empty order id is invalid
    The status request uses an all-zero order identifier. It returns a validation problem before a valid order can be loaded.
    Given I have an empty order id for status update
    When I change the order status to "Paid"
    Then the order status response is 400

  @products-api
  @allure.label.scenarioId:INV01_UpdateOrderStatus_06
  Scenario: A created order can become cancelled
    An existing order has Created status and saved product snapshots. Changing it to Cancelled returns a successful order response and the persisted status becomes Cancelled.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Cancelled"
    Then the order status response is 200 with status "Cancelled"

  @products-api
  @allure.label.scenarioId:INV01_UpdateOrderStatus_07
  Scenario: A created order cannot be changed back to created
    An order is already Created. Requesting Created again is an invalid transition and preserves its saved status and update timestamp.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Created"
    Then the order status response is 400 and the stored status is "Created"

  @products-api
  Scenario Outline: A terminal status cannot be changed
    The first request moves a Created order to the example terminal status. The next requested transition is rejected, including repeated Paid, while the saved terminal status and timestamp remain unchanged.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "<current>"
    Then the order status response is 200 with status "<current>"
    When I change the order status to "<requested>"
    Then the order status response is 400 and the stored status is "<current>"

    Examples:
      | current   | requested | scenarioId |
      | Paid      | Paid      | INV01_UpdateOrderStatus_08 |
      | Paid      | Cancelled | INV01_UpdateOrderStatus_09 |
      | Cancelled | Paid      | INV01_UpdateOrderStatus_10 |
      | Cancelled | Cancelled | INV01_UpdateOrderStatus_11 |

  @products-api
  @allure.label.scenarioId:INV01_UpdateOrderStatus_12
  Scenario: Concurrent status changes cannot both succeed
    Paid and Cancelled requests compete for the same Created order. Exactly one succeeds; the loser is a terminal-state 400 or a stale-write 409 depending on read timing, and the saved order matches the winner.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I concurrently change the order status to Paid and Cancelled
    Then exactly one status change succeeds and the stored order matches it
