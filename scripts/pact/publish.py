"""Publish Invoice evidence for a pinned consumer Pact to the portfolio registry."""

import argparse
import hashlib
import json
import os
from pathlib import Path
from urllib.parse import urlencode

from registry import Registry

ROOT = Path(__file__).resolve().parents[2]
REPOSITORY = "https://github.com/MichalBoczula/ECommerceStoreInvoice"
CONSUMERS = {
    "payments": ("refs/heads/main", "payments-invoice-order-read"),
    "web": ("refs/heads/master", "web-invoice-cart-lookup"),
    "bff": ("refs/heads/master", "bff-invoice-registration-cart"),
}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("report", type=Path)
    parser.add_argument("receipt", type=Path)
    parser.add_argument("--consumer", choices=CONSUMERS, default="payments")
    args = parser.parse_args()

    if os.environ["GITHUB_EVENT_NAME"] != "push" or os.environ["GITHUB_REF"] != "refs/heads/master":
        raise ValueError("Registry publication requires an Invoice master push.")

    consumer_ref, ci_check_id = CONSUMERS[args.consumer]
    source = json.loads((ROOT / f"contracts/pact/{args.consumer}-source.json").read_bytes())
    report = json.loads(args.report.read_bytes())
    sha = os.environ["GITHUB_SHA"]
    if (
        report["consumerCommitSha"] != source["commitSha"]
        or report["contractSha256"] != source["contractSha256"]
        or report["providerCommitSha"] != sha
    ):
        raise ValueError("The provider report does not match the pinned Pact and workflow commit.")

    call = Registry().call

    filter_by = {
        "sourceApplicationId": args.consumer, "consumerApplicationId": args.consumer,
        "providerApplicationId": "invoice", "kind": "ConsumerPact", "sourceRef": consumer_ref,
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
        raise RuntimeError(f"Publish the pinned {args.consumer} workflow artifact before publishing Invoice evidence.")
    publication, = matches
    digest = source["contractSha256"]
    if hashlib.sha256(call(f"/api/artifacts/{digest}/content", raw=True)).hexdigest() != digest:
        raise RuntimeError("Registry artifact bytes differ from the verified Pact.")

    run = os.environ["GITHUB_RUN_ID"]
    build_url = f"{REPOSITORY}/actions/runs/{run}"
    call("/api/applications/invoice/versions", {"commitSha": sha, "repositoryUrl": REPOSITORY})
    receipt = call("/api/verification-runs", {
        **report,
        "consumerPublicationId": publication["id"], "consumerApplicationId": args.consumer,
        "providerApplicationId": "invoice", "ciRunId": run, "ciAttempt": int(os.environ["GITHUB_RUN_ATTEMPT"]),
        "ciCheckId": ci_check_id, "sourceRef": "refs/heads/master",
        "buildUrl": build_url, "evidenceUrls": [build_url],
    })
    if receipt["contractSha256"] != digest or receipt["status"] != report["status"]:
        raise RuntimeError("The registry returned a different verification result.")
    args.receipt.write_text(json.dumps(receipt, indent=2) + "\n", encoding="utf-8")
    print(f"Published Invoice verification {receipt['id']} ({receipt['status']}).")


if __name__ == "__main__":
    main()
