Feature: Get completed invoice by order id
  Scenario Outline: Order lookup returns only completed invoices
    Given an invoice lookup fixture in state "<state>"
    When I retrieve the invoice by its order id
    Then invoice order lookup returns status <status>

    Examples:
      | state      | status |
      | Completed  | 200    |
      | legacy     | 200    |
      | Generating | 404    |
      | Failed     | 404    |
      | missing    | 404    |
      | empty      | 400    |
