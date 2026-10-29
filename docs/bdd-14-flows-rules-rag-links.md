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
