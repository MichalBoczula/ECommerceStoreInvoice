Feature: Invoice creation rules
  An invoice requires the correct client, a paid order and client data.

  @products-api
  Scenario: An order that has not been paid cannot be invoiced
    Given I have an existing order id with setup data
      | Field    | Value |
      | Quantity | 2     |
    When I request an invoice for the current order
    Then invoice creation fails with status 400

  @products-api
  Scenario: Another client's order cannot be invoiced
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When a different client requests an invoice for the order
    Then invoice creation fails with status 404

  @products-api
  Scenario: A paid order without a client data version cannot be invoiced
    Given I have a paid order without client data
    When I request an invoice for the current order
    Then invoice creation fails with status 404

  @products-api
  Scenario: An empty client identifier is invalid
    Given I have a paid order for invoice creation
      | Field    | Value |
      | Quantity | 2     |
    When an empty client id requests an invoice for the order
    Then invoice creation fails with status 400
