@allure.description:Ensures_creating_invoice_for_paid_order_returns_a_success_response_with_created_invoice_fields.
Feature: Create invoice for order

  Scenario: Create invoice for order returns created invoice
    Given I have a paid order for invoice creation
    When I submit the create invoice for order request
    Then the invoice is created successfully
      | Field                    | Value |
      | StatusCode               | 200   |
      | HasId                    | true  |
      | HasOrderId               | true  |
      | HasClientDataVersionId   | true  |
      | HasStorageUrl            | true  |
      | HasCreatedAt             | true  |
