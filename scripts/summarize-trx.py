#!/usr/bin/env python3
"""Validate and display the counters from one .NET test run."""

import sys
import xml.etree.ElementTree as ET
from pathlib import Path

if len(sys.argv) not in (3, 4):
    raise SystemExit("Usage: summarize-trx.py SUITE RESULTS_DIR [SUMMARY_FILE]")

suite, directory = sys.argv[1], Path(sys.argv[2])
files = list(directory.rglob("*.trx"))
if len(files) != 1:
    raise SystemExit(f"Expected one TRX for {suite}, found {len(files)} in {directory}")

root = ET.parse(files[0]).getroot()
counters = root.find(".//{*}Counters")
if counters is None:
    raise SystemExit(f"Missing test counters in {files[0]}")
total, passed, failed, skipped = (
    int(counters.get(name, "0")) for name in ("total", "passed", "failed", "notExecuted")
)
summary = (
    "| Suite | Total | Passed | Failed | Skipped |\n"
    "| --- | ---: | ---: | ---: | ---: |\n"
    f"| {suite} | {total} | {passed} | {failed} | {skipped} |\n"
)
print(summary)
if len(sys.argv) == 4 and sys.argv[3]:
    with Path(sys.argv[3]).open("a", encoding="utf-8") as output:
        output.write(summary)

if total == 0 or passed == 0 or failed != 0:
    raise SystemExit(f"Invalid test result for {suite}: total={total}, passed={passed}, failed={failed}")
