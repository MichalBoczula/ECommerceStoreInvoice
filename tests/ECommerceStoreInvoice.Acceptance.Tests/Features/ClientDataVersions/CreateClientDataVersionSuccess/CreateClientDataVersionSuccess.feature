@allure.description:Ensures_creating_client_data_version_returns_a_success_response_with_the_created_payload_fields.
Feature: Create client data version

  Scenario: Create client data version returns created client data version
    Given I have a valid client id for client data version creation
    And I have a valid create client data version request
      | Field           | Value             |
      | ClientName      | John Doe          |
      | PostalCode      | 00-001            |
      | City            | NewYork           |
      | Street          | Main.St           |
      | BuildingNumber  | 10A               |
      | ApartmentNumber | 5                 |
      | PhoneNumber     | 123456789         |
      | PhonePrefix     | 48                |
      | AddressEmail    | john.doe@test.com |
    When I submit the create client data version request
    Then the client data version is created successfully
      | Field            | Value             |
      | StatusCode       | 200               |
      | HasId            | true              |
      | HasClientId      | true              |
      | ClientName       | John Doe          |
      | PostalCode       | 00-001            |
      | City             | NewYork           |
      | Street           | Main.St           |
      | BuildingNumber   | 10A               |
      | ApartmentNumber  | 5                 |
      | PhoneNumber      | 123456789         |
      | PhonePrefix      | 48                |
      | AddressEmail     | john.doe@test.com |
      | HasCreatedAt     | true              |
