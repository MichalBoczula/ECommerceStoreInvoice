Feature: Get flow documentation

  @allure.label.scenarioId:INV01_GetFlowDocumentation_02
  Scenario: Get flow documentation returns descriptors for all flows
    The Invoice API is available without any business entity fixture. The documentation endpoint returns the published flow descriptors and the expected inventory.
    Given the Invoice API is available
    When I request flow documentation
    Then the flow documentation is returned successfully
      | Field                             | Value |
      | StatusCode                        | 200   |
      | FlowsCount                        | 15    |
      | ContainsGetShoppingCartByClientIdDescriptor | true  |
      | ContainsGetCreateShoppingCartDescriptor     | true  |
      | ContainsGetCreateOrderDescriptor            | true  |
      | ContainsGetCreateInvoiceForOrderDescriptor  | true  |
      | ContainsGetCreateClientDataVersionDescriptor | true  |
