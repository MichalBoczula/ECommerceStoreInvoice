Feature: Update order status
  Status changes are validated by the Order flow before they are persisted.

  Scenario: A created order can become paid
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I change the order status to "Paid"
    Then the order status response is 200 with status "Paid"

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
