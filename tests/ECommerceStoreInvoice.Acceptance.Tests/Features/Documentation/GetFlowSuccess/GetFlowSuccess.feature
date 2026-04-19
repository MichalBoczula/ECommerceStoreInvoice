Feature: Get flow documentation

  Scenario: Get flow documentation returns descriptors for all flows
    When I request flow documentation
    Then the flow documentation is returned successfully
      | Field                             | Value |
      | StatusCode                        | 200   |
      | FlowsCount                        | 13    |
      | ContainsGetShoppingCartByClientIdDescriptor | true  |
      | ContainsGetCreateShoppingCartDescriptor     | true  |
      | ContainsGetCreateOrderDescriptor            | true  |
      | ContainsGetCreateInvoiceForOrderDescriptor  | true  |
      | ContainsGetCreateClientDataVersionDescriptor | true  |
