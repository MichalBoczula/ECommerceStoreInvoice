Feature: Get client data version

  @client-version-evidence @allure.label.scenarioId:INV01_GetClientDataVersionByClientId_02
  Scenario: Get client data version by client id returns latest client data version
    The client has three distinct historical versions, inserted out of timestamp order. Retrieval returns the complete original Warsaw fixture from the greatest CreatedAt, excludes the other client newer version, and preserves all populated collections and historical invoice references.
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
    When I send this client version lookup request
      | Field    | Value                                   |
      | Method   | GET                                     |
      | Endpoint | /client-data-versions/client/{clientId} |
      | Client   | A                                       |
    Then the client version response is this complete profile
      | Field           | Value                |
      | StatusCode      | 200                  |
      | ContentType     | application/json     |
      | Id              | saved-version        |
      | Client          | A                    |
      | Version         | a-latest             |
      | ClientName      | John Doe             |
      | PostalCode      | 00-001               |
      | City            | Warsaw               |
      | Street          | Main                 |
      | BuildingNumber  | 10                   |
      | ApartmentNumber | 5                    |
      | PhoneNumber     | 123456789            |
      | PhonePrefix     | 48                   |
      | AddressEmail    | john@example.com     |
      | CreatedAt       | 2024-01-03T08:00:00Z |
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

  @client-version-evidence @allure.label.scenarioId:INV06_GetClientDataVersionByClientId_CreationOrder
  Scenario: Latest client version follows creation time rather than insertion order
    The newest target version is inserted first and the oldest target version last, with another client newer profile interleaved. The response must select by the requested client and CreatedAt, without mutating any document.
    Given these complete historical client data versions were inserted in this order
      | Version  | Client | CreatedAt            | ClientName   | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail        |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe     | 00-001     | Warsaw  | Main     | 10             | 5               | 123456789   | 48          | john@example.com    |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak   | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com    |
      | a-middle | A      | 2024-01-02T08:00:00Z | John Updated | 80-001     | Gdansk  | Long.St  | 31C            | 9               | 222333444   | 48          | updated@example.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak   | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com    |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Doe     | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com   |
    And these paid orders and completed invoices reference historical client versions
      | Client | Version | CreatedAt            | PaidAt               | Status | ProductId                            | ProductName       | Brand | PriceAmount | PriceCurrency | Quantity | IsActive | CartQuantity | StorageUrl                        | InvoiceCreatedAt     | GenerationStatus | GenerationAttemptId | GenerationLeaseUntil |
      | A      | a-old   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z | Paid   | 11111111-1111-4111-8111-111111111111 | Historical.Phone  | Alpha | 123.45      | PLN           | 3        | true     | 5            | file:///fixtures/client-a-old.pdf | 2024-01-06T10:00:00Z | Completed        | <null>              | <null>               |
      | B      | b-old   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z | Paid   | 22222222-2222-4222-8222-222222222222 | Historical.Tablet | Beta  | 19.95       | USD           | 2        | true     | 7            | file:///fixtures/client-b-old.pdf | 2024-01-07T10:00:00Z | Completed        | <null>              | <null>               |
    When I send this client version lookup request
      | Field    | Value                                   |
      | Method   | GET                                     |
      | Endpoint | /client-data-versions/client/{clientId} |
      | Client   | A                                       |
    Then the client version response is this complete profile
      | Field           | Value                |
      | StatusCode      | 200                  |
      | ContentType     | application/json     |
      | Id              | saved-version        |
      | Client          | A                    |
      | Version         | a-latest             |
      | ClientName      | John Doe             |
      | PostalCode      | 00-001               |
      | City            | Warsaw               |
      | Street          | Main                 |
      | BuildingNumber  | 10                   |
      | ApartmentNumber | 5                    |
      | PhoneNumber     | 123456789            |
      | PhonePrefix     | 48                   |
      | AddressEmail    | john@example.com     |
      | CreatedAt       | 2024-01-03T08:00:00Z |
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

  @client-version-evidence @allure.label.scenarioId:INV06_GetClientDataVersionByClientId_OtherClient
  Scenario: Latest client version lookup isolates the second client history
    Reading the second client returns its own complete latest profile while a later inserted older version and the first client history remain excluded. Both invoice references and every stored field remain unchanged.
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
    When I send this client version lookup request
      | Field    | Value                                   |
      | Method   | GET                                     |
      | Endpoint | /client-data-versions/client/{clientId} |
      | Client   | B                                       |
    Then the client version response is this complete profile
      | Field           | Value                |
      | StatusCode      | 200                  |
      | ContentType     | application/json     |
      | Id              | saved-version        |
      | Client          | B                    |
      | Version         | b-latest             |
      | ClientName      | Anna Nowak           |
      | PostalCode      | 50-001               |
      | City            | Wroclaw              |
      | Street          | River.St             |
      | BuildingNumber  | 20B                  |
      | ApartmentNumber | 8                    |
      | PhoneNumber     | 987654321            |
      | PhonePrefix     | 48                   |
      | AddressEmail    | anna@example.com     |
      | CreatedAt       | 2024-01-05T08:00:00Z |
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
