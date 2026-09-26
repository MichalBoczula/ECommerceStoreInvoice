#!/usr/bin/env bash
set -euo pipefail

results_dir="${1:?Provide the test results directory}"
output_dir="${2:?Provide the coverage output directory}"
assembly="${3:?Provide the target assembly}"
minimum="${4:-}"
summary_file="${5:-}"

tool_dir="${REPORTGENERATOR_TOOL_DIR:-artifacts/verification/tools}"
if [[ -z "$(find "$results_dir" -type f -name coverage.cobertura.xml -print -quit)" ]]; then
  echo "Missing $assembly Cobertura coverage input in $results_dir" >&2
  exit 1
fi
mkdir -p "$tool_dir"
if [[ ! -x "$tool_dir/reportgenerator" ]]; then
  dotnet tool install dotnet-reportgenerator-globaltool --tool-path "$tool_dir" --version 5.4.7
fi

"$tool_dir/reportgenerator" \
  -reports:"$results_dir/**/coverage.cobertura.xml" \
  -targetdir:"$output_dir" \
  '-reporttypes:Html;TextSummary' \
  -assemblyfilters:"+$assembly"

summary="$output_dir/Summary.txt"
test -s "$summary" || { echo "Missing $assembly coverage report" >&2; exit 1; }
coverage="$(sed -n 's/^[[:space:]]*Line coverage:[[:space:]]*\([0-9.]*\)%.*/\1/p' "$summary" | head -n 1)"
[[ "$coverage" =~ ^[0-9]+([.][0-9]+)?$ ]] || { echo "Invalid $assembly line coverage" >&2; exit 1; }

echo "$assembly line coverage: $coverage%${minimum:+ (required: $minimum%)}"
if [[ -n "$summary_file" ]]; then
  {
    echo "## $assembly coverage"
    cat "$summary"
  } >> "$summary_file"
fi
if [[ -n "$minimum" ]]; then
  [[ "$minimum" =~ ^[0-9]+([.][0-9]+)?$ ]] || { echo "Invalid coverage threshold: $minimum" >&2; exit 1; }
  awk -v actual="$coverage" -v required="$minimum" 'BEGIN { exit !(actual + 0 >= required + 0) }' || {
    echo "$assembly line coverage $coverage% is below $minimum%" >&2
    exit 1
  }
fi
