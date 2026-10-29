Feature: Get validation documentation

  @allure.label.scenarioId:INV01_GetValidationDocumentation_02
  Scenario: Get validation documentation returns validation descriptors
    The documentation request reads the registered validation-policy descriptions. The response contains the expected policy definitions without implying that a business rule executed.
    Given I am ready to retrieve validation documentation
    When I request validation documentation
    Then the validation documentation is returned successfully
      | Field                                       | Value |
      | StatusCode                                  | 200   |
      | ValidationsCount                            | 8     |
      | HasPolicy_ClientValidationPolicy            | true  |
      | HasPolicy_ShoppingCartLineValidationPolicy  | true  |
      | HasPolicy_ClientDataVersionValidationPolicy | true  |
      | HasPolicy_OrderValidationPolicy             | true  |
      | HasPolicy_UpdateOrderValidationPolicy       | true  |
      | HasPolicy_InvoiceValidationPolicy           | true  |
      | HasPolicy_ProductVersionValidationPolicy    | true  |
      | HasPolicy_GuidCollectionValidationPolicy   | true  |
