Feature: Download invoice PDF
  Scenario Outline: Only completed invoices with an available PDF can be downloaded
    Given an invoice PDF fixture in state "<state>"
    When I download the invoice PDF
    Then invoice PDF download returns status <status>

    Examples:
      | state        | status |
      | Completed    | 200    |
      | Generating   | 404    |
      | Failed       | 404    |
      | missing      | 404    |
      | missing-file | 404    |
      | empty        | 400    |

  @pdf-download-fails
  Scenario: Storage failure produces a safe error
    Given an invoice PDF fixture in state "Completed"
    When I download the invoice PDF
    Then invoice PDF download returns status 500
