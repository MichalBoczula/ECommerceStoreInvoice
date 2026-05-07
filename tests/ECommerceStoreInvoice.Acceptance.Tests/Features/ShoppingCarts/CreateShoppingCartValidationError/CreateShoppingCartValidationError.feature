Feature: Create shopping cart validation error

  Scenario: Create shopping cart returns problem details when validation fails
    Given I have an invalid client id for shopping cart creation
    When I submit the create shopping cart request with invalid data
      | Field         | Value                      |
      | Method        | POST                       |
      | PathTemplate  | /shopping-carts/{clientId} |
      | ContentType   | application/json           |
      | Body          | null                       |
    Then problem details are returned for create shopping cart validation error
      | Field             | Value                                                            |
      | StatusCode        | 400                                                              |
      | Title             | Validation failed.                                               |
      | Detail            | One or more validation errors occurred.                          |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1     |
      | Instance          | /shopping-carts/{clientId}                                       |
      | ErrorsCount       | 1                                                                |
      | FirstErrorMessage | ClientId cannot be empty Guid.                                   |
