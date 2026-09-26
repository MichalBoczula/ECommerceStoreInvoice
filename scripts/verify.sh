#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
python3 scripts/test-architecture.py
python3 scripts/check-architecture.py
python3 scripts/check-operation-links.py

solution=ECommerceStoreInvoice.slnx
dotnet restore "$solution"
dotnet build "$solution" --configuration Release --no-restore
bash scripts/verify-format.sh

results_dir="$PWD/artifacts/verification"
mkdir -p "$results_dir"
for suite in Domain.UnitTests Application.UnitTests Infrastructure.UnitTests ExternalProviders.IntegrationTests Acceptance.Tests; do
  project="tests/ECommerceStoreInvoice.$suite/ECommerceStoreInvoice.$suite.csproj"
  collection=()
  case "$suite" in
    Domain.UnitTests|Application.UnitTests|Infrastructure.UnitTests)
      collection=(--collect 'XPlat Code Coverage') ;;
  esac
  if [[ "$suite" == Infrastructure.UnitTests ]]; then
    collection+=(--settings tests/ECommerceStoreInvoice.Infrastructure.UnitTests/coverage.runsettings)
  fi
  dotnet test "$project" --configuration Release --no-restore \
    --logger "trx;LogFileName=$suite.trx" --results-directory "$results_dir/$suite" \
    "${collection[@]}"
  python3 scripts/summarize-trx.py "$suite" "$results_dir/$suite"
  case "$suite" in
    Domain.UnitTests)
      bash scripts/report-coverage.sh "$results_dir/$suite" "$results_dir/domain-coverage" ECommerceStoreInvoice.Domain 70 ;;
    Application.UnitTests)
      bash scripts/report-coverage.sh "$results_dir/$suite" "$results_dir/application-coverage" ECommerceStoreInvoice.Application 70 ;;
    Infrastructure.UnitTests)
      python3 scripts/check-infrastructure-coverage.py "$results_dir/$suite"
      bash scripts/report-coverage.sh "$results_dir/$suite" "$results_dir/infrastructure-coverage" ECommerceStoreInvoice.Infrastructure 70 ;;
  esac
done

bash scripts/validate-openapi.sh "$results_dir/openapi.json"
docker build -f src/ECommerceStoreInvoice.API/Dockerfile -t ecommerce-store-invoice:verify .
echo 'Local verification passed.'
