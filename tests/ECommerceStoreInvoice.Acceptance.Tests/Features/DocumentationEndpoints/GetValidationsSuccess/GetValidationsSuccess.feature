Feature: Get validation documentation

  Scenario: Get validation documentation returns validation descriptors
    Given I am ready to retrieve validation documentation
    When I request validation documentation
    Then the validation documentation is returned successfully
      | Field                                       | Value |
      | StatusCode                                  | 200   |
      | ValidationsCount                            | 7     |
      | HasPolicy_ClientValidationPolicy            | true  |
      | HasPolicy_ShoppingCartLineValidationPolicy  | true  |
      | HasPolicy_ClientDataVersionValidationPolicy | true  |
      | HasPolicy_OrderValidationPolicy             | true  |
      | HasPolicy_UpdateOrderValidationPolicy       | true  |
      | HasPolicy_InvoiceValidationPolicy           | true  |
      | HasPolicy_ProductVersionValidationPolicy    | true  |
