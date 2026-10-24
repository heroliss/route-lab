"""Copy only historical source/resources from a user-specified backup; never mutate it."""
from pathlib import Path
import argparse
import hashlib
import json
import shutil

parser = argparse.ArgumentParser()
parser.add_argument("source", type=Path)
args = parser.parse_args()
root = Path(__file__).resolve().parent.parent
target = root / "legacy" / "2016-original"
allow = {".cs", ".csproj", ".sln", ".resx", ".settings"}
skip = {"bin", "obj", ".vs", "packages"}
manifest = []
for source in sorted(args.source.rglob("*")):
    relative = source.relative_to(args.source)
    if not source.is_file() or source.suffix.lower() not in allow or any(p.lower() in skip for p in relative.parts):
        continue
    before = hashlib.sha256(source.read_bytes()).hexdigest()
    destination = target / relative
    destination.parent.mkdir(parents=True, exist_ok=True)
    shutil.copyfile(source, destination)
    after = hashlib.sha256(source.read_bytes()).hexdigest()
    copied = hashlib.sha256(destination.read_bytes()).hexdigest()
    assert before == after == copied, f"Source or copy changed: {relative}"
    manifest.append({"path": relative.as_posix(), "sha256": copied, "bytes": source.stat().st_size})
(root / "legacy" / "source-manifest.json").write_text(json.dumps({"source_relative": "2016年/旅行商问题", "import_date": "2026-10-24", "read_only_source": True, "files": manifest}, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
print(f"Copied and hash-verified {len(manifest)} legacy source/resource files; source unchanged.")
