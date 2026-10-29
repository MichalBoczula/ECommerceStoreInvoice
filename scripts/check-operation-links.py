#!/usr/bin/env python3
"""Project endpoint, executed flow, policy and acceptance links from source."""

import csv
import json
import subprocess
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
API = ROOT / "src/ECommerceStoreInvoice.API/Endpoints"
APP = ROOT / "src/ECommerceStoreInvoice.Application"
DOMAIN = ROOT / "src/ECommerceStoreInvoice.Domain"
FEATURES = ROOT / "tests/ECommerceStoreInvoice.Acceptance.Tests/Features"
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


def statement_end(code, start):
    """Find the registration terminator, ignoring handler bodies and quoted semicolons."""
    depth = 0
    quote = None
    comment = None
    i = start
    while i < len(code):
        ch, pair = code[i], code[i:i + 2]
        if comment:
            if comment == '//' and ch == '\n': comment = None
            elif comment == '/*' and pair == '*/': comment = None; i += 1
        elif quote:
            if ch == '\\': i += 1
            elif ch == quote: quote = None
        elif pair in {'//', '/*'}: comment = pair; i += 1
        elif ch in {'"', "'"}: quote = ch
        elif ch in '({[': depth += 1
        elif ch in ')}]': depth -= 1
        elif ch == ';' and depth == 0: return i
        i += 1
    raise AssertionError('Unterminated endpoint registration')


def endpoint_registrations(code, prefix):
    result = []
    for match in re.finditer(r'group\.Map(Get|Post|Put|Patch|Delete|Head|Options|Trace)\("([^"]+)"', code):
        verb, suffix = match.groups()
        section = code[match.start():statement_end(code, match.start())]
        if '.ExcludeFromDescription()' in section:
            assert (verb, prefix + suffix) == ('Post', '/orders/{clientId:guid}') and re.search(
                r'MapPost\("/\{clientId:guid\}",\s*CreateOrder\)', section), 'Unexpected hidden HTTP operation'
            continue
        names = re.findall(r'\.WithName\("([^"]+)"\)', section)
        assert len(names) == 1, f'Expected one operation name: {verb} {prefix + suffix}'
        statuses = re.findall(r'\.Produces(?:<[^;()]+>)?\s*\(\s*StatusCodes\.Status(\d{3})', section)
        assert statuses and len(statuses) == len(set(statuses)), f'Missing or duplicate response metadata: {names[0]}'
        result.append((verb.upper(), prefix + suffix, names[0], section, sorted(map(int, statuses))))
    return result


def scenarios(feature):
    """Read the English scenario/Examples subset used by this suite, not step tables."""
    result = {}
    current = None
    header = None
    examples = False
    docstring = None
    pending_tags = []
    description = False
    for line in feature.read_text(encoding="utf-8-sig").splitlines():
        stripped = line.strip()
        if stripped.startswith(('"""', '```')):
            delimiter = stripped[:3]
            docstring = None if docstring == delimiter else delimiter
            continue
        if docstring:
            continue
        if stripped.startswith("@"):
            pending_tags.extend(stripped.split())
            continue
        match = re.fullmatch(r"Scenario( Outline)?:\s*(.+)", stripped)
        if match:
            outline, name = match.groups()
            assert name not in result, f"Duplicate scenario title: {feature} {name}"
            current = {"outline": bool(outline), "examples": [], "tags": pending_tags, "description": []}
            result[name] = current
            pending_tags = []
            description = True
            examples, header = False, None
        elif stripped.startswith("Examples:"):
            assert current and current["outline"], f"Examples without outline: {feature}"
            description = False
            examples, header = True, None
        elif examples and stripped.startswith("|"):
            # Gherkin table escapes: \n, \| and \\. Step tables are never read here.
            cells = [re.sub(r"\\([n|\\])", lambda m: "\n" if m[1] == "n" else m[1], cell.strip())
                     for cell in re.split(r"(?<!\\)\|", stripped)[1:-1]]
            if header is None:
                assert cells and all(cells) and len(cells) == len(set(cells)), f"Invalid Examples header: {feature}"
                header = cells
            else:
                assert len(cells) == len(header), f"Invalid Examples row: {feature} {stripped}"
                current["examples"].append(dict(zip(header, cells)))
        elif stripped and not stripped.startswith(("#", "@")):
            examples = False
            if re.match(r"(?:Given|When|Then|And|But|\*)\s", stripped):
                description = False
            elif current and description:
                current["description"].append(stripped)
            elif stripped.startswith("Feature:"):
                pending_tags = []
    assert result, f"No scenarios found: {feature}"
    for name, item in result.items():
        assert not item["outline"] or item["examples"], f"Outline without examples: {feature} {name}"
    return result




