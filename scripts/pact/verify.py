"""Replay a pinned consumer Pact against an isolated Invoice HTTP process."""

import argparse
import hashlib
import http.client
import json
import os
import shutil
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
from tempfile import TemporaryDirectory
from uuid import UUID, uuid4, uuid5

from bson.binary import Binary
from bson.decimal128 import Decimal128
from pact import Verifier
from pymongo import MongoClient
from pymongo.errors import AutoReconnect, OperationFailure

ROOT = Path(__file__).resolve().parents[2]
HOST = "127.0.0.1"
CREATED = "a Created order exists for payment"
MISSING = "the order does not exist"
CART_LOOKUP = "one populated shopping cart exists and another client has no cart"
BFF_CART_EXISTS = "the BFF registration cart exists"
BFF_CART_ABSENT = "the BFF registration cart is absent"
PAID_FOR_FULFILLMENT = "a Paid order exists for fulfillment"
CREATED_FOR_FULFILLMENT = "a Created order exists for fulfillment"
COMPLETED_INVOICE = "a completed invoice exists for the order"
NO_INVOICE = "the order has no completed invoice"
INVOICE_CREATION = "a Paid order and client data exist without an invoice"
ORDER_STATES = {CREATED, MISSING, PAID_FOR_FULFILLMENT, CREATED_FOR_FULFILLMENT, INVOICE_CREATION}
INVOICE_STATES = {COMPLETED_INVOICE, NO_INVOICE, INVOICE_CREATION}
PAID_ORDER_STATUS = 0
CREATED_ORDER_STATUS = 2


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


