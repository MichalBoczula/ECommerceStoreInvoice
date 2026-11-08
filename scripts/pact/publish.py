"""Publish Invoice evidence for the pinned Payments Pact to the portfolio registry."""

import argparse
import hashlib
import json
import os
from pathlib import Path
from urllib.error import HTTPError
from urllib.parse import urlencode, urlsplit
from urllib.request import HTTPRedirectHandler, Request, build_opener

ROOT = Path(__file__).resolve().parents[2]
REPOSITORY = "https://github.com/MichalBoczula/ECommerceStoreInvoice"


class NoRedirect(HTTPRedirectHandler):
    def redirect_request(self, req, fp, code, msg, headers, newurl):
        return None


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("report", type=Path)
    parser.add_argument("receipt", type=Path)
    args = parser.parse_args()

    if os.environ["GITHUB_EVENT_NAME"] != "push" or os.environ["GITHUB_REF"] != "refs/heads/master":
        raise ValueError("Registry publication requires an Invoice master push.")
    registry = urlsplit(os.environ.get("PACT_REGISTRY_URL", ""))
    token = os.environ.get("PACT_PUBLISHER_TOKEN", "")
    if (
        registry.scheme != "https" or not registry.hostname or registry.username is not None
        or registry.password is not None or registry.path not in ("", "/")
        or registry.query or registry.fragment or not token
    ):
        raise ValueError("Configure an HTTPS PACT_REGISTRY_URL origin and the Invoice PACT_PUBLISHER_TOKEN.")

    source = json.loads((ROOT / "contracts/pact/payments-source.json").read_bytes())
    report = json.loads(args.report.read_bytes())
    sha = os.environ["GITHUB_SHA"]
    if (
        report["consumerCommitSha"] != source["commitSha"]
        or report["contractSha256"] != source["contractSha256"]
        or report["providerCommitSha"] != sha
    ):
        raise ValueError("The provider report does not match the pinned Pact and workflow commit.")

    opener = build_opener(NoRedirect())

    def call(path, body=None, raw=False):
        headers = {} if body is None else {"Authorization": f"Bearer {token}", "Content-Type": "application/json"}
        req = Request(
            f"https://{registry.netloc}{path}",
            data=None if body is None else json.dumps(body).encode(),
            headers=headers,
        )
        try:
            with opener.open(req, timeout=30) as response:
                return response.read() if raw else json.load(response)
        except HTTPError as error:
            raise RuntimeError(f"Pact registry rejected {path.partition('?')[0]} with HTTP {error.code}.") from None

    filter_by = {
        "sourceApplicationId": "payments", "consumerApplicationId": "payments",
        "providerApplicationId": "invoice", "kind": "ConsumerPact", "sourceRef": "refs/heads/main",
        "commitSha": source["commitSha"], "contractSha256": source["contractSha256"], "limit": 100,
    }
    consumer_build = f"https://github.com/{source['repository']}/actions/runs/{source['workflowRunId']}"
    matches = []
    seen = set()
    while True:
        page = call("/api/contract-publications?" + urlencode(filter_by))
        matches.extend(item for item in page["items"] if item["buildUrl"] == consumer_build)
        after = page["nextCursor"]
        if after is None:
            break
        if after in seen:
            raise RuntimeError("The registry repeated a publication cursor.")
        seen.add(after)
        filter_by["after"] = after
    if len(matches) != 1:
        raise RuntimeError("Publish the pinned Payments workflow artifact before publishing Invoice evidence.")
    publication, = matches
    digest = source["contractSha256"]
    if hashlib.sha256(call(f"/api/artifacts/{digest}/content", raw=True)).hexdigest() != digest:
        raise RuntimeError("Registry artifact bytes differ from the verified Pact.")

    run = os.environ["GITHUB_RUN_ID"]
    build_url = f"{REPOSITORY}/actions/runs/{run}"
    call("/api/applications/invoice/versions", {"commitSha": sha, "repositoryUrl": REPOSITORY})
    receipt = call("/api/verification-runs", {
        **report,
        "consumerPublicationId": publication["id"], "consumerApplicationId": "payments",
        "providerApplicationId": "invoice", "ciRunId": run, "ciAttempt": int(os.environ["GITHUB_RUN_ATTEMPT"]),
        "ciCheckId": "payments-invoice-order-read", "sourceRef": "refs/heads/master",
        "buildUrl": build_url, "evidenceUrls": [build_url],
    })
    if receipt["contractSha256"] != digest or receipt["status"] != report["status"]:
        raise RuntimeError("The registry returned a different verification result.")
    args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(f"Published Invoice verification {receipt['id']} ({receipt['status']}).")


if __name__ == "__main__":
    main()
