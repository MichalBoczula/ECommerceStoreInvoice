#!/usr/bin/env bash
set -Eeuo pipefail

cd "$(dirname "$0")/.."
stage_name="Setup"
trap 'status=$?; echo "Verification stage $stage_name failed (exit $status)." >&2' ERR
stage() {
  stage_name="$1"
  echo "==> $stage_name"
}

results_dir="$PWD/artifacts/verification"
mkdir -p "$results_dir"
rm -rf "$results_dir/domain" "$results_dir/application" "$results_dir/infrastructure" \
  "$results_dir/external-providers" "$results_dir/acceptance" \
  "$results_dir/domain-coverage" "$results_dir/application-coverage" \
  "$results_dir/infrastructure-coverage"
rm -f "$results_dir/summary.md" "$results_dir/openapi.json"

stage "Source checks"
python3 scripts/test-architecture.py
python3 scripts/check-architecture.py
python3 scripts/check-operation-links.py

stage "Restore and build"
solution=ECommerceStoreInvoice.slnx
dotnet restore "$solution"
dotnet build "$solution" --configuration Release --no-restore

stage "Format"
bash scripts/verify-format.sh

run_suite() {
  local name="$1" project="$2"
  shift 2
  dotnet test "$project" --configuration Release --no-restore \
    --logger "trx;LogFileName=$name.trx" --results-directory "$results_dir/$name" "$@"
  python3 scripts/summarize-trx.py "$name" "$results_dir/$name" "$results_dir/summary.md"
}

stage "Domain"
run_suite domain tests/ECommerceStoreInvoice.Domain.UnitTests/ECommerceStoreInvoice.Domain.UnitTests.csproj \
  --collect 'XPlat Code Coverage'
bash scripts/report-coverage.sh "$results_dir/domain" "$results_dir/domain-coverage" \
  ECommerceStoreInvoice.Domain 70 "$results_dir/summary.md"

stage "Application"
run_suite application tests/ECommerceStoreInvoice.Application.UnitTests/ECommerceStoreInvoice.Application.UnitTests.csproj \
  --collect 'XPlat Code Coverage'
bash scripts/report-coverage.sh "$results_dir/application" "$results_dir/application-coverage" \
  ECommerceStoreInvoice.Application 70 "$results_dir/summary.md"

stage "Infrastructure"
run_suite infrastructure tests/ECommerceStoreInvoice.Infrastructure.UnitTests/ECommerceStoreInvoice.Infrastructure.UnitTests.csproj \
  --collect 'XPlat Code Coverage' \
  --settings tests/ECommerceStoreInvoice.Infrastructure.UnitTests/coverage.runsettings
python3 scripts/check-infrastructure-coverage.py "$results_dir/infrastructure"
bash scripts/report-coverage.sh "$results_dir/infrastructure" "$results_dir/infrastructure-coverage" \
  ECommerceStoreInvoice.Infrastructure 70 "$results_dir/summary.md"

stage "ExternalProviders"
run_suite external-providers \
  tests/ECommerceStoreInvoice.ExternalProviders.IntegrationTests/ECommerceStoreInvoice.ExternalProviders.IntegrationTests.csproj

stage "Acceptance"
run_suite acceptance tests/ECommerceStoreInvoice.Acceptance.Tests/ECommerceStoreInvoice.Acceptance.Tests.csproj

stage "OpenAPI"
bash scripts/validate-openapi.sh "$results_dir/openapi.json"

stage "Docker"
docker build -f src/ECommerceStoreInvoice.API/Dockerfile -t ecommerce-store-invoice:verify .
echo 'Local verification passed.'
