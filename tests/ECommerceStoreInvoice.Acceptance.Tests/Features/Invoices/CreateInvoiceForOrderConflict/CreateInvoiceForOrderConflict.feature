@allure.description:Ensures_creating_invoice_for_order_when_invoice_already_exists_returns_conflict_problem_details.
Feature: Create invoice for order conflict

  @products-api
  Scenario: Create invoice for order returns conflict when invoice already exists
    Given I have an existing invoice for a paid order
      | Field            | Value             |
      | ClientName       | John Doe          |
      | PostalCode       | 00-001            |
      | City             | NewYork           |
      | Street           | Main.St           |
      | BuildingNumber   | 10A               |
      | ApartmentNumber  | 5                 |
      | PhoneNumber      | 123456789         |
      | PhonePrefix      | 48                |
      | AddressEmail     | john.doe@test.com |
      | ProductName      | Xiaomi POCO F7 12/512GB Black            |
      | ProductBrand     | Xiaomi            |
      | UnitPriceAmount  | 2499.00            |
      | UnitPriceCurrency| PLN               |
      | Quantity         | 2                 |
      | OrderStatus      | Paid              |
    When I submit the duplicate create invoice for order request
      | Field    | Value |
      | HasBody  | false |
    Then duplicate create invoice for order returns conflict
      | Field               | Value                                                     |
      | StatusCode          | 409                                                       |
      | Title               | Conflict.                                                 |
      | HasDetailWithOrderId| true                                                      |
      | Type                | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8 |
