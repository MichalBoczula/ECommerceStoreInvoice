# INV-BDD-13 — Request/response contracts

Preserve all 315 previous BDD cases, their scenario/instance identities, titles,
descriptions and behavioral Examples, and all 37 standalone Acceptance checks.
Add five BDD cases and 26 standalone contract regressions. Inventory is
**320 BDD + 63 standalone = 383 Acceptance**, **689 tests** across all five suites
(Domain 133, Application 95, Infrastructure 75, External providers 3).
All 77 baseline cases remain; generated feature C# remains ignored output.

## Generated contract and actual serializer

Swashbuckle still generates the only OpenAPI document from routes and DTOs.
Source XML describes every published model and field. Source-owned typed objects
produce complete, correctly typed schema/property examples with web naming.
No second handwritten schema or OpenAPI file is committed. Twenty-two component
schemas and all 97 fields are compared to actual CLR types and System.Text.Json
metadata, including nested arrays, dictionaries and references. All fifteen
operations, required UUID route parameters, body/media declarations and success
and problem representations are checked. Shape contracts describe serialization;
business validation remains separately exercised and documented.

Five client properties (clientName, city, street, buildingNumber, apartmentNumber)
now have nullable request/response annotations matching Task 9's actual accepted
null values. They remain required JSON members. Missing clientName or
apartmentNumber returns the complete safe six-field binding problem without
changing any populated document. Existing explicit-null business cases run through
the stronger response checker. No policy, persistence mapping or runtime binding
option changes. Postal/phone/email business failures remain as before.

Every response DTO field is declared present, including nullable updatedAt and
deactivatedAt and all five invoice fields. The existing clietDataVersionId spelling
is preserved. Generated prices/totals remain JSON numbers with the generator's
double format, while source types and arithmetic remain decimal. No integer-minor-
unit or single-currency guarantee is added. StorageUrl stays plain string text:
normal generated storage URIs have no SAS, but legacy metadata reads do not validate
stored text as an absolute URI.

Request schemas permit unknown members, matching actual web serializer behavior.
An actual cart case verifies case-insensitive root/nested names and ignored unknown
fields; another verifies quoted int32 quantities. Schemas show canonical JSON
numbers and descriptions explicitly record this web-binding compatibility. They
are not a complete grammar of every accepted lexical spelling. Both cases verify
exact ordered stored lines, full responses, update timestamp bounds, every other
field and all unrelated populated collections. The primary response retains UTC
ticks while Mongo persists milliseconds: its timestamp must lie in the request
window and truncate exactly to the stored value; subsequent reads match storage. Complete supporting reads make no
catalog requests. No stricter deserialization options are enabled.

## Safe problems and binary PDFs

Problem writers' complete non-null common fields are required: type, title,
status, detail, instance and traceId. Business validation additionally requires
errors and each error's message/name/entity. Schema filters describe the existing
writer guarantees without changing the writer implementations. The three body
operations describe their 400 as oneOf the seven-field business problem and the
six-field malformed/missing-body problem. Closed problem shapes keep the two
representations distinct; other status declarations remain unchanged.

PDF 200 declares raw string/binary bytes with documented Content-Disposition and
Cache-Control headers and typed string header examples. The response guard checks
the actual PDF signature, route-derived filename/filename*, attachment disposition
and exact private/no-store directives. No JSON/base64 PDF example, fake completed
cancellation status, shared local-file guarantee or live Blob deployment claim is
introduced. Existing real PDF/cancellation/Blob adapter cases remain distinct.

## Guard regressions and evidence

The response guard now checks scalar kinds, required presence, explicit nullability,
UUID/date-time/int32/number formats, nested collections/dictionaries, closed object
members and exactly one applicable problem alternative. It no longer skips null
properties or silently stops at excessive nesting. The helper supports the schema
vocabulary emitted by this service; it is not a general JSON Schema implementation.
The existing byte recorder remains inside validation so actual response bytes
survive a contract rejection.

Thirteen metadata mutations exercise nullability/presence, scalar/nested types,
formats/examples/descriptions, unknown request members, problem fields/alternatives
and PDF binary/header declarations. Twelve simulated HTTP mutations prove the
actual response handler rejects bad UUIDs, null non-nullable fields, string money,
fractional quantities, string booleans, date-only timestamps, missing/extra members,
nested dictionary drift and null problem traceId. A separate positive regression
accepts explicit nullable fields while still rejecting their omission. These
simulated guard regressions remain outside Allure BDD and are not claimed as real
business HTTP exchanges.

The five added BDD cases use all five populated collections and centralized real
HTTP evidence, readable descriptions, stable IDs and authored Gherkin tables.
Four named cases and one separate technical Swagger case join the matrix:
**281 named + 28 framework + 11 technical = 320**. All fifteen operations retain
**59/59** declared status pairs with zero gaps; no new business status is invented.
Source guards retain all 28 regressions.

Parent master is Task 12 merge d20eb900bd02eafbfcc8259d6efb1aa7243f7e55;
its CI 40870688895 passed, including master image publication and Blob archive.
Local .NET 10.0.100 now supports compilation/export and focused contract tests.
Local full solution formatting cannot open Roslyn's named pipe in this workspace;
folder whitespace formatting is available. Docker is unavailable, so complete
container suites and full formatting/image gates require CI. Full CI and fresh
archive/state/rendered-Allure audits must pass before review. Production services,
policies, mappings, dependencies, workflows and security/coverage floors remain
unchanged. Tasks 14–15 stay open; master publication and the selected manual
LiveDocs refresh/deployment remain unchanged.
