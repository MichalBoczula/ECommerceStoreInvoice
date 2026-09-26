#!/usr/bin/env python3
"""Publish and validate the counters from one .NET test run."""

import os
import sys
import xml.etree.ElementTree as ET
from pathlib import Path

suite, directory = sys.argv[1], Path(sys.argv[2])
files = list(directory.rglob("*.trx"))
if len(files) != 1:
    raise SystemExit(f"Expected one TRX for {suite}, found {len(files)} in {directory}")

root = ET.parse(files[0]).getroot()
counters = root.find(".//{*}Counters")
if counters is None:
    raise SystemExit(f"No test counters in {files[0]}")
total, passed, failed, skipped = (
    int(counters.get(name, "0")) for name in ("total", "passed", "failed", "notExecuted")
)
summary = f"| Suite | Total | Passed | Failed | Skipped |\n|---|---:|---:|---:|---:|\n| {suite} | {total} | {passed} | {failed} | {skipped} |\n"
print(summary)
if "GITHUB_STEP_SUMMARY" in os.environ:
    with open(os.environ["GITHUB_STEP_SUMMARY"], "a", encoding="utf-8") as output:
        output.write(summary)
if total == 0 or passed == 0 or failed != 0:
    raise SystemExit(f"Invalid test result for {suite}: total={total}, passed={passed}, failed={failed}")
