Feature: Deterministic order concurrency and actual transaction rollback
  Observable HTTP outcomes are joined to real conditional writes, distinct Mongo sessions and complete stored state.

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
      | A      | 2024-01-01T10:00:00Z | 2024-01-02T10:00:00Z | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | 2        |
      | A      | 2024-01-01T10:00:00Z | 2024-01-02T10:00:00Z | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | 3        |
      | B      | 2024-01-01T11:00:00Z | 2024-01-02T11:00:00Z | 55555555-5555-4555-8555-555555555555 | 7        |
    And these completed invoice references finish the populated business fixture
      | Invoice | Order  | Version | CreatedAt            | StorageUrl                          |
      | a       | paid-a | a-old   | 2024-01-07T10:00:00Z | file:///fixtures/business-a-old.pdf |
      | b       | paid-b | b-old   | 2024-01-08T10:00:00Z | file:///fixtures/business-b-old.pdf |
    And checkout concurrency uses these exact catalog products
      | ProductId                            | Name                                      | Brand  | PriceAmount | PriceCurrency | Quantity |
      | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | Xiaomi POCO F7 12/512GB Black             | Xiaomi | 2499.00     | PLN           | 2        |
      | a243cf6f-dde1-4c05-8a58-2fa7f04ed984 | Xiaomi Redmi 15 5G 4/128GB Midnight Black | Xiaomi | 999.00      | PLN           | 3        |

  @order-concurrency-evidence
  Scenario Outline: Both validated status writes produce a deterministic stale conflict
    Each request reads the same Created order and passes domain validation before either conditional Mongo write. Either requested status can be selected as winner. The exact 409 loser cannot change the full winning order, snapshots, other clients or historical invoice references.
    When both validated status requests compete with "<Winner>" winning
    Then only the winning complete status profile is persisted and the stale request returns 409

    Examples:
      | Winner    | scenarioId                        |
      | Paid      | INV11_StatusRace_PaidWins          |
      | Cancelled | INV11_StatusRace_CancelledWins     |

  @products-api @order-concurrency-evidence
  Scenario Outline: A failure after a real checkout write rolls back every staged change
    Actual Mongo sessions contain two immutable product snapshots, then an order, then a cleared cart as the real application reaches each stage. Independent reads see only the populated original database. A test-only exception after the selected successful write triggers the application's real abort; every field is restored before a successful HTTP retry.
    When checkout fails after the real "<Stage>" write and is retried
    Then checkout preserves the exact winning order snapshots and cleared cart

    Examples:
      | Stage     | scenarioId                     |
      | Snapshots | INV11_Rollback_AfterSnapshots   |
      | Order     | INV11_Rollback_AfterOrder       |
      | Cart      | INV11_Rollback_AfterCart        |

  @products-api @order-concurrency-evidence
  Scenario Outline: Competing real checkouts retain only the committed transaction
    Two HTTP requests load the populated cart, fetch the actual catalog independently and stage two snapshots plus one order in distinct real Mongo sessions. Both wait before the cart write. Either selected transaction commits first; the other's real cart write conflict triggers rollback and the existing safe 500. Only the winning full order and snapshots survive, all other data is unchanged and a later empty-cart checkout returns the exact 400.
    When two real checkout transactions compete with "<Winner>" winning
    Then checkout preserves the exact winning order snapshots and cleared cart

    Examples:
      | Winner | scenarioId                     |
      | first  | INV11_CheckoutRace_FirstWins    |
      | second | INV11_CheckoutRace_SecondWins   |
