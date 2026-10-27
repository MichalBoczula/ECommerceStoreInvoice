Feature: Invoice creation rules
  An invoice requires the correct client, a paid order and client data.

  @products-api
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_06
  Scenario: An order that has not been paid cannot be invoiced
    An order still has Created status. Requesting an invoice returns a validation problem because invoice generation requires a paid order.
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I request an invoice for the current order
    Then invoice creation fails with status 400

  @products-api
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_07
  Scenario: Another client's order cannot be invoiced
    A paid order belongs to one client. A different client requests an invoice and receives the safe not-found response for that order.
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When a different client requests an invoice for the order
    Then invoice creation fails with status 404

  @products-api
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_08
  Scenario: A paid order without a client data version cannot be invoiced
    A paid order exists without a client data version. Invoice creation returns a missing-resource problem instead of completing an invoice without client details.
    Given I have a paid order without client data
    When I request an invoice for the current order
    Then invoice creation fails with status 404

  @products-api
  @allure.label.scenarioId:INV01_CreateInvoiceForOrder_09
  Scenario: An empty client identifier is invalid
    An invoice request uses an all-zero client identifier for an existing order. The request is rejected with a validation problem before invoice generation.
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When an empty client id requests an invoice for the order
    Then invoice creation fails with status 400
