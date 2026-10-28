# INV-BDD-09 — Business validation

## Scope

Preserve all 106 earlier BDD cases, their identities/titles/behavioral Examples,
and all 27 standalone acceptance checks. Strengthen three original cart validation
cases without changing their empty-ID or product/quantity-zero causes. Add 96 BDD
cases: 56 rejections and 40 accepted boundaries. The inventory is **202 BDD + 27
standalone = 229 acceptance checks** across 39 feature files. No public API,
business rule, persistence, dependency or CI/coverage/security gate changes.

All business cases declare complete typed default JSON in Gherkin tables. Single
field cases replace exactly one declared JSON pointer; aggregation cases explicitly
list every mutation. Captured requests/responses are actual HTTP with stable
scenario/instance/exchange identities. Expectations are separate from observed
state. Historical fixtures are labelled direct Mongo setup, not invented API calls.

## Rejections and accepted boundaries

The client cases exercise every required/format/positive branch of phone number
and prefix, all email required/format branches, postal required/format branches,
and each optional address component's invalid-character branch. Null/empty/blank,
zero/all-zero, signs, non-ASCII digits and signed 64-bit overflow are distinct
inputs. Positive 1 and Int64 maximum, leading zeroes, valid email forms, minimum/
maximum and Unicode postal digits, optional null/blank values and permitted
punctuation are accepted. ClientName currently has no domain validation; null,
empty and blank names are explicitly documented as accepted behavior.

These accepted null strings expose an existing DTO/OpenAPI mismatch: the generated
request/response properties are required strings without nullable metadata. The
current shared response checker skips null properties. This package independently
asserts the actual null values and preserves current behavior; Task 13 must align
contract nullability with the observed policy. Passing the current OpenAPI gate
does not prove these nullable values are accurately described.

Aggregation retains all eight ordered client errors, including repeated rule
names. A separate null-postal/invalid-city case verifies the address rule's actual
early return. It does not mislabel the unexecuted city check as successful.

Cart cases isolate empty product IDs, negative and Int32-minimum quantities. The
original quantity-zero case now has exact complete evidence against a populated
two-line cart. A repeated-error case retains both ID errors then both quantity
errors. Minimum positive 1 and Int32 maximum, empty lines, duplicate product IDs and
unknown nonempty IDs document current acceptance without Products calls. No
catalog existence, maximum-cart-total or duplicate-line rule is invented.

Order status cases distinguish parser errors from transition-policy errors. Null,
empty, blank and unknown strings produce exact parser descriptors. Numeric strings
2, -1 and 99 parse but fail the transition policy. Numeric 0/1, mixed case and
surrounding whitespace can make legal Created → Paid/Cancelled transitions.
Task 05's complete terminal rejection scenarios remain. Added empty-client cases
cover client-version creation, cart replacement and checkout before catalog access.
The shared GUID rule's existing ShoppingCart-labelled descriptor is preserved.

Every validation problem must have exactly type/title/status/detail/instance/
traceId/errors, the complete existing safe signature, exact ordered three-field
error objects, no PDF disposition and the existing no-cache/no-store directive set.
Repeated errors are compared with multiplicity, rather than converted to a set.

## Populated state and accepted effects

