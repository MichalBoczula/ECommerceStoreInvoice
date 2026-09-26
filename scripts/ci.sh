#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/.."
command="${1:?Usage: scripts/ci.sh source|build|format|contract|test SUITE}"
shift
results_dir="${VERIFY_RESULTS_DIR:-$PWD/artifacts/verification}"

case "$command" in
  source)
    python3 scripts/test-architecture.py
    python3 scripts/check-architecture.py
    python3 scripts/check-operation-links.py
    ;;
  build)
    dotnet restore ECommerceStoreInvoice.slnx
    dotnet build ECommerceStoreInvoice.slnx --configuration Release --no-restore
    ;;
  format)
    bash scripts/verify-format.sh
    ;;
  contract)
    bash scripts/validate-openapi.sh "$results_dir/openapi.json"
    ;;
  test)
    suite="${1:?Provide a test suite}"
    test_args=()
    assembly=""
    threshold=""
    case "$suite" in
      domain)
        project=tests/ECommerceStoreInvoice.Domain.UnitTests/ECommerceStoreInvoice.Domain.UnitTests.csproj
        assembly=ECommerceStoreInvoice.Domain
        threshold=70
        test_args=(--collect 'XPlat Code Coverage')
        ;;
      application)
        project=tests/ECommerceStoreInvoice.Application.UnitTests/ECommerceStoreInvoice.Application.UnitTests.csproj
        assembly=ECommerceStoreInvoice.Application
        threshold=70
        test_args=(--collect 'XPlat Code Coverage')
        ;;
      infrastructure)
        project=tests/ECommerceStoreInvoice.Infrastructure.UnitTests/ECommerceStoreInvoice.Infrastructure.UnitTests.csproj
        assembly=ECommerceStoreInvoice.Infrastructure
        threshold=70
        test_args=(--collect 'XPlat Code Coverage'
          --settings tests/ECommerceStoreInvoice.Infrastructure.UnitTests/coverage.runsettings)
        ;;
      external-providers)
        project=tests/ECommerceStoreInvoice.ExternalProviders.IntegrationTests/ECommerceStoreInvoice.ExternalProviders.IntegrationTests.csproj
        ;;
      acceptance)
        project=tests/ECommerceStoreInvoice.Acceptance.Tests/ECommerceStoreInvoice.Acceptance.Tests.csproj
        ;;
      *)
        echo "Unknown test suite: $suite" >&2
        exit 2
        ;;
    esac

    mkdir -p "$results_dir"
    rm -rf "$results_dir/$suite" "$results_dir/$suite-coverage"
    summary_file="${VERIFY_SUMMARY_FILE:-$results_dir/summary.md}"
    dotnet restore ECommerceStoreInvoice.slnx
    test_status=0
    dotnet test "$project" --configuration Release --no-restore \
      --logger "trx;LogFileName=$suite.trx" --results-directory "$results_dir/$suite" \
      "${test_args[@]}" || test_status=$?
    summary_status=0
    python3 scripts/summarize-trx.py "$suite" "$results_dir/$suite" "$summary_file" || summary_status=$?
    if (( test_status != 0 )); then exit "$test_status"; fi
    if (( summary_status != 0 )); then exit "$summary_status"; fi

    if [[ -n "$assembly" ]]; then
      if [[ "$suite" == infrastructure ]]; then
        python3 scripts/check-infrastructure-coverage.py "$results_dir/$suite"
      fi
      bash scripts/report-coverage.sh "$results_dir/$suite" "$results_dir/$suite-coverage" \
        "$assembly" "$threshold" "$summary_file"
    fi
    ;;
  *)
    echo "Unknown verification command: $command" >&2
    exit 2
    ;;
esac
