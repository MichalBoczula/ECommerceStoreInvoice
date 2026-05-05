@allure.description:Ensures_creating_invoice_for_non_existing_order_returns_RFC7231_not_found_problem_details_with_request_context.
Feature: Create invoice for order not found

  Scenario: Create invoice for order returns problem details when order does not exist
    Given I have an existing client and a non-existing order id for invoice creation
      | Field           | Value             |
      | ClientName      | John Doe          |
      | PostalCode      | 00-001            |
      | City            | NewYork           |
      | Street          | Main.St           |
      | BuildingNumber  | 10A               |
      | ApartmentNumber | 5                 |
      | PhoneNumber     | 123456789         |
      | PhonePrefix     | 48                |
      | AddressEmail    | john.doe@test.com |
    When I submit create invoice for a non-existing order request
      | Field    | Value                         |
      | Endpoint | /invoices/{clientId}/{orderId} |
      | Method   | POST                          |
      | BodyJson | null                          |
    Then problem details are returned for create invoice order not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /invoices/{clientId}/{orderId}                               |
      | HasTraceId | true                                                         |