Every new/strengthened case seeds four interleaved immutable client versions,
three orders (including both clients' paid orders), three exact product snapshots,
two populated carts (target initially has two lines), and two completed invoices
that retain older client-version references. All five collections are populated.

Raw complete documents are compared before/after primary and supporting requests.
Fixture and expected BSON field layouts match the typed persistence documents;
full BSON comparisons retain ordered lines and every original field.
Rejections cannot alter any field, ordered line, timestamp or historical reference.
Accepted client requests append exactly one new distinct version; accepted cart
requests replace only target lines/UpdatedAt; accepted statuses change only target
status/UpdatedAt. All generated timestamps are UTC and bounded by the actual HTTP
request window; Mongo's millisecond storage precision is accounted for explicitly.

Nine complete supporting GETs inspect both carts, both clients' latest versions,
all three orders with saved snapshot details/totals, and both historical invoices.
Every field, JSON type/null and nested line must match independently constructed
expectations. All nine expected profiles share one identified JSON attachment per
scenario; each actual HTTP request and response retains its own shared-capture
attachment. This keeps the complete evidence inside the existing 10,000-file
LiveDocs manifest limit without changing the schema or filtering exchanges. Supporting reads preserve all five collections. The actual named
Products HttpClient has the shared recorder and request counter installed; every
case requires zero attempted catalog requests.

## HTTP reachability and standalone rules

| Rule surface | Evidence boundary |
| --- | --- |
| Client ID policy | Empty GUID is HTTP reachable across the public operations; prior Task 05–08 cases and Task 09 additions preserve the exact shared descriptor. Non-GUID routing belongs to Task 10. |
| Client contact/address policy | Actual HTTP client-version creation, isolated branches, accepted boundaries, ordered aggregation and complete populated state in this package. |
| Shopping-cart line ID/quantity policy | Actual HTTP cart replacement. Null collection/items and missing/wrong-type bodies are mapping/framework paths in Task 10; a standalone null-line rule does not establish a safe HTTP policy rejection. |
| Order transition / invoice paid-order policy | Actual HTTP in Tasks 05/07 and parser/boundary additions here. Ownership/missing orders remain distinct from invalid field values. |
| Order lines policy | Real empty-cart checkout is existing Task 04 HTTP evidence. Domain null-order/null-lines descriptors do not establish an HTTP null-order request contract. Public checkout constructs its order from cart/catalog data. |
| Product snapshot policy | Checkout validates catalog-derived snapshots. There is no public product-version create endpoint and no request body containing snapshot prices/names/currency. Existing Domain/Application tests plus new isolated boundary tests are standalone evidence, not invented HTTP cases or a substitute catalog. |
| GUID collection policy | Used by the application batch product-version service, which has no public HTTP route. Null/empty/empty-item and valid collections remain standalone Domain/Application evidence. Duplicate nonempty IDs are accepted by the policy; service deduplication is separate. |

Twenty-one added standalone Domain cases cover nonpositive/positive decimal
boundaries, Money currency normalization, exact isolated/paired currency errors,
isolated null/empty/blank snapshot names/brands, and null/duplicate GUID collections.
They are reported in Domain TRX, not Allure BDD or acceptance counts. Positive
snapshot decimal limits are policy acceptance only; they do not claim full order
arithmetic can never overflow. Money construction uppercases currency; a null
currency passed to its constructor is not a successfully executed policy branch.

## Verification

`bash scripts/ci.sh source` passes all 22 regressions, the unchanged frozen 77-case
baseline, 202 exact matrix links and all 15 operation profiles. Guaranteed status
intent remains 54/56; the same Task 10/11 gaps remain. A baseline-guard regression
now explicitly mutates an original frozen example, rather than assuming the first
current outline is original when a new feature sorts earlier. The guard itself
and its baseline are unchanged. `git diff --check` passes.

Local .NET/Docker are unavailable. Runtime verification uses actual CI; the
completed implementation and artifact audit are recorded below. Task 08 PR 227
was merged as master c0220519e7f738ea802f026af9f54ca9e7ce4bf3 before this branch.
Master source publication stays unchanged. Selected LiveDocs refresh is manual.

## Verified implementation evidence

Implementation `5e1fc8bb60a90a3c30b10573a415705cd11a9f1f` passes full
[CI 40786144594](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40786144594).
Tested merge `e7b0f8ea02ea5491063b307faf8a3c37cda05482` exactly matches submitted
tree `c5935fec36b5881e6b9af94150943a0ec9af8e56`. Release build, formatting,
audited restore, generated OpenAPI, dependency/secret checks, packaging, quality
gate, Docker build and high/critical image scan pass.

| Suite | Passed, zero failures/skips | Line coverage |
| --- | ---: | ---: |
| Domain | 133 | 89.5% |
| Application | 95 | 86.3% |
| Infrastructure | 73 | 93.2% |
| ExternalProviders | 3 | — |
| Acceptance | 229 | — |

All **533 tests** pass above unchanged separate 70% coverage floors. Actual TRX
preserves all prior suite members with multiplicity and adds exactly 96 BDD cases
and 21 Domain cases. All 106 prior BDD scenario/instance identities and titles
remain; three original cart cases now have complete populated evidence. The 39
features contain 87 declarations and 202 exact expanded matrix links. All 22
source regressions and the fixed 77-case baseline pass, with the same two later
status gaps. Task 08 merged-master CI 40782266531 also passed including publication.

Independently downloaded outer Actions ZIPs match their official digests. The
inner documentation bundle is **9027876 bytes**, SHA-256
`33d7f810cf957b781c0d398255274404342f0b26d0bb07f172ecfbaf0e84a9a6`.
All **9817 manifest entries** have verified hashes/sizes and closed inventory;
all 39 feature files byte-match reviewed source. The existing 10000-file schema
limit is enforced; later additions must keep budgeting complete evidence within
that limit. OpenAPI and flow/policy exports are unchanged. All 15 original
operation profiles and 106 prior case links remain; projection adds 96 links.
The nullable-string contract mismatch described above remains explicit Task 13
work rather than being hidden by the passing current OpenAPI gate.

The independent raw-state/HTTP audit verifies all **99 affected cases** and
**99 primary requests**: 59 exact safe rejections and 40 complete successes.
Successes append 31 distinct client versions, replace five cart line collections
and make four legal status changes. Generated values have bounded UTC timestamps.
Both eight-error client aggregation and four-error repeated-line aggregation
retain exact order/multiplicity. The separate postal short-circuit also matches
the declared rule behavior. All **891 supporting GETs** return complete profiles
for both carts/latest client versions, three orders/snapshots and two historical
invoices. All five populated collections match their complete expected state;
old versions/references remain exact and actual Products attempts stay zero.
An independent rule model also agrees with all 90 isolated Gherkin input/outcome
pairs. Earlier Task 05–08 state, recovery, private-Blob and PDF audits still pass.

All 202 passed BDD results contain **1616 paired captured HTTP exchanges**:
1611 API, three checkout-catalog and two fixture probes; phases are 11 Given /
222 When / 1383 Then. All **9370 attachments** resolve without orphans: 7910
valid JSON, 1453 CSV tables and seven PDFs. Captured byte counts/base64/hashes
match. Grouping all nine expected supporting profiles into one JSON per business
scenario saves 792 files while retaining each actual request/response, every
expected field and all raw state observations. No exchange filtering or schema
limit change is used. All seven existing binary PDFs still match their actual
responses and declared artifacts and parse with at least one page.

Pinned Allure **2.46.1** renders all 202 passed cases. Every identity, title,
description and attachment name/type/bytes matches raw results; every case,
attachment and entry asset resolves over local static HTTP. These checks do not
claim browser pixels, deployed Azure permissions or a refreshed portal. This
snapshot stays frozen to the implementation run; the documentation-only
completion commit receives the normal full CI and independent artifact recheck.
