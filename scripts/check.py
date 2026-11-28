"""Checks each mod's manifest.json and config.json, and that the README names
each mod's current version. The mods themselves need the game's DLLs to build,
so CI cannot build them."""
import json
import pathlib
import sys

root = pathlib.Path(__file__).resolve().parent.parent
readme = (root / "README.md").read_text()
need = ["Name", "Author", "Version", "UniqueID", "EntryDll", "MinimumApiVersion"]
bad = []
for manifest in sorted(root.glob("*/manifest.json")):
    mod = manifest.parent
    m = json.loads(manifest.read_text())
    bad += [f"{manifest}: no {k}" for k in need if not m.get(k)]
    if m.get("EntryDll") != f"{mod.name}.dll":
        bad.append(f"{manifest}: EntryDll is not {mod.name}.dll")
    if f"version {m.get('Version')}" not in readme:
        bad.append(f"README.md: does not say {m.get('Name')} is version {m.get('Version')}")
    for extra in mod.glob("config.json"):
        json.loads(extra.read_text())
if bad:
    sys.exit("\n".join(bad))
print("manifests ok")
