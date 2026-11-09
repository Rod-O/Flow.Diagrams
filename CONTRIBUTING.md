# Contributing

This applies to people and to coding agents alike.

- **One topic per pull request**, against `master`, with a description of what was wrong, what changed and how it is
  tested.
- **Tests first for fixes.** A fix comes with a test that fails without it.
- **Zero warnings.** `dotnet build Flow.Diagrams.sln -c Release --no-incremental` must report 0 warnings and 0 errors.
  Every public member is documented (`GenerateDocumentationFile` is on): keep the XML comments true when behaviour
  changes, and update the guides in `samples/Catalog/Guides` that describe it.
- **Changelog fragment, not CHANGELOG.md.** Add `changelog.d/<branch>.<type>.md`; see
  [changelog.d/README.md](changelog.d/README.md). CI fails a pull request that changes `src/` without one, unless it is
  labelled `no-changelog`.
- **Parallel worktrees:** do not use `git stash`; every worktree of a repository shares one stash.

## Checks before opening a pull request

```
dotnet build Flow.Diagrams.sln -c Release --no-incremental
dotnet test tests/Flow.Diagrams.Core.Tests
dotnet test tests/Flow.Diagrams.Tests
dotnet build samples/Catalog
```

When a change touches the catalog or the guides, run `tools/catalog-previews` (see its README): every example and
guide must load without errors.

## Releasing

`tools/prepare-release.py <version>` gathers the fragments into `CHANGELOG.md` and sets `<Version>`; commit, push and
tag `v<version>`. The release workflow tests, packs, creates the GitHub release from that CHANGELOG section and pushes
to nuget.org. Pre-release versions use dotted numbers (`1.0.0-alpha.1`) so that NuGet orders them correctly.
