"""Replay the pinned Payments Pact against an isolated Invoice HTTP process."""

import argparse
import hashlib
import http.client
import json
import os
import socket
import subprocess
import threading
import time
from contextlib import contextmanager
from datetime import UTC, datetime
from decimal import Decimal
from http.server import BaseHTTPRequestHandler, ThreadingHTTPServer
from importlib.metadata import version
from pathlib import Path
from uuid import UUID, uuid4, uuid5

from bson.binary import Binary
from bson.decimal128 import Decimal128
from pact import Verifier
from pymongo import MongoClient
from pymongo.errors import AutoReconnect, OperationFailure

ROOT = Path(__file__).resolve().parents[2]
CONTRACT = ROOT / "contracts/pact/payments-invoice.json"
SOURCE = CONTRACT.with_name("payments-source.json")
HOST = "127.0.0.1"
CREATED = "a Created order exists for payment"
MISSING = "the order does not exist"


def timestamp():
    return datetime.now(UTC).isoformat(timespec="milliseconds")


def write_json(path, data):
    path.write_text(json.dumps(data, indent=2) + "\n", encoding="utf-8")


def wait_for(operation, timeout=60):
    until = time.monotonic() + timeout
    while time.monotonic() < until:
        try:
            if operation():
                return
        except (OSError, AutoReconnect, OperationFailure):
            pass
        time.sleep(0.25)
    raise RuntimeError(f"Timed out waiting for {operation.__name__}")


def request(port, path):
    connection = http.client.HTTPConnection(HOST, port, timeout=5)
    try:
        connection.request("GET", path)
        response = connection.getresponse()
        return response.status, response.getheader("Content-Type"), response.read()
    finally:
        connection.close()


@contextmanager
def invoice_api(dll, mongo_uri, database_name, results):
    with socket.socket() as listener:
        listener.bind((HOST, 0))
        port = listener.getsockname()[1]
    env = {
        **os.environ,
        "ASPNETCORE_ENVIRONMENT": "Testing",
        "DOTNET_ENVIRONMENT": "Testing",
        "ASPNETCORE_URLS": f"http://{HOST}:{port}",
        "MongoDbSettings__ConnectionString": mongo_uri,
        "MongoDbSettings__DatabaseName": database_name,
        "ExternalServices__ProductCatalog__BaseUrl": f"http://{HOST}:1",
    }
    with (results / "invoice.log").open("w", encoding="utf-8") as log:
        process = subprocess.Popen(
            ["dotnet", str(dll)], cwd=dll.parent, env=env, stdout=log, stderr=subprocess.STDOUT
        )
        try:
            def invoice_ready():
                if process.poll() is not None:
                    raise RuntimeError("Invoice exited before readiness. See invoice.log.")
                return request(port, "/health/ready")[0] == 200

            wait_for(invoice_ready)
            yield port
        finally:
            if process.poll() is None:
                process.terminate()
                try:
                    process.wait(timeout=10)
                except subprocess.TimeoutExpired:
                    process.kill()
                    process.wait()


def provider_state(database, name, action, parameters):
    params = parameters or {}
    order_id = UUID(params["orderId"])
    snapshot_ids = [uuid5(order_id, f"snapshot-{index}") for index in range(2)]
    if name not in (CREATED, MISSING) or action not in ("setup", "teardown"):
        raise ValueError("Unknown Pact provider state or action")

    database.orders.delete_one({"_id": Binary.from_uuid(order_id)})
    database["product-versions"].delete_many({"_id": {"$in": [Binary.from_uuid(value) for value in snapshot_ids]}})
    if action == "teardown" or name == MISSING:
        return

    amounts = [Decimal(amount) for amount in params["lineAmounts"]]
    currency = params["currency"]
    if len(amounts) != len(snapshot_ids) or sum(amounts) != Decimal(params["totalAmount"]):
        raise ValueError("Inconsistent Payments Pact state amounts")
    created = datetime(2026, 1, 1, tzinfo=UTC)
    database["product-versions"].insert_many([
        {
            "_id": Binary.from_uuid(snapshot_id), "IsActive": True, "CreatedAt": created, "DeactivatedAt": None,
            "ProductId": Binary.from_uuid(uuid5(snapshot_id, "product")),
            "PriceAmount": Decimal128(amount), "PriceCurrency": currency, "Name": f"Pact product {index}", "Brand": "Pact",
        }
        for index, (snapshot_id, amount) in enumerate(zip(snapshot_ids, amounts, strict=True))
    ])
    database.orders.insert_one({
        "_id": Binary.from_uuid(order_id), "ClientId": Binary.from_uuid(uuid5(order_id, "client")),
        "CreatedAt": created, "UpdatedAt": None, "Status": 2,
        "Lines": [{"ProductVersionId": Binary.from_uuid(value), "Quantity": 1} for value in snapshot_ids],
    })


