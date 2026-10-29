# INV-BDD-10 — Framework failures

## Scope and behavior

Preserve all 202 prior BDD cases, identities/titles/behavioral Examples and all
27 standalone acceptance checks. Add 93 BDD cases and three recorder regressions:
**295 BDD + 30 standalone = 325 acceptance checks**. The original thirteen
controlled-server-failure cases retain their Examples and now use complete
populated state. A fourteenth case covers GetInvoiceByOrderId's declared 500.

The three JSON-body operations each reject malformed, absent, empty, null,
array/string/number/boolean roots, absent required members and invalid UTF-8.
Separate cases exercise wrong member tokens, missing nested cart-line fields,
integer overflow, null line collections/items and otherwise valid objects with
trailing garbage. No client repair or reserialization occurs: ByteArrayContent
sends the declared UTF-8 or explicit hex bytes, and the recorder retains their
actual length/base64/hash. Unsupported text/XML/absent media types use otherwise
valid complete bodies and return 415 before application writes.

The API maps unsupported media to a complete safe 415 rather than 500. Empty
routing 404/405 responses now get complete safe problems with their original
status and exact router Allow set. Required cart collection/item structure is
checked before mapping and returns the existing safe six-field 400 signature.
The three body endpoints declare 415 in generated OpenAPI. See
[ADR-0010](adr/0010-framework-rejections.md). Health/Swagger responses stay outside
this fallback; application/domain problems retain their existing signatures.

Twenty-eight routing cases exercise unknown paths, invalid GUID constraints,
wrong methods across all route groups, and the actual Allow set including the
hidden order-creation alias. Routing failures use the shared byte recorder and
exact framework assertions without claiming a selected OpenAPI operation.
The dedicated [framework matrix](framework-scenarios.tsv) projects these cases as
frameworkCases, separate from named operation/status coverage. All scenario
links and identities are still checked; framework outcomes cannot hide a gap.

Every framework/server problem has exactly type/title/status/detail/instance/
traceId, its complete safe signature, correct media/cache headers, no PDF
filename/location, and the exact Allow set. Internal fault markers, parser
exceptions/positions and stacks cannot leak. Controlled 500s inject an application
service or descriptor fault before writes; they do not claim real Mongo failure
or transaction rollback. The original CreateOrder fault retains its legacy route.

## Populated state and evidence

Each of the 106 affected cases seeds four interleaved client versions, three
saved orders and snapshots, two populated carts and two completed invoices with
historical client references. Complete raw before/after Mongo documents and
ordered lines must remain identical. Nine complete supporting GETs inspect both
carts/latest client versions, all three snapshot-backed orders and both invoices.
The actual named Products client counter must remain zero.

To keep complete reports within the existing 10000-file LiveDocs limit, named
setup/state/expectation observations are snapshotted at RecordEvidence time and
collected in one identified Scenario observations JSON. Every entry retains its
name, phase and complete typed value, including repeated names. Finalization
flushes observations even after a failed step or cleanup failure. Actual HTTP
request/response attachments and binary PDFs remain individual records and keep
their existing identity/sequence. HTTP responses now also capture the real Allow
header. No exchange filtering or schema/security/coverage gate changes.

Three recorder regressions verify repeated observation names and separate HTTP
pairs/Allow, immutable snapshots and phase boundaries, and concurrent scenario
isolation. These are standalone acceptance checks, outside Allure BDD counts.

## Verification and limits

Source checks preserve all 77 baseline cases, all 202 previous matrix rows and all
15 named operations. The matrix contains 267 named cases plus 28 framework-only
cases. Guaranteed business status intent is 58/59: the by-order 500 gap is closed,
three declared 415 responses have actual cases, and deterministic stale status
409 remains INV-BDD-11. Twenty-five source/link regressions check stale/duplicate
framework links and prohibit routing status intent from inflating business
coverage. Generated feature C# remains ignored build output.

Local .NET/Docker are unavailable. Full CI build/format/OpenAPI, all runtime
suites, package/security/image checks and independent archive/state/Allure audits
passed in the frozen verification below. Parent master is Task 09 merge
bfabbb834bc637de044df8decb0e4b47620bc945. No business rule, persistence, dependency,
Azure configuration or deployment changes. Current optional-null client values
and their OpenAPI mismatch remain explicit Task 13 work. This covers managed HTTP
binding/routing behavior; it does not claim all Kestrel/proxy transport limits or
deployed ingress behavior. Selected LiveDocs refresh remains manual.


