Feature: Create client data version validation error

  @allure.label.scenarioId:INV01_CreateClientDataVersion_02
  Scenario: Create client data version returns problem details when validation fails
    The client-data payload contains a phone number with nondigit characters. Creation returns the expected phone validation error and safe problem fields.
    Given I have a valid client id for client data version validation error
    And I have an invalid create client data version request with phone number validation error
      | Field           | Value             |
      | ClientName      | John Doe          |
      | PostalCode      | 00-001            |
      | City            | NewYork           |
      | Street          | Main.St           |
      | BuildingNumber  | 10A               |
      | ApartmentNumber | 5                 |
      | PhoneNumber     | abc               |
      | PhonePrefix     | 48                |
      | AddressEmail    | john.doe@test.com |
    When I submit the create client data version request with invalid data
    Then problem details are returned for create client data version validation error
      | Field             | Value                                                            |
      | StatusCode        | 400                                                              |
      | Title             | Validation failed.                                               |
      | Detail            | One or more validation errors occurred.                          |
      | Type              | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1     |
      | Instance          | /client-data-versions/{clientId}                                 |
      | ErrorsCount       | 1                                                                |
      | FirstErrorMessage | Phone number must contain digits only.                           |
