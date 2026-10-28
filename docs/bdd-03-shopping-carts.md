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
The workspace has no .NET SDK or Docker daemon; build/format, the full five suites,
OpenAPI export, coverage, security, packaging and image checks run in PR CI.
The PR records actual runtime results and an independent bundle/TRX audit before
completion.

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
