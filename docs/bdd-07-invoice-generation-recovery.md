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
Reads/rejections preserve all populated state; successful generation may change
only the correct invoice reservation. No Products API request is permitted.

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
