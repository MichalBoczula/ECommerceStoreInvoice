Feature: Get completed invoice by order id
  Scenario Outline: Order lookup returns only completed invoices
    Each example selects an invoice generation state or an absent/invalid order identifier. Lookup by order returns completed and legacy completed metadata, while incomplete or missing invoices stay hidden.
    Given an invoice lookup fixture in state "<state>"
    When I retrieve the invoice by its order id
    Then invoice order lookup returns status <status>

    Examples:
      | state      | status | scenarioId |
      | Completed  | 200    | INV01_GetInvoiceByOrderId_01 |
      | legacy     | 200    | INV01_GetInvoiceByOrderId_02 |
      | Generating | 404    | INV01_GetInvoiceByOrderId_03 |
      | Failed     | 404    | INV01_GetInvoiceByOrderId_04 |
      | missing    | 404    | INV01_GetInvoiceByOrderId_05 |
      | empty      | 400    | INV01_GetInvoiceByOrderId_06 |
