# INV-BDD-11 — Concurrency and atomicity

Preserve all 295 prior BDD cases, their identities/titles/behavioral Examples and
all 30 standalone acceptance checks. Add eight BDD cases: two deterministic status
winners, three real checkout rollback stages, two checkout winners and one expired
invoice attempt. Inventory: **303 BDD + 30 standalone = 333 Acceptance**; all five
suites total **637** tests. The original status race is strengthened without
replacing its identity, setup table or conservative optional-status matrix row.
The report audit also found and repairs an inherited duplicate-cart case missing
a Gherkin table: it now checks its declared request table and identified JSON
contract, retaining its identity, title and original behavior.

## Real writes and deterministic order

Test-only scoped decorators invoke the existing repositories and transaction.
No fake write returns success, no sleep chooses a winner, and no service/domain
rule changes. Gates have finite timeouts and finally blocks release/drain requests.
Distinct scoped attempt labels are captured from actual X-Bdd-Attempt headers in
canonical HTTP pairs. Each observed stage records its scope/session identity,
transaction activity, exception type, session-visible documents and an independent
snapshot of all five collections. Values use the existing immutable observation
JSON; Gherkin tables and HTTP/PDF attachments remain individual evidence.

Both status requests read Created and pass domain validation before either real
conditional replacement. Either Paid or Cancelled can win. Its complete response,
updated timestamp, every saved field, snapshots, clients and historical invoice
references are verified. The stale writer must throw the real
OrderWriteConflictException and return the exact safe six-field 409; it cannot
change the winning database. Complete supporting reads preserve that state and
make no catalog call. The original status race now uses the same coordination.

Both checkout requests load the two-product cart and each fetch the actual
ProductsCatalog once. Distinct Mongo sessions stage two snapshots plus one order
before either cart write. Outside readers still see the entire original database.
The selected transaction clears the cart and commits; the loser's actual Mongo
cart write conflict triggers the application's rollback. Checkout retains its
existing safe 500 for this unhandled Mongo error; status 409 is a different cause.
Only the complete winning snapshots/order/cleared cart survive. Exact product
IDs, quantities, names/brands, prices, currency, totals, dates and every unrelated
document are checked. Supporting reads make no catalog calls; a later empty-cart
checkout returns the complete existing 400 and adds no catalog fetch or writes.

Three separate cases fail after the real snapshot, order or cart write succeeds.
Session reads prove the staged state at each reached step, while independent reads
prove it is still invisible. An injected post-write IOException triggers real
Mongo abort. All five populated collections equal the complete original state
before retry; no commit occurred, the session is cleared and a fresh HTTP checkout
succeeds. These are actual transaction rollback paths, unlike Task 10's pre-write
controlled service exceptions. Native replica-set behavior does not prove deployed
Azure DocumentDB transaction semantics; deployment verification remains separate.

## Invoice ownership and attempt cleanup

Retain Task 7's deterministic active-claim 409, exact completed winner and physical
failed-attempt cleanup/retry cases. The added scenario holds a real claim before
PDF generation and explicitly expires only its lease as a documented fixture
change. A second HTTP request reclaims the same invoice with a new attempt and
completes a real PDF. The resumed old request writes its own PDF, but its actual
conditional completion rejects the stale claim; its release changes no field.
All five collections remain the complete winner's state.

Production intentionally preserves the old PDF after an attempted ambiguous
completion. The scenario then explicitly invokes the real storage cleanup for
that old invoice/attempt identity: only the old file disappears. Winning metadata,
created timestamp/latest client reference and exact downloaded PDF bytes/hash,
filename and cache headers remain intact. This demonstrates attempt fencing, not
an automated orphan sweeper or shared/durable local storage. Existing Blob adapter
checks remain distinct. No invoice lease/recovery policy changes.

## Coverage and verification

The matrix retains every prior row and adds eight named cases. All fifteen named
operations and **59/59 declared status pairs** have guaranteed scenario intent;
the final UpdateOrderStatus 409 gap is removed. Twenty-eight routing cases remain
separate and cannot inflate operation coverage. Source regressions still reject
conditional-only 409 evidence and obsolete status gaps. Task 13's optional-null
contract follow-up stays open; no later package is marked complete.

Parent master is Task 10 merge a007d166088970cc35c13d82d1b3f8c255bec033.
Local .NET/Docker are unavailable. Full CI build/format/generated OpenAPI, all five
suites, security/package/image gates and independent archive/state/Allure audits
must pass before the PR is ready for review or merge. Generated feature C# remains
ignored output; no dependency, CI gate, Azure resource or runtime behavior change.
Master image/Blob publication remains, and selected LiveDocs refresh stays manual.


## Frozen implementation evidence

