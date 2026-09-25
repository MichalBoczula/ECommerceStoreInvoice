Feature: Update order status
  Status changes are validated by the Order flow before they are persisted.

  @products-api
  Scenario: A created order can become paid
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Paid"
    Then the order status response is 200 with status "Paid"

  @products-api
  Scenario: An unknown status is rejected
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Unknown"
    Then the order status response is 400 and the stored status is "Created"

  Scenario: A missing order cannot be updated
    Given I have an order id that does not exist
    When I change the order status to "Paid"
    Then the order status response is 404

  Scenario: An empty order id is invalid
    Given I have an empty order id for status update
    When I change the order status to "Paid"
    Then the order status response is 400

  @products-api
  Scenario: A created order can become cancelled
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Cancelled"
    Then the order status response is 200 with status "Cancelled"

  @products-api
  Scenario: A created order cannot be changed back to created
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Created"
    Then the order status response is 400 and the stored status is "Created"

  @products-api
  Scenario Outline: A terminal status cannot be changed
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "<current>"
    Then the order status response is 200 with status "<current>"
    When I change the order status to "<requested>"
    Then the order status response is 400 and the stored status is "<current>"

    Examples:
      | current   | requested |
      | Paid      | Paid      |
      | Paid      | Cancelled |
      | Cancelled | Paid      |
      | Cancelled | Cancelled |

  @products-api
  Scenario: Concurrent status changes cannot both succeed
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I concurrently change the order status to Paid and Cancelled
    Then exactly one status change succeeds and the stored order matches it
