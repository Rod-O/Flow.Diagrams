#!/usr/bin/env bash
# Builds the API reference into apidocs/_site. DocFX reads the compiled assemblies, not the projects, because it does not
# run the Razor source generator: from source, every component's partial class loses its ComponentBase base.
set -euo pipefail
cd "$(dirname "$0")"
rm -rf bin api _site
for p in Flow.Diagrams.Core Flow.Diagrams Flow.Diagrams.Algorithms; do
  dotnet build "../src/$p" -c Release -f net10.0 -o bin -p:GenerateDocumentationFile=true -p:CopyLocalLockFileAssemblies=true -v q -nologo
done
root="$(dotnet --list-sdks | tail -1 | sed -E 's/.*\[(.*)\]/\1/')/.."
mkdir -p bin/refs
for pack in Microsoft.NETCore.App.Ref Microsoft.AspNetCore.App.Ref; do
  cp "$(ls -d "$root/packs/$pack"/10.* | sort -V | tail -1)"/ref/net10.0/*.dll bin/refs/
done
docfx docfx.json --warningsAsErrors
