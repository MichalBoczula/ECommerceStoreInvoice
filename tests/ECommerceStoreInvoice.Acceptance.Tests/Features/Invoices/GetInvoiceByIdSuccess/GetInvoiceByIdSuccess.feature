Feature: Get invoice by id

  @products-api
  @allure.label.scenarioId:INV01_GetInvoiceById_03
  Scenario: Get invoice by id returns an existing invoice
    Setup creates an invoice for a paid order using explicit client details. Retrieval by invoice ID returns the expected invoice identity, order and storage metadata.
    Given I have an existing invoice id
    And the get invoice by id request is documented as
      | Field              | Value                 |
      | Method             | GET                   |
      | EndpointTemplate   | /invoices/{invoiceId} |
      | HasInvoiceId       | true                  |
      | InvoiceIdSource    | setup-created-invoice |
      | Accept             | application/json      |
    When I request invoice by id
    Then the invoice is returned successfully by id
      | Field                  | Value |
      | StatusCode             | 200   |
      | HasId                  | true  |
      | HasOrderId             | true  |
      | HasClientDataVersionId | true  |
      | HasStorageUrl          | true  |
      | HasCreatedAt           | true  |
      | OrderIdSource          | setup-created-order |
      | IdSource               | setup-created-invoice |
