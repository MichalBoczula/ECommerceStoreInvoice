#!/usr/bin/env python3
"""Keep the Infrastructure coverage exclusion limited to generated Kiota code."""

import sys
import xml.etree.ElementTree as ET
from pathlib import Path

if len(sys.argv) != 2:
    raise SystemExit("Usage: check-infrastructure-coverage.py RESULTS_DIR")

reports = list(Path(sys.argv[1]).rglob("coverage.cobertura.xml"))
if not reports:
    raise SystemExit("Infrastructure Cobertura report is missing")

required = (
    "/Repositories/OrderRepository.cs",
    "/Repositories/InvoiceGenerationRepository.cs",
    "/Health/MongoReadinessHealthCheck.cs",
    "/Configuration/MongoInitializer.cs",
    "/ApiClients/Concret/Products/ExternalProductServiceClient.cs",
)

for report in reports:
    files = {
        item.get("filename", "").replace("\\", "/")
        for item in ET.parse(report).findall(".//class")
        if "ECommerceStoreInvoice.Infrastructure/" in item.get("filename", "").replace("\\", "/")
    }
    generated = [path for path in files if "/ApiClients/Abstract/Products/" in path]
    missing = [path for path in required if not any(name.endswith(path) for name in files)]
    if generated or missing:
        raise SystemExit(
            f"Invalid Infrastructure coverage scope in {report}: "
            f"generated={generated}, missing handwritten files={missing}"
        )

print(f"Checked {len(reports)} Infrastructure coverage report(s): generated Kiota excluded; handwritten behavior included")
