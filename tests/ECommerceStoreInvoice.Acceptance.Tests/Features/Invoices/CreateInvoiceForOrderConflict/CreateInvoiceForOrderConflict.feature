@allure.description:Ensures_creating_invoice_for_order_when_invoice_already_exists_returns_conflict_problem_details.
Feature: Create invoice for order conflict

  Scenario: Create invoice for order returns conflict when invoice already exists
    Given I have an existing invoice for a paid order
    When I submit the duplicate create invoice for order request
    Then duplicate create invoice for order returns conflict
      | Field               | Value |
      | StatusCode          | 409   |
      | Title               | Conflict. |
      | HasDetailWithOrderId| true  |
      | Type                | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.8 |
