# INV-BDD-03: complete shopping-cart success evidence

The three original create/get/update success cases retain their titles, explicit
scenario IDs, request values and outcomes. Four additional cases exercise a
populated GET, replacement with a changed quantity/removal, clearing a populated
cart and an empty replacement on an already empty cart. All 77 original cases
remain: **81 BDD + 26 standalone = 107 acceptance cases**, in the same 38 features
with 56 declarations. The 15 operations, 54/56 guaranteed response pairs and two
owned task-10/task-11 gaps remain unchanged.

## Response and persistence checks

| Operation | Checked behavior |
| --- | --- |
| Create | Requested ClientId, fresh nonempty cart ID, empty Lines, UTC CreatedAt/UpdatedAt in the measured HTTP window, exactly one new document and another populated client cart unchanged. |
| Get | Complete identity, timestamps and exact product IDs/quantities for both empty and populated carts; the entire collection remains unchanged. |
| Update | Exact replacement lines, retained cart ID/ClientId/CreatedAt, UpdatedAt advanced within the measured window, no added document and another populated client cart unchanged. |
| Empty update | Explicit `Lines: []` clears populated contents or preserves an already empty collection; identity/CreatedAt remain and UpdatedAt advances. |

Every success has explicit request/response and persistence tables. Missing
expected values fail rather than falling back to response fields. Lines are
compared as complete product/quantity records without promising HTTP ordering.
Original presence flags remain, but complete response-to-fixture and
response-to-database checks now establish the values behind them.

`ShoppingCartSuccessState` is scenario-local and shared by the three small
binding classes and persistence bindings. Real raw BSON documents are read from
the factory's isolated MongoDB database immediately before and after the primary
request. Evidence retains the complete documents, including binary GUIDs and
stored dates in Extended JSON. Every known cart and line field is projected and
checked; there is no new history/version collection in this service.

For a deterministic timestamp assertion, populated/empty update fixtures are
inserted directly into MongoDB with the explicit historical UTC dates in their
Given tables. They model pre-existing carts, not successful HTTP setup writes.
The original empty-GET fixture is still created through the real HTTP API; its
complete setup response and persistence are checked. BSON dates have millisecond
precision: response-to-storage comparisons normalize only for that storage
precision, while generated HTTP timestamps are checked against the actual request
window. No delays, clock substitutions or production timestamp changes are used.

Every case starts with a second populated client cart. Its entire raw BSON
document, including both lines and timestamps, must remain identical. Supporting
GETs through the shared checked HTTP client verify each persisted cart again,
then a final MongoDB read proves those supporting reads also left all documents
unchanged.

## Allure evidence

Task 2's canonical HTTP request/response recorder remains the only client
pipeline. Primary, HTTP setup and supporting reads retain their actual phases
and exchange identities. Additional JSON evidence is queued with the same
`service`, `scenarioId` and `instanceId`:

- explicit stored fixtures, setup response and setup database;
- complete database before/after and after supporting reads;
- response, payload and persistence expectations and the measured request window;
- complete response and a persistence-verification record emitted only after all
  assertions pass.

The Gherkin tables remain visible through Allure's adapter. Snapshot evidence is
queued before assertions so failed assertions retain the observed state. Fixture
inserts are labelled as database setup, never fabricated as HTTP exchanges.

## Verification and boundaries

`bash scripts/ci.sh source` passes all 22 source/link regressions and checks all
81 expanded cases against the fixed 77-case baseline. `git diff --check` passes.
The workspace has no .NET SDK or Docker daemon; actual build/format/runtime and
image verification uses CI.

The implementation at `e48e1776535de10b9882474a7cd69d4af73101c9` was tested by
[PR CI 40692028779](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40692028779)
through merge commit `92ab12ee7315975f86664a0812567864f0585b9f`. Its tree exactly
matches the submitted implementation. Release build, formatting, audited restore,
generated OpenAPI, all five suites, dependency/secret checks, quality gate,
packaging and Docker build/high-critical image scan pass:

| Suite | Passed, no failures/skips | Line coverage |
| --- | ---: | ---: |
| Domain | 112 | 89.5% |
| Application | 95 | 86.3% |
| Infrastructure | 73 | 93.2% |
| ExternalProviders | 3 | — |
| Acceptance | 107 | — |

The downloaded bundle's outer Actions ZIP SHA-256 matches the official artifact
metadata. Its inner bundle is **827848 bytes**, SHA-256
`504404caa1d597ce52135a5c8bd869fb43dc4596679c9c529afb58d0992641b4`.
All **1024 manifest entries** have verified sizes/hashes and closed inventory.
All 38 bundled feature files byte-match the reviewed source.

The independent TRX audit preserves all 103 task-2 cases and adds only the four
new cart cases. All 77 original BDD IDs, titles and explicit instance IDs remain.
The actual 81 passed results have **219 paired HTTP exchanges** (96 Given,
88 When, 35 Then) and **820 resolving attachments**: 704 JSON, 113 CSV tables,
two text and one PDF. All byte-count/base64/hash envelopes match, all JSON parses,
and the PDF bytes equal the captured response. Every one of the seven cart cases
contains its complete response/expectations and database before/after/supporting
read evidence with matching identities. Their stored lines equal the actual
responses and PUT bodies; every other-client document remains exact.

Pinned Allure **2.46.1** renders all 81 passed cases. Each rendered title,
description, identity and every attachment name/type/byte content matches its
raw result. Every case, attachment and entry asset resolves over local static
HTTP. This is generated-report verification, not a browser-pixel or deployed
ACA/Nginx check. Exported OpenAPI, flows, policies and original operation metadata
are unchanged; the link projection adds only the four scenario links. The audit
snapshot stays frozen to this implementation run; documentation-only completion
updates do not rewrite its provenance.

Task 2 was independently confirmed from merged PR #221 and successful master
run 40584879097 at `85327a2d9437046d572fb3a46b37ad502c0adb7f`.
Its actual bundle contains 77 passed cases with descriptions/explicit identities,
666 resolving attachments and 862 valid inventory entries. The inner bundle is
717662 bytes with SHA-256
`78b7a1c14e9825a707745b3bf2e55a74494ec492f9def6f1fe941475e46cb83c`.

No production source, package, route, DTO, business policy, repository, index,
transaction, CI gate or Azure resource changes. Validation/framework rejection
state, deterministic concurrency and wider contracts remain their later tasks.
The existing master archive/image publication remains; selecting a new LiveDocs
bundle/image and deploying Azure remain the manual final handoff.
