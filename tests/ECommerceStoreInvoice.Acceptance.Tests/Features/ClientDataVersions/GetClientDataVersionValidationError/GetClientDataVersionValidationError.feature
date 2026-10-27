Feature: Get client data version validation error

  @allure.label.scenarioId:INV01_GetClientDataVersionByClientId_03
  Scenario: Get client data version by client id returns problem details when validation fails
    The lookup request uses an all-zero client identifier. It returns the expected identifier validation problem.
    Given I have an invalid client data version request
      | Field    | Value                                |
      | ClientId | 00000000-0000-0000-0000-000000000000 |
    When I request the client data version by invalid client id
    Then problem details are returned for get client data version validation error
      | Field             | Value                                                        |
      | StatusCode        | 400                                                          |
      | Title             | Validation failed.                                           |
      | Detail            | One or more validation errors occurred.                      |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance          | /client-data-versions/client/00000000-0000-0000-0000-000000000000 |
      | ErrorsCount       | 1                                                            |
      | FirstErrorMessage | ClientId cannot be empty Guid.                               |
