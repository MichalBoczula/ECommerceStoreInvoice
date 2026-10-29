# INV-BDD-02: scenario identities and shared HTTP evidence

All 38 features / 52 declarations / 77 expanded BDD cases from task 1 remain.
Each declaration now has a readable narrative describing its actual fixture,
request and outcome. Existing titles and behavioral Examples values remain;
obsolete feature-level description tags are replaced by real scenario prose.
Two former When/Then-only scenarios gain an explicit Given that checks the real
plain-text liveness endpoint. This adds read-only setup, not a business write.

The acceptance inventory is **103 cases: 77 BDD and 26 standalone**. The original
eight standalone checks remain; 18 additional recorder regressions exercise the
capture helper without being relabelled as BDD. Domain/Application/Infrastructure/
ExternalProviders membership stays 112/95/73/3. Separate 70% coverage floors and
all security, contract and image gates remain unchanged.

## Identity contract

The task-1 matrix's 77 `INV01_...` IDs are now published as `scenarioId` labels:
48 plain scenarios use explicit Gherkin tags; the four outlines supply their
29 example IDs in a new metadata column. No original behavioral column/value is
removed or replaced. The source gate verifies the published identity against
the exact matrix feature/title/behavioral-example link and requires a narrative.

Each result also has `service: invoices`, an `instanceId` label and a JSON
`Scenario identity` attachment containing its feature, title and ordinal-sorted
example parameters. Identity metadata is excluded from those parameters.
Instance IDs are lowercase SHA-256 of the compact System.Text.Json serialization
of `{ scenarioId, parameters }`, using ordinal-sorted parameter names. Narrative,
title, feature wording, timestamps and random fixture data are not hash inputs.
This starts the explicit identity contract; adapter-generated history IDs can
change when the metadata Examples column is introduced. Retention is checked
against original source identities/behavioral values rather than pretending that
new metadata is an unchanged adapter parameter set.

Every actual client exchange receives `exchangeId: <instanceId>/<sequence>`.
An Interlocked counter is shared by all clients in that scenario, including the
fault-injection host. The sequence identifies observed calls, not a business
branch or a cross-run ordering guarantee for concurrent requests. Immutable
bundle commit/run/hash metadata supplies the provenance for a particular run.

## Capture pipeline and attachments

ScenarioApiContext owns the checked client factory. Its pipeline is:

`OpenApiResponseHandler → HttpEvidenceHandler → TestServer`

The inner handler buffers actual client-visible content before the outer
contract validator. Existing response bytes, content and selected headers remain
available to both validation and caller assertions. Captured events are queued
on the explicit scenario object; they do not require Allure's ambient context
inside server callbacks or concurrent tasks. Hooks publish them after each step
and flush again before cleanup, including when an assertion fails.

For every call, paired `HTTP nnn request` and `HTTP nnn response` JSON attachments
share service/scenario/instance/exchange IDs, actual method/path/query and the
actual Given/When/Then phase. Responses include numeric status and available
Location, Content-Disposition and Cache-Control values. Setup calls, follow-up
reads, competing requests and repeated When phases remain distinct. This task
does not infer a single primary outcome or executed branch from those phases.

Content envelopes distinguish:

| Format | Evidence |
| --- | --- |
| `json` | Parsed JSON value plus decoded text, exact buffered bytes, byte count and SHA-256. JSON literal null remains a JSON value. |
| `invalid-json` | Original text/bytes advertised as JSON that could not parse; it is not relabelled as a parsed payload. |
| `text` | Actual text media, such as health responses, with text/bytes/hash retained. |
| `binary` | Exact base64 bytes, count and hash; no invented decoded text or JSON value. |
| `none` | No HttpContent was supplied; no invented body or bytes/hash. |
| `empty` | HttpContent exists with zero bytes; empty bytes and their hash remain distinct from absence. |
| `unavailable` | Request-content buffering failed; unknown bytes/count/hash stay null and only the exception type is recorded. |

An actual PDF response also gets a downloadable `application/pdf` attachment
whose name contains the same exchange ID. Its bytes must match the binary JSON
envelope and SHA-256. Current successful PDF download BDD uses the existing local
download fixture; this does not establish that a real generated invoice's text
or durable Azure delivery has been fully verified. Those remain later packages.

A transport or buffering failure produces a paired failure event with stage,
exception type and cancellation flags, and is rethrown. It does not invent a
completed response/status or copy private exception messages. The completion
signal used by cancellation regressions fires only after capture finishes.
An outer contract failure retains the actual response already captured inside it.

The hooks own one read-only OpenAPI document for the scenario's clients and
dispose it once during cleanup. Each client no longer disposes a shared document
while another client can still use it. Existing supplementary JSON fixtures and
expected-table attachments remain in place.

## Scope and verification

The only direct BDD client is the deliberate read-only Swagger bootstrap in the
BeforeScenario hook, before the checked pipeline exists. It is excluded from
business exchange evidence. Standalone export/health/protocol/body tests keep
their own scope and do not claim BDD attachments. The source gate rejects direct
or fault-client bypasses elsewhere in feature code.

`bash scripts/ci.sh source` runs 22 source/link regressions: task 1's 18 checks plus
missing description, stale/ambiguous identity and client-bypass mutations. The
18 standalone recorder cases cover JSON/plain/malformed/null/empty bodies,
bodyless/multiple/nested clients, true concurrent pairs, outer validation failure,
transport/request/response-buffering failures, cancellation completion, parameter
isolation/stability and non-UTF8 PDF bytes with headers/downloadable attachment.

Before completing the PR, inspect the full CI TRX and actual bundle: retain all
77 original cases/outcomes and eight original standalone checks, verify 77
identities/narratives, every request/terminal pair and actual phase, every PDF's
bytes/hash, all attachment references and manifest integrity. Compare exported
OpenAPI/flow/policy semantics with task 1. Local .NET/Docker are unavailable;
actual build/format/runtime/image verification uses CI.

The two task-1 coverage gaps stay open. Richer persistence, validation, framework,
contract and flow/RAG assertions remain tasks 3–14. No business-rule, repository,
route, response-schema, package, Azure resource or selected LiveDocs change is
part of this package.

## Mongo runner compatibility

Task 1's PR passed, but master run
[40577968011](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40577968011)
failed twice while starting its shared MongoDB replica set on Ubuntu 24. The
container had stopped before Testcontainers could initialize the replica set;
83 acceptance cases failed from that common setup failure. The logs did not
expose the server's exit reason. A host-compatibility cause is an inference from
the same boundary previously investigated in Users, not an observed application
regression or proof of a specific MongoDB defect.

The two Mongo-dependent jobs temporarily use the Ubuntu 22 runner validated by
Users. Their existing source script now prints the actual Docker-server kernel,
pulls the same Mongo 8 image and checks mongosh before running the full suites.
That CLI preflight is diagnostic, not a substitute for replica-set/write tests.
No tests, startup requirements, thresholds or security gates are skipped. Review
the runner pin when its support or Mongo/kernel compatibility changes.

## INV-BDD-10 observation grouping

Actual HTTP request/response/PDF attachments retain individual exchange IDs and
bytes. Responses also record the actual Allow method set. Named non-HTTP
observations are snapshotted immediately and stored in one Scenario observations
JSON per scenario. Each ordered entry has name, stepPhase and value; repeated
names remain distinct. The outer service/scenarioId/instanceId identifies the
whole collection. AfterStep flushes HTTP; final AfterScenario flushes observations
even if cleanup fails. This preserves every value while respecting the existing
10000-file report limit. Three additional recorder regressions cover grouping,
immutable snapshots/phases and concurrent isolation.