def request(port, path, *, method="GET", body=None, headers=None):
    connection = http.client.HTTPConnection(HOST, port, timeout=5)
    try:
        connection.request(method, path, body=body, headers=headers or {})
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
        "InvoiceStorage__ConnectionString": "",
    }
    with (
        TemporaryDirectory(prefix="invoice-pact-") as directory,
        (results / "invoice.log").open("w", encoding="utf-8") as log,
    ):
        # The local PDF store locates the solution root from the assembly path or working directory.
        candidate = Path(directory)
        shutil.copytree(dll.parent, candidate, dirs_exist_ok=True)
        process = subprocess.Popen(
            ["dotnet", str(candidate / dll.name)], cwd=candidate, env=env, stdout=log, stderr=subprocess.STDOUT
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


def cart_lookup_state(database, action, params):
    cart = params["cart"]
    client_id = Binary.from_uuid(UUID(cart["clientId"]))
    missing_client_id = Binary.from_uuid(UUID(params["missingClientId"]))
    if client_id == missing_client_id:
        raise ValueError("The Web Pact requires distinct cart clients")
    carts = database["shopping-carts"]
    carts.delete_many({"ClientId": {"$in": [client_id, missing_client_id]}})
    if action == "setup":
        carts.insert_one({
            "_id": Binary.from_uuid(UUID(cart["id"])),
            "ClientId": client_id,
            "CreatedAt": datetime.fromisoformat(cart["createdAt"]),
            "UpdatedAt": datetime.fromisoformat(cart["updatedAt"]),
            "Lines": [
                {"ProductId": Binary.from_uuid(UUID(line["productId"])), "Quantity": line["quantity"]}
                for line in cart["lines"]
            ],
        })


def registration_cart_state(database, name, action, params):
    client = UUID(params["clientId"])
    client_id = Binary.from_uuid(client)
    carts = database["shopping-carts"]
    carts.delete_many({"ClientId": client_id})
    if action == "setup" and name == BFF_CART_EXISTS:
        created = datetime(2026, 1, 1, tzinfo=UTC)
        carts.insert_one({
            "_id": Binary.from_uuid(uuid5(client, "registration-cart")),
            "ClientId": client_id,
            "CreatedAt": created,
            "UpdatedAt": created,
            "Lines": [{"ProductId": Binary.from_uuid(UUID(params["productId"])), "Quantity": params["quantity"]}],
        })


def order_state(database, name, action, params):
    order_id = UUID(params["orderId"])
    snapshot_ids = [uuid5(order_id, f"snapshot-{index}") for index in range(2)]

    database.orders.delete_one({"_id": Binary.from_uuid(order_id)})
    database["product-versions"].delete_many({"_id": {"$in": [Binary.from_uuid(value) for value in snapshot_ids]}})
    if action == "teardown" or name == MISSING:
        return

    amounts = [Decimal(amount) for amount in params["lineAmounts"]]
    currency = params["currency"]
    if len(amounts) != len(snapshot_ids) or sum(amounts) != Decimal(params["totalAmount"]):
        raise ValueError("Inconsistent Payments Pact state amounts")
    created = datetime(2026, 1, 1, tzinfo=UTC)
    client_id = uuid5(order_id, "client") if name == CREATED else UUID(params["clientId"])
    status = PAID_ORDER_STATUS if name in (PAID_FOR_FULFILLMENT, INVOICE_CREATION) else CREATED_ORDER_STATUS
    database["product-versions"].insert_many([
        {
            "_id": Binary.from_uuid(snapshot_id), "IsActive": True, "CreatedAt": created, "DeactivatedAt": None,
            "ProductId": Binary.from_uuid(uuid5(snapshot_id, "product")),
            "PriceAmount": Decimal128(amount), "PriceCurrency": currency, "Name": f"Pact product {index}", "Brand": "Pact",
        }
        for index, (snapshot_id, amount) in enumerate(zip(snapshot_ids, amounts, strict=True))
    ])
    database.orders.insert_one({
        "_id": Binary.from_uuid(order_id), "ClientId": Binary.from_uuid(client_id),
        "CreatedAt": created, "UpdatedAt": None, "Status": status,
        "Lines": [{"ProductVersionId": Binary.from_uuid(value), "Quantity": 1} for value in snapshot_ids],
    })


def invoice_state(database, name, action, params):
    order_id = Binary.from_uuid(UUID(params["orderId"]))
    database.invoices.delete_many({"OrderId": order_id})
    if name == NO_INVOICE:
        return

    version_id = Binary.from_uuid(UUID(params["clientDataVersionId"]))
    created = datetime(2026, 10, 1, tzinfo=UTC)
    if name == COMPLETED_INVOICE and action == "setup":
        database.invoices.insert_one({
            "_id": Binary.from_uuid(UUID(params["invoiceId"])),
            "OrderId": order_id,
            "ClientDataVersionId": version_id,
            "StorageUrl": "file:///invoice.pdf",
            "CreatedAt": created,
            "GenerationStatus": "Completed",
        })
    if name == INVOICE_CREATION:
        versions = database["client-data-versions"]
        versions.delete_one({"_id": version_id})
        if action == "setup":
            versions.insert_one({
                "_id": version_id,
                "ClientId": Binary.from_uuid(UUID(params["clientId"])),
                "ClientName": "Pact Buyer",
                "PostalCode": "00-001",
                "City": "Warsaw",
                "Street": "Sample",
                "BuildingNumber": "1",
                "ApartmentNumber": "2",
                "PhoneNumber": "500600700",
                "PhonePrefix": "+48",
                "AddressEmail": "buyer@example.test",
                "CreatedAt": created,
            })


def provider_state(database, name, action, params):
    if action not in ("setup", "teardown"):
        raise ValueError("Unknown Pact provider action")
    if name == CART_LOOKUP:
        cart_lookup_state(database, action, params)
        return
    if name in (BFF_CART_EXISTS, BFF_CART_ABSENT):
        registration_cart_state(database, name, action, params)
        return
    if name not in ORDER_STATES and name not in INVOICE_STATES:
        raise ValueError("Unknown Pact provider state")
    if name in ORDER_STATES:
        order_state(database, name, action, params)
    if name in INVOICE_STATES:
        invoice_state(database, name, action, params)


def verify(port, database, contract, results, name):
    def handle_state(state, action, parameters):
        provider_state(database, state, action, parameters)

    verifier = (
        Verifier("invoice", host=HOST)
        .add_transport(url=f"http://{HOST}:{port}")
        .add_source(contract)
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
        def forward(self):
            length = int(self.headers.get("Content-Length", 0))
            payload = self.rfile.read(length) if length else None
            headers = {key: self.headers[key] for key in ("Accept", "Content-Type") if key in self.headers}
            status, content_type, body = request(
                provider_port, self.path, method=self.command, body=payload, headers=headers
            )
            if self.command == "GET" and self.path == target and status == 200:
                data = json.loads(body)
                del data["totalAmount"]
                body = json.dumps(data).encode("utf-8")
            self.send_response(status)
            if content_type:
                self.send_header("Content-Type", content_type)
            self.send_header("Content-Length", str(len(body)))
            self.end_headers()
            self.wfile.write(body)

        do_GET = forward
        do_PATCH = forward
        do_POST = forward

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
    parser.add_argument("--consumer", choices=("payments", "web", "bff"), default="payments")
    parser.add_argument("--provider-dll", type=Path, required=True)
    parser.add_argument("--mongo-port", type=int, required=True)
    parser.add_argument("--mongo-member", required=True)
    parser.add_argument("--results", type=Path, required=True)
    args = parser.parse_args()
    args.results.mkdir(parents=True, exist_ok=True)
    os.environ["PACT_DO_NOT_TRACK"] = "true"

    contract = ROOT / f"contracts/pact/{args.consumer}-invoice.json"
    source = json.loads(contract.with_name(f"{args.consumer}-source.json").read_bytes())
    content = contract.read_bytes()
    pact = json.loads(content)
    interactions = pact["interactions"]
    if (
        hashlib.sha256(content).hexdigest() != source["contractSha256"]
        or len(interactions) != source["interactionCount"]
        or pact["consumer"]["name"] != args.consumer
        or pact["provider"]["name"] != "invoice"
        or pact["metadata"]["pactSpecification"]["version"] != "4.0"
    ):
        raise ValueError(f"Pinned {args.consumer} Pact identity or bytes changed")

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
                result = verify(port, database, contract, args.results, "pact-results")
                status, passed, failed, issues = normalize(result, interactions)
                if args.consumer == "payments" and status == "Passed":
                    case, = [item for item in interactions if item["description"] == "read a Created order in PLN"]
                    with without_total_amount(port, case["request"]["path"]) as proxy_port:
                        broken = verify(proxy_port, database, contract, args.results, "negative-control")
                    probe, probe_passed, probe_failed, probe_issues = normalize(broken, interactions)
                    if (
                        (probe, probe_passed, probe_failed) != ("Failed", len(interactions) - 1, 1)
                        or len(probe_issues) != 1
                        or probe_issues[0]["interaction"] != case["description"]
                        or probe_issues[0]["jsonPath"] != "$.totalAmount"
                        or probe_issues[0]["actualPresent"] is not False
                    ):
                        raise RuntimeError("Pact did not isolate the missing totalAmount regression")
                    restored = verify(port, database, contract, args.results, "restored-control")
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
