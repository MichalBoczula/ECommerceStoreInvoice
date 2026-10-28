# INV-BDD-06 — Client data versions

## Scope and preserved behavior

The five existing client-data scenarios retain their identities, titles, original
valid profile fields, invalid phone value and empty-client identifier. They now
exercise populated histories rather than a single version or an empty database.
Four new cases cover interleaved updates, identical payloads, reversed insertion
order and the second client lookup: **94 BDD + 27 standalone = 121 acceptance checks**.
All 77 baseline cases and the subsequent Task 03–05 cases remain.

Every case uses three historical versions for client A and two for B, with
explicit, distinct historical UTC timestamps. Insertion order differs from
creation order. The original successful lookup still returns John Doe's complete
Warsaw profile; another client's later timestamp cannot affect that selection.
The reversed-order case inserts the newest target version first and oldest last.
No equal-timestamp tie-break is claimed: the repository only sorts CreatedAt.

Actual HTTP POSTs append a new ID and all nine contact/address fields. Complete
response JSON must contain exactly all 12 string fields, with valid typed UUIDs,
matching client identity and a UTC creation timestamp inside the measured request
window. Complete raw BSON is compared to a copy of the before state plus exactly
one expected version derived from the explicit request fields. Repeating identical
payloads creates a distinct version; interleaved A/B/A updates preserve every
previous field and change only the corresponding latest lookup. Before successive
POSTs, a bounded measured clock check crosses the previous stored millisecond,
matching BSON precision without relying on arbitrary fixed delays or tie ordering.

## Populated state, invoice references and evidence

Each fixture supplies all five collections explicitly. Both clients have complete
paid orders, immutable product snapshots, populated carts and completed invoices
referencing their oldest client versions. Fixture tables include complete contact,
address, price, quantity, date, completion and storage-marker values. Direct raw
Mongo insertion is labelled historical fixture setup; it does not claim checkout,
invoice generation or PDF storage. Task 04 separately covers real checkout and
Tasks 07/08 cover generation/recovery and PDF behavior.

Before/after primary operations and supporting reads record complete raw BSON.
Every read and validation failure must preserve all five populated collections.
Only successful creation may append one complete client-data version; all old
IDs, values, timestamps and invoice references must remain exact. The original
invalid phone, empty ID and missing-client cases verify complete observed problem
fields, exact rule name/entity/message, generated trace ID and request-path
instance. Public spelling and error behavior are preserved, including
`clietDataVersionId` and the existing ShoppingCart-labelled empty-ID rule.

Every outcome reads both complete latest profiles and both historical invoices
by invoice ID and order ID. All five invoice metadata fields must still match the
stored old reference, storage marker and timestamp. Both lookup results must
match independently defined fixtures/requests, not each other. The final database
snapshot must remain identical. Shared capture records actual HTTP bytes, hashes,
phases and service/scenario/instance/exchange IDs; fixture/request/expectation/state
JSON uses the same scenario identities. Observed snapshots precede assertions.

The `client-version-evidence` tag instruments the real named Products HttpClient
without starting Products/SQL containers or replacing the adapter. All primary
and supporting operations must attempt zero catalog requests. MongoDB still uses
the real per-scenario database and existing shared replica set.

## Verification

`bash scripts/ci.sh source` passes all 22 regressions, all 94 matrix cases, the
fixed baseline and 15 operation links. Guaranteed status coverage remains 54/56;
the same framework/concurrency gaps stay assigned to Tasks 10/11. `git diff
--check` passes. Local .NET/Docker are unavailable; actual build, formatting,
runtime suites, packaging and image checks will be verified through full CI.

Task 06 starts from merged Task 05 master
`d4c3a036a569bdb5e7f850d9afaad956d91490f9`. No production source, API contract,
policy, repository/index/transaction behavior, dependencies, CI/security/coverage
gate or Azure configuration changes. Existing successful-master publication stays
intact; selecting and deploying the new LiveDocs snapshot remains manual.
