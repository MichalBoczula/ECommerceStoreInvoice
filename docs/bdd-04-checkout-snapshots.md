# INV-BDD-04: checkout and immutable product snapshots

The original `Create order returns created order` case keeps its title, scenario
identity, seeded product, quantity, prices and HTTP outcome. It now checks the
complete response, all stored fields and successful coordinated state. Two new
cases cover multiple real catalog products and snapshot preservation after real
catalog changes and unavailability.

The inventory is **83 BDD + 27 standalone = 110 acceptance cases**, in the same
38 features with 58 declarations. All original 77 cases and task 3's four cases
remain. The additional standalone recorder check verifies that API and Products
exchanges share identities/sequences while retaining their distinct targets and
roles. The same 15 operations, 54/56 guaranteed response pairs and two owned
later-task gaps remain.

## Scenarios and complete state

| Case | Explicit inputs and assertions |
| --- | --- |
| Original single-product checkout | Seeded POCO product, quantity 2 at PLN 2499.00, exact line/order total PLN 4998.00. |
| Multiple products | POCO quantity 2 and Redmi quantity 3 at PLN 999.00: complete per-product snapshots, exact totals 4998.00 + 2997.00 = PLN 7995.00. |
| Catalog changes and outage | Two private catalog products at PLN 123.45 × 3 and PLN 19.95 × 2: exact total PLN 410.25. Later name, brand, amount and currency changes are confirmed over actual Products HTTP; saved order/snapshot reads remain exact before and after stopping that catalog API. |

Each scenario has authored intent and explicit Gherkin tables for identity/date
fixtures, complete cart products, another client's populated cart, bodyless
checkout request, order profile, every snapshot-line field, persistence counts
and supporting reads. Expected values are required; no response values fill
missing expectations.

Complete responses include order/client IDs, UTC CreatedAt, null UpdatedAt,
Created status, currency/total, quantities and line totals; each nested snapshot
includes its generated ID, original ProductId, IsActive, CreatedAt, null
DeactivatedAt, name, brand and complete money. Snapshot IDs must join the exact
stored order lines, remain distinct and differ from catalog product IDs. Totals
are checked against explicit table values and independently recomputed from
expected quantities/prices.

The test factory reads full raw BSON for shopping carts, orders, product versions,
invoices and client data versions before/after checkout and supporting reads.
Every known order, line, snapshot and cart field is compared. Exactly one order
and the requested number of snapshots are added; the target cart retains its
identity/client/CreatedAt, clears all lines and advances UpdatedAt within the
measured request window. The other populated client cart and unrelated
collections remain exact. Any pre-existing orders/snapshots must remain exact.

Fixture carts are inserted directly with the historical UTC dates in their
Given table, making timestamp advancement deterministic without waiting or
substituting the production clock. Response-to-storage date comparisons normalize
only BSON's millisecond precision. Order-by-ID, client-list and both cart reads
are actual checked HTTP calls, followed by a complete five-collection comparison.
Successful final state does not substitute for task 11's deterministic race and
actual rollback evidence.

## Actual catalog evidence and isolation

Scenarios tagged `checkout-evidence` add the shared byte recorder to the existing
named Products HttpClient. The real production Kiota client and adapter still
execute against the digest-pinned Products API and real SQL Server. An independent
handler counts actual outbound requests: each successful checkout must perform
exactly one Products request; subsequent order/cart reads cannot increase it.
The actual `/mobile-phones/by-ids` request/response body is available in Allure,
including the requested IDs and returned product values.

HTTP envelopes now also identify `targetService` and `exchangeRole`. Normal
inbound API calls retain target `invoices` / role `api`; outbound calls use
`products` / `checkout-catalog`. Explicit fixture-verification calls use
`products` / `catalog-fixture` and do not increment the application's fetch
counter. All targets share the same scenario/instance IDs and exchange sequence.
This distinguishes supporting catalog probes from business fetches without
inventing server responses or a single inferred primary branch.

