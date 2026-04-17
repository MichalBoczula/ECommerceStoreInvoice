Feature: Get shopping cart not found

  Scenario: Get shopping cart by client id returns problem details when shopping cart does not exist
    Given I have a non-existing client id for shopping cart retrieval
    When I request the shopping cart by client id for non-existing client
    Then problem details are returned for get shopping cart not found
      | Field      | Value                                                        |
      | StatusCode | 404                                                          |
      | Title      | Resource not found.                                          |
      | Type       | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.4 |
      | HasDetail  | true                                                         |
      | Instance   | /shopping-carts/client/{clientId}                            |
      | HasTraceId | true                                                         |
