Feature: Get invoice by id not found

  @allure.label.scenarioId:INV01_GetInvoiceById_02
  Scenario: Get invoice by id returns problem details when invoice does not exist
    The invoice identifier has no saved completed invoice. Retrieval returns the expected not-found problem.
    Given I have a non-existing invoice id for invoices retrieval
      | Field           | Value                 |
      | Method          | GET                   |
      | Endpoint        | /invoices/{invoiceId} |
      | InvoiceIdSource | GenerateNewGuid       |
    When I request invoice by id for non-existing invoice
    Then problem details are returned for get invoice by id not found
      | Field              | Value                                                        |
      | StatusCode         | 404                                                          |
      | Title              | Resource not found.                                          |
      | Type               | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail          | true                                                         |
      | DetailContains     | Invoice                                                      |
      | DetailContainsGuid | true                                                         |
      | Instance           | /invoices/{invoiceId}                                        |
      | HasTraceId         | true                                                         |
