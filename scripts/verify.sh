#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
python3 scripts/test-architecture.py
python3 scripts/check-architecture.py
python3 scripts/check-operation-links.py

solution=ECommerceStoreInvoice.slnx
dotnet restore "$solution"
dotnet build "$solution" --configuration Release --no-restore
dotnet format "$solution" --verify-no-changes --no-restore

results_dir="$PWD/artifacts/verification"
mkdir -p "$results_dir"
for suite in Domain.UnitTests Application.UnitTests Infrastructure.UnitTests ExternalProviders.IntegrationTests Acceptance.Tests; do
  project="tests/ECommerceStoreInvoice.$suite/ECommerceStoreInvoice.$suite.csproj"
  dotnet test "$project" --configuration Release --no-restore \
    --logger "trx;LogFileName=$suite.trx" --results-directory "$results_dir/$suite"
done

bash scripts/validate-openapi.sh "$results_dir/openapi.json"
docker build -f src/ECommerceStoreInvoice.API/Dockerfile -t ecommerce-store-invoice:verify .
echo 'Local verification passed.'
