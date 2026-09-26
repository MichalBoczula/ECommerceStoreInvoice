#!/usr/bin/env python3
"""Exercise the architecture gate against an intentionally invalid dependency."""

import importlib.util
import tempfile
from pathlib import Path

path = Path(__file__).with_name("check-architecture.py")
spec = importlib.util.spec_from_file_location("architecture", path)
module = importlib.util.module_from_spec(spec)
spec.loader.exec_module(module)

assert not module.check(module.ROOT), "The repository already violates architecture boundaries"
with tempfile.TemporaryDirectory() as directory:
    root = Path(directory)
    for layer in module.LAYERS:
        target = root / "src" / f"{module.PREFIX}.{layer}"
        target.mkdir(parents=True)
        (target / f"{module.PREFIX}.{layer}.csproj").write_text("<Project />")
    domain = root / "src" / f"{module.PREFIX}.Domain"
    (domain / "Leaking.cs").write_text("using ECommerceStoreInvoice.Infrastructure.Repositories;")
    assert any("forbidden Domain -> Infrastructure" in error for error in module.check(root))
print("Architecture gate regression verified.")
