Feature: Invoice generation is single-writer and retryable
  The Order already contains product snapshots; invoice requests use those saved versions.

  @products-api @pdf-fails-once
  Scenario: A failed PDF generation can be retried for the same order
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When I request an invoice for the current order
    Then PDF failure returns 500 and leaves a retryable reservation
    When I request an invoice for the current order
    Then retry completes the same invoice and its PDF is available

  @products-api
  Scenario: Concurrent invoice requests create one accessible PDF
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When I concurrently request two invoices for the current order
    Then one invoice is completed and its PDF is available
