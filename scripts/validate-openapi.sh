#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
output="${1:-artifacts/verification/openapi.json}"
mkdir -p "$(dirname "$output")"
output="$(cd "$(dirname "$output")" && pwd)/$(basename "$output")"
rm -f "$output"

OPENAPI_EXPORT_PATH="$output" dotnet test \
  tests/ECommerceStoreInvoice.Acceptance.Tests/ECommerceStoreInvoice.Acceptance.Tests.csproj \
  --configuration Release \
  --filter 'FullyQualifiedName~OpenApiExportTests'

test -s "$output"
npx --yes @redocly/cli@2.53.3 lint "$output" --extends=spec

python3 scripts/check-operation-links.py
python3 scripts/check-openapi-contract.py "$output"
