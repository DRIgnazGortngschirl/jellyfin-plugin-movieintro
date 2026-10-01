#!/bin/sh
# Builds the plugin and packs <name>_<version>.zip (dll + meta.json) the way Jellyfin installs it.
# usage: package.sh <version>
set -eu
VERSION=$1
dotnet publish Jellyfin.Plugin.MovieIntro -c Release -o out \
  -p:Version="$VERSION" -p:AssemblyVersion="$VERSION" -p:FileVersion="$VERSION"
python3 - "$VERSION" <<'PY'
import datetime, json, sys
m = json.load(open("meta.json"))
m["version"] = sys.argv[1]
m["timestamp"] = datetime.datetime.now(datetime.timezone.utc).strftime("%Y-%m-%dT%H:%M:%SZ")
json.dump(m, open("out/meta.json", "w"), indent=2)
PY
ZIP="movie-intro_${VERSION}.zip"
rm -f "$ZIP"
(cd out && zip -X "../$ZIP" Jellyfin.Plugin.MovieIntro.dll meta.json)
echo "$ZIP"
