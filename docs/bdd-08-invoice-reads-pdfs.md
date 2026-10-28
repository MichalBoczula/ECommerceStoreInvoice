# INV-BDD-08 — Invoice reads and PDFs

## Scope and preserved behavior

Sixteen existing by-ID, by-order and PDF cases retain their identities, titles,
original Examples and HTTP outcomes. Seven new cases cover legacy/incomplete
by-ID reads, legacy PDF download and private Blob success/missing/authentication
failures. Inventory: **106 BDD + 27 standalone = 133 acceptance checks**. All
77 baseline cases and subsequent Task 03–07 additions remain.

Explicit Gherkin tables define complete client histories, two paid orders and
saved product lines, two populated carts, invoice state/artifact contracts,
complete safe problem signatures, requests, responses and unchanged-state rules.
An unrelated completed invoice references another client's older version. All
five Mongo collections stay populated in every case. Historical records are
labelled raw fixture setup. The original successful by-ID case still generates
its target invoice and real Chromium PDF through an actual setup POST; its order
and client inputs are explicit stored fixtures. Real checkout remains Task 04.

Metadata requires exactly all five typed string fields, correct invoice/order/
client-version IDs, storage URL and UTC timestamp. Public `clietDataVersionId`
spelling is preserved. Both lookup routes hide Generating/Failed records even
when their PDFs exist. Legacy records omit all generation fields and reads must
leave those fields absent. Historical references remain fixed despite newer
versions for both clients. Missing/empty identifiers return the complete existing
safe signature, including the ShoppingCart-labelled empty-ID validation rule.

## Binary PDFs and read-only storage

The local/private-Blob fixtures are valid one-page PDFs with a deliberately
non-UTF8 binary header marker, complete object offsets and cross-reference table.
They are explicit binary fixtures, not claims of invoice rendering. The original
by-ID setup uses a genuinely rendered PDF instead. Successful downloads must
match every declared byte and SHA-256, `application/pdf` without a charset,
attachment filename/filename-star, `Cache-Control: private, no-store`, and exact
Content-Length when the streaming response provides it. The primary request is
repeated and must produce the same complete outcome. Shared capture preserves
actual bytes/base64/hash plus a binary PDF attachment, rather than decoding PDFs
as text. Complete error responses have no PDF disposition or cache headers.

Raw snapshots compare all five collections before/after every primary and
supporting phase. Storage inventories independently compare all owned artifacts'
existence, byte length, SHA-256, write timestamp, content type and Blob ETag where
applicable. Every existing/missing object and unrelated file must remain exact.
Both invoice metadata routes are checked for all stored records and missing target
identities, then the primary request is repeated and all state checked again.

A scenario-local observer forwards the actual PDF reader without consuming,
replacing or buffering its returned stream. It records the selected URL, found/
missing result, exception type and Azure exception status. Invalid/incomplete/
missing invoices and all metadata operations must make zero storage-reader calls.
Complete PDF records make one call per download. The original simulated reader
failure remains explicitly distinguished from a real Blob adapter failure.
All operations must attempt zero Products requests using the real named-client
counter/capture; no Products/SQL containers or substitute catalog are needed here.

## Private Blob adapter boundary

Three new HTTP cases start owned Azurite 3.35.0 containers with private containers
and runtime-generated fixture credentials. The host uses the existing production
Blob registration, Azure SDK and Blob adapter. The fixture uploads binary objects
with `application/pdf` and verifies private access through SDK container policy.
A successful read streams exact bytes through the API. A valid absent attempt
object returns the exact PDF 404. Authentication failure uploads with correct
fixture credentials but configures the API with another valid-shaped test key:
the real adapter receives 403 and the API returns only its generic safe 500.

SDK fixture writes/inventory reads are labelled direct storage observations,
separate from captured application HTTP; credentials are never attachments.
Local cases explicitly clear ambient Blob settings. The observer does not replace
the Blob adapter or synthesize its failure. Existing Infrastructure Azurite tests
for adapter replacement, private anonymous access, immutable uploads and targeted
cleanup stay separate and required by full CI. These emulator checks do not claim
production Azure permissions or deployed multi-replica verification.

## Verification and limits

`bash scripts/ci.sh source` passes all 22 regressions, the fixed baseline and all
106 exact matrix links across 15 operations. Guaranteed status coverage remains
54/56 with the same two Task 10/11 gaps. `git diff --check` passes. Local .NET and
Docker are unavailable; build, formatting, runtime suites and image checks use CI.

Task 07 PR 226 was already merged when this package started. The base is master
`bd9d6a677d3977b2dd9940d954dd866cfc26b58a`. No production/API/error, dependency,
repository/index/transaction, CI/security/coverage gate or Azure configuration
changes. All local files/Azurite containers belong to their scenario and are
cleaned after evidence capture. Existing successful-master publication remains;
selected LiveDocs refresh and deployment remain manual.
