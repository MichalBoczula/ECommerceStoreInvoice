@allure.description:Ensures_creating_invoice_for_order_with_invalid_order_id_returns_RFC7231_bad_request_problem_details_with_validation_errors.
Feature: Create invoice for order validation error

  Scenario: Create invoice for order returns problem details when order id validation fails
    Given I have invoice creation identifiers
      | Field    | Value                                |
      | ClientId | 11111111-1111-1111-1111-111111111111 |
      | OrderId  | 00000000-0000-0000-0000-000000000000 |
    When I submit the create invoice request with invalid order id
    Then problem details are returned for create invoice for order validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /invoices/11111111-1111-1111-1111-111111111111/00000000-0000-0000-0000-000000000000 |
      | ErrorsCount       | 1                                                            |
      | FirstErrorMessage | ClientId cannot be empty Guid.                               |
