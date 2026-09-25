Feature: Safe unexpected errors for every API operation
  Every endpoint must return a stable problem response when its dependency fails.

  Scenario Outline: <operationId> returns a safe 500
    Given the "<operationId>" dependency fails unexpectedly
    When I call the failing "<operationId>" endpoint
    Then the Invoice server error is safe

    Examples:
      | operationId                     |
      | GetShoppingCartByClientId       |
      | CreateShoppingCart              |
      | UpdateShoppingCart              |
      | GetOrderById                    |
      | GetOrdersByClientId             |
      | CreateOrder                     |
      | UpdateOrderStatus               |
      | GetInvoiceById                  |
      | CreateInvoiceForOrder           |
      | GetClientDataVersionByClientId  |
      | CreateClientDataVersion         |
      | GetFlowDocumentation            |
      | GetValidationDocumentation      |
