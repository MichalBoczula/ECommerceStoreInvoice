# INV-BDD-14 — Flows, rules and RAG links

Preserve all 320 prior BDD cases, their identities/titles/descriptions/behavioral
Examples and 63 standalone Acceptance checks. Add one populated source-documentation
BDD case and six standalone join regressions: **321 BDD + 69 standalone = 390
Acceptance**, **696 tests** across all five suites. All 77 original cases remain.

## Authored source knowledge

Documentation attributes give explicit stable IDs and readable summaries to all
sixteen application flow descriptors, eight installed policies and seventeen
installed rule classes. All 128 steps retain their original order/BPMN identity;
their qualified IDs include the flow identity because BPMN names repeat across
flows. StepName now displays authored prose rather than a method identifier.
Source projection retains both the method and readable description. The existing
Application ordering assertion checks stable BPMN IDs. Method bodies, business
rules, persistence and transaction/claim behavior do not change.

Thirteen flows are direct named HTTP entries. Two other descriptors are published
without a direct HTTP entry (single-product create/read). The internal bulk-product
flow is neither published nor a direct HTTP entry; its ID-collection validation
runs in the service immediately outside the descriptor and is explicitly linked.
GuidCollectionValidationPolicy is registered/published but internal-service-only.
Being installed in a policy does not mean every rule can be reached over HTTP:
null guards can be preempted by binding or non-null domain construction.

The status parser and internal bulk empty-catalog guard have their own two inline
rule identities, separate from installed policy classes. The shared GUID rule
retains the entity supplied by its installing policy, including ShoppingCart for
several other route groups. Response error names identify observed rules; route
names do not override their entity or imply a different policy.

Sixty-five authored outcome families cover all 59 named operation/status pairs and
six routing/health/Swagger/cancellation families. Each has a condition, effects and
failure limits. A family deliberately represents a status category with multiple
possible causes. The existing case cause, response and state evidence distinguish
what a scenario demonstrates. It is not an executed branch identity. In particular,
HTTP 500 never implies universal rollback: ambiguous invoice completion can retain
a completed invoice and PDF, while active checkout transactions normally abort.

The existing operation-link generator projects the source graph into the same
operation-links.json. IDs, flow/step order/prose, installed memberships, route/status
families and reachability are validated. The export gate compares actual flow and
policy responses to that source projection. OpenAPI/wire schema and policy JSON
are unchanged; only flow StepName display prose changes. No second maintained
schema, retrieval service, agent, ingestion job or Azure resource is added.

## Actual exchange evidence

The central recorder snapshots its immutable actual HTTP request/response/failure
records. Before cleanup, one source-link observation is added to the existing
scenario JSON group. This avoids a new attachment per exchange and retains every
old observation and individual HTTP/PDF attachment under the existing archive cap.
The metadata points to canonical exchange IDs and logical request/outcome attachment
names; the archive audit resolves those through actual Allure attachment contents.

Each exchange retains service/instance/phase/attempt/role and its actual status or
cancellation failure. A nominal operation is matched against the generated routes
with UUID constraints; the old checkout alias is labelled explicitly. Each nominal
operation links to declared flow/step/policy/rule IDs and an outcome family. Returned
validation error names are mapped to source rule IDs and must belong to that flow.
Successful requests do not prove every declared rule was executed. Conflicts have
state context, not fabricated validation errors. Executed steps/branches are never
claimed. Products exchanges remain external dependencies; peer reads keep their
separate role and cannot supply primary-host scenario witnesses.

Every guaranteed named/routing/technical outcome needs an actual eligible When or
Then witness, with When preferred and then sequence order. Given/Setup cannot
satisfy a missing scenario outcome. Multiple concurrent or recovery candidates
remain visible; retries and follow-ups are not collapsed into one invented request.
Optional statuses are separate from guaranteed obligations. Cancelled PDF cases
have a null-status witness and retain nominal source context without inventing 499
or a completed PDF response. Authored rule expectations in Examples are checked
against actual response errors when present.

Six standalone regressions check catalogue identity/reachability, setup-versus-
outcome selection, missing witnesses, orphan records, unrelated error rules and
truthful cancellation. Seven source mutations reject missing IDs/prose, duplicate
qualified step IDs, missing rule metadata, wrong status, empty effects and unknown
installed rules. Existing 28 source regressions remain; no gate is removed.

## Verification boundaries

The new BDD case compares source identities/prose and published payloads over all
five populated collections, verifies honest internal-only boundaries and performs
nine complete supporting reads. Every BDD result must retain identified JSON and
Gherkin tables. Inventory is **282 named + 28 framework + 11 technical = 321**;
all fifteen operations retain **59/59** declared status pairs with zero gaps.

Parent master is Task 13 merge 0ff3889e996ac257f19b5fa152aff916bab62de8.
Local .NET compilation/focused tests, source checks and an offline replay of the
actual previous 320-case/2987-exchange archive are available; replay is metadata
validation of historical bytes, not a new live business test. Local full formatter
needs unavailable Roslyn named-pipe permissions; folder whitespace formatting is
available. Docker-backed suites, full formatting and image scan run in CI. Full
CI and fresh source/state/exchange/archive/rendered-Allure audits must pass before
review. Existing separate coverage, security, dependencies and workflows remain.
Task 15 and the selected manual LiveDocs refresh/deployment remain open.

## Frozen implementation evidence

