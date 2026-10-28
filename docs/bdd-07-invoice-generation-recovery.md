# INV-BDD-07 — Invoice generation and recovery

## Scope

Eleven existing invoice-creation/rule/recovery scenarios retain their identities,
titles, original contact/product/quantity values and HTTP outcomes. Five new
cases cover Cancelled rejection, active reservations, failed/expired reclaims and
a failure after a real PDF write. Inventory: **99 BDD + 27 standalone = 126**.
All original 77 BDD cases and the subsequent Task 03–06 cases remain.

Explicit historical fixtures populate all five MongoDB collections: two clients
have orders, immutable snapshots, populated carts and client-data histories; an
unrelated completed invoice references an older version. Missing-owner-data cases
keep the other client's populated history. Fixed scenario-local client identities
preserve the original invalid-order fixture; databases remain isolated. These
are labelled stored fixtures, not fabricated checkout HTTP. Task 04 separately
covers actual checkout. Invoice generation itself uses real application flows,
Mongo repositories, client-version selection and Chromium PDF rendering.

Complete responses require every metadata field, actual claimed invoice/order/
client-version identities, the reservation timestamp and the current attempt's
local PDF URL. Existing public `clietDataVersionId` spelling is preserved. Complete
safe problem signatures distinguish missing orders/ownership, missing owner data,
empty IDs, Created/Cancelled status, existing completed invoices and active claims.
Reads and pre-generation rejections preserve all populated state; PDF failures
and successful generation may change only the correct invoice reservation. No Products API request is permitted.

## Real write and attempt observations

A scenario-local observer decorates the registered generation repository and PDF
service, forwarding every normal call to their actual implementations. It records
full before/after raw BSON around claims, completion and release, and captures the
complete order, snapshot and selected client-version inputs used for PDF rendering.
These observations use the shared queued recorder and scenario identities; they
do not invoke ambient Allure state from server callbacks. All observed snapshots
are attached before step assertions. Every primary/supporting HTTP exchange still
uses the central byte recorder and normal OpenAPI response handler.

Assertions compare each repository exchange to an independent copy of its before
state with only explicitly permitted generation fields changed. New reservations
must contain all eight fields, a distinct attempt and a ten-minute UTC lease in
the measured repository window. Failed/expired reclaims preserve invoice ID and
CreatedAt, replace the attempt and select the latest owner version. Successful
completion clears attempt/lease fields; failure release retains the failed attempt
ID and clears its lease. Intermediate states form an exact chain to the final
full five-collection snapshot. Every unrelated field and document stays exact.

The original first-failure wrapper still fails before rendering. Its retry also
creates a newer client version over actual HTTP and proves that reclaim switches
the same invoice to that version with a new attempt. A separate wrapper fails only
after real Chromium writes the PDF, proving actual deletion of that exact failed
attempt file before a distinct retry succeeds. File observations include existence,
length, SHA-256 and `%PDF-` signature. Earlier successful PDFs remain unchanged and
all previously deleted attempt files stay absent. Scenario cleanup deletes only
observer-owned generated files after evidence has been captured.

The original lost-ack wrapper commits completion in real Mongo first, then throws.
The observer records both that exception and the actual Completed state. The
application's reconciliation must return success without releasing the claim or
deleting the available, unchanged PDF.

The original concurrent case now uses a bounded scenario-local gate after a real
claim and before rendering. While the winner is paused, both read routes must hide
its Generating reservation; a second overlapping request loses the real claim and
returns the complete active-generation 409 without changing held state. Releasing
the gate permits one complete invoice and one PDF. No timing race or sleep selects
the winner. Broader competing writes/rollback/stale ownership remain Task 11.

Both invoice metadata routes and latest-version lookups are checked after every
outcome, including hidden incomplete reservations and missing target invoices.
Complete metadata and version profiles match stored values, then all collections
are compared again. Multi-product reclaim cases include an inactive immutable
snapshot; full PDF inputs must retain every saved field, quantity and price.

## Verification and limits

`bash scripts/ci.sh source` passes all 22 regressions, the fixed baseline and 99
exact matrix links across 15 operations. Guaranteed status coverage remains 54/56;
the same Task 10/11 gaps remain. `git diff --check` passes. Local .NET/Docker are
unavailable; actual build, formatting, runtime suites and image checks use CI.

