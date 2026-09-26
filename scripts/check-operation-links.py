#!/usr/bin/env python3
"""Project endpoint, executed flow, policy and acceptance links from source."""

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
API = ROOT / "src/ECommerceStoreInvoice.API/Endpoints"
APP = ROOT / "src/ECommerceStoreInvoice.Application"
DOMAIN = ROOT / "src/ECommerceStoreInvoice.Domain"
FEATURES = ROOT / "tests/ECommerceStoreInvoice.Acceptance.Tests/Features"
ROUTE = re.compile(r'group\.Map(Get|Post|Put|Patch|Delete)\("([^"]+)"([\s\S]*?)\.WithName\("([^"]+)"\)')
CALL = re.compile(r'\b(\w+Service)\.(\w+)\(')
DESCRIPTOR = re.compile(r'var descriptor = new (\w+Descriptor)\(\);')
POLICY = re.compile(r'IValidationPolicy<(.+?)>')


def source(path):
    return path.read_text(encoding="utf-8-sig")


def normalize(name):
    return re.sub(r"\W", "", name).lower()


def registrations():
    code = source(DOMAIN / "DependencyInjection.cs")
    entries = re.findall(r'AddScoped<IValidationPolicy<(.+?)>,\s*(\w+)>\(\)', code)
    policies = {normalize(kind): name for kind, name in entries}
    published = set(re.findall(r'AddScoped<IValidationPolicyDescriptorProvider,\s*(\w+)>', code))
    assert policies and set(policies.values()) == published, "Validation policies differ from published documentation"
    return policies


def service_flow(service, method):
    files = list((APP / "Services/Concrete").rglob(f"{service[0].upper() + service[1:]}.cs"))
    assert len(files) == 1, f"Expected one implementation for {service}"
    code = source(files[0])
    match = re.search(r'public\s+(?:async\s+)?Task(?:<[^\n]+?>)?\s+' + re.escape(method) + r'\s*\(', code)
    assert match, f"Missing service method {service}.{method}"
    next_method = re.search(r'\n\s*public\s+(?:async\s+)?Task', code[match.end():])
    body = code[match.end():match.end() + next_method.start()] if next_method else code[match.end():]
    descriptors = DESCRIPTOR.findall(body)
    assert len(descriptors) == 1, f"Expected one executed flow in {service}.{method}: {descriptors}"
    return descriptors[0]


def scenarios(name):
    aliases = {
        "GetShoppingCartByClientId": "GetShoppingCart",
        "GetOrderById": "GetOrdersById",
        "GetClientDataVersionByClientId": "GetClientDataVersion",
        "GetFlowDocumentation": "GetFlow",
        "GetValidationDocumentation": "GetValidations",
    }
    prefix = aliases.get(name, name)
    found = sorted(path.relative_to(ROOT).as_posix() for path in FEATURES.rglob("*.feature")
                   if path.stem.startswith(prefix))
    if name == "UpdateOrderStatus":
        found += sorted(path.relative_to(ROOT).as_posix() for path in FEATURES.rglob("UpdateOrderStatus.feature"))
        found = sorted(set(found))
    assert found, f"No acceptance scenario for {name}"
    return found


def generate():
    policies = registrations()
    published = source(API / "DocumentationEndpoints.cs")
    operations = []
    for file in sorted(API.glob("*Endpoints.cs")):
        code = source(file)
        code = re.sub(r'group\.MapPost\("/\{clientId:guid\}", CreateOrder\)\s*\.ExcludeFromDescription\(\);', '', code)
        group = re.search(r'MapGroup\("([^"]+)"\)', code)
        assert group, f"Missing route group in {file.name}"
        for match in ROUTE.finditer(code):
            verb, suffix, body, name = match.groups()
            if file.name == "DocumentationEndpoints.cs":
                flow, linked_policies = "Documentation", []
            else:
                calls = CALL.findall(body)
                if not calls and name == "CreateOrder":
                    handler = re.search(r'private static async Task<IResult> CreateOrder\([^)]*\)([\s\S]*?)\n        }', code)
                    assert handler, "Missing CreateOrder handler"
                    calls = CALL.findall(handler.group(1))
                assert len(calls) == 1, f"Expected one service call in {name}: {calls}"
                descriptor = service_flow(*calls[0])
                files = list((APP / "Descriptors").rglob(f"{descriptor}.cs"))
                assert len(files) == 1, f"Missing flow descriptor {descriptor}"
                kinds = {normalize(kind) for kind in POLICY.findall(source(files[0]))}
                assert kinds <= policies.keys(), f"Unregistered policies in {descriptor}: {kinds - policies.keys()}"
                linked_policies = sorted({policies[kind] for kind in kinds})
                provider_method = descriptor if descriptor.startswith("Get") else "Get" + descriptor
                assert f".{provider_method}" in published, f"Flow {descriptor} is not exposed by documentation"
                flow = descriptor
            responses = {int(status) for status in re.findall(r'\.Produces[^()]*\(StatusCodes\.Status(\d+)', code[match.end():code.find(";", match.end())])}
            assert responses, f"Missing response metadata in {name}"
            operations.append({"operationId": name, "method": verb.upper(),
                               "path": group.group(1) + suffix, "flow": flow,
                               "policies": linked_policies, "scenarios": scenarios(name),
                               "responses": sorted(responses)})
    names = [item["operationId"] for item in operations]
    assert len(names) == len(set(names)) == 13, f"Unexpected operation inventory: {names}"
    return {"operations": operations}


if __name__ == "__main__":
    try:
        document = generate()
        if len(sys.argv) > 1:
            Path(sys.argv[1]).write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
        print(f"Linked {len(document['operations'])} operations to flows, policies and scenarios.")
    except (AssertionError, OSError, ValueError) as error:
        print(f"Operation links drift: {error}", file=sys.stderr)
        sys.exit(1)
