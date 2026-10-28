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
by-ID setup uses a genuinely rendered PDF instead and also downloads that real
artifact through the API after both metadata lookups. Successful downloads must
match every declared byte and SHA-256, `application/pdf` without a charset,
attachment filename/filename-star, `Cache-Control: private, no-store`, and exact
Content-Length when the streaming response provides it. The primary request is
repeated and must produce the same complete outcome. Shared capture preserves
actual bytes/base64/hash plus a binary PDF attachment, rather than decoding PDFs
as text. Complete error responses have no PDF disposition and retain the existing
no-cache/no-store middleware policy. Cache directives are compared as an exact
set because HTTP client parsing normalizes their order.

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


## Verified implementation evidence

Implementation `9da89ffd99d82429b831a7fa68e072ab1f5debbf` passes full
[CI 40767337541](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40767337541).
Tested merge `576ef065850361b4f0dd22e510a4eb26eae0dc08` exactly matches submitted
tree `13af46feceb50481c667b545f3bd3f9f86162848`. Release build, formatting,
audited restore, generated OpenAPI, dependency/secret checks, packaging, quality
gate, Docker build and high/critical image scan pass.

| Suite | Passed, zero failures/skips | Line coverage |
| --- | ---: | ---: |
| Domain | 112 | 89.5% |
| Application | 95 | 86.3% |
| Infrastructure | 73 | 93.2% |
| ExternalProviders | 3 | — |
| Acceptance | 133 | — |

All **416 tests** pass with unchanged separate 70% coverage floors. Actual TRX
preserves all 126 previous acceptance members and adds exactly seven BDD cases.
All 99 previous BDD scenario/instance identities and titles remain. The 38 feature
files now contain 75 declarations and 106 expanded cases. All 22 source regressions
and 106 exact matrix links pass with the same two later-package gaps. Task 07's
merged master CI 40763054167 also passed, including image/archive publication.

Independently downloaded outer Actions ZIPs match their official digests. The
inner bundle is **3963813 bytes**, SHA-256
`b81722267b1461cf34c79a1003479c59cc1e3155f5caf2cced77178768ade1b4`.
All **4426 manifest entries** have verified hashes/sizes and closed inventory;
all 38 features byte-match reviewed source. OpenAPI, flow/policy exports, all
15 original operation profiles and 99 prior case links stay unchanged. The
operation projection adds only seven case links.

The independent audit joins all **23 Task 08 cases** and **23 primary requests**
(seven successes, 16 rejections) to raw Mongo data, explicit table values, complete
HTTP metadata/problems, all five unchanged populated collections and unchanged
storage inventories. All **116 supporting GETs** check both metadata routes,
repeat the complete primary outcome and download the genuine rendered setup PDF.
One actual setup POST creates that invoice from the explicit paid-order inputs.

All **15 storage-reader calls** match their declared URL/outcome and mode. All
metadata, incomplete, missing-invoice and invalid-ID paths avoid storage reads.
There are three private-Blob HTTP cases. Two actual SDK authentication 403 results
map to generic API 500 problems; two deliberately simulated local-reader faults
remain separately identified. Missing local/Blob artifacts return the exact PDF
404 while metadata remains available. The three legacy cases keep all generation
fields absent. Both clients' historical references and every artifact's bytes,
write timestamps, content type and Blob ETag remain exact; Products calls stay zero.
Earlier Task 05/06/07 state and response audits also pass unchanged.

All 106 passed BDD results contain **631 paired captured HTTP exchanges**:
626 API, three checkout-catalog and two fixture probes; phases are 13 Given /
126 When / 492 Then. Direct SDK fixture writes/inventories are separately labelled
storage observations and are not counted as captured application HTTP exchanges.
All **4172 attachments** resolve without orphans: 3491 valid JSON, 672 CSV tables,
seven PDFs and two text files. Captured byte counts/base64/hashes match.

The seven binary attachments are byte-identical to their actual HTTP responses
and declared local/Blob artifacts, including the real Chromium output. Each PDF
parses successfully with at least one page. The six static responses preserve the
non-UTF8 binary marker. Download MIME, filename/filename-star and exact private/
no-store directive set match; present lengths equal body bytes. Error cache
control is the existing no-cache/no-store set, with no PDF disposition.

Pinned Allure **2.46.1** renders all 106 passed cases. Every identity, title,
description and attachment name/type/bytes matches raw results. Every rendered
case, attachment and entry asset resolves over local static HTTP. These checks
do not claim browser pixels or deployed ACA/Nginx/Azure permissions. This audit
snapshot stays frozen to the implementation run; the documentation-only
completion commit receives the normal full CI recheck.
