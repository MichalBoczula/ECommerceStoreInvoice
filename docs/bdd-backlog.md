# Orders/Invoices BDD documentation backlog

Implement one scoped package at a time. Preserve Minimal APIs, application
services/flow descriptors, Domain policies and MongoDB. Checkout atomically saves
snapshots/order/cleared cart; repeat Paid is 400 and an actual stale write is 409.
Invoice claims, attempt-specific PDF cleanup and ambiguous-completion recovery
remain distinct. Do not introduce Users-style history collections or silently
change business rules to match another service's examples.

`[x]` records an implemented and verified package; `[ ]` remains. Full CI is
required before merging. Existing master image/archive publication stays in
place, while application deployment and selected LiveDocs refresh remain manual.

- [x] **INV-BDD-01 — Baseline and coverage guard.** Preserve 38 features / 52
  declarations / 77 expanded cases and eight standalone checks. Validate all
  matrix examples and response pairs, record two existing gaps honestly, untrack
  generated code and pin the Products test image. See [baseline](bdd-01-baseline.md).
- [x] **INV-BDD-02 — Shared BDD evidence.** Readable scenario descriptions,
  stable scenario/instance/exchange IDs and centralized truthful capture for all
  main, setup, failure, concurrent and supporting clients, including binary PDFs.
  See [HTTP evidence and identity contract](bdd-02-http-evidence.md).
- [x] **INV-BDD-03 — Shopping carts.** Complete create/get/update profiles,
  exact persisted lines/timestamps, empty contents and other-client preservation.
  Implementation and verification: [shopping carts](bdd-03-shopping-carts.md).
- [ ] **INV-BDD-04 — Checkout and product snapshots.** Multiple products,
  complete prices/quantities/totals, one catalog fetch, immutable snapshots and
  successful coordinated writes/cleared cart evidence.
  Implementation and verification: [checkout and snapshots](bdd-04-checkout-snapshots.md).
- [ ] **INV-BDD-05 — Order reads and statuses.** Complete get/list/isolation
  responses and stored state for valid and rejected transitions; preserve the
  distinction between terminal rejection and a stale-write conflict.
- [ ] **INV-BDD-06 — Client data versions.** Multiple immutable versions,
  latest selection/client isolation and preservation of existing invoice references.
- [ ] **INV-BDD-07 — Invoice generation and recovery.** Complete ownership,
  paid-order/client-version checks, reservations, duplicates, retries, hidden
  incomplete states and lost-acknowledgment evidence.
- [ ] **INV-BDD-08 — Invoice reads and PDFs.** Complete by-ID/by-order metadata,
  legacy/completed-only behavior, binary download evidence/headers and truthful
  missing/storage failure state. Keep real Blob adapter checks distinct from local files.
- [ ] **INV-BDD-09 — Business validation.** Isolated invalid values and accepted
  boundaries, exact safe errors, unchanged populated state and honest classification
  of HTTP-reachable versus standalone policies/rules.
- [ ] **INV-BDD-10 — Framework failures.** Raw malformed/missing/null/wrong-type
  requests, routing/method/media failures and complete safe problems without writes.
  Add the missing GetInvoiceByOrderId server-error case.
- [ ] **INV-BDD-11 — Concurrency and atomicity.** Deterministic stale-status 409,
  competing checkout/claim winners, exact full state, actual rollback and attempt
  cleanup. Keep injected pre-write failure and real transactions distinct.
- [ ] **INV-BDD-12 — Technical scenarios.** Health/readiness/startup/index
  failures, cancellation, host/database isolation and documentation behavior.
  Retain appropriate lifecycle facts outside BDD and report their counts separately.
- [ ] **INV-BDD-13 — Request/response contracts.** Scalar types, required fields,
  nullability/formats/nesting, complete safe error shapes, typed descriptions/examples
  and PDF contracts derived from source, without a second handwritten OpenAPI.
- [ ] **INV-BDD-14 — Flows, rules and RAG links.** Stable identities and authored
  conditions/effects/failures, operation-to-scenario/exchange joins, observed versus
  declared rules and reachable versus standalone documentation. No runtime tracing
  or implemented retrieval service is implied.
- [ ] **INV-BDD-15 — Final report audit.** Full CI, baseline retention, every
  rendered case/attachment, contract/state checks, immutable archive identity and
  the manual LiveDocs image-selection/deployment handoff.
