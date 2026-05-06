@allure.description:Ensures_getting_invoice_by_id_returns_the_existing_invoice_payload.
Feature: Get invoice by id

  Scenario: Get invoice by id returns an existing invoice
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
