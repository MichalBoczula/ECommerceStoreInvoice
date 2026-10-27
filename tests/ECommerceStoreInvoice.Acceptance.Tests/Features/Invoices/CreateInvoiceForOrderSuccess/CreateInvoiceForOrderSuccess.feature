Feature: Create invoice for order

  @products-api
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_04
  Scenario: Create invoice for order returns created invoice
    A paid order and client data version exist. Invoice creation returns generated invoice metadata linked to that order, client version and the available local test PDF.
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
      | ProductName                | Xiaomi POCO F7 12/512GB Black            |
      | ProductBrand               | Xiaomi            |
      | UnitPriceAmount            | 2499.00            |
      | UnitPriceCurrency          | PLN               |
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