def inventory():
    return [{'feature': feature.relative_to(FEATURES).as_posix(), 'scenario': name, 'example': example}
            for feature in sorted(FEATURES.rglob('*.feature'))
            for name, item in scenarios(feature).items()
            for example in (item['examples'] if item['outline'] else [None])]


def case_key(case):
    # Task 2 may add explicit identity metadata without replacing behavioral examples.
    example = case['example']
    if example is not None:
        assert isinstance(example, dict) and all(isinstance(k, str) and isinstance(v, str) for k, v in example.items())
        example = {k: v for k, v in example.items() if k != 'scenarioId'}
    return json.dumps({'feature': case['feature'], 'scenario': case['scenario'], 'example': example}, sort_keys=True)


def verify_baseline(cases):
    baseline = json.loads((ROOT / 'docs/bdd-baseline.json').read_text(encoding='utf-8'))
    assert baseline['schemaVersion'] == 1 and baseline['cases'], 'Invalid BDD baseline'
    original = [case_key(c) for c in baseline['cases']]
    current = [case_key(c) for c in cases]
    assert len(original) == len(set(original)) and len(current) == len(set(current)), 'Duplicate expanded BDD case'
    assert set(original) <= set(current), 'Missing original BDD scenario/example; review an explicit baseline migration'


def status_set(value):
    values = value.split(',') if value else []
    assert all(re.fullmatch(r'[1-5]\d\d', item) for item in values) and len(values) == len(set(values)), 'Invalid status list'
    return set(map(int, values))


def read_tsv(path, columns):
    with path.open(encoding='utf-8', newline='') as stream:
        reader = csv.DictReader(stream, delimiter='\t')
        assert reader.fieldnames and len(reader.fieldnames) == len(set(reader.fieldnames)) and set(reader.fieldnames) == columns, f'Unexpected matrix columns: {path}'
        rows = list(reader)
    assert all(None not in row and all(value is not None for value in row.values()) for row in rows), f'Malformed matrix row: {path}'
    return rows


