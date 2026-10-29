Feature: Generated invoice request and response contracts
  Source-owned schemas describe existing serializer and safe problem behavior; complete HTTP evidence preserves all populated collections.

  Background:
    Given these complete client histories support business validation
      | Version  | Client | CreatedAt            | ClientName  | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail            |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Doe    | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com       |
      | b-latest | B      | 2024-01-04T08:00:00Z | Anna Latest | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna.latest@example.com |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Latest | 00-001     | Warsaw  | Main     | 10             | 5               | 123456789   | 48          | john.latest@example.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Old    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna.old@example.com    |
    And these exact stored order snapshots support business validation
      | Order  | Client | Status  | CreatedAt            | UpdatedAt            | ProductId                            | Name              | Brand | PriceAmount | PriceCurrency | Quantity |
      | target | A      | Created | 2024-01-06T08:00:00Z | <null>               | 33333333-3333-3333-3333-333333333333 | Target.Phone      | Alpha | 12.50       | PLN           | 2        |
      | paid-a | A      | Paid    | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z | 44444444-4444-4444-4444-444444444444 | Historical.Tablet | Beta  | 19.95       | USD           | 3        |
      | paid-b | B      | Paid    | 2024-01-08T08:00:00Z | 2024-01-08T09:00:00Z | 55555555-5555-4555-8555-555555555555 | Other.Phone       | Gamma | 4.25        | PLN           | 7        |
    And these populated carts support business validation
      | Client | CreatedAt            | UpdatedAt            | ProductId                            | Quantity |
      | A      | 2024-01-01T10:00:00Z | 2024-01-02T10:00:00Z | 33333333-3333-3333-3333-333333333333 | 5        |
      | A      | 2024-01-01T10:00:00Z | 2024-01-02T10:00:00Z | 44444444-4444-4444-4444-444444444444 | 3        |
      | B      | 2024-01-01T11:00:00Z | 2024-01-02T11:00:00Z | 55555555-5555-4555-8555-555555555555 | 7        |
    And these completed invoice references finish the populated business fixture
      | Invoice | Order  | Version | CreatedAt            | StorageUrl                          |
      | a       | paid-a | a-old   | 2024-01-07T10:00:00Z | file:///fixtures/business-a-old.pdf |
      | b       | paid-b | b-old   | 2024-01-08T10:00:00Z | file:///fixtures/business-b-old.pdf |

  @technical-evidence @allure.label.scenarioId:INV13_Contracts_Metadata
  Scenario: Every published field and typed example matches actual serializer metadata
    The actual generated document is compared with all serializer member sets, required presence, nullable annotations, scalar and nested types, UUID/date/number formats, complete typed examples, all route parameters, safe problem alternatives and binary PDF headers. The documentation read leaves populated business data unchanged.
    Given the generated invoice contract includes these fields
      | Schema                            | Field              | Type    | Required | Nullable | Format    |
      | CreateClientDataVersionRequestDto | clientName         | string  | true     | true     | <none>    |
      | CreateClientDataVersionRequestDto | apartmentNumber    | string  | true     | true     | <none>    |
      | CreateClientDataVersionRequestDto | postalCode         | string  | true     | false    | <none>    |
      | ClientDataVersionResponseDto      | clientName         | string  | true     | true     | <none>    |
      | ClientDataVersionResponseDto      | city               | string  | true     | true     | <none>    |
      | ClientDataVersionResponseDto      | street             | string  | true     | true     | <none>    |
      | ClientDataVersionResponseDto      | buildingNumber     | string  | true     | true     | <none>    |
      | ClientDataVersionResponseDto      | apartmentNumber    | string  | true     | true     | <none>    |
      | ShoppingCartLineRequestDto        | quantity           | integer | true     | false    | int32     |
      | OrderResponseDto                 | updatedAt          | string  | true     | true     | date-time |
      | OrderResponseDto                 | totalAmount        | number  | true     | false    | double    |
      | OrderLineResponseDto             | productVersion     | object  | true     | false    | <none>    |
      | ProductVersionResponseDto        | isActive           | boolean | true     | false    | <none>    |
      | ProductVersionResponseDto        | deactivatedAt      | string  | true     | true     | date-time |
      | InvoiceResponseDto               | clietDataVersionId | string  | true     | false    | uuid      |
      | InvoiceResponseDto               | storageUrl         | string  | true     | false    | <none>    |
      | ApiProblemDetails                | status             | integer | true     | false    | int32     |
      | ApiProblemDetails                | errors             | array   | true     | false    | <none>    |
      | ProblemDetails                   | traceId            | string  | true     | false    | <none>    |
    When I read the generated invoice contract over populated data
    Then all invoice schema fields match serializer metadata and typed examples
    And the contract operation preserves complete expected state and supporting reads

  @business-validation-evidence
  Scenario Outline: A nullable client member remains required in the JSON object
    Existing business cases retain accepted explicit null values for these members. Omitting a required nullable member fails actual binding with the complete six-field safe 400 problem. Every field, ordered line and historical reference in all five populated collections stays unchanged.
    Given the canonical required client profile JSON is
      """
      {"clientName":"Anna Example","postalCode":"00-001","city":"Warsaw","street":"Main","buildingNumber":"10","apartmentNumber":"5","phoneNumber":"123456789","phonePrefix":"48","addressEmail":"anna@example.com"}
      """
    When I omit required nullable client member "<Field>"
    Then the omitted client member returns this complete safe problem
      | Field       | Value                                                              |
      | Status      | 400                                                                |
      | ContentType | application/problem+json                                           |
      | Type        | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1          |
      | Title       | Invalid JSON payload.                                              |
      | Detail      | The request body must contain valid JSON.                          |
    And the contract operation preserves complete expected state and supporting reads

    Examples:
      | Field           | scenarioId                      |
      | clientName      | INV13_Client_RequiredName        |
      | apartmentNumber | INV13_Client_RequiredApartment   |

  @business-validation-evidence
  Scenario Outline: Canonical cart types coexist with documented web serializer compatibility
    Actual web binding matches member names case-insensitively, ignores unknown root and nested members, and accepts quoted int32 quantities. The published schema uses canonical JSON integers and explicitly describes this compatibility. Only the target cart lines and update timestamp change; complete responses, all other documents, historical references and supporting reads stay exact.
    Given the canonical serializer cart JSON is
      """
      {"lines":[{"productId":"33333333-3333-3333-3333-333333333333","quantity":2},{"productId":"44444444-4444-4444-4444-444444444444","quantity":1}]}
      """
    When I replace the cart using serializer representation "<Mode>"
    Then the accepted serializer cart has these exact ordered lines
      | ProductId                            | Quantity |
      | 33333333-3333-3333-3333-333333333333 | 2        |
      | 44444444-4444-4444-4444-444444444444 | 1        |
    And the contract operation preserves complete expected state and supporting reads

    Examples:
      | Mode             | scenarioId                   |
      | case-and-unknown | INV13_Cart_CaseUnknown        |
      | quoted-int32     | INV13_Cart_QuotedInt          |
