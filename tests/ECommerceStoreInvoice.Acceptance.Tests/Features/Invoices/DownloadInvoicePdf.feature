Feature: Download invoice PDF
  Scenario Outline: Only completed invoices with an available PDF can be downloaded
    Each example selects a completed, incomplete, missing-file, missing-invoice or invalid-ID fixture. Only a completed invoice with an available file returns a PDF; other states return the expected safe error status.
    Given an invoice PDF fixture in state "<state>"
    When I download the invoice PDF
    Then invoice PDF download returns status <status>

    Examples:
      | state        | status | scenarioId |
      | Completed    | 200    | INV01_DownloadInvoicePdf_01 |
      | Generating   | 404    | INV01_DownloadInvoicePdf_02 |
      | Failed       | 404    | INV01_DownloadInvoicePdf_03 |
      | missing      | 404    | INV01_DownloadInvoicePdf_04 |
      | missing-file | 404    | INV01_DownloadInvoicePdf_05 |
      | empty        | 400    | INV01_DownloadInvoicePdf_06 |

  @pdf-download-fails
  @allure.label.scenarioId:INV01_DownloadInvoicePdf_07
  Scenario: Storage failure produces a safe error
    A completed invoice exists, but its PDF storage reader throws an internal error. Download returns a safe server problem without exposing the private storage failure message.
    Given an invoice PDF fixture in state "Completed"
    When I download the invoice PDF
    Then invoice PDF download returns status 500
