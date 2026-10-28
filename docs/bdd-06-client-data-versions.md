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
runtime suites, packaging and image checks use actual CI.

Task 06 starts from merged Task 05 master
`d4c3a036a569bdb5e7f850d9afaad956d91490f9`. No production source, API contract,
policy, repository/index/transaction behavior, dependencies, CI/security/coverage
gate or Azure configuration changes. Existing successful-master publication stays
intact; selecting and deploying the new LiveDocs snapshot remains manual.


## Verified implementation evidence

Implementation `dbc6421f208456372a9ece5b6155d32793117734` passes full
[CI 40725649880](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40725649880).
Tested merge `7e009a45988ccea4e105652e52fb6cee8e587d90` has exactly the submitted
tree `e8aae3aad311b3ceadac49e08c667752a5bdc6ca`. Release build, formatting,
audited restore, generated OpenAPI, dependency/secret checks, packaging, quality
gate, Docker build and high/critical image scan pass.

| Suite | Passed, zero failures/skips | Line coverage |
| --- | ---: | ---: |
| Domain | 112 | 89.5% |
| Application | 95 | 86.3% |
| Infrastructure | 73 | 93.2% |
| ExternalProviders | 3 | — |
| Acceptance | 121 | — |

All **404 tests** pass, with unchanged separate 70% coverage floors. Actual TRX
preserves all previous 117 test members and adds exactly four BDD cases. The
38 feature files now contain 65 declarations and 94 expanded cases; all prior
90 scenario/instance identities and titles remain. The existing 22 source
regressions and all 94 exact matrix links pass. Master CI 40723804985 also
completed successfully after the Task 05 merge, including image/archive publication.

The official outer Actions ZIP digests match independently downloaded bytes.
The inner bundle is **2193617 bytes**, SHA-256
`48806a35c96d8aa991d23f27befa989d66b930c3a23ade7f8808c23564701807`.
All **2514 manifest entries** match their sizes/hashes and closed inventory;
all 38 bundled features byte-match reviewed source. OpenAPI, flows and policy
exports are unchanged. All 15 original operation profiles and previous 90 links
remain exact; the operation projection adds only the four new cases.

The independent state/HTTP audit checks all nine Task 06 cases and all 12 primary
requests: six successful appends, three successful primary lookups and three
rejections. Every append adds one complete version with a new identity and
strictly later stored timestamp. All old versions and every document in the other
four collections remain byte-for-field identical. Every read/rejection preserves
the full populated database. The 72 supporting GETs verify 24 complete latest
profiles and 48 complete historical invoice responses against raw stored state,
including both owners and both lookup routes, with zero Products requests.
The previous Task 05 full-state/response audit also passes unchanged.

All 94 passed BDD results have **446 paired HTTP exchanges**: 441 API, three
checkout-catalog and two fixture probes; phases are 62 Given / 109 When / 275 Then.
All **2284 attachments** resolve without orphans: 1927 valid JSON, 354 CSV tables,
two text files and one PDF. Captured byte counts/base64/SHA-256 match and the PDF
attachment equals the actual captured response bytes.

Pinned Allure **2.46.1** renders all 94 passed cases. Every rendered identity,
title, description and attachment name/type/bytes matches raw results. Every
case, attachment and entry asset resolves over local static HTTP. These are
generated-report/resource checks, not browser-pixel or deployed ACA/Nginx claims.
This audit snapshot is frozen to the implementation run; the documentation-only
completion commit receives the normal full CI recheck.
