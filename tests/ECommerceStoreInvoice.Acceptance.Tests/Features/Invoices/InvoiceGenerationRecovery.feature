Feature: Invoice generation is single-writer and retryable
  The Order already contains product snapshots; invoice requests use those saved versions.

  @products-api @pdf-fails-once
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_10
  Scenario: A failed PDF generation can be retried for the same order
    A paid order and client data are ready for invoicing. The first PDF attempt fails and leaves a retryable reservation; the next request completes the same invoice and leaves its local test PDF available.
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When I request an invoice for the current order
    Then PDF failure returns 500 and leaves a retryable reservation
    When I request an invoice for the current order
    Then retry completes the same invoice and its PDF is available

  @products-api
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_11
  Scenario: Concurrent invoice requests create one accessible PDF
    Two requests attempt to invoice the same paid order. One completes and one receives a conflict, leaving one completed invoice document with an available local test PDF.
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When I concurrently request two invoices for the current order
    Then one invoice is completed and its PDF is available

  @products-api @completion-ack-lost
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_12
  Scenario: Lost completion acknowledgement is recovered from Mongo
    Invoice completion commits in MongoDB before the caller receives an injected acknowledgement failure. Reading the saved completion recovers the successful invoice and preserves its PDF.
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When I request an invoice for the current order
    Then the completed invoice is returned after the acknowledgement is lost
