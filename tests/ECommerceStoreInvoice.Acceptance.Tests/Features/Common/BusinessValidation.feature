Feature: Business validation and accepted boundaries
  Domain rejection preserves both clients' populated documents; accepted requests have exact bounded effects.

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
    And business requests have these complete defaults
      | Operation                 | Method | Path                              | BodyJson                                                                                                                                                                                                         |
      | CreateClientDataVersion   | POST   | /client-data-versions/{clientId}  | {"clientName":"John Doe","postalCode":"00-001","city":"NewYork","street":"Main.St","buildingNumber":"10A","apartmentNumber":"5","phoneNumber":"123456789","phonePrefix":"48","addressEmail":"john.doe@test.com"} |
      | UpdateShoppingCart        | PUT    | /shopping-carts/{clientId}        | {"lines":[{"productId":"33333333-3333-3333-3333-333333333333","quantity":2},{"productId":"44444444-4444-4444-4444-444444444444","quantity":4}]}                                                                  |
      | UpdateOrderStatus         | PATCH  | /orders/{orderId}/status          | {"status":"Paid"}                                                                                                                                                                                                |
      | CreateShoppingCart        | POST   | /shopping-carts/{clientId}        | null                                                                                                                                                                                                             |
      | GetShoppingCartByClientId | GET    | /shopping-carts/client/{clientId} | null                                                                                                                                                                                                             |
      | CreateOrder               | POST   | /orders/client/{clientId}         | null                                                                                                                                                                                                             |
    And business validation problems have this complete signature
      | Field        | Value                                                       |
      | StatusCode   | 400                                                         |
      | ContentType  | application/problem+json                                    |
      | Type         | https://datatracker.ietf.org/doc/html/rfc7231#section-6.5.1 |
      | Title        | Validation failed.                                          |
      | Detail       | One or more validation errors occurred.                     |
      | Instance     | request-path                                                |
      | TraceId      | generated                                                   |
      | CacheControl | no-cache,no-store                                           |

  @business-validation-evidence
  Scenario Outline: Each invalid client field returns its exact isolated rule error
    Exactly one JSON scalar changes from the fully declared valid request. The full ordered error array contains one cause; all five populated collections and all nine complete supporting reads remain exact.
    Given the selected business request is "CreateClientDataVersion"
    And I change exactly these business JSON values
      | Pointer   | JsonValue   |
      | <Pointer> | <JsonValue> |
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name   | Message   | Entity            |
      | <Rule> | <Message> | ClientDataVersion |
    And business rejection preserves every populated document and supporting read

    Examples:
      | Pointer          | JsonValue             | Rule                                   | Message                                                                                                           | scenarioId                              |
      | /phoneNumber     | null                  | ClientDataVersionPhoneValidationRule   | Phone number cannot be null or whitespace.                                                                        | INV09_Client_PhoneNumber_Null           |
      | /phoneNumber     | ""                    | ClientDataVersionPhoneValidationRule   | Phone number cannot be null or whitespace.                                                                        | INV09_Client_PhoneNumber_Empty          |
      | /phoneNumber     | "   "                 | ClientDataVersionPhoneValidationRule   | Phone number cannot be null or whitespace.                                                                        | INV09_Client_PhoneNumber_Whitespace     |
      | /phoneNumber     | "0"                   | ClientDataVersionPhoneValidationRule   | Phone number must be greater than 0.                                                                              | INV09_Client_PhoneNumber_Zero           |
      | /phoneNumber     | "000"                 | ClientDataVersionPhoneValidationRule   | Phone number must be greater than 0.                                                                              | INV09_Client_PhoneNumber_AllZeros       |
      | /phoneNumber     | "-1"                  | ClientDataVersionPhoneValidationRule   | Phone number must contain digits only.                                                                            | INV09_Client_PhoneNumber_Negative       |
      | /phoneNumber     | "12x"                 | ClientDataVersionPhoneValidationRule   | Phone number must contain digits only.                                                                            | INV09_Client_PhoneNumber_NonDigit       |
      | /phoneNumber     | "+48"                 | ClientDataVersionPhoneValidationRule   | Phone number must contain digits only.                                                                            | INV09_Client_PhoneNumber_Plus           |
      | /phoneNumber     | "١٢٣"                 | ClientDataVersionPhoneValidationRule   | Phone number must contain digits only.                                                                            | INV09_Client_PhoneNumber_UnicodeDigits  |
      | /phoneNumber     | "9223372036854775808" | ClientDataVersionPhoneValidationRule   | Phone number must be greater than 0.                                                                              | INV09_Client_PhoneNumber_Overflow       |
      | /phonePrefix     | null                  | ClientDataVersionPhoneValidationRule   | Phone prefix cannot be null or whitespace.                                                                        | INV09_Client_PhonePrefix_Null           |
      | /phonePrefix     | ""                    | ClientDataVersionPhoneValidationRule   | Phone prefix cannot be null or whitespace.                                                                        | INV09_Client_PhonePrefix_Empty          |
      | /phonePrefix     | "   "                 | ClientDataVersionPhoneValidationRule   | Phone prefix cannot be null or whitespace.                                                                        | INV09_Client_PhonePrefix_Whitespace     |
      | /phonePrefix     | "0"                   | ClientDataVersionPhoneValidationRule   | Phone prefix must be greater than 0.                                                                              | INV09_Client_PhonePrefix_Zero           |
      | /phonePrefix     | "000"                 | ClientDataVersionPhoneValidationRule   | Phone prefix must be greater than 0.                                                                              | INV09_Client_PhonePrefix_AllZeros       |
      | /phonePrefix     | "-1"                  | ClientDataVersionPhoneValidationRule   | Phone prefix must contain digits only.                                                                            | INV09_Client_PhonePrefix_Negative       |
      | /phonePrefix     | "12x"                 | ClientDataVersionPhoneValidationRule   | Phone prefix must contain digits only.                                                                            | INV09_Client_PhonePrefix_NonDigit       |
      | /phonePrefix     | "+48"                 | ClientDataVersionPhoneValidationRule   | Phone prefix must contain digits only.                                                                            | INV09_Client_PhonePrefix_Plus           |
      | /phonePrefix     | "١٢٣"                 | ClientDataVersionPhoneValidationRule   | Phone prefix must contain digits only.                                                                            | INV09_Client_PhonePrefix_UnicodeDigits  |
      | /phonePrefix     | "9223372036854775808" | ClientDataVersionPhoneValidationRule   | Phone prefix must be greater than 0.                                                                              | INV09_Client_PhonePrefix_Overflow       |
      | /addressEmail    | null                  | ClientDataVersionEmailValidationRule   | Address email cannot be null or whitespace.                                                                       | INV09_Client_Email_Null                 |
      | /addressEmail    | ""                    | ClientDataVersionEmailValidationRule   | Address email cannot be null or whitespace.                                                                       | INV09_Client_Email_Empty                |
      | /addressEmail    | "   "                 | ClientDataVersionEmailValidationRule   | Address email cannot be null or whitespace.                                                                       | INV09_Client_Email_Whitespace           |
      | /addressEmail    | "a.b.c"               | ClientDataVersionEmailValidationRule   | Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com). | INV09_Client_Email_MissingAt            |
      | /addressEmail    | "a@b"                 | ClientDataVersionEmailValidationRule   | Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com). | INV09_Client_Email_MissingDomainDot     |
      | /addressEmail    | "a+b@c.d"             | ClientDataVersionEmailValidationRule   | Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com). | INV09_Client_Email_Plus                 |
      | /addressEmail    | "a_b@c.d"             | ClientDataVersionEmailValidationRule   | Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com). | INV09_Client_Email_Underscore           |
      | /addressEmail    | "a@b-c.d"             | ClientDataVersionEmailValidationRule   | Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com). | INV09_Client_Email_DomainHyphen         |
      | /addressEmail    | "ł@a.b"               | ClientDataVersionEmailValidationRule   | Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com). | INV09_Client_Email_Unicode              |
      | /postalCode      | null                  | ClientDataVersionAddressValidationRule | Postal code cannot be null or whitespace.                                                                         | INV09_Client_Postal_Null                |
      | /postalCode      | ""                    | ClientDataVersionAddressValidationRule | Postal code cannot be null or whitespace.                                                                         | INV09_Client_Postal_Empty               |
      | /postalCode      | "   "                 | ClientDataVersionAddressValidationRule | Postal code cannot be null or whitespace.                                                                         | INV09_Client_Postal_Whitespace          |
      | /postalCode      | "00001"               | ClientDataVersionAddressValidationRule | Postal code must match pattern nn-nnn (for example: 00-001).                                                      | INV09_Client_Postal_NoHyphen            |
      | /postalCode      | "0-001"               | ClientDataVersionAddressValidationRule | Postal code must match pattern nn-nnn (for example: 00-001).                                                      | INV09_Client_Postal_Short               |
      | /postalCode      | "000-001"             | ClientDataVersionAddressValidationRule | Postal code must match pattern nn-nnn (for example: 00-001).                                                      | INV09_Client_Postal_Long                |
      | /postalCode      | "aa-bbb"              | ClientDataVersionAddressValidationRule | Postal code must match pattern nn-nnn (for example: 00-001).                                                      | INV09_Client_Postal_Letters             |
      | /city            | "New York"            | ClientDataVersionAddressValidationRule | Address city may contain only letters, digits, '.' and ','.                                                       | INV09_Client_City_InvalidCharacter      |
      | /street          | "Main-St"             | ClientDataVersionAddressValidationRule | Address street may contain only letters, digits, '.' and ','.                                                     | INV09_Client_Street_InvalidCharacter    |
      | /buildingNumber  | "10/2"                | ClientDataVersionAddressValidationRule | Address building number may contain only letters, digits, '.' and ','.                                            | INV09_Client_Building_InvalidCharacter  |
      | /apartmentNumber | "#5"                  | ClientDataVersionAddressValidationRule | Address apartment number may contain only letters, digits, '.' and ','.                                           | INV09_Client_Apartment_InvalidCharacter |

  @business-validation-evidence
  Scenario Outline: Accepted client boundaries append one exact immutable version
    One accepted scalar changes, including optional blanks, currently unvalidated client names and the actual Unicode postal-code pattern. A distinct version and bounded UTC timestamp are stored; every earlier version and historical invoice reference remains exact. These cases document current rules, not stricter contact validation.
    Given the selected business request is "CreateClientDataVersion"
    And I change exactly these business JSON values
      | Pointer   | JsonValue   |
      | <Pointer> | <JsonValue> |
    When I submit the selected business request
    Then the complete business success has exactly this effect
      | Field       | Value                 |
      | StatusCode  | 200                   |
      | ContentType | application/json      |
      | Effect      | append-client-version |

    Examples:
      | Pointer          | JsonValue                   | scenarioId                            |
      | /phoneNumber     | "1"                         | INV09_Client_PhoneNumber_Minimum      |
      | /phoneNumber     | "9223372036854775807"       | INV09_Client_PhoneNumber_Maximum      |
      | /phoneNumber     | "0001"                      | INV09_Client_PhoneNumber_LeadingZeros |
      | /phonePrefix     | "1"                         | INV09_Client_PhonePrefix_Minimum      |
      | /phonePrefix     | "9223372036854775807"       | INV09_Client_PhonePrefix_Maximum      |
      | /phonePrefix     | "0001"                      | INV09_Client_PhonePrefix_LeadingZeros |
      | /addressEmail    | "a@b.c"                     | INV09_Client_Email_Minimal            |
      | /addressEmail    | "A.B9@EXAMPLE.COM"          | INV09_Client_Email_Case               |
      | /addressEmail    | "user.name@sub.example.com" | INV09_Client_Email_Subdomain          |
      | /postalCode      | "00-000"                    | INV09_Client_Postal_Minimum           |
      | /postalCode      | "99-999"                    | INV09_Client_Postal_Maximum           |
      | /postalCode      | "١٢-٣٤٥"                    | INV09_Client_Postal_UnicodeDigits     |
      | /city            | null                        | INV09_Client_City_Null                |
      | /city            | ""                          | INV09_Client_City_Empty               |
      | /city            | "   "                       | INV09_Client_City_Whitespace          |
      | /city            | "A1.,"                      | INV09_Client_City_Punctuation         |
      | /street          | null                        | INV09_Client_Street_Null              |
      | /street          | ""                          | INV09_Client_Street_Empty             |
      | /street          | "   "                       | INV09_Client_Street_Whitespace        |
      | /street          | "A1.,"                      | INV09_Client_Street_Punctuation       |
      | /buildingNumber  | null                        | INV09_Client_Building_Null            |
      | /buildingNumber  | ""                          | INV09_Client_Building_Empty           |
      | /buildingNumber  | "   "                       | INV09_Client_Building_Whitespace      |
      | /buildingNumber  | "A1.,"                      | INV09_Client_Building_Punctuation     |
      | /apartmentNumber | null                        | INV09_Client_Apartment_Null           |
      | /apartmentNumber | ""                          | INV09_Client_Apartment_Empty          |
      | /apartmentNumber | "   "                       | INV09_Client_Apartment_Whitespace     |
      | /apartmentNumber | "A1.,"                      | INV09_Client_Apartment_Punctuation    |
      | /clientName      | null                        | INV09_Client_Name_Null                |
      | /clientName      | ""                          | INV09_Client_Name_Empty               |
      | /clientName      | "   "                       | INV09_Client_Name_Whitespace          |

  @business-validation-evidence @allure.label.scenarioId:INV09_Client_MultipleErrors
  Scenario: All invalid client rules are reported in policy order
    Independent phone, email, postal and optional component errors are retained in the exact array without a write. Repeated rule names are not deduplicated.
    Given the selected business request is "CreateClientDataVersion"
    And I change exactly these business JSON values
      | Pointer          | JsonValue    |
      | /phoneNumber     | "abc"        |
      | /phonePrefix     | "0"          |
      | /addressEmail    | "bad"        |
      | /postalCode      | "bad"        |
      | /city            | "Bad City"   |
      | /street          | "Bad Street" |
      | /buildingNumber  | "10/2"       |
      | /apartmentNumber | "#5"         |
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name                                   | Message                                                                                                           | Entity            |
      | ClientDataVersionPhoneValidationRule   | Phone number must contain digits only.                                                                            | ClientDataVersion |
      | ClientDataVersionPhoneValidationRule   | Phone prefix must be greater than 0.                                                                              | ClientDataVersion |
      | ClientDataVersionEmailValidationRule   | Address email must use only letters, digits and '.', and include '@' with a domain (for example: user@gmail.com). | ClientDataVersion |
      | ClientDataVersionAddressValidationRule | Postal code must match pattern nn-nnn (for example: 00-001).                                                      | ClientDataVersion |
      | ClientDataVersionAddressValidationRule | Address city may contain only letters, digits, '.' and ','.                                                       | ClientDataVersion |
      | ClientDataVersionAddressValidationRule | Address street may contain only letters, digits, '.' and ','.                                                     | ClientDataVersion |
      | ClientDataVersionAddressValidationRule | Address building number may contain only letters, digits, '.' and ','.                                            | ClientDataVersion |
      | ClientDataVersionAddressValidationRule | Address apartment number may contain only letters, digits, '.' and ','.                                           | ClientDataVersion |
    And business rejection preserves every populated document and supporting read

  @business-validation-evidence @allure.label.scenarioId:INV09_Client_PostalShortCircuit
  Scenario: A missing postal value short circuits the optional address checks
    The postal required-value branch returns before the same address rule inspects an invalid city. The complete error array contains just the required postal error; persisted histories and references remain exact.
    Given the selected business request is "CreateClientDataVersion"
    And I change exactly these business JSON values
      | Pointer     | JsonValue  |
      | /postalCode | null       |
      | /city       | "Bad City" |
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name                                   | Message                                   | Entity            |
      | ClientDataVersionAddressValidationRule | Postal code cannot be null or whitespace. | ClientDataVersion |
    And business rejection preserves every populated document and supporting read

  @business-validation-evidence
  Scenario Outline: Invalid cart scalars preserve all original stored lines
    One line scalar changes while another valid requested line remains. Rejection must preserve both original cart lines, their quantities and timestamps and every other populated document.
    Given the selected business request is "UpdateShoppingCart"
    And I change exactly these business JSON values
      | Pointer   | JsonValue   |
      | <Pointer> | <JsonValue> |
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name   | Message   | Entity           |
      | <Rule> | <Message> | ShoppingCartLine |
    And business rejection preserves every populated document and supporting read

    Examples:
      | Pointer            | JsonValue                              | Rule                                   | Message                                   | scenarioId                  |
      | /lines/0/productId | "00000000-0000-0000-0000-000000000000" | ShoppingCartLineGuidValidationRule     | ShoppingCartLine Id cannot be empty Guid. | INV09_Cart_EmptyProduct     |
      | /lines/0/quantity  | -1                                     | ShoppingCartLineQuantityValidationRule | Quantity must be greater than zero.       | INV09_Cart_NegativeQuantity |
      | /lines/0/quantity  | -2147483648                            | ShoppingCartLineQuantityValidationRule | Quantity must be greater than zero.       | INV09_Cart_MinimumInteger   |

  @business-validation-evidence @allure.label.scenarioId:INV09_Cart_RepeatedErrors
  Scenario: Cart validation retains repeated errors from separate lines
    Both requested product identifiers and quantities fail. The array retains two identical ID errors followed by two identical quantity errors; no original line or timestamp changes.
    Given the selected business request is "UpdateShoppingCart"
    And I change exactly these business JSON values
      | Pointer            | JsonValue                              |
      | /lines/0/productId | "00000000-0000-0000-0000-000000000000" |
      | /lines/1/productId | "00000000-0000-0000-0000-000000000000" |
      | /lines/0/quantity  | 0                                      |
      | /lines/1/quantity  | -1                                     |
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name                                   | Message                                   | Entity           |
      | ShoppingCartLineGuidValidationRule     | ShoppingCartLine Id cannot be empty Guid. | ShoppingCartLine |
      | ShoppingCartLineGuidValidationRule     | ShoppingCartLine Id cannot be empty Guid. | ShoppingCartLine |
      | ShoppingCartLineQuantityValidationRule | Quantity must be greater than zero.       | ShoppingCartLine |
      | ShoppingCartLineQuantityValidationRule | Quantity must be greater than zero.       | ShoppingCartLine |
    And business rejection preserves every populated document and supporting read

  @business-validation-evidence
  Scenario Outline: Accepted cart boundaries replace exactly the declared lines
    Minimum and maximum integers, empty lines, repeated products and a non-catalog product are accepted by the current cart policy. The full response and persisted ordered lines match the request; only the target lines and UpdatedAt may change and no catalog call occurs.
    Given the selected business request is "UpdateShoppingCart"
    And I change exactly these business JSON values
      | Pointer   | JsonValue   |
      | <Pointer> | <JsonValue> |
    When I submit the selected business request
    Then the complete business success has exactly this effect
      | Field       | Value              |
      | StatusCode  | 200                |
      | ContentType | application/json   |
      | Effect      | replace-cart-lines |

    Examples:
      | Pointer            | JsonValue                              | scenarioId                   |
      | /lines/0/quantity  | 1                                      | INV09_Cart_MinimumQuantity   |
      | /lines/0/quantity  | 2147483647                             | INV09_Cart_MaximumQuantity   |
      | /lines             | []                                     | INV09_Cart_EmptyLines        |
      | /lines/1/productId | "33333333-3333-3333-3333-333333333333" | INV09_Cart_DuplicateProducts |
      | /lines/0/productId | "ffffffff-ffff-ffff-ffff-ffffffffffff" | INV09_Cart_UnknownProduct    |

  @business-validation-evidence
  Scenario Outline: Status parsing and transition rejection have distinct signatures
    Explicit string or null values reach the existing application parser. Unparsable values produce its status error; parseable Created or undefined numeric values reach the transition policy. Every rejection preserves the complete original order and other collections.
    Given the selected business request is "UpdateOrderStatus"
    And I change exactly these business JSON values
      | Pointer | JsonValue   |
      | /status | <JsonValue> |
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name   | Message   | Entity |
      | <Rule> | <Message> | Order  |
    And business rejection preserves every populated document and supporting read

    Examples:
      | JsonValue | Rule                                      | Message                                                                      | scenarioId                    |
      | null      | status                                    | Order status '' is invalid. Allowed values: Paid, Cancelled, Created.        | INV09_Status_Null             |
      | ""        | status                                    | Order status '' is invalid. Allowed values: Paid, Cancelled, Created.        | INV09_Status_Empty            |
      | "   "     | status                                    | Order status '   ' is invalid. Allowed values: Paid, Cancelled, Created.     | INV09_Status_Whitespace       |
      | "pending" | status                                    | Order status 'pending' is invalid. Allowed values: Paid, Cancelled, Created. | INV09_Status_Unknown          |
      | "2"       | UpdateOrderStatusTransitionValidationRule | Only transitions from Created to Paid or Cancelled are allowed.              | INV09_Status_CreatedNumeric   |
      | "-1"      | UpdateOrderStatusTransitionValidationRule | Only transitions from Created to Paid or Cancelled are allowed.              | INV09_Status_NegativeNumeric  |
      | "99"      | UpdateOrderStatusTransitionValidationRule | Only transitions from Created to Paid or Cancelled are allowed.              | INV09_Status_UndefinedNumeric |

  @business-validation-evidence
  Scenario Outline: Accepted status spellings retain complete saved orders
    The current parser accepts numeric enum strings, case differences and surrounding whitespace. A Created order makes one legal transition; the exact response, saved target status and bounded timestamp change while snapshots and every unrelated record remain exact.
    Given the selected business request is "UpdateOrderStatus"
    And I change exactly these business JSON values
      | Pointer | JsonValue   |
      | /status | <JsonValue> |
    When I submit the selected business request
    Then the complete business success has exactly this effect
      | Field       | Value               |
      | StatusCode  | 200                 |
      | ContentType | application/json    |
      | Effect      | update-order-status |
      | SavedStatus | <SavedStatus>       |

    Examples:
      | JsonValue  | SavedStatus | scenarioId                    |
      | "0"        | Paid        | INV09_Status_PaidNumeric      |
      | "1"        | Cancelled   | INV09_Status_CancelledNumeric |
      | "pAiD"     | Paid        | INV09_Status_CaseInsensitive  |
      | "  Paid  " | Paid        | INV09_Status_Trimmed          |

  @business-validation-evidence @allure.label.scenarioId:INV09_CreateClientDataVersion_EmptyClient
  Scenario: Client version creation rejects an empty client before writing
    An otherwise complete valid request uses an empty route identifier. The existing shared ShoppingCart-labelled GUID rule returns its complete safe problem before persistence or Products access.
    Given the selected business request is "CreateClientDataVersion"
    And business route parameter "clientId" is the empty identifier
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name                          | Message                        | Entity       |
      | ClientIdIsEmptyValidationRule | ClientId cannot be empty Guid. | ShoppingCart |
    And business rejection preserves every populated document and supporting read

  @business-validation-evidence @allure.label.scenarioId:INV09_UpdateShoppingCart_EmptyClient
  Scenario: Cart replacement rejects an empty client before loading
    An otherwise complete valid request uses an empty route identifier. The existing shared ShoppingCart-labelled GUID rule returns its complete safe problem before persistence or Products access.
    Given the selected business request is "UpdateShoppingCart"
    And business route parameter "clientId" is the empty identifier
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name                          | Message                        | Entity       |
      | ClientIdIsEmptyValidationRule | ClientId cannot be empty Guid. | ShoppingCart |
    And business rejection preserves every populated document and supporting read

  @business-validation-evidence @allure.label.scenarioId:INV09_CreateOrder_EmptyClient
  Scenario: Checkout rejects an empty client before catalog access
    An otherwise complete valid request uses an empty route identifier. The existing shared ShoppingCart-labelled GUID rule returns its complete safe problem before persistence or Products access.
    Given the selected business request is "CreateOrder"
    And business route parameter "clientId" is the empty identifier
    When I submit the selected business request
    Then the complete business validation problem contains exactly these ordered errors
      | Name                          | Message                        | Entity       |
      | ClientIdIsEmptyValidationRule | ClientId cannot be empty Guid. | ShoppingCart |
    And business rejection preserves every populated document and supporting read
