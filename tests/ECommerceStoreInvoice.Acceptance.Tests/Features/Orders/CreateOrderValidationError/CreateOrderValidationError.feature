Feature: Create order validation error

  @allure.label.scenarioId:INV01_CreateOrder_05
  Scenario: Create order returns problem details when validation fails
    The client has an empty shopping cart. Checkout cannot create a valid order with no lines and returns the expected order validation problem.
    Given I have a client with an empty shopping cart for order creation
    When I submit the create order request for the empty shopping cart
      | Field         | Value              |
      | Method        | POST               |
      | PathTemplate  | /orders/{clientId} |
      | ContentType   | application/json   |
      | Body          | null               |
    Then problem details are returned for create order validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /orders/{clientId}                                           |
      | ErrorsCount       | 1                                                            |
      | FirstErrorMessage | Order lines cannot be empty.                                 |
