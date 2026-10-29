Feature: Technical HTTP behavior preserves invoice data
  Probe formats, real outages, aborted reads and isolated hosts have explicit evidence without inventing business operations.

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

  @technical-evidence
  Scenario Outline: Healthy probes return the exact non-business health format
    A real replica-set host serves the health alias, liveness and readiness routes. The entire plain status body, media and cache headers are checked. Liveness performs no readiness commands; readiness performs actual hello and database ping. Startup indexes have their exact keys and uniqueness, and all populated collections remain unchanged.
    When I request the technical health route "<Path>"
    Then technical operations preserve all populated collections and complete supporting reads

    Examples:
      | Path          | scenarioId                  |
      | /health       | INV12_Health_Alias          |
      | /health/live  | INV12_Health_Live           |
      | /health/ready | INV12_Health_Ready          |

  @technical-evidence @owned-mongo-replica @allure.label.scenarioId:INV12_Readiness_RealOutageRecovery
  Scenario: An owned Mongo outage changes readiness while liveness and documentation remain available
    The host remains running while only its owned real Mongo replica stops. Alias/liveness stay Healthy, readiness returns the exact safe Unhealthy 503, and both documentation endpoints remain byte-identical to their complete source projections. Restarting that same Mongo restores readiness and all five original populated collections. Shared test containers are never stopped.
    When the owned Mongo replica stops and restarts while the host stays running
    Then technical operations preserve all populated collections and complete supporting reads

  @technical-evidence @owned-mongo-standalone @allure.label.scenarioId:INV12_Readiness_StandaloneRejected
  Scenario: Standalone Mongo permits startup indexes but is not transaction-ready
    An actual standalone Mongo server initializes the host and stores the populated fixture. Alias/liveness remain Healthy. Its actual hello lacks a replica set or router, so readiness returns Unhealthy 503 without a ping. Complete healthy business reads still preserve all documents; this does not claim that standalone Mongo can perform checkout transactions.
    When the standalone Mongo host is live but is not ready for transactions
    Then technical operations preserve all populated collections and complete supporting reads

  @technical-evidence
  Scenario Outline: Complete source documentation is stable over populated data
    Each response is compared with every current source-generated descriptor and its complete nested values: fifteen flow descriptors or all eight registered validation policies. A second HTTP request returns exactly the same bytes. No business policy execution or database write is implied by reading documentation.
    When I read complete technical documentation "<Kind>" twice
    Then technical operations preserve all populated collections and complete supporting reads

    Examples:
      | Kind        | scenarioId                        |
      | flows       | INV12_Documentation_CompleteFlows |
      | validations | INV12_Documentation_AllPolicies   |

  @technical-evidence
  Scenario Outline: Swagger exposes the generated document and real UI entry
    The real Swagger JSON matches the host's complete generated source document with fifteen unique named operations. The UI representation serves its actual HTML entry and bootstrap script referencing that document. These HTTP checks do not claim browser rendering or full request/response contract semantics, which remain later packages.
    When I request Swagger representation "<Representation>"
    Then technical operations preserve all populated collections and complete supporting reads

    Examples:
      | Representation | scenarioId                |
      | json           | INV12_Swagger_Generated   |
      | ui             | INV12_Swagger_UiEntry     |

  @technical-evidence @technical-cancellation
  Scenario Outline: Aborting a real PDF read releases the stream and preserves data
    A completed invoice has a real immutable local PDF. The actual file stream is held after opening before headers, or after reading its first 32 bytes during the response body. Cancelling the HTTP request propagates RequestAborted, disposes the real stream and records a transport/body failure without fabricating an HTTP 499 or a complete PDF. A subsequent real download has exactly the original bytes and safe PDF headers; every stored document and artifact remains unchanged.
    Given the completed technical invoices have real immutable PDF files
    When the real PDF request is cancelled "<Mode>"
    Then the cancelled PDF can be downloaded intact with all data unchanged

    Examples:
      | Mode           | scenarioId                         |
      | before-headers | INV12_Cancellation_BeforeHeaders   |
      | during-body    | INV12_Cancellation_DuringBody      |

  @technical-evidence @allure.label.scenarioId:INV12_Isolation_HostDatabaseCleanup
  Scenario: Disposing one host drops only its own database despite identical business IDs
    Two real hosts share Mongo infrastructure but use distinct databases and exactly the same business identifiers with different values in all five collections. Complete API reads return each host's own values. Disposing the peer actually drops only its database; the original host, records and readiness remain intact. All peer clients and the owned host are cleaned up even on failure.
    When another host uses the same business IDs in its own database and is disposed
    Then technical operations preserve all populated collections and complete supporting reads
