#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
suite="${1:?Provide the test suite name}"
assembly="${2:?Provide the target assembly name}"
minimum="${3:-}"
reports="$PWD/TestResults/$suite/**/coverage.cobertura.xml"
target="$PWD/TestResults/CoverageReport/$suite"
tool_dir="${RUNNER_TEMP:-$PWD/artifacts/verification}/reportgenerator-tool"
mkdir -p "$tool_dir"
if [[ ! -x "$tool_dir/reportgenerator" ]]; then
  dotnet tool install dotnet-reportgenerator-globaltool --tool-path "$tool_dir" --version 5.4.7
fi
"$tool_dir/reportgenerator" -reports:"$reports" -targetdir:"$target" \
  '-reporttypes:Html;TextSummary' -assemblyfilters:"+$assembly"

summary="$target/Summary.txt"
percent="$(sed -n 's/^[[:space:]]*Line coverage:[[:space:]]*\([0-9.]*\)%.*/\1/p' "$summary" | head -n 1)"
[[ "$percent" =~ ^[0-9]+([.][0-9]+)?$ ]] || { echo "Missing $suite line coverage" >&2; exit 1; }
echo "$suite line coverage: $percent%"
{
  echo "## $suite coverage"
  cat "$summary"
} >> "${GITHUB_STEP_SUMMARY:-/dev/null}"
if [[ -n "$minimum" ]]; then
  awk -v value="$percent" -v required="$minimum" 'BEGIN { exit !(value + 0 >= required + 0) }' || {
    echo "$suite line coverage $percent% is below $minimum%" >&2
    exit 1
  }
fi