The `isolated-products` case owns a separate Products API, SQL Server and Docker
network using the same pinned image. Parameterized direct SQL fixture updates
change only its seeded rows. These updates are labelled as fixture changes, not
as Products business writes/history verification. An actual HTTP query confirms
every changed field before snapshot reads. Testcontainers stops that owned API;
reads must still succeed without any attempted catalog request. Both containers
and their network are disposed with the scenario. Shared seeded catalog data is
never mutated or stopped.

Additional JSON evidence carries the service/scenario/instance identities and
complete fixtures, five-collection snapshots, expectations, request windows,
actual profiles and verification records emitted after assertions pass. State is
queued before assertions so failures retain observed data. Task 2's canonical
capture retains actual bytes/counts/hashes and real Given/When/Then phases.

## Verification and scope

`bash scripts/ci.sh source` passes all 22 regressions and the protected baseline:
15 operations, 83 expanded cases and the same two response-coverage gaps.
`git diff --check` passes. Local .NET/Docker are unavailable; runtime verification uses the existing CI.

The implementation at `7730040eb31e174754475343c714424e36c2ff81` is verified by
[CI 40699754304](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40699754304)
through tested merge commit `6a30d54f52ae1b24582776a4a26ba37b80c2e579`.
Its tree exactly matches the submitted implementation. Release build, formatting,
audited restore, generated OpenAPI, dependency/secret checks, packaging, quality
gate, Docker build and high/critical image scan pass, with all five suites:

| Suite | Passed, zero failures/skips | Line coverage |
| --- | ---: | ---: |
| Domain | 112 | 89.5% |
| Application | 95 | 86.3% |
| Infrastructure | 73 | 93.2% |
| ExternalProviders | 3 | — |
| Acceptance | 110 | — |

The independently downloaded bundle's outer ZIP digest matches the official
Actions artifact metadata. The inner bundle is **956967 bytes**, SHA-256
`ac28b98b6f8016287baf1f5e65556fc8f433d53128c17bb630039dbdaeb94392`.
All **1167 manifest entries** have verified sizes/hashes and closed inventory;
all 38 bundled feature files byte-match the reviewed source.

Actual TRX retains all prior 107 cases and adds only two BDD cases and the one
standalone recorder check. All 81 prior BDD scenario/instance identities and
titles remain, including the original checkout. The 83 passed BDD results have
**244 paired actual HTTP exchanges**: 239 API, three checkout-to-Products and
two explicit catalog-fixture probes; phases are 95 Given / 93 When / 56 Then.
Each checkout has exactly one actual outbound Products POST containing exactly
the requested product IDs. The full response/saved snapshot/quantity/decimal
price/line-total/order-total joins were independently checked. All five raw
MongoDB collections remain exact across every supporting read and after catalog
changes/outage; both initial and changed catalog values are actual HTTP data.

All **959 attachments** resolve without orphans: 817 valid JSON, 139 CSV tables,
one PDF and two text files. Every captured body byte count/base64/SHA-256 matches,
and the PDF bytes equal its captured response. Pinned Allure **2.46.1** renders
all 83 passed cases. Every rendered identity, title, description and attachment
name/type/byte content matches its raw result; every case, attachment and entry
asset resolves over local static HTTP. This verifies the generated report,
not browser pixels or a deployed ACA/Nginx instance.

OpenAPI, flow/policy exports and original operation metadata remain unchanged;
the projection adds only two scenario links. The audit snapshot remains frozen
to this implementation run; documentation-only completion updates retain its
provenance. Task 04 builds on Task 03 PR #222 and is reviewed in PR #223; merge
the dependency first while it remains open.

No production source, public API, repository behavior, transaction boundaries,
package, coverage/security gate or Azure resource changes. Mixed-currency checkout
semantics remain a separate decision; this package checks same-currency orders.
Existing master archive/image publication stays intact; selected LiveDocs refresh
and Azure rollout remain the manual final handoff.