[CI run 40890855992](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40890855992)
verified implementation head `9897b477e0fcd962c3a7e256934b1b7116cd1811`.
The packaged tested merge `6ecd66a568301307f5e9741be466d695c7d9eee4`
has Task 13 master and that head as parents, with exact implementation tree
`44cb853b970cde1194f13ddc92f744bc3a578992`. Build/full formatting,
generated OpenAPI and actual source-export comparisons, audited restore, secret/
dependency checks, all five suites and package/quality gates passed. The completion
documentation head requires full CI, including image build/scan, and the same
fresh-artifact checks before review. Its exact head/run/archive are recorded on the
PR rather than claimed self-referentially in this frozen runtime/package record.

| Suite | Passed | Failed/skipped | Line coverage |
| --- | ---: | ---: | ---: |
| Domain | 133 | 0 | 89.2% |
| Application | 95 | 0 | 85.8% |
| Infrastructure | 75 | 0 | 94.4% |
| External providers | 3 | 0 | n/a |
| Acceptance | 390 | 0 | n/a |
| Total | 696 | 0 | n/a |

All **689 previous TRX members** remain, with exactly one new BDD case and six
standalone join regressions. All **320** prior BDD scenario/instance identities,
titles, descriptions, behavioral Examples and named observation occurrences
(including duplicates) remain. Inventory is **45 feature files, 106 declarations,
321 BDD + 69 standalone**. The source stage passes the existing 28 plus seven new
mutation checks; all 321 links retain fifteen operations and 59/59 declared status
pairs with zero gaps. Each separate 70% coverage floor is unchanged. Metadata
constructors/attributes add measured lines; no coverage exclusion is introduced.

Every official Actions ZIP digest passes before safe extraction. The inner
`documentation-bundle.zip` is **10926063 bytes**, SHA-256
`3ed4396702d7e781dd65a448068e217893a9162ef07e94ff98cef13d18599ee7`.
The outer Actions artifact SHA-256 is
`7822cb400b42b07a22bfc67224c737a413d17f589ef92296b6095986a1467828`.
The manifest records the exact tested merge/run. Every length/hash and exact path
passes: **9616 entries**, below the unchanged 10000-file cap.

Every passed BDD result has a readable description, explicit service/scenario/
instance labels, identified JSON and at least one Gherkin CSV table. All **8925
referenced attachments** exist without orphans: **6689 JSON, 2222 CSV, four text
DocStrings and ten binary PDFs**. All 321 cases have correctly joined observation
groups, including the eight legacy cases that previously needed no grouped state
observation. New instance identity hashing is independently reproduced. Each PDF
matches successful HTTP bytes/hash and parses as a nonempty PDF.

Independent graph checks verify **236 globally unique authored IDs**: sixteen
flows, 128 qualified steps, eight policies, seventeen installed rules, two inline
rules and 65 outcome families. All graph edges, publication classifications,
policy/rule membership and actual runtime flow order/BPMN/prose match their source.
The internal-only ID-collection policy remains distinct from HTTP reachability.
OpenAPI is unchanged; validation-policy export bytes are identical to Task 13.
All prior operation/case fields remain, with source context added and one case linked.
Executable bodies of all original descriptor/policy/rule files are unchanged after
removing authored attributes and formatting; FlowDescriberBase intentionally selects
step display prose, and its ordering regression checks preserved BPMN identities.

All **2998 actual request/outcome pairs** resolve to exactly one source join and
to the correct logical Allure request/response-or-failure attachment. The audit
checks IDs, phases, target/role, actual query and concurrent attempt labels, UUID-
constrained nominal routes, legacy aliases, declared flow/step/policy/rule refs,
outcome-family status and actual response-error rule IDs. It verifies **339 actual
When/Then guaranteed-outcome witnesses** with exact selection priority and **91
observed rule references**, without inventing executed steps or branches. There are
408 eligible scenario-outcome candidates, 2564 supporting links, ten setup links
and sixteen external-dependency links; candidates remain visible rather than
being collapsed into one hypothetical request.

Raw request roles remain 2957 API, sixteen technical API, nine peer API, fourteen
checkout catalog and two catalog fixture. Phases remain twelve Given, 396 When and
2590 Then. The two retained cancellation failures have null-status witnesses and
no fabricated 499. New-case BSON reconstruction verifies all five unchanged
populated collections and nine complete supporting reads. All existing Task 11–13
state, race, rollback, claim, probe, cancellation, isolation and contract audits pass.

Independent **jsonschema 4.23.0** checks pass all 22 complete typed examples, all
97 field examples and **2953 actual JSON responses**, including **289 safe
problems**. Pinned **Allure 2.46.1**, verified against its official release digest,
renders all 321 cases. Every rendered identity/title/description/outcome and
attachment name/type/byte hash matches raw results, including AfterScenario JSON.
Every case, attachment and entry-page asset passes local static HTTP. These checks
do not claim deployed Azure/browser behavior or RAG retrieval execution.

Local Release solution build, 33 focused contract/export/join tests, Domain 133,
Application 95, External providers 3, 35 source checks, Redocly 2.53.3 lint and
`git diff --check` pass. The historical actual-archive replay independently passed
all 320 previous cases / 2987 request-outcome pairs through the compiled joiner;
that is historical evidence validation, separate from the fresh live CI suite.
Local full formatter/Docker restrictions remain explicit; CI verifies those gates.

The final documentation head must pass full CI and the same fresh source/state/
exchange/archive/rendered-report checks before the PR is ready. PR publication and
Blob archive remain skipped; existing master publication and the selected manual
LiveDocs refresh/deployment stay unchanged. Task 15 remains open.
