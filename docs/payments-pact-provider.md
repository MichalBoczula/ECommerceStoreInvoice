# Payments provider verification (PACT/09 and PACT/10)

Invoice CI verifies the Payments order-read and fulfillment contract with Pact
Python 4.2.0. The required `pact-provider` job starts the
candidate Invoice API on loopback and uses a disposable MongoDB replica set.
The state handler creates order and product snapshots from each interaction's
parameters. It checks PLN, JPY, KWD and USD Created orders plus a missing order.

The consumer-owned file in `contracts/pact/` is copied verbatim from the
[Payments CI artifact](https://github.com/MichalBoczula/ECommerceStorePayments/actions/runs/42487924388/artifacts/13620266576).
The adjacent source record pins its originating commit, workflow run, archive
hash and contract hash. A local copy keeps provider PR checks independent of
GitHub artifact expiry and registry availability. To change the pin, download
the consumer artifact from the intended successful Payments main CI run, verify
its manifest hash, and review the file and source record together.

The ten interactions cover five original order reads plus the paid order read,
Created-to-Paid transition, completed and missing invoice lookups, and invoice
creation. The creation interaction uses the real PDF renderer and a temporary
local store. Each replay seeds its own provider states in the disposable replica
set and removes the database and local files at the end.

## Run the check

Run with .NET 10, Docker, uv and PowerShell.

```bash
dotnet build src/ECommerceStoreInvoice.API/ECommerceStoreInvoice.API.csproj --configuration Release
pwsh src/ECommerceStoreInvoice.API/bin/Release/net10.0/playwright.ps1 install --with-deps chromium --only-shell
bash scripts/pact/run.sh
```

CI retains the official verifier JSON, the candidate log and the normalized
result. A negative control forwards requests to the real Invoice API and
removes `totalAmount` from one PLN response. The verifier must report exactly
that mismatch, then pass again against the original response. The negative
control is CI evidence. Registry publication uses the unmodified provider result.

## Enable hosted evidence

1. In both GitHub repositories, set `PACT_REGISTRY_URL` to the deployed Pact API
   HTTPS origin. Set `PACT_PUBLISHER_TOKEN` as a secret in each repository, using
   the Payments-scoped credential in Payments and the Invoice-scoped credential
   in Invoice.
2. From Payments `main`, manually run **Publish Payments Pact** with
   `source_run_id` from the source record to publish the pin manually. The workflow
   verifies the source run and publishes its bytes under its exact consumer SHA.
   Later successful Payments main CI runs publish automatically when configured.
3. Merge the Invoice provider change after the bootstrap. A protected Invoice
   `master` push publishes its exact provider commit, verification and receipt.
   The publisher selects the pinned consumer publication and checks the registry's
   bytes before attaching evidence. Verify the receipt links the intended commit
   pair and the `payments-invoice-order-read` check with ten verified interactions.

PRs always run the provider check without registry credentials. A missing
registry URL skips publication, while an incorrect configured credential or
an unavailable pinned publication fails the publishing job.
