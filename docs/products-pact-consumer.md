# PACT/10 Invoice consumer contract for Products

Invoice calls Products through the production Kiota client and
`ExternalProductServiceClient` at checkout. The contract covers
`POST /mobile-phones/by-ids` through that adapter and the real snapshot mapper.
It uses the registry identities `invoice` and `products`.

| Interaction | Consumer expectation |
| --- | --- |
| Every distinct requested ID belongs to an active phone | HTTP 200 returns the priced phone with its ID, name, brand and exact price/currency. Invoice deduplicates repeated IDs and maps the returned snapshot. |
| One requested phone exists and the other is missing | HTTP 404 for the whole lookup maps to an empty snapshot collection. |

Both interactions use the same provider state with synthetic IDs and exact
priced fixture values.
Extra fields such as photos and specifications are outside this consumer
contract. Empty IDs and Invoice's handling of missing checkout snapshots remain
covered by Invoice's own tests.

## Generate and publish

The required ExternalProviders job uses PactNet 5.0.1 over loopback TCP.
Two independent verifications must produce byte-identical Pact 4.0 output.
It uploads `invoice-products-pact` with the original JSON and SHA-256 manifest.
To generate the same files locally from the repository root:

```bash
INVOICE_PACT_DIR="$PWD/artifacts/verification/external-providers/pacts" bash scripts/ci.sh test external-providers
```

On a successful `master` build, `publish-invoice-products-pact` uses the
existing `PACT_REGISTRY_URL` repository variable and Invoice-scoped
`PACT_PUBLISHER_TOKEN` secret. It registers the directed integration and binds
the original bytes to the exact Invoice commit. The publication receipt is
`invoice-products-publication`.

## Products provider handoff

Use the artifact from the intended successful Invoice `master` build and
verify its exact manifest hash. Seed the priced phone in isolated Products SQL
state and ensure the missing fixture ID stays absent. Replay both interactions
against the real Products API. Provider evidence uses a separate credential
scoped to `products`. Consumer publication alone records an unverified contract.
