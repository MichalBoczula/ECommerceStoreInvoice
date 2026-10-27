Feature: Safe unexpected errors for every API operation
  Every endpoint must return a stable problem response when its dependency fails.

  Scenario Outline: <operationId> returns a safe 500
    A scenario-specific application service or descriptor provider throws an injected internal exception. The endpoint returns the generic server problem and trace identifier without exposing the private failure marker; this is a controlled dependency fault.
    Given the "<operationId>" dependency fails unexpectedly
    When I call the failing "<operationId>" endpoint
    Then the Invoice server error is safe

    Examples:
      | operationId                     | scenarioId |
      | GetShoppingCartByClientId       | INV01_GetShoppingCartByClientId_01 |
      | CreateShoppingCart              | INV01_CreateShoppingCart_01 |
      | UpdateShoppingCart              | INV01_UpdateShoppingCart_01 |
      | GetOrderById                    | INV01_GetOrderById_01 |
      | GetOrdersByClientId             | INV01_GetOrdersByClientId_01 |
      | CreateOrder                     | INV01_CreateOrder_01 |
      | UpdateOrderStatus               | INV01_UpdateOrderStatus_01 |
      | GetInvoiceById                  | INV01_GetInvoiceById_01 |
      | CreateInvoiceForOrder           | INV01_CreateInvoiceForOrder_01 |
      | GetClientDataVersionByClientId  | INV01_GetClientDataVersionByClientId_04 |
      | CreateClientDataVersion         | INV01_CreateClientDataVersion_03 |
      | GetFlowDocumentation            | INV01_GetFlowDocumentation_01 |
      | GetValidationDocumentation      | INV01_GetValidationDocumentation_01 |