def verify(port, database, results, name):
    def handle_state(state, action, parameters):
        provider_state(database, state, action, parameters)

    verifier = (
        Verifier("invoice", host=HOST)
        .add_transport(url=f"http://{HOST}:{port}")
        .add_source(CONTRACT)
        .state_handler(handle_state, teardown=True)
    )
    try:
        verifier.verify()
    except RuntimeError:
        # Pact uses RuntimeError for a failed verification. Its report is the evidence.
        pass
    result = verifier.results
    write_json(results / f"{name}.json", result)
    return result


@contextmanager
def without_total_amount(provider_port, target):
    class FaultProxy(BaseHTTPRequestHandler):
        def do_GET(self):
            status, content_type, body = request(provider_port, self.path)
            if self.path == target and status == 200:
                data = json.loads(body)
                del data["totalAmount"]
                body = json.dumps(data).encode("utf-8")
            self.send_response(status)
            if content_type:
                self.send_header("Content-Type", content_type)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

    with ThreadingHTTPServer((HOST, 0), FaultProxy) as server:
        thread = threading.Thread(target=server.serve_forever, daemon=True)
        thread.start()
        try:
            yield server.server_port
        finally:
            server.shutdown()
            thread.join()


def normalize(result, interactions):
    expected = {item["description"]: item for item in interactions}
    rows = result.get("interactionResults", [])
    descriptions = [row["description"] for row in rows]
    if len(rows) != len(expected) or set(descriptions) != expected.keys() or any(row["pending"] for row in rows):
        return "Error", 0, 0, [{
            "kind": "ExecutionError", "code": "IncompletePactExecution", "message": "Pact did not report every interaction.",
        }]

    passed = failed = 0
    issues = []
    for row in rows:
        if row["result"] == "OK":
            passed += 1
            continue
        name = row["description"]
        mismatches = row.get("mismatch", {}).get("mismatches", [])
        if not mismatches:
            issues.append({
                "kind": "ExecutionError", "code": "PactExecutionError",
                "message": "Pact could not execute this interaction. See the CI artifact.", "interaction": name,
            })
            continue
        failed += 1
        for mismatch in mismatches:
            operation = expected[name]["request"]
            issue = {
                "kind": "Mismatch", "code": mismatch["type"], "message": mismatch["mismatch"][:4096],
                "interaction": name, "method": operation["method"], "path": operation["path"],
                "jsonPath": mismatch.get("path") or mismatch.get("key") or "$",
                "expected": mismatch.get("expected"), "actualPresent": "actual" in mismatch,
            }
            if issue["actualPresent"]:
                issue["actual"] = mismatch["actual"]
            if issue["message"] == "Actual map is missing the following keys: totalAmount" and issue["jsonPath"] == "$":
                issue["jsonPath"] = "$.totalAmount"
                issue["expected"] = expected[name]["response"]["body"]["content"]["totalAmount"]
                issue["actualPresent"] = False
                issue.pop("actual", None)
            issues.append(issue)
    if any(issue["kind"] == "ExecutionError" for issue in issues) or (not result["result"] and not issues):
        if not issues:
            issues.append({"kind": "ExecutionError", "code": "PactExecutionError", "message": "Pact reported an execution error."})
        return "Error", passed, failed, issues
    return ("Failed" if failed else "Passed"), passed, failed, issues


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--provider-dll", type=Path, required=True)
    parser.add_argument("--mongo-port", type=int, required=True)
    parser.add_argument("--mongo-member", required=True)
    parser.add_argument("--results", type=Path, required=True)
    args = parser.parse_args()
    args.results.mkdir(parents=True, exist_ok=True)
    os.environ["PACT_DO_NOT_TRACK"] = "true"

    source = json.loads(SOURCE.read_bytes())
    content = CONTRACT.read_bytes()
    pact = json.loads(content)
    interactions = pact["interactions"]
    if (
        hashlib.sha256(content).hexdigest() != source["contractSha256"]
        or len(interactions) != source["interactionCount"]
        or pact["consumer"]["name"] != "payments" or pact["provider"]["name"] != "invoice"
    ):
        raise ValueError("Pinned Payments Pact identity or bytes changed")

    started = timestamp()
    mongo_uri = f"mongodb://{HOST}:{args.mongo_port}/?directConnection=true"
    with MongoClient(mongo_uri, serverSelectionTimeoutMS=1000) as mongo:
        def mongo_ready():
            return mongo.admin.command("ping")["ok"] == 1

        wait_for(mongo_ready)
        mongo.admin.command("replSetInitiate", {"_id": "pact", "members": [{"_id": 0, "host": args.mongo_member}]})

        def primary_ready():
            return mongo.admin.command("hello")["isWritablePrimary"]

        wait_for(primary_ready)
        database = mongo[f"invoice_pact_{uuid4().hex}"]
        try:
            with invoice_api(args.provider_dll.resolve(strict=True), mongo_uri, database.name, args.results) as port:
                result = verify(port, database, args.results, "pact-results")
                status, passed, failed, issues = normalize(result, interactions)
                if status == "Passed":
                    case, = [item for item in interactions if item["description"] == "read a Created order in PLN"]
                    with without_total_amount(port, case["request"]["path"]) as proxy_port:
                        broken = verify(proxy_port, database, args.results, "negative-control")
                    probe, probe_passed, probe_failed, probe_issues = normalize(broken, interactions)
                    if (
                        (probe, probe_passed, probe_failed) != ("Failed", len(interactions) - 1, 1)
                        or len(probe_issues) != 1
                        or probe_issues[0]["interaction"] != case["description"]
                        or probe_issues[0]["jsonPath"] != "$.totalAmount"
                        or probe_issues[0]["actualPresent"] is not False
                    ):
                        raise RuntimeError("Pact did not isolate the missing totalAmount regression")
                    restored = verify(port, database, args.results, "restored-control")
                    if normalize(restored, interactions)[0] != "Passed":
                        raise RuntimeError("Pact did not pass after restoring the provider response")
        finally:
            mongo.drop_database(database.name)

    configuration = b"\0".join(Path(__file__).with_name(name).read_bytes() for name in ("verify.py", "requirements.txt"))
    write_json(args.results / "verification.json", {
        "consumerCommitSha": source["commitSha"], "contractSha256": source["contractSha256"],
        "providerCommitSha": subprocess.check_output(["git", "rev-parse", "HEAD"], cwd=ROOT, text=True).strip(),
        "verifierName": "pact-python", "verifierVersion": version("pact-python"),
        "configurationSha256": hashlib.sha256(configuration).hexdigest(),
        "status": status, "verifiedInteractions": passed, "failedInteractions": failed, "issues": issues,
        "summary": f"Invoice HTTP provider Pact verification {status.lower()} with {passed} matching interactions.",
        "startedAt": started, "finishedAt": timestamp(),
    })
    return 0 if status == "Passed" else 1


if __name__ == "__main__":
    raise SystemExit(main())
