# BFF registration cart Pact provider verification

Invoice CI replays the [pinned BFF contract](../contracts/pact/bff-invoice.json)
against the Invoice API and a disposable MongoDB replica set. The
[source pin](../contracts/pact/bff-source.json) records the exact BFF master
commit, Actions artifact and SHA-256 checksums.

| Interaction | Expected outcome |
| --- | --- |
| Look up an existing customer's cart | HTTP 200 with the matching client ID, a cart ID and the saved product ID and quantity. |
| Look up a customer without a cart | HTTP 404 with Problem Details. |
| Create a cart for a customer | HTTP 200 with a new cart ID, the matching client ID and empty lines. |
| Create a cart for a customer who already has one | HTTP 409 with Problem Details. |

Provider states prepare the specific customer cart and clear it after each
interaction. Run the verifier locally with Docker, uv and .NET 10.

```bash
dotnet build src/ECommerceStoreInvoice.API/ECommerceStoreInvoice.API.csproj --configuration Release
bash scripts/pact/run.sh bff
```

The required Pact provider job uploads `bff-invoice-verification`. On an
Invoice `master` push, the publisher checks the exact BFF publication and
registry artifact before posting the result. The
`bff-invoice-verification-receipt` artifact identifies the registry record.
