@allure.description:Ensures_creating_client_data_version_with_invalid_data_returns_RFC7231_bad_request_problem_details_with_validation_errors.
Feature: Create client data version validation error

  Scenario: Create client data version returns problem details when validation fails
    Given I have an invalid client id for client data version creation
    And I have a create client data version request for validation error
    When I submit the create client data version request with invalid data
    Then problem details are returned for create client data version validation error
      | Field             | Value                                                            |
      | StatusCode        | 400                                                              |
      | Title             | Validation failed.                                               |
      | Detail            | One or more validation errors occurred.                          |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1     |
      | Instance          | /client-data-versions/00000000-0000-0000-0000-000000000000      |
      | ErrorsCount       | 1                                                                |
      | FirstErrorMessage | ClientId cannot be empty Guid.                                   |
