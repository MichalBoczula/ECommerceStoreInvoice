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
bash scripts/ci.sh source

stage "Restore and build"
bash scripts/ci.sh build

stage "Format"
bash scripts/ci.sh format

stage "Domain"
bash scripts/ci.sh test domain

stage "Application"
bash scripts/ci.sh test application

stage "Infrastructure"
bash scripts/ci.sh test infrastructure

stage "ExternalProviders"
bash scripts/ci.sh test external-providers

stage "Acceptance"
bash scripts/ci.sh test acceptance

stage "OpenAPI"
bash scripts/ci.sh contract

stage "Docker"
docker build -f src/ECommerceStoreInvoice.API/Dockerfile -t ecommerce-store-invoice:verify .
echo 'Local verification passed.'
