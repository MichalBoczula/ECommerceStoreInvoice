Feature: Stale invoice attempts cannot publish or delete a replacement

  @invoice-generation-evidence @invoice-hold-pdf @allure.label.scenarioId:INV11_Invoice_StaleAttemptFenced
  Scenario: An expired old attempt cannot replace the completed invoice or delete its PDF
    A real old claim is held before PDF generation. The fixture expires only its lease, allowing a second real HTTP request to reclaim and complete the same invoice with a new attempt. The resumed old request writes its own PDF but its stale completion fails and its release changes nothing. Production preserves that ambiguous old file; an explicit old-attempt storage cleanup deletes only that file. The winning metadata and exact downloaded PDF remain unchanged. This is not an automatic orphan sweeper.
    Given these full invoice client versions exist in this insertion order
      | Version  | Client | CreatedAt            | ClientName    | PostalCode | City    | Street   | BuildingNumber | ApartmentNumber | PhoneNumber | PhonePrefix | AddressEmail      |
      | a-latest | A      | 2024-01-03T08:00:00Z | John Doe      | 00-001     | NewYork | Main.St  | 10A            | 5               | 123456789   | 48          | john.doe@test.com |
      | b-latest | B      | 2024-01-05T08:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
      | a-old    | A      | 2024-01-01T08:00:00Z | John Previous | 00-002     | Warsaw  | Old.St   | 1              | 2               | 111222333   | 48          | previous@test.com |
      | b-old    | B      | 2024-01-01T09:00:00Z | Anna Nowak    | 50-001     | Wroclaw | River.St | 20B            | 8               | 987654321   | 48          | anna@example.com  |
    And these full invoice order profiles exist
      | Order  | Client | Status | CreatedAt            | UpdatedAt            |
      | target | A      | Paid   | 2024-01-06T08:00:00Z | 2024-01-06T09:00:00Z |
      | other  | B      | Paid   | 2024-01-07T08:00:00Z | 2024-01-07T09:00:00Z |
    And these exact saved product lines belong to the invoice orders
      | Order  | ProductId                            | IsActive | CreatedAt            | DeactivatedAt | Name                          | Brand  | PriceAmount | PriceCurrency | Quantity |
      | target | 0f62c3e1-8e3e-4b1f-9d74-3d6e2ff2c6d2 | true     | 2024-01-06T08:00:00Z | <null>        | Xiaomi POCO F7 12/512GB Black | Xiaomi | 2499.00     | PLN           | 2        |
      | other  | 33333333-3333-4333-8333-333333333333 | true     | 2024-01-07T08:00:00Z | <null>        | Other.Client.Phone            | Beta   | 99.95       | USD           | 3        |
      | target | 66666666-6666-4666-8666-666666666666 | true     | 2024-01-06T08:00:00Z | <null>        | Second.Saved.Phone            | Gamma  | 999.00      | PLN           | 3        |
    And both invoice clients have these populated carts
      | Client | ProductId                            | Quantity | CreatedAt            | UpdatedAt            |
      | A      | 44444444-4444-4444-8444-444444444444 | 5        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
      | B      | 55555555-5555-4555-8555-555555555555 | 7        | 2024-01-04T08:00:00Z | 2024-01-04T09:00:00Z |
    And these complete historical invoice records exist
      | Order | ClientVersion | StorageUrl                                | CreatedAt            | GenerationStatus | GenerationAttemptId | GenerationLeaseUntil |
      | other | b-old         | file:///fixtures/other-client-invoice.pdf | 2024-01-07T10:00:00Z | Completed        | <null>              | <null>               |
    When an expired held invoice attempt resumes after its replacement completes
      | Field        | Value                                 |
      | LeaseFixture | expire-only-held-lease                |
      | Completion   | real-replacement-before-old-resumes   |
      | Cleanup      | explicit-old-attempt-only             |
    Then the replacement invoice and PDF survive stale completion release and cleanup