def verify_matrix(operations, cases):
    rows = read_tsv(ROOT / 'docs/acceptance-matrix.tsv', {'scenarioId', 'operationId', 'method', 'path', 'statuses', 'optionalStatuses', 'cause', 'feature', 'scenario', 'example'})
    by_name = {op['operationId']: op for op in operations}
    assert {row['operationId'] for row in rows} == set(by_name), 'Missing or stale matrix operation'
    ids, mapped, covered = set(), set(), set()
    for row in rows:
        assert re.fullmatch(r'INV\d{2}_[A-Za-z0-9_]+', row['scenarioId']) and row['scenarioId'] not in ids, 'Invalid or duplicate scenarioId'
        ids.add(row['scenarioId'])
        op = by_name[row['operationId']]
        assert (row['method'], row['path']) == (op['method'], op['path']), f'Stale matrix route: {row["scenarioId"]}'
        assert row['cause'].strip(), 'Missing exercised cause'
        guaranteed, optional = status_set(row['statuses']), status_set(row['optionalStatuses'])
        assert guaranteed and not guaranteed & optional and guaranteed | optional <= set(op['responses']), f'Undeclared/overlapping scenario statuses: {row["scenarioId"]}'
        key = case_key(dict(row, example=json.loads(row['example'])))
        assert key not in mapped, 'Expanded example linked more than once'
        mapped.add(key)
        covered.update((op['operationId'], status) for status in guaranteed)
    for row in framework_rows():
        assert re.fullmatch(r'INV\d{2}_[A-Za-z0-9_]+', row['scenarioId']) and row['scenarioId'] not in ids, 'Invalid or duplicate scenarioId'
        ids.add(row['scenarioId'])
        assert row['stage'] == 'framework' and re.fullmatch(r'[A-Z]+', row['method']) and row['path'].startswith('/'), 'Invalid framework route metadata'
        assert row['cause'].strip() and row['optionalStatuses'] == '' and status_set(row['statuses']) in ({404}, {405}), 'Invalid framework status intent'
        key = case_key(dict(row, example=json.loads(row['example'])))
        assert key not in mapped, 'Expanded example linked more than once'
        mapped.add(key)
    assert mapped == {case_key(case) for case in cases}, 'Missing or stale scenario/example links'
    declared = {(op['operationId'], status) for op in operations for status in op['responses']}
    gaps = read_tsv(ROOT / 'docs/acceptance-gaps.tsv', {'operationId', 'status', 'task', 'reason'})
    gap_pairs = set()
    for gap in gaps:
        assert re.fullmatch(r'INV-BDD-\d{2}', gap['task']) and gap['reason'].strip(), 'Gap needs an owning task and reason'
        statuses = status_set(gap['status'])
        assert len(statuses) == 1, 'A coverage gap identifies exactly one status'
        pair = (gap['operationId'], next(iter(statuses)))
        assert pair not in gap_pairs, 'Duplicate coverage gap'
        gap_pairs.add(pair)
    assert gap_pairs == declared - covered, 'Undocumented or stale operation/status gaps'
    return rows, gaps, len(covered)


def framework_rows():
    return read_tsv(ROOT / 'docs/framework-scenarios.tsv', {'scenarioId', 'stage', 'method', 'path', 'statuses', 'optionalStatuses', 'cause', 'feature', 'scenario', 'example'})


def verify_generation_sources():
    project = ROOT / 'tests/ECommerceStoreInvoice.Acceptance.Tests/ECommerceStoreInvoice.Acceptance.Tests.csproj'
    assert not re.search(r'ReqnrollFeatureFile\b[^>]*\bRemove\s*=', source(project)), 'Do not remove acceptance feature inputs'
    for feature in FEATURES.rglob('*.feature'):
        assert not re.search(r'@(?:ignore|skip|pending)(?=\s|$)', source(feature), re.I), f'Disabled BDD source: {feature}'
    tracked = subprocess.check_output(['git', 'ls-files', '--', '*.feature.cs'], cwd=ROOT, text=True)
    assert not tracked.strip(), 'Generated .feature.cs files must not be tracked'


def verify_case_identity(item, example, expected):
    assert item['description'], 'Missing readable scenario description'
    prefix = '@allure.label.scenarioId:'
    tags = [tag[len(prefix):] for tag in item['tags'] if tag.startswith(prefix)]
    argument = (example or {}).get('scenarioId')
    assert len(tags) + (argument is not None) == 1, 'Specify scenarioId exactly once'
    actual = argument if argument is not None else tags[0]
    assert actual == expected, 'Scenario evidence identity differs from the matrix'


def verify_client_source(code):
    receivers = re.findall(r'\b(\w+)\.Create(?:Default)?Client\s*\(', code)
    assert all(receiver in {'context', '_apiContext'} for receiver in receivers) and not re.search(
        r'new\s+HttpClient\s*\(', code), 'BDD client bypasses shared HTTP evidence'


