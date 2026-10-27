Feature: Get client data version

  @allure.label.scenarioId:INV01_GetClientDataVersionByClientId_02
  Scenario: Get client data version by client id returns latest client data version
    A client has a stored data version created from the explicit contact and address fixture. Retrieval returns that version with its fields, identifiers and creation timestamp.
    Given I have an existing client data version for retrieval
      | Field           | Value            |
      | ClientName      | John Doe         |
      | PostalCode      | 00-001           |
      | City            | Warsaw           |
      | Street          | Main             |
      | BuildingNumber  | 10               |
      | ApartmentNumber | 5                |
      | PhoneNumber     | 123456789        |
      | PhonePrefix     | 48               |
      | AddressEmail    | john@example.com |
    When I request the client data version by client id
    Then the client data version is returned successfully
      | Field           | Value            |
      | StatusCode      | 200              |
      | HasId           | true             |
      | HasClientId     | true             |
      | ClientName      | John Doe         |
      | PostalCode      | 00-001           |
      | City            | Warsaw           |
      | Street          | Main             |
      | BuildingNumber  | 10               |
      | ApartmentNumber | 5                |
      | PhoneNumber     | 123456789        |
      | PhonePrefix     | 48               |
      | AddressEmail    | john@example.com |
      | HasCreatedAt    | true             |
