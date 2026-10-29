Feature: Authored invoice source descriptions and evidence links
  Source identities and readable meaning stay separate from actual request and response observations.

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
    And technical outcomes use this complete contract
      | Field           | Value                         |
      | Collections     | all-five-populated-unchanged  |
      | CatalogRequests | 0                             |
      | ProbeFormat     | plain-health-status           |

  @technical-evidence @allure.label.scenarioId:INV14_Source_DescriptionsAndJoins
  Scenario: Authored source documentation preserves identities and honest HTTP boundaries
    Every compiled flow identity, qualified BPMN step, authored step description, policy/rule membership and outcome family is checked against source metadata and real documentation reads. Thirteen flows have direct HTTP entries; two are published only, and the internal bulk service is unpublished. Reading documentation does not execute those rules or prove an internal branch trace. Populated records and complete supporting reads remain unchanged.
    Given source documentation has this authored inventory
      | Kind            | Count |
      | Flows           | 16    |
      | PublishedFlows  | 15    |
      | HttpFlows       | 13    |
      | Steps           | 128   |
      | Policies        | 8     |
      | PolicyRules     | 17    |
      | InlineRules     | 2     |
      | OutcomeFamilies | 65    |
    When I read the authored flow and policy descriptions
    Then every source identity and description is coherent without inventing HTTP reachability
    And technical operations preserve all populated collections and complete supporting reads