def verify_evidence_sources(rows):
    expected = {case_key(dict(row, example=json.loads(row['example']))): row['scenarioId'] for row in rows}
    for feature in FEATURES.rglob('*.feature'):
        for title, item in scenarios(feature).items():
            for example in (item['examples'] if item['outline'] else [None]):
                case = dict(feature=feature.relative_to(FEATURES).as_posix(), scenario=title, example=example)
                verify_case_identity(item, example, expected[case_key(case)])
    for path in FEATURES.rglob('*.cs'):
        if path.name.endswith('.feature.cs') or path.name in {'TestHooks.cs', 'ScenarioApiContext.cs'}:
            continue
        verify_client_source(source(path))
    # The one direct hook client is the read-only contract bootstrap, before the checked pipeline exists.
    hook = source(FEATURES / 'Common/TestHooks.cs')
    direct = re.findall(r'\b(\w+)\.Create(?:Default)?Client\s*\(', hook)
    assert direct.count('_factory') == 1 and all(receiver in {'_factory', '_apiContext'} for receiver in direct), 'Unexpected hook client bypass'



def generate():
    verify_generation_sources()
    cases = inventory()
    verify_baseline(cases)
    policies = registrations()
    published = source(API / "DocumentationEndpoints.cs")
    operations = []
    for file in sorted(API.glob("*Endpoints.cs")):
        code = source(file)
        group = re.search(r'MapGroup\("([^"]+)"\)', code)
        assert group, f"Missing route group in {file.name}"
        for verb, path, name, body, responses in endpoint_registrations(code, group.group(1)):
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
            operations.append({"operationId": name, "method": verb, "path": path, "flow": flow,
                               "policies": linked_policies, "responses": responses})
    names = [item["operationId"] for item in operations]
    assert names and len(names) == len(set(names)), f"Duplicate/empty operation inventory: {names}"
    rows, gaps, covered = verify_matrix(operations, cases)
    framework = framework_rows()
    verify_evidence_sources(rows + framework)
    for operation in operations:
        linked = [row for row in rows if row['operationId'] == operation['operationId']]
        operation['scenarios'] = sorted({'tests/ECommerceStoreInvoice.Acceptance.Tests/Features/' + row['feature'] for row in linked})
        operation['cases'] = [dict(scenarioId=row['scenarioId'], feature=row['feature'], scenario=row['scenario'],
                                   example=json.loads(row['example']), statuses=sorted(status_set(row['statuses'])),
                                   optionalStatuses=sorted(status_set(row['optionalStatuses'])), cause=row['cause']) for row in linked]
    framework_cases = [dict(scenarioId=row['scenarioId'], stage=row['stage'], method=row['method'], path=row['path'],
                            feature=row['feature'], scenario=row['scenario'], example=json.loads(row['example']),
                            statuses=sorted(status_set(row['statuses'])), cause=row['cause']) for row in framework]
    return {"operations": operations, "frameworkCases": framework_cases,
            "coverage": {"expandedBddCases": len(cases), "matrixCases": len(rows) + len(framework),
            "operationCases": len(rows), "frameworkCases": len(framework),
            "declaredOperationStatusPairs": sum(len(op['responses']) for op in operations),
            "guaranteedScenarioStatusPairs": covered, "gaps": gaps}}


if __name__ == "__main__":
    try:
        document = generate()
        if len(sys.argv) > 1:
            Path(sys.argv[1]).write_text(json.dumps(document, indent=2) + "\n", encoding="utf-8")
        coverage = document['coverage']
        print(f"Linked {len(document['operations'])} operations and {coverage['matrixCases']} expanded BDD cases; "
              f"{coverage['guaranteedScenarioStatusPairs']}/{coverage['declaredOperationStatusPairs']} declared status pairs have guaranteed scenario intent; "
              f"{len(coverage['gaps'])} documented gaps.")
    except (AssertionError, KeyError, OSError, ValueError) as error:
        print(f"Operation links drift: {error}", file=sys.stderr)
        sys.exit(1)
