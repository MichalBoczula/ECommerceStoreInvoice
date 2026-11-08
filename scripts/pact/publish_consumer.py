"""Publish the original Invoice consumer Pact emitted by the required CI suite."""

import argparse
import base64
import hashlib
import json
import os
from pathlib import Path

from registry import Registry

REPOSITORY = "https://github.com/MichalBoczula/ECommerceStoreInvoice"
PACT_FILE = "invoice-products.json"


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("artifact", type=Path)
    parser.add_argument("receipt", type=Path)
    args = parser.parse_args()
    ref = os.environ["GITHUB_REF"]
    if os.environ["GITHUB_EVENT_NAME"] != "push" or ref != "refs/heads/master":
        raise ValueError("Consumer Pact publication requires an Invoice master push.")

    manifest = json.loads((args.artifact / "manifest.json").read_bytes())
    content = (args.artifact / PACT_FILE).read_bytes()
    pact = json.loads(content)
    digest = hashlib.sha256(content).hexdigest()
    if (
        manifest["schemaVersion"] != 1 or manifest["file"] != PACT_FILE or manifest["sha256"] != digest
        or (manifest["consumer"], manifest["provider"]) != ("invoice", "products")
        or (pact["consumer"]["name"], pact["provider"]["name"]) != ("invoice", "products")
        or manifest["pactSpecification"] != "4.0" or pact["metadata"]["pactSpecification"]["version"] != "4.0"
        or manifest["interactionCount"] != len(pact["interactions"])
    ):
        raise ValueError("Artifact bytes or Pact identity differ from the Invoice manifest.")

    sha = os.environ["GITHUB_SHA"]
    build = f"{REPOSITORY}/actions/runs/{os.environ['GITHUB_RUN_ID']}"
    call = Registry().call
    call("/api/applications/invoice/versions", {"commitSha": sha, "repositoryUrl": REPOSITORY})
    call("/api/integrations", {"consumerApplicationId": "invoice", "providerApplicationId": "products"})
    publication = call("/api/contract-publications", {
        "sourceApplicationId": "invoice", "commitSha": sha, "kind": "ConsumerPact",
        "consumerApplicationId": "invoice", "providerApplicationId": "products",
        "sourceRef": ref, "buildUrl": build, "contentBase64": base64.b64encode(content).decode("ascii"),
    })
    if publication["artifactSha256"] != digest:
        raise RuntimeError("The registry returned a different contract hash.")
    args.receipt.write_text(json.dumps({
        "publicationId": publication["id"], "commitSha": sha, "sha256": digest, "buildUrl": build,
    }, indent=2) + "\n", encoding="utf-8")
    print(f"Published Invoice Products Pact {digest} as {publication['id']}.")


if __name__ == "__main__":
    main()
