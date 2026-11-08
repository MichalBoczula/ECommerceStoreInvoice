#!/usr/bin/env bash
set -euo pipefail

cd "$(dirname "$0")/../.."
results="$PWD/artifacts/verification/pact-provider"
mkdir -p "$results"
rm -f "$results"/{verification.json,pact-results.json,negative-control.json,restored-control.json,invoice.log,mongo.log}
container=$(docker run --rm -d -p 127.0.0.1::27017 mongo:8.0 --replSet pact --bind_ip_all)
cleanup() {
  docker logs "$container" > "$results/mongo.log" 2>&1 || true
  docker rm -f "$container" >/dev/null 2>&1 || true
}
trap cleanup EXIT
address=$(docker port "$container" 27017/tcp)
PACT_DO_NOT_TRACK=true uv run --no-project --python 3.14.7 --with-requirements scripts/pact/requirements.txt \
  python scripts/pact/verify.py \
    --provider-dll src/ECommerceStoreInvoice.API/bin/Release/net10.0/ECommerceStoreInvoice.API.dll \
    --mongo-port "${address##*:}" --mongo-member localhost:27017 --results "$results"