## Frozen implementation verification

Full [CI run 40793465941](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40793465941)
passed for implementation head `4a4dca68bb73e9fb8ffb98ca8867095f44aa5131`.
GitHub tested merge `91abd2c0122030eb888f34c556f292b06bad5c4c` has parents
Task 09 master and that head, and the same implementation tree
`d43d3b6875f1dc758b75c991fca36a62af159d7c`. Build, formatting, generated OpenAPI,
source/link checks, audited restores, secret/dependency checks, packaging, quality
gate and Docker build/image scan all passed. PR image publication and Blob archive
were correctly skipped; master publication and selected LiveDocs refresh keep
their existing workflow and manual handoff.

| Suite | Passed | Failed/skipped | Line coverage |
| --- | ---: | ---: | ---: |
| Domain | 133 | 0 | 89.5% |
| Application | 95 | 0 | 86.3% |
| Infrastructure | 73 | 0 | 93.2% |
| External providers | 3 | 0 | n/a |
| Acceptance | 325 | 0 | n/a |
| Total | 629 | 0 | n/a |

All prior TRX members remain present. Acceptance adds exactly 93 BDD cases and
three recorder checks. The 295 BDD cases span 40 features and 91 scenario
declarations. All 77 original baseline cases and all 202 previous matrix rows
remain present; the 25 source/link regressions pass. Coverage retains the separate
70% Domain/Application/Infrastructure floors and existing generated-code handling.

The official Actions artifact digests were checked before extraction. The inner
`documentation-bundle.zip` is **9111861 bytes**, SHA-256
`2ef39291aa5c80b6cb2e54da76500584652a21f5b02a690cf827cdedf91f0926`.
Its manifest identifies the tested merge and workflow above; every file length/hash
and the exact file set were independently verified: **8685 manifest entries**
within the unchanged 10000-file cap.

All 295 results passed with stable service/scenario/instance identities and readable
descriptions. All **8051 attachments** were referenced and verified: **5955 JSON,
2089 CSV tables and seven binary PDFs**. The audit paired all **2663 actual HTTP
exchanges** (2658 API, three checkout catalog, two catalog fixture), checked exact
body byte lengths/base64/hashes, phases (11 Given, 315 When, 2337 Then) and matching
request/response identities. All prior task-specific state/response/PDF audits pass.
All 4425 previous named observation occurrences survive grouping across all 202
prior cases, including duplicates. Exactly 283 cases have one identified observation
JSON, linked through the Allure AfterScenario fixture; the original step tables and
individual HTTP/PDF attachments remain visible alongside it.

The focused audit checks every one of the 106 affected primary exchanges:
55 body 400s, nine media 415s, fifteen route 404s, thirteen method 405s and fourteen
controlled 500s. Exact raw request bytes, safe six-field signatures, media/cache
headers, router Allow sets and absence of private markers are verified. All five
populated Mongo collections remain exactly equal before/after every primary and
its nine healthy supporting reads: **954 complete supporting GETs**, zero Products
calls. Controlled service faults do not claim real database rollback.

Pinned Allure **2.46.1** generated all 295 cases. Each rendered case's identity,
title, description, outcome and every attachment name/type/byte hash, including
AfterScenario observations, matched the raw results. Every case JSON, attachment
and entry-page asset was served and read successfully through local static HTTP.
This is artifact/report integrity verification, not a claim of deployed Azure
browser or ingress validation.

Generated flows and validation policies are unchanged from Task 09. OpenAPI differs
only by the three safe 415 declarations. All fifteen operation identities/metadata
and all 202 previous named links are retained; 65 named cases and 28 separately
classified routing cases are added. Guaranteed named status intent is 58/59 with
only deterministic UpdateOrderStatus 409 deferred to Task 11. Task 13's optional-null
contract follow-up remains explicit. No later package is marked complete.

This section intentionally freezes the verified implementation rather than pointing
to a documentation commit that contains its own run identity. The final documentation
head is checked again by full CI and recorded on the PR, with equivalent generated
exports, BDD identities/descriptions/outcomes and all 629 TRX members.
