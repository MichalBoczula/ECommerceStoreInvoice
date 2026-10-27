Feature: Get invoice by id validation error

  @allure.label.scenarioId:INV01_GetInvoiceById_04
  Scenario: Get invoice by id returns problem details when validation fails
    The invoice lookup uses an all-zero identifier. It returns the expected identifier validation problem.
    Given I have an invalid invoice id for invoice retrieval
      | Field     | Value                                |
      | InvoiceId | 00000000-0000-0000-0000-000000000000 |
    When I request invoice by invalid invoice id
    Then problem details are returned for get invoice by id validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /invoices/00000000-0000-0000-0000-000000000000              |
      | ErrorsCount       | 1                                                            |
      | FirstErrorName    | ClientIdIsEmptyValidationRule                                 |
      | FirstErrorEntity  | ShoppingCart                                                 |
      | FirstErrorMessage | ClientId cannot be empty Guid.                               |
