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
must pass before this package is marked complete. Parent master is Task 09 merge
bfabbb834bc637de044df8decb0e4b47620bc945. No business rule, persistence, dependency,
Azure configuration or deployment changes. Current optional-null client values
and their OpenAPI mismatch remain explicit Task 13 work. This covers managed HTTP
binding/routing behavior; it does not claim all Kestrel/proxy transport limits or
deployed ingress behavior. Selected LiveDocs refresh remains manual.
