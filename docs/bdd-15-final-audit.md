# INV-BDD-15 — Final audit and LiveDocs handoff

Status: final producer, source, archive and rendered-report audit complete,
2026-10-29. Tasks 01–14 are implemented and verified. Task 15 delivers this frozen
audit and the exact manual refresh handoff. LiveDocs manifest selection, its new
image publication and the Azure rollout remain manual and are not claimed here.

This record selects one successful master archive. A later documentation-only
commit does not change its identity or select another bundle automatically.

## Frozen producer and publication

| Evidence | Identity |
| --- | --- |
| Invoice source | `924b86388f4c3b7e3c7475972d80d6007975c82a`, merged [PR #233](https://github.com/MichalBoczula/ECommerceStoreInvoice/pull/233) |
| Source tree | `b50a89103f06ab96dbb27c7739a6db95609aa25d` |
| Master CI | [40899913192, attempt 1](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40899913192) |
| Generated at | `2026-10-29T15:23:03Z` |
| Inner bundle SHA-256 | `3e6fd918f7f01272a73bdba17fb93c3261ced002650f3b143a9db3aea8234fa8` |
| Inner bundle size | 10,927,950 bytes |
| Outer Actions ZIP SHA-256 | `0c5c7036279412caf364dd0aa4f9630ada7b34dc4b80ca38364218b4484c7b3f` |
| Bundle Actions artifact | [12909108156](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40899913192/artifacts/12909108156) |
| Archive receipt artifact | [12910755146](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40899913192/artifacts/12910755146) |
| Published invoice API image | `mb0101/ecommerce-store-invoice-api@sha256:7379782fae06d1a13733344823425d4080b6e60d39d50edeba72689fbc90d81c` |

The image above belongs to the invoice API. The manual refresh needs a separately
built **LiveDocs image**. Both invoice image tags identify the same scanned image;
master publication and Blob archival passed. PR Dependency Review is correctly
skipped on master and passed on the Task 14 PR. All other required master jobs,
audited restores, format, source/contract checks, tests, coverage and image scan
passed. No workflow, dependency, security exclusion or coverage floor changed.

The master source tree exactly matches the reviewed Task 14 final tree. Its four
exports, all 321 BDD identities/titles/descriptions/outcomes and all 696 TRX members
match the independently audited final Task 14 PR evidence. The master receipt's
source/run/time/hash/size match this independently downloaded bundle. Successful
archive-job evidence is distinct from an unauthenticated public Blob download;
the archive remains private.

## Runtime, source and evidence inventory

| Suite | Passed; zero failures/skips | Line coverage |
| --- | ---: | ---: |
| Domain | 133 | 89.2% |
| Application | 95 | 85.8% |
| Infrastructure | 75 | 94.4% |
| External providers | 3 | — |
| Acceptance | 390 | — |
| Total | 696 | — |

Acceptance is **321 BDD cases plus 69 standalone checks**. Standalone recorder,
contract, lifecycle and join tests are not Gherkin scenarios. Domain, Application
and Infrastructure keep separate 70% floors; only the existing generated Kiota
client is excluded from Infrastructure measurement. Native MongoDB, catalog/SQL,
PDF and container/image verification ran in CI. Docker is unavailable locally;
Roslyn named-pipe permissions prevent full local formatting. Full CI formatting
passed, and the source gate passed locally with all 35 link/source regressions.
The Mongo suites ran on Docker kernel `6.8.0-1070-azure`. The original Acceptance
TRX from [baseline master run 40214951632](https://github.com/MichalBoczula/ECommerceStoreInvoice/actions/runs/40214951632)
was independently downloaded and its official artifact hash verified; all eight
original standalone member identities still execute and pass in this master run.

| Requirement | Verified result |
| --- | --- |
| Original baseline | All 77 original behavioral cases survive the frozen source guard; all 38 original feature inputs and eight standalone Acceptance checks remain. Generated feature C# stays ignored and untracked. |
| Current source | 45 feature files, 106 declarations and 321 expanded cases. Every feature byte matches reviewed source. All results have readable descriptions, resolved Given/When/Then, stable service/scenario/instance IDs and actual CSV Gherkin tables. |
| Coverage links | Fifteen named operations; 282 operation cases, 28 framework cases and eleven technical cases. All 59/59 declared operation/status pairs have guaranteed scenario intent; no documented gaps remain. |
| Source graph | Sixteen flows, 128 qualified steps, eight policies, seventeen installed rule classes, two inline guards and 65 outcome families; 236 unique authored IDs. Thirteen flows are directly HTTP-linked, two are published-only and one is internal/unpublished. |
| Actual joins | 2,998 paired HTTP exchanges, including two cancelled PDF requests without a fabricated response/status. All canonical joins resolve actual attachment IDs; 339 guaranteed actual When/Then witnesses and 91 observed error-rule references pass. Setup, peers, catalog and supporting traffic retain their actual roles. |
| Raw attachments | 8,925 referenced attachments: 6,689 JSON, 2,222 CSV tables, four text DocStrings and ten binary PDFs. Every JSON-labelled attachment parses; all references resolve with no orphans. Each case has one identified grouped observation JSON. |
| Archive integrity | Every one of 9,616 manifest entries has the exact recorded path, byte count and SHA-256, with closed inventory and safe ZIP paths. The unchanged 10,000-file cap remains enforced. Outer Actions ZIP digests also match GitHub artifact metadata. |
| Contracts | Generated OpenAPI remains the sole schema. Independent jsonschema 4.23.0 checks all 22 complete examples, 97 field examples/descriptions and 2,953 real JSON responses, including 289 complete safe problems. Nullability/member presence, scalar formats and nested types remain distinct. |
| PDFs | All ten binary attachments parse as real PDFs and exactly match response lengths/hashes/bytes. Metadata, filename/media/cache headers, completed-only reads and cancellation outcomes remain truthful. |

The outcome/status inventory proves the declared scenario coverage. It does not
prove every invalid-value combination, all possible exceptions or executed
internal branches. Nominal flow/policy links and authored conditions/effects are
separate from observed error rules. Every join leaves executed steps empty and
does not claim branch execution. RAG ingestion/retrieval/evaluation remains future
work.

## Complete state and behavior audit

All previous scenario and test identities remain. Runtime assertions cover the
complete cart, snapshot, order, client-version, invoice/recovery and technical
contracts introduced by Tasks 03–14. Independent raw evidence checks additionally
reconstruct complete supporting response profiles from persisted BSON and compare
all five collections, including ordered lines, timestamps and historical references.

- Seven original complete cart profiles retain exact contents, identity/date
  rules, empty-cart behavior and other-client preservation. Checkout retains
  multi-product quantities, immutable prices/totals, actual catalog calls and
  coordinated snapshot/order/cleared-cart state.
- Order reads/statuses preserve complete snapshots, client filtering, empty
  results, Created-to-Paid/Cancelled rules, terminal 400 rejection and stale-write
  409. Client histories retain immutable versions, latest selection, client
  isolation and invoices' older version references.
- Invoice generation retains paid-order ownership, unique reservations, hidden
  incomplete attempts, retries and ambiguous completion acknowledgments. Invoice
  reads preserve complete legacy/completed metadata, exact storage inventories
  and missing/storage errors. All 23 read scenarios preserve primary/supporting
  database and storage state.
- All 99 affected business cases retain 59 exact rejections and 40 accepted
  boundaries, complete populated fixture state and exact expected effects.
  All 106 framework/server-failure cases preserve their populated state and
  complete safe problems. Malformed/empty/absent/null request bytes remain actual
  evidence rather than repaired JSON.
- Independent full BSON comparisons verify all 230 business-family fixtures,
  unchanged supporting reads, order/client fixtures and 1,998 complete
  supporting GET profiles reconstructed from BSON. Separate deterministic status
  races, two competing checkout winners and three real transaction rollback/retry
  stages retain full winning/losing state and actual session/catalog evidence.
- Real stale invoice claims/completions, rejected old completion and no-op release
  preserve the winner and its PDF. Attempt-specific explicit cleanup remains
  distinct from an automatic orphan-cleanup service. Health/outage/recovery,
  startup/index failures, both cancelled reads, host/database isolation and all
  Task 13 populated contract-state checks remain verified.

Native MongoDB tests do not certify deployed Azure DocumentDB transaction behavior.
Controlled pre-write faults remain separate from actual transaction rollback.
Instance-local file storage is not shared Blob storage. Hosted authorization,
private storage permissions, real ACA recovery and mixed-currency domain decisions
remain their existing operational concerns; this audit changes none of them.

## Generated report verification

Generated the master results with pinned Allure **2.46.1**, verifying the official
distribution SHA-256
`37428164f310c8d137cfdd2c3a25d049b233261ddab13e1bd4dfd7627b5cbf8d`.
The summary contains 321 passed cases and zero failed/broken/skipped/unknown.
Every rendered identity/title/description/outcome, ordered/nested step name/status
and attachment name/type/byte hash matches the raw source. Every case renders CSV
tables. All case/attachment resources and entry assets resolve through local HTTP;
JSON resources parse.

This is exhaustive report-data, attachment and local static-HTTP verification.
It does not claim pixel-by-pixel browser inspection, a new production Nginx image
or a live Azure check. Those image/hosting checks run during the manual LiveDocs
refresh.

## Exact manual LiveDocs refresh handoff

Inspected LiveDocs main `0ca91fc8be1231f6b6f8ad17c41226e3bc2bf6f2`.
Its development-v1 manifest still selects invoices source
`43af2ef1720b3b66b0a5f7392d5321b4de5b7d87`, run `40214951632`.
Uploading a new bundle or rebuilding that unchanged selection does not select this
report. LiveDocs serves the documentation baked into its image.

Replace only the `invoices` reference in LiveDocs `manifests/portal.json` with the
successful master receipt's exact reference below. Retain Products, Users,
Payments, existing version state, all retained versions and the latest pointer.

```json
{
  "project": "invoices",
  "commitSha": "924b86388f4c3b7e3c7475972d80d6007975c82a",
  "generatedAt": "2026-10-29T15:23:03Z",
  "workflowRunId": 40899913192,
  "artifact": {
    "url": "https://stecproductsdevmb261013.blob.core.windows.net/livedocs/reports/invoices/924b86388f4c3b7e3c7475972d80d6007975c82a/3e6fd918f7f01272a73bdba17fb93c3261ced002650f3b143a9db3aea8234fa8.zip",
    "sha256": "3e6fd918f7f01272a73bdba17fb93c3261ced002650f3b143a9db3aea8234fa8",
    "sizeBytes": 10927950
  }
}
```

1. Verify the selected receipt/source/run/hash/size above, then run the normal
   LiveDocs manifest PR checks: private archive prefetch outside Docker, complete
   package validation, actual Allure generation, production-container HTTP and
   image scan. The manifest contains no SAS; existing secrets stay in CI.
2. After review/merge, let LiveDocs main CI publish its tested image. Record its
   commit, workflow run and immutable digest from `livedocs-image/image.json`.
3. In the shared `ECommerceStore.Terraform` root, update only `app_images.livedocs`
   to that **LiveDocs digest**, preserve other images, review a fresh Terraform
   plan and apply manually. No new resources or runtime Blob credentials are needed.
4. After rollout, compare `/build-info.json` with the selected LiveDocs image
   commit. Verify `/livedoc/v1/invoices/bundle.json` has the exact producer
   source/run/time/hash inventory, and
   `/livedoc/v1/invoices/allure/widgets/summary.json` has 321 passed cases.
   Open `/livedoc/v1/invoices/` and its Allure report for browser review; verify
   representative JSON/table/PDF attachments and the retained project routes.

Record the new LiveDocs commit/run/digest and rollout checks when performed.
This handoff leaves those manual actions explicit; no LiveDocs manifest, image
selection or Terraform deployment is changed by the invoice audit PR.
