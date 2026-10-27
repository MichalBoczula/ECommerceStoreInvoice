Feature: Get client data version not found

  @allure.label.scenarioId:INV01_GetClientDataVersionByClientId_01
  Scenario: Get client data version by client id returns problem details when client data version does not exist
    The client identifier is valid but has no saved client data version. Retrieval returns the expected missing-resource problem.
    Given I have a non-existing client id for client data version retrieval
      | Field          | Value                                    |
      | Method         | GET                                      |
      | Endpoint       | /client-data-versions/client/{clientId} |
      | ClientIdSource | GenerateNewGuid                          |
    When I request the client data version by client id for non-existing client
    Then problem details are returned for get client data version not found
      | Field              | Value                                                        |
      | StatusCode         | 404                                                          |
      | Title              | Resource not found.                                          |
      | Type               | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail          | true                                                         |
      | DetailContains     | ClientDataVersion                                            |
      | DetailContainsGuid | true                                                         |
      | Instance           | /client-data-versions/client/{clientId}                      |
      | HasTraceId         | true                                                         |
