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

## Frozen implementation evidence

[CI run 40877013276](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40877013276)
passed for implementation head `d20be263a8d9e494548e8859d67d09760cc78f72`.
The packaged tested merge `88b42a77492922c48ac11029a24d991ee5efe955`
has Task 12 master and that head as parents and the exact implementation tree
`37ff95b5223038605001f584ca5a9d7c31b91c06`. Build, full formatting,
generated OpenAPI, source/link regressions, audited restores, secret/dependency
checks, all five suites, package/quality and Docker build/image scan passed.
The completion-documentation head requires full CI and the same fresh-artifact
checks before review; its exact head/run/archive are recorded on the PR. This is
a frozen runtime/package reference rather than a self-referential final CI claim.

| Suite | Passed | Failed/skipped | Line coverage |
| --- | ---: | ---: | ---: |
| Domain | 133 | 0 | 89.5% |
| Application | 95 | 0 | 86.3% |
| Infrastructure | 75 | 0 | 94.4% |
| External providers | 3 | 0 | n/a |
| Acceptance | 383 | 0 | n/a |
| Total | 689 | 0 | n/a |

All **658 previous TRX members** remain. Exactly five new BDD cases and 26
standalone guard regressions are added; the standalone count remains explicit.
All **315** prior scenario/instance identities, titles, descriptions, behavioral
Examples and named observation occurrences (including duplicates) remain.
Inventory: **44 feature files, 105 declarations, 320 BDD + 63 standalone**.
The 28 source regressions and all 320 matrix links pass, retaining fifteen named
operations and 59/59 declared status pairs with zero gaps and unchanged floors.

Every official Actions ZIP digest passes before safe extraction. The inner
`documentation-bundle.zip` is **10370054 bytes**, SHA-256
`b3e6b7444639adae638dac44eb9bb973167b6db6095f8aa3c3588aa3349f97ea`.
The outer Actions artifact SHA-256 is
`04684109094729b6ed7418577626c934ba4c17bf713ff2bba2b5a12b87809898`.
Its manifest records the tested merge/run above. Every file length/hash and the
exact file set pass: **9575 entries**, below the unchanged 10000-file cap.

All 320 passed BDD results have readable descriptions, explicit service/scenario/
instance labels, identified JSON and at least one Gherkin CSV table. All **8887
referenced attachments** exist without orphans: **6657 JSON, 2216 CSV, four text
DocStrings and ten binary PDFs**. All 312 observation groups join their correct
AfterScenario fixture. New instance IDs are independently reproduced from stable
IDs and sorted authored parameters. All ten PDFs match successful HTTP bytes/hashes
and parse as nonempty PDFs.

The independent audit joins **2987 actual HTTP request/outcome pairs**: 2946 API,
sixteen technical API, nine peer API, fourteen checkout catalog and two catalog
fixture exchanges. Phases are twelve Given, 394 When and 2581 Then. Exact byte
lengths/base64/SHA-256, identities, request attempts and phases pass. Two retained
cancellation failures have no fabricated completed status.

An independent **jsonschema 4.23.0** check converts only OpenAPI 3.0 nullable
semantics and verifies all 22 complete typed examples, all 97 field examples and
**2942 complete real JSON responses**, including **289 safe problems**, against
the packaged generated contract. Health text, Swagger source, binary streams and
bodyless routing responses are checked in their separate evidence paths. Flow and
policy exports remain byte-equivalent to Task 12; all previous link records remain,
with exactly four named and one separate technical case added.

Independent new-case checks reconstruct the full five-collection expected state
from BSON and verify all **45 supporting reads**. They check required-nullable
omissions and exact safe 400/no-write state, actual uppercase/unknown and quoted
quantity requests, exact ordered cart replacements, UTC response timestamp bounds
and its exact stored millisecond value, complete responses and every unrelated
field/reference. Existing Task 11 race/rollback/checkout/claim and Task 12 probe,
outage, cancellation, host-isolation and cleanup audits also pass.

Pinned **Allure 2.46.1**, checked against its official release SHA-256, renders all
320 cases. Every rendered identity/title/description/outcome and attachment name/
type/byte hash matches raw results, including AfterScenario JSON. Every case JSON,
attachment and entry-page asset passes local static HTTP. This verifies generated
report/archive integrity; it does not claim deployed Azure or browser rendering.

Local Release solution build, all 27 focused contract/export tests, Domain 133,
Application 95, External providers 3, Redocly 2.53.3 lint, source checks and
`git diff --check` pass. Folder whitespace formatting passes; CI verifies the full
formatter because local Roslyn named pipes are unavailable. Local publish confirms
all three required XML files travel with the API. Runtime container behavior and
image scanning are verified by CI. No dependency/workflow/security/coverage floor
changes. The final documentation head must pass full CI and the same fresh checks
before the PR is ready; PR publication/archive remain skipped. Tasks 14–15 stay open
and the selected LiveDocs refresh/deployment remains manual.
