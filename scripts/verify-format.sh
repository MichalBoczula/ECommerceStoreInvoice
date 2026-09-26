#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
mapfile -d '' generated_features < <(find tests -type f -name '*.feature.cs' -print0)
dotnet format ECommerceStoreInvoice.slnx --verify-no-changes --no-restore --exclude "${generated_features[@]}"
