#!/usr/bin/env python3
"""Writes meta.json for the release zip and adds the release to manifest.json (the Jellyfin repository file).

usage: update_manifest.py <version> <zip path> <zip download url> <changelog>
"""
import datetime
import hashlib
import json
import pathlib
import sys

version, zip_path, source_url, changelog = sys.argv[1:5]
meta = json.loads(pathlib.Path("meta.json").read_text())

checksum = hashlib.md5(pathlib.Path(zip_path).read_bytes()).hexdigest()
timestamp = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")

manifest_path = pathlib.Path("manifest.json")
manifest = json.loads(manifest_path.read_text()) if manifest_path.exists() else []
package = next((p for p in manifest if p["guid"] == meta["guid"]), None)
if package is None:
    package = {"guid": meta["guid"], "versions": []}
    manifest.append(package)

package.update({
    "name": meta["name"],
    "description": meta["description"],
    "overview": meta["overview"],
    "owner": meta["owner"],
    "category": meta["category"],
})
package["versions"] = [v for v in package["versions"] if v["version"] != version]
package["versions"].insert(0, {
    "version": version,
    "changelog": changelog,
    "targetAbi": meta["targetAbi"],
    "sourceUrl": source_url,
    "checksum": checksum,
    "timestamp": timestamp,
})

manifest_path.write_text(json.dumps(manifest, indent=2) + "\n")
print(f"manifest.json: {meta['name']} {version} md5={checksum}")
