# Web cart Pact provider verification

Invoice CI runs the [pinned Web contract](../contracts/pact/web-invoice.json)
against the Invoice candidate and a disposable MongoDB replica set. The contract
exercises `GET /shopping-carts/client/{clientId}` with a populated cart (HTTP 200)
and a client without a cart (HTTP 404). The provider state seeds two cart lines
and clears the fixture after each interaction.

The [source record](../contracts/pact/web-source.json) pins the Web commit, workflow
artifact and SHA-256. The verifier checks the exact bytes before replay. The Web
workflow published those bytes to the registry before Invoice pinned them.
[Web consumer artifact](https://github.com/MichalBoczula/ecommerce-store-web/actions/runs/43066814124/artifacts/13874909852)
and [publication receipt](https://github.com/MichalBoczula/ecommerce-store-web/actions/runs/43066814124/artifacts/13875864206).

With Docker, uv and the .NET SDK available, run from the repository root.

```bash
dotnet build src/ECommerceStoreInvoice.API/ECommerceStoreInvoice.API.csproj --configuration Release
bash scripts/pact/run.sh web
```

The required `pact-provider` job verifies both Web and Payments and uploads Web
results as `web-invoice-verification`. On an Invoice `master` push with the
registry configured, CI finds the exact Web publication, compares its bytes with
the verified digest and publishes the result using the existing Invoice publisher.
The `web-invoice-verification-receipt` artifact identifies the registry record.
