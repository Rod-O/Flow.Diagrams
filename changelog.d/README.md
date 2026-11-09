# Changelog fragments

Every pull request that changes what users of the packages see adds one fragment here instead of editing
`CHANGELOG.md`. Fragments never conflict, so parallel branches merge cleanly; a release gathers them.

**Name:** `<branch>.<type>.md`, where type is `added`, `changed`, `fixed` or `removed`. A branch with changes of
several types adds one file per type (`fix-routing.fixed.md`, `fix-routing.removed.md`).

**Content:** one or more Markdown bullets, written for someone upgrading: what changed and what it means for them.
Mark breaking changes.

```markdown
- `Rectangle.Overlap` removed; use `Rectangle.Intersects`, which gives the same result. Breaking.
```

**No fragment** for changes users do not see (tests, CI, the catalog's own code): label the pull request
`no-changelog`.

**At release:** `tools/prepare-release.py <version>` moves the fragments into a new `CHANGELOG.md` section, sets
`<Version>` in `src/Directory.Build.props` and deletes the fragments. Commit, push, then tag `v<version>`.
