#!/usr/bin/env python3
"""Compare generated OpenAPI operations with source metadata and acceptance links."""

import json
import re
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).resolve().parent))
from importlib.machinery import SourceFileLoader

links = SourceFileLoader("operation_links", str(Path(__file__).with_name("check-operation-links.py"))).load_module()


def normalize(path):
    return re.sub(r"\{([^}:]+):[^}]+}", r"{\1}", path)


def check(document):
    expected = links.generate()["operations"]
    actual = {}
    for path, routes in document["paths"].items():
        for method, operation in routes.items():
            if method not in {"get", "post", "put", "patch", "delete", "head", "options", "trace"}:
                continue
            name = operation.get("operationId")
            assert name, f"Missing OpenAPI operationId: {method} {path}"
            assert name not in actual, f"Duplicate operationId: {name}"
            actual[name] = (method.upper(), path, operation)
    assert set(actual) == {item["operationId"] for item in expected}, "OpenAPI operation inventory differs from endpoints"
    for item in expected:
        name = item["operationId"]
        method, path, operation = actual[name]
        assert (method, path) == (item["method"], normalize(item["path"])), f"Method or path drift: {name}"
        declared = operation["responses"]
        assert set(map(int, declared)) == set(item["responses"]), f"Response status drift: {name}"
        for status in item["responses"]:
            response = declared[str(status)]
            media = "application/problem+json" if status >= 400 else (
                "application/pdf" if name == "DownloadInvoicePdf" else "application/json")
            assert media in response.get("content", {}), f"Missing {media}: {name} {status}"
            assert response["content"][media].get("schema"), f"Missing response schema: {name} {status}"
    return len(expected)


if __name__ == "__main__":
    try:
        path = Path(sys.argv[1])
        count = check(json.loads(path.read_text(encoding="utf-8")))
        documentation = links.generate()["sourceDocumentation"]
        links.SOURCE_DOCUMENTATION["check_exports"](
            documentation, json.loads(path.with_name("flows.json").read_text()), json.loads(path.with_name("validation-policies.json").read_text()))
        print(f"Verified generated OpenAPI for {count} linked operations.")
    except (AssertionError, KeyError, IndexError, OSError, ValueError) as error:
        print(f"OpenAPI contract drift: {error}", file=sys.stderr)
        sys.exit(1)
