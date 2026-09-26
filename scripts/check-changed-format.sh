#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
base="${1:?Provide the pull request base or previous push commit SHA}"
git cat-file -e "$base^{commit}"

mapfile -d '' changed < <(git diff --name-only -z --diff-filter=ACMRT "$base" HEAD -- 'src/**/*.cs' 'tests/**/*.cs')
if (( ${#changed[@]} == 0 )); then
  echo 'No C# source changed; formatting check is not needed.'
  exit 0
fi

dotnet format ECommerceStoreInvoice.slnx --verify-no-changes --no-restore --include "${changed[@]}"