The branch starts from merged Task 06 master
`fedb63d773e61b155defb03d4aff1b15a1688a9e`. No production, public contract/error,
dependency, repository/index/transaction, CI/security/coverage gate or Azure changes.
Fixture storage URLs are labelled markers; generated PDFs are actual instance-local
test files, not shared Blob storage. Download headers, binary download failures,
legacy metadata and real Blob adapter checks remain Task 08. Existing master
publication stays intact; selected LiveDocs refresh and deployment remain manual.


## Verified implementation evidence

Implementation `e2e9fbc5474a39bfc3d0c390546b19b2cc7f385c` passes full
[CI 40731854022](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40731854022).
Tested merge `47f101f188671f44d6df84905af3a67d337dd611` exactly matches submitted
tree `3a5f6d12768fc17234a83d70adbf63284c076c8e`. Release build, formatting,
audited restore, generated OpenAPI, dependency/secret checks, packaging, quality
gate, Docker build and high/critical image scan pass.

| Suite | Passed, zero failures/skips | Line coverage |
| --- | ---: | ---: |
| Domain | 112 | 89.5% |
| Application | 95 | 86.3% |
| Infrastructure | 73 | 93.2% |
| ExternalProviders | 3 | — |
| Acceptance | 126 | — |

All **409 tests** pass with unchanged separate 70% coverage floors. Actual TRX
preserves all 121 previous acceptance members and adds exactly five BDD cases.
All prior 94 BDD identities/titles and the fixed 77-case baseline survive. The
38 features now contain 70 declarations and 99 expanded cases. Source checks
pass 22 regressions and all 99 matrix links; the same two Task 10/11 gaps remain.
Merged Task 06 master CI 40729322729 also passed, including image/archive publication.

The independently downloaded outer Actions ZIPs match their official digests.
The inner bundle is **2831025 bytes**, SHA-256
`e8ce173a3e4b343484f811236ba51025a462e8441e7f53952dfe37e45b3f37fb`.
All **3246 manifest entries** match hashes/sizes and closed inventory. All 38
bundled features byte-match reviewed source. OpenAPI, flow/policy exports and
all 15 original operation profiles stay unchanged; only five case links are added.

The independent state/HTTP audit checks all **16 Task 07 cases**, **20 primary
invoice requests**, the retry's actual client-version POST, **107 supporting
reads** and two hidden reads while the winning reservation is held. It joins
actual HTTP bodies to raw Mongo state and verifies all 22 repository exchanges:
12 claims attempted, ten won, two lost, four reclaims, eight completions and two
releases. Each exchange changes exactly its allowed fields; its complete before/
after state forms an uninterrupted chain to the primary final state. Every other
collection remains exact; reads and pre-generation rejections leave full state unchanged.

All ten PDF attempts have complete saved order/product/client inputs checked.
Four attempts use multiple products, including an inactive snapshot. Eight
succeed; two fail. Both cleanup calls target the failed attempt IDs. One failure
occurs before writing, while the other has an actual `%PDF-` file and hash before
its verified deletion. A retry completes a different attempt using the same invoice
ID and creation timestamp. The first retry also selects the version appended over
HTTP. The lost-ack case records a committed Completed document plus the injected
exception, and returns success without any release or deletion. Deterministic
competition proves hidden live state, exact active-claim 409 and one complete winner.
All generation and supporting operations make zero Products calls. Earlier Task
05/06 complete state/response audits also pass unchanged.

All 99 passed BDD cases have **513 paired HTTP exchanges**: 508 API, three
checkout-catalog and two fixture probes; phases are 18 Given / 119 When / 376 Then.
All **3006 attachments** resolve without orphans: 2509 valid JSON, 494 CSV tables,
two text files and one PDF. Captured byte counts/base64/hashes match and the binary
PDF attachment equals its actual HTTP response bytes. Task 07's additional generated
files are independently observed real local PDFs; download evidence remains Task 08.

Pinned Allure **2.46.1** renders all 99 passed cases. Every identity, title,
description and attachment name/type/bytes matches raw results, and every case,
attachment and entry asset resolves over local static HTTP. These generated-report
checks do not claim browser pixels or deployed ACA/Nginx verification. The audit
snapshot remains frozen to this implementation run; the documentation-only
completion commit receives the normal full CI recheck.
