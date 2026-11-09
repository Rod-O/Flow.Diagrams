#!/usr/bin/env bash
# Builds the API reference into apidocs/_site. DocFX reads the compiled assemblies, not the projects, because it does not
# run the Razor source generator: from source, every component's partial class loses its ComponentBase base.
set -euo pipefail
cd "$(dirname "$0")"
rm -rf bin api _site
for p in Flow.Diagrams.Core Flow.Diagrams Flow.Diagrams.Algorithms; do
  dotnet build "../src/$p" -c Release -f net10.0 -o bin -p:GenerateDocumentationFile=true -p:CopyLocalLockFileAssemblies=true -v q -nologo
done
# The shared runtime, not the reference packs: on Linux DocFX resolves System.Runtime from the installed runtime, whose
# facade forwards to System.Private.CoreLib, which only the runtime folder has.
root="$(dotnet --list-sdks | tail -1 | sed -E 's/.*\[(.*)\]/\1/')/.."
mkdir -p bin/refs
for framework in Microsoft.NETCore.App Microsoft.AspNetCore.App; do
  cp "$(ls -d "$root/shared/$framework"/10.* | sort -V | tail -1)"/*.dll bin/refs/
done
docfx docfx.json --warningsAsErrors
