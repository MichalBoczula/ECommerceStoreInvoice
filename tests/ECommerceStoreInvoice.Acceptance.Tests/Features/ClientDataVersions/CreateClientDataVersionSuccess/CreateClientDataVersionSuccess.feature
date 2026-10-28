Feature: Create client data version

  @client-version-evidence @allure.label.scenarioId:INV01_CreateClientDataVersion_01
  Scenario: Create client data version returns created client data version
    Creating the original John Doe profile returns every supplied field, a distinct version ID and a generated UTC timestamp. Only one version is appended; both clients historical versions, paid orders, snapshots, populated carts and completed invoice references remain exact.
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
      | Version         | a-created                        |
      | ClientName      | John Doe                         |
      | PostalCode      | 00-001                           |
      | City            | NewYork                          |
      | Street          | Main.St                          |
      | BuildingNumber  | 10A                              |
      | ApartmentNumber | 5                                |
      | PhoneNumber     | 123456789                        |
      | PhonePrefix     | 48                               |
      | AddressEmail    | john.doe@test.com                |
    Then the client version response is this complete profile
      | Field           | Value              |
      | StatusCode      | 200                |
      | ContentType     | application/json   |
      | Id              | generated-distinct |
      | Client          | A                  |
      | Version         | a-created          |
      | ClientName      | John Doe           |
      | PostalCode      | 00-001             |
      | City            | NewYork            |
      | Street          | Main.St            |
      | BuildingNumber  | 10A                |
      | ApartmentNumber | 5                  |
      | PhoneNumber     | 123456789          |
      | PhonePrefix     | 48                 |
      | AddressEmail    | john.doe@test.com  |
      | CreatedAt       | generated          |
    And the client version operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | append-version |
      | AddedVersion    | a-created      |
      | ClientVersions  | 6              |
      | orders          | 2              |
      | productVersions | 2              |
      | shoppingCarts   | 2              |
      | invoices        | 2              |
      | CatalogRequests | 0              |
    And these complete latest lookups and historical invoice reads preserve the database
      | Client | Version   |
      | A      | a-created |
      | B      | b-latest  |

  @client-version-evidence @allure.label.scenarioId:INV06_CreateClientDataVersion_AppendHistory
  Scenario: Interleaved client updates append immutable versions and preserve historical invoice references
    Two clients update their contact and address profiles through actual HTTP requests. Each append changes only its own latest selection; every prior version and both completed invoices continue to reference the original identities and data.
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
      | Version         | a-created                        |
      | ClientName      | John Updated                     |
      | PostalCode      | 80-001                           |
      | City            | Gdansk                           |
      | Street          | Long.St                          |
      | BuildingNumber  | 31C                              |
      | ApartmentNumber | 9                                |
      | PhoneNumber     | 222333444                        |
      | PhonePrefix     | 48                               |
      | AddressEmail    | updated@example.com              |
    Then the client version response is this complete profile
      | Field           | Value               |
      | StatusCode      | 200                 |
      | ContentType     | application/json    |
      | Id              | generated-distinct  |
      | Client          | A                   |
      | Version         | a-created           |
      | ClientName      | John Updated        |
      | PostalCode      | 80-001              |
      | City            | Gdansk              |
      | Street          | Long.St             |
      | BuildingNumber  | 31C                 |
      | ApartmentNumber | 9                   |
      | PhoneNumber     | 222333444           |
      | PhonePrefix     | 48                  |
      | AddressEmail    | updated@example.com |
      | CreatedAt       | generated           |
    And the client version operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | append-version |
      | AddedVersion    | a-created      |
      | ClientVersions  | 6              |
      | orders          | 2              |
      | productVersions | 2              |
      | shoppingCarts   | 2              |
      | invoices        | 2              |
      | CatalogRequests | 0              |
    And these complete latest lookups and historical invoice reads preserve the database
      | Client | Version   |
      | A      | a-created |
      | B      | b-latest  |
    When I send this complete client version creation request
      | Field           | Value                            |
      | Method          | POST                             |
      | Endpoint        | /client-data-versions/{clientId} |
      | Client          | B                                |
      | Version         | b-created                        |
      | ClientName      | Anna Updated                     |
      | PostalCode      | 30-001                           |
      | City            | Krakow                           |
      | Street          | Short.St                         |
      | BuildingNumber  | 42D                              |
      | ApartmentNumber | 7                                |
      | PhoneNumber     | 555666777                        |
      | PhonePrefix     | 48                               |
      | AddressEmail    | other@example.com                |
    Then the client version response is this complete profile
      | Field           | Value              |
      | StatusCode      | 200                |
      | ContentType     | application/json   |
      | Id              | generated-distinct |
      | Client          | B                  |
      | Version         | b-created          |
      | ClientName      | Anna Updated       |
      | PostalCode      | 30-001             |
      | City            | Krakow             |
      | Street          | Short.St           |
      | BuildingNumber  | 42D                |
      | ApartmentNumber | 7                  |
      | PhoneNumber     | 555666777          |
      | PhonePrefix     | 48                 |
      | AddressEmail    | other@example.com  |
      | CreatedAt       | generated          |
    And the client version operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | append-version |
      | AddedVersion    | b-created      |
      | ClientVersions  | 7              |
      | orders          | 2              |
      | productVersions | 2              |
      | shoppingCarts   | 2              |
      | invoices        | 2              |
      | CatalogRequests | 0              |
    And these complete latest lookups and historical invoice reads preserve the database
      | Client | Version   |
      | A      | a-created |
      | B      | b-created |
    When I send this complete client version creation request
      | Field           | Value                            |
      | Method          | POST                             |
      | Endpoint        | /client-data-versions/{clientId} |
      | Client          | A                                |
      | Version         | a-newest                         |
      | ClientName      | John Doe                         |
      | PostalCode      | 00-001                           |
      | City            | Warsaw                           |
      | Street          | Main                             |
      | BuildingNumber  | 10                               |
      | ApartmentNumber | 5                                |
      | PhoneNumber     | 123456789                        |
      | PhonePrefix     | 48                               |
      | AddressEmail    | john@example.com                 |
    Then the client version response is this complete profile
      | Field           | Value              |
      | StatusCode      | 200                |
      | ContentType     | application/json   |
      | Id              | generated-distinct |
      | Client          | A                  |
      | Version         | a-newest           |
      | ClientName      | John Doe           |
      | PostalCode      | 00-001             |
      | City            | Warsaw             |
      | Street          | Main               |
      | BuildingNumber  | 10                 |
      | ApartmentNumber | 5                  |
      | PhoneNumber     | 123456789          |
      | PhonePrefix     | 48                 |
      | AddressEmail    | john@example.com   |
      | CreatedAt       | generated          |
    And the client version operation has exactly this stored effect
      | Field           | Value          |
      | Effect          | append-version |
      | AddedVersion    | a-newest       |
      | ClientVersions  | 8              |
      | orders          | 2              |
      | productVersions | 2              |
      | shoppingCarts   | 2              |
      | invoices        | 2              |
      | CatalogRequests | 0              |
    And these complete latest lookups and historical invoice reads preserve the database
      | Client | Version   |
      | A      | a-newest  |
      | B      | b-created |

  @client-version-evidence @allure.label.scenarioId:INV06_CreateClientDataVersion_IdenticalData
  Scenario: Identical client data creates separate immutable versions
    Repeating identical valid contact data creates another version with a distinct identity and strictly later stored timestamp. The latest lookup follows the new version while the prior versions and invoice references remain unchanged.
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
      | Version         | a-identical-first                |
      | ClientName      | John Doe                         |
      | PostalCode      | 00-001                           |
      | City            | NewYork                          |
      | Street          | Main.St                          |
      | BuildingNumber  | 10A                              |
      | ApartmentNumber | 5                                |
      | PhoneNumber     | 123456789                        |
      | PhonePrefix     | 48                               |
      | AddressEmail    | john.doe@test.com                |
    Then the client version response is this complete profile
      | Field           | Value              |
      | StatusCode      | 200                |
      | ContentType     | application/json   |
      | Id              | generated-distinct |
      | Client          | A                  |
      | Version         | a-identical-first  |
      | ClientName      | John Doe           |
      | PostalCode      | 00-001             |
      | City            | NewYork            |
      | Street          | Main.St            |
      | BuildingNumber  | 10A                |
      | ApartmentNumber | 5                  |
      | PhoneNumber     | 123456789          |
      | PhonePrefix     | 48                 |
      | AddressEmail    | john.doe@test.com  |
      | CreatedAt       | generated          |
    And the client version operation has exactly this stored effect
      | Field           | Value             |
      | Effect          | append-version    |
      | AddedVersion    | a-identical-first |
      | ClientVersions  | 6                 |
      | orders          | 2                 |
      | productVersions | 2                 |
      | shoppingCarts   | 2                 |
      | invoices        | 2                 |
      | CatalogRequests | 0                 |
    And these complete latest lookups and historical invoice reads preserve the database
      | Client | Version           |
      | A      | a-identical-first |
      | B      | b-latest          |
    When I send this complete client version creation request
      | Field           | Value                            |
      | Method          | POST                             |
      | Endpoint        | /client-data-versions/{clientId} |
      | Client          | A                                |
      | Version         | a-identical-second               |
      | ClientName      | John Doe                         |
      | PostalCode      | 00-001                           |
      | City            | NewYork                          |
      | Street          | Main.St                          |
      | BuildingNumber  | 10A                              |
      | ApartmentNumber | 5                                |
      | PhoneNumber     | 123456789                        |
      | PhonePrefix     | 48                               |
      | AddressEmail    | john.doe@test.com                |
    Then the client version response is this complete profile
      | Field           | Value              |
      | StatusCode      | 200                |
      | ContentType     | application/json   |
      | Id              | generated-distinct |
      | Client          | A                  |
      | Version         | a-identical-second |
      | ClientName      | John Doe           |
      | PostalCode      | 00-001             |
      | City            | NewYork            |
      | Street          | Main.St            |
      | BuildingNumber  | 10A                |
      | ApartmentNumber | 5                  |
      | PhoneNumber     | 123456789          |
      | PhonePrefix     | 48                 |
      | AddressEmail    | john.doe@test.com  |
      | CreatedAt       | generated          |
    And the client version operation has exactly this stored effect
      | Field           | Value              |
      | Effect          | append-version     |
      | AddedVersion    | a-identical-second |
      | ClientVersions  | 7                  |
      | orders          | 2                  |
      | productVersions | 2                  |
      | shoppingCarts   | 2                  |
      | invoices        | 2                  |
      | CatalogRequests | 0                  |
    And these complete latest lookups and historical invoice reads preserve the database
      | Client | Version            |
      | A      | a-identical-second |
      | B      | b-latest           |
