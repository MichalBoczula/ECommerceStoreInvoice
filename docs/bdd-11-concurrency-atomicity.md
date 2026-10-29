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
must pass before marking this package complete. Generated feature C# remains
ignored output; no dependency, CI gate, Azure resource or runtime behavior change.
Master image/Blob publication remains, and selected LiveDocs refresh stays manual.
