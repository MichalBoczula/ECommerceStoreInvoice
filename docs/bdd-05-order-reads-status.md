# INV-BDD-05: complete order reads and status state

The original order-by-ID, client-list and empty-list cases retain their titles,
identities, known POCO quantities/prices/totals and outcomes. Ten original
non-concurrent status cases retain their identities, requested values, terminal
Examples and outcomes. Their responses and stored-state assertions are now
complete. Seven added BDD cases cover Paid/Cancelled multi-line reads, exact
mixed-status client filtering, terminal-to-Created rejection and multi-line
successful transitions.

The inventory is **90 BDD + 27 standalone = 117 acceptance cases**, in the same
38 features with 61 declarations. All 77 original baseline cases and all 83
task-4 cases remain. The same 15 operations, 54/56 guaranteed response pairs
and task-10/task-11 gaps remain. No standalone test is relabelled as BDD.

## Read and transition evidence

| Scope | Complete assertions |
| --- | --- |
| By ID | Exact requested order/client identity, status, CreatedAt/nullable UpdatedAt, total/currency and every nested snapshot/quantity/line total. Created, Paid and Cancelled profiles remain distinct. |
| Client list | Exact returned ID set and complete profiles, including multiple orders with different statuses/line counts/totals. Other-client orders and USD profiles are excluded without imposing result ordering. |
| Empty list | Exactly an empty JSON array while another client owns a paid order, invoice and snapshots; both client carts and all five populated collections remain unchanged. |
| Created to Paid/Cancelled | Complete resulting profile, a generated UTC UpdatedAt inside the measured request window, and exactly one target order changed in only Status and UpdatedAt. Multi-line/inactive-snapshot cases retain every saved field. |
| Rejected status | Exact complete problems for unknown status, empty/missing IDs, Created-to-Created and all six terminal-to-valid-enum pairs. Every document/timestamp in all five populated collections remains exact. |

Each of the 20 strengthened/new cases supplies explicit Gherkin fixture,
request, profile/line or problem, and persistence tables. Expected profile and
line values come from authored fixtures and tables, never from fallback response
values. GUIDs resolve explicit order/client/snapshot keys. Full response DTOs
are compared to independent fixture models; each nested snapshot retains its ID,
ProductId, active/deactivation fields, timestamps, name, brand and complete money.
Quantities and decimal line/order totals are checked independently. Inactive
saved snapshots remain readable and are not reactivated by status changes.

## Populated database and read isolation

Fixtures insert explicit historical orders/snapshots directly into the isolated
MongoDB database. Task 4 separately verifies real checkout through Products and
SQL Server; these are stored-state fixtures, not fabricated setup HTTP calls.
Both clients have populated carts. A paid order owned by the other client has a
referenced client-data version and completed invoice, so every one of the five
collections is nonempty for every case. The invoice's fixture storage URL is a
marker; this package does not claim PDF generation or download verification.

The factory reads complete raw BSON before and after every primary operation,
including both operations in terminal outlines. Reads and rejections preserve
all five complete projections. Accepted status changes are compared to a deep
copy of the entire before state with only the target Status and UpdatedAt
replaced; counts, IDs, lines, snapshots, carts, invoices, references and client
versions must remain exact. Prior terminal timestamps cannot change on rejection.
Generated HTTP timestamps are checked in the measured window; response/storage
comparisons account only for BSON millisecond precision.

Every outcome is followed by complete HTTP reads of each known order, both
client lists and both populated carts, then another complete five-collection
comparison. The `order-state-evidence` tag installs the existing named-HttpClient
request counter and shared Products capture without starting a catalog fixture.
Every primary and supporting read/status request must leave that counter at
zero. No Products adapter, production use case or repository is substituted.

## Problems and shared evidence

Observed problem fields are checked exactly: status/media, title, type, detail,
request-path instance, generated nonempty trace ID and applicable complete error
array. Validation errors require exactly one message/name/entity signature;
not-found problems omit that array. Current public error wording is retained,
including the existing ShoppingCart-labelled empty-ID rule and status-not-found
detail construction. This package documents their observed contract rather than
changing application errors to fit test expectations.

Task 2/4's shared capture keeps actual request/response bytes, counts, hashes,
targets, roles and phases for all primary/supporting exchanges. Additional JSON
with matching service/scenario/instance identities records fixture tables and
resolved profiles/IDs, expected/actual five-collection snapshots, request windows,
complete profiles/problems, persistence expectations and verified outcomes.
Observed state is queued before assertions; verification records follow passed
assertions. Repeated When phases remain separate exchanges.

The existing timing-dependent concurrent status case is preserved. Its loser
can still be terminal 400 or stale-write 409; this package does not relabel it
as deterministic conflict coverage. Task 11 retains that explicit gap and the
full concurrent winning-state/rollback work.

## Verification and scope

`bash scripts/ci.sh source` passes all 22 regressions, the fixed baseline and
90 exact matrix links. `git diff --check` passes. .NET/Docker are unavailable
locally; actual build/format, all five suites, generated OpenAPI, coverage,
security, packaging and image checks run through full CI. The PR records actual
runtime results and an independent bundle/TRX/rendered Allure audit.

Task 5 starts from merged master `c2bade2346d746f92a8fd11bfbe26ff9fda5fd67`,
whose CI 40704424202 passed after Task 03/04 merges. No production source,
package, route/DTO, public error, policy, repository, index, transaction,
coverage/security gate or Azure resource changes. Wider input/framework cases,
deterministic conflicts and contract/RAG work remain their later packages.
Existing successful-master image/archive publication stays intact; selected
LiveDocs refresh and Azure rollout remain the manual final handoff.
