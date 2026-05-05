@allure.description:Ensures_creating_invoice_for_paid_order_returns_a_success_response_with_created_invoice_fields.
Feature: Create invoice for order

  Scenario: Create invoice for order returns created invoice
    Given I have a paid order for invoice creation
      | Field                      | Value             |
      | ClientName                 | John Doe          |
      | PostalCode                 | 00-001            |
      | City                       | NewYork           |
      | Street                     | Main.St           |
      | BuildingNumber             | 10A               |
      | ApartmentNumber            | 5                 |
      | PhoneNumber                | 123456789         |
      | PhonePrefix                | 48                |
      | AddressEmail               | john.doe@test.com |
      | ProductName                | Laptop            |
      | ProductBrand               | Lenovo            |
      | UnitPriceAmount            | 999.99            |
      | UnitPriceCurrency          | usd               |
      | Quantity                   | 2                 |
      | OrderStatusAfterCreation   | Paid              |
    When I submit the create invoice for order request
      | Field      | Value                          |
      | HttpMethod | POST                           |
      | Route      | /invoices/{clientId}/{orderId} |
    Then the invoice is created successfully
      | Field                    | Value |
      | StatusCode               | 200   |
      | HasId                    | true  |
      | HasOrderId               | true  |
      | HasClientDataVersionId   | true  |
      | HasStorageUrl            | true  |
      | HasCreatedAt             | true  |
