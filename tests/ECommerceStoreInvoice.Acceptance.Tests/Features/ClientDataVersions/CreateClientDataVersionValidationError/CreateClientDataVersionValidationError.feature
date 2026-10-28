Feature: Create client data version validation error

  @client-version-evidence @allure.label.scenarioId:INV01_CreateClientDataVersion_02
  Scenario: Create client data version returns problem details when validation fails
    Only the original phone number is invalid. Rejection returns the exact phone-rule error and complete safe problem, appends no version and preserves populated histories, paid orders, snapshots, carts and invoice references.
    Given these complete historical client data versions were inserted in this order
      | Version  | Client | CreatedAt            | ClientName   | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail        |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Doe     | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com   |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak   | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com    |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe     | 00-001     | Warsaw  | Main     | 10             | 5               | 123456789   | 48          | john@example.com    |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak   | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com    |
      | a-middle | A      | 2024-01-02T08:00:00Z | John Updated | 80-001     | Gdansk  | Long.St  | 31C            | 9               | 222333444   | 48          | updated@example.com |
    And these paid orders and completed invoices reference historical client versions
      | Client | Version | CreatedAt            | PaidAt               | Status | ProductId                            | ProductName       | Brand | PriceAmount | PriceCurrency | Quantity | IsActive | CartQuantity | StorageUrl                        | InvoiceCreatedAt     | GenerationStatus | GenerationAttemptId | GenerationLeaseUntil |
      | A      | a-old   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z | Paid   | 11111111-1111-4111-8111-111111111111 | Historical.Phone  | Alpha | 123.45      | PLN           | 3        | true     | 5            | file:///fixtures/client-a-old.pdf | 2024-01-06T10:00:00Z | Completed        | <null>              | <null>               |
      | B      | b-old   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z | Paid   | 22222222-2222-4222-8222-222222222222 | Historical.Tablet | Beta  | 19.95       | USD           | 2        | true     | 7            | file:///fixtures/client-b-old.pdf | 2024-01-07T10:00:00Z | Completed        | <null>              | <null>               |
    When I send this complete client version creation request
      | Field           | Value                            |
      | Method          | POST                             |
      | Endpoint        | /client-data-versions/{clientId} |
      | Client          | A                                |
      | Version         | rejected                         |
      | ClientName      | John Doe                         |
      | PostalCode      | 00-001                           |
      | City            | NewYork                          |
      | Street          | Main.St                          |
      | BuildingNumber  | 10A                              |
      | ApartmentNumber | 5                                |
      | PhoneNumber     | abc                              |
      | PhonePrefix     | 48                               |
      | AddressEmail    | john.doe@test.com                |
    Then the client version request returns this complete problem
      | Field        | Value                                                       |
      | StatusCode   | 400                                                         |
      | ContentType  | application/problem+json                                    |
      | Title        | Validation failed.                                          |
      | Detail       | One or more validation errors occurred.                     |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Instance     | request-path                                                |
      | TraceId      | generated                                                   |
      | ErrorsCount  | 1                                                           |
      | ErrorName    | ClientDataVersionPhoneValidationRule                        |
      | ErrorEntity  | ClientDataVersion                                           |
      | ErrorMessage | Phone number must contain digits only.                      |
    And the client version operation has exactly this stored effect
      | Field           | Value     |
      | Effect          | unchanged |
      | AddedVersion    | none      |
      | ClientVersions  | 5         |
      | orders          | 2         |
      | productVersions | 2         |
      | shoppingCarts   | 2         |
      | invoices        | 2         |
      | CatalogRequests | 0         |
    And these complete latest lookups and historical invoice reads preserve the database
      | Client | Version  |
      | A      | a-latest |
      | B      | b-latest |
