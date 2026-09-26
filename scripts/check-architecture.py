#!/usr/bin/env python3
"""Verify project and source dependency boundaries without requiring .NET."""

import re
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PREFIX = "ECommerceStoreInvoice"
LAYERS = ("Domain", "Application", "Infrastructure", "API")
ALLOWED = {
    "Domain": set(),
    "Application": {"Domain"},
    "Infrastructure": {"Domain"},
    "API": {"Domain", "Application", "Infrastructure"},
}
COMPOSITION = {"Program.cs", "Configuration/Extensions/ServiceCollectionExtensions.cs"}
NON_CODE = re.compile(r'//[^\n]*|/\*[\s\S]*?\*/|@"(?:""|[^"])*"|\$?"(?:\\.|[^"\\])*"', re.M)
NAMESPACE = re.compile(r"\bECommerceStoreInvoice\.(Domain|Application|Infrastructure|API)(?:\.[\w]+)*")
STORAGE = re.compile(r"\b(?:MongoDB|Microsoft\.EntityFrameworkCore|Dapper)(?:\.[\w]+)*")


def code_without_literals(contents):
    return NON_CODE.sub(lambda match: "\n" * match.group().count("\n") + " ", contents)


def check(root):
    errors = []
    for layer in LAYERS:
        project = root / "src" / f"{PREFIX}.{layer}" / f"{PREFIX}.{layer}.csproj"
        if not project.exists():
            errors.append(f"missing project: {project}")
            continue
        tree = ET.parse(project)
        for ref in tree.findall(".//ProjectReference"):
            target = Path(ref.attrib["Include"].replace("\\", "/")).stem
            if target not in {f"{PREFIX}.{item}" for item in ALLOWED[layer]}:
                errors.append(f"{project.relative_to(root)}: forbidden project reference {target}")
        if layer in ("Domain", "Application"):
            for ref in tree.findall(".//PackageReference"):
                package = ref.attrib.get("Include", "")
                if package.startswith(("MongoDB", "Microsoft.EntityFrameworkCore", "Dapper")):
                    errors.append(f"{project.relative_to(root)}: persistence package {package}")
        for file in project.parent.rglob("*.cs"):
            relative = file.relative_to(project.parent).as_posix()
            if relative.startswith(("bin/", "obj/")):
                continue
            code = code_without_literals(file.read_text(encoding="utf-8-sig"))
            for match in NAMESPACE.finditer(code):
                target = match.group(1)
                if target == layer:
                    continue
                if target not in ALLOWED[layer] or (layer == "API" and target == "Infrastructure" and relative not in COMPOSITION):
                    errors.append(f"{file.relative_to(root)}:{code.count(chr(10), 0, match.start()) + 1}: forbidden {layer} -> {target}")
            if layer in ("Domain", "Application", "API"):
                for match in STORAGE.finditer(code):
                    errors.append(f"{file.relative_to(root)}:{code.count(chr(10), 0, match.start()) + 1}: persistence type outside Infrastructure")
    return errors


if __name__ == "__main__":
    violations = check(ROOT)
    for violation in violations:
        print(violation, file=sys.stderr)
    if violations:
        sys.exit(1)
    print("Architecture boundaries verified.")
