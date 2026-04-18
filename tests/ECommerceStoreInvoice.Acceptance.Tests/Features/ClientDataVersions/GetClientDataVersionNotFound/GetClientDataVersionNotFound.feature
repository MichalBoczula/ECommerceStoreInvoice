@allure.description:Ensures_getting_a_non_existing_client_data_version_returns_RFC7231_not_found_problem_details_with_request_context.
Feature: Get client data version not found

  Scenario: Get client data version by client id returns problem details when client data version does not exist
    Given I have a non-existing client id for client data version retrieval
    When I request the client data version by client id for non-existing client
    Then problem details are returned for get client data version not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /client-data-versions/client/{clientId}                      |
      | HasTraceId | true                                                         |