[CI run 40850282469](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40850282469)
verified implementation head `6da235bbb9f9433462c76844fca2f8a62741ecc0`.
The packaged tested merge `bcde1da91ebcbac7ce640900d2f066781a07b0dd`
has parents Task 10 master and that head, with the exact implementation tree
`40b71071285cc61d47af52eb6335b4a46b609273`. Build, formatting, generated
OpenAPI, source/link checks, audited restores, secret/dependency checks, all five
runtime suites, packaging and the quality gate passed. The documentation head
is verified by full CI again, including the Docker build/image scan, and its exact
head/run/archive is recorded on the PR before ready-for-review. This section is a
frozen runtime/package reference and does not claim an overall final-head CI
conclusion in a self-referential documentation commit.

| Suite | Passed | Failed/skipped | Line coverage |
| --- | ---: | ---: | ---: |
| Domain | 133 | 0 | 89.5% |
| Application | 95 | 0 | 86.3% |
| Infrastructure | 73 | 0 | 93.2% |
| External providers | 3 | 0 | n/a |
| Acceptance | 333 | 0 | n/a |
| Total | 637 | 0 | n/a |

All 629 previous TRX members remain, with exactly eight new BDD cases and no
additional standalone cases. All 295 previous scenario/instance identities,
titles and behavioral Examples remain; only the strengthened original status
race description changes. Inventory: **42 feature files, 95 declarations,
303 BDD + 30 standalone Acceptance**. Source checks pass 25 regressions and all
303 links: 275 named operation cases plus 28 separate routing cases. All fifteen
named operations and all 59 declared status pairs have guaranteed scenario intent.
Coverage retains each separate 70% floor and existing generated-code exclusions.

Every official Actions ZIP digest was verified before extraction. The inner
`documentation-bundle.zip` is **9650578 bytes**, SHA-256
`1ebcbeb78b2295cc33055f66d08ada45d9d0bf9f2f20c1b8c2a3c41344d8bc50`.
Its manifest identifies the tested merge/run above; its exact file set and every
length/hash pass the independent audit: **9006 manifest entries**, within the
unchanged 10000-file cap. The outer Actions artifact digest is
`023a225f6e05966d12d5be45a2b4d4c323eb673769b3774d1d233ef79a0a3201`.

All **303** actual passed results have readable descriptions and explicit
service/scenario/instance identities. Every case has identified JSON and at least
one rendered Gherkin CSV table, including the repaired inherited duplicate-cart
case. All **8354 referenced attachments** exist with no orphan attachment:
**6215 JSON, 2131 CSV and eight binary PDFs**. The 293 scenario observation
collections retain all prior named occurrences and duplicates through correctly
linked AfterScenario fixtures. Every new instance ID is independently reproduced
from its stable identity and sorted authored parameters.

The audit joins all **2784 real paired HTTP exchanges**: 2768 API, fourteen
checkout catalog and two catalog fixture exchanges. Exact body lengths/base64/
SHA-256, request/response identities, actual attempt headers and phases are checked:
12 Given, 346 When and 2426 Then. All eight captured PDF attachments match the
HTTP response bytes and parse as nonempty PDFs.

Independent state checks cover both new status winners and the strengthened
original race, all three rollback/retry stages and both checkout winners. Session
identities/activity, reached write stages, complete session-visible collections,
external invisibility, exact winning state and absence of losing snapshots/orders
are checked. They reconstruct full API profiles, quantities, prices, totals and
historical references directly from BSON and verify all **85 complete supporting
GETs**. Real catalog HTTP requests supply the two exact product IDs and expected
names/brands/prices; reads and the later empty-cart rejection add no catalog fetch.
All 409/500/empty-cart 400 signatures are checked independently.

The invoice audit verifies both real claim attempts, the completed replacement,
its retained invoice creation timestamp/latest client reference, rejected stale
completion, no-op old release and complete unchanged winning collections. Both
real PDF attempts have distinct attempt paths. Production's ambiguous old PDF is
preserved until the explicit old-attempt cleanup removes only that file; the exact
winning PDF and both metadata routes remain intact. This is local file behavior
and explicit cleanup, not automated orphan recovery or deployed Blob validation.

Pinned Allure **2.46.1**, checked against its official release SHA-256, generates
all 303 cases. Every rendered identity/title/description/outcome and attachment
name/type/byte hash matches the raw results, including AfterScenario JSON.
Every case JSON, attachment and entry-page asset passes local static HTTP.
This verifies source/archive/report integrity; it does not claim deployed Azure
browser or database behavior. OpenAPI, flows and validation policies are unchanged
from Task 10; every prior operation/link record is retained, with eight named cases
added and the final stale-write gap removed. `git diff --check` passes.

The final documentation head must pass full CI and the same fresh-artifact audit
before the PR becomes ready. PRs do not publish images or Blob archives. Master
publication and the manual selected LiveDocs refresh/deployment remain unchanged.
Tasks 12–15 stay open, including Task 13's nullable-client contract follow-up.
