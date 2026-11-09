#!/usr/bin/env python3
"""Prepares a release: gathers changelog.d/ fragments into CHANGELOG.md and sets <Version>.

    tools/prepare-release.py 1.0.0-alpha.1            # writes CHANGELOG.md, src/Directory.Build.props, deletes fragments
    tools/prepare-release.py 1.0.0-alpha.1 --dry-run  # prints the new CHANGELOG section, changes nothing

Then commit, push, and tag vX.Y.Z: the release workflow takes its notes from the new section.
"""
import argparse
import datetime
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
FRAGMENTS = ROOT / "changelog.d"
CHANGELOG = ROOT / "CHANGELOG.md"
PROPS = ROOT / "src" / "Directory.Build.props"
SECTIONS = {"added": "Added", "changed": "Changed", "fixed": "Fixed", "removed": "Removed"}


def fragments():
    found = {kind: [] for kind in SECTIONS}
    for path in sorted(FRAGMENTS.glob("*.md")):
        if path.name == "README.md":
            continue
        kind = path.stem.rsplit(".", 1)[-1]
        if kind not in SECTIONS:
            sys.exit(f"{path.name}: the type must be one of {', '.join(SECTIONS)} (name it <branch>.<type>.md)")
        lines = [line.rstrip() for line in path.read_text(encoding="utf-8").splitlines() if line.strip()]
        if not lines or not lines[0].startswith("- "):
            sys.exit(f"{path.name}: write one or more Markdown bullets (lines starting with '- ')")
        found[kind].append((path, lines))
    return found


def section(version, date, found):
    parts = [f"## [{version}] - {date}"]
    for kind, title in SECTIONS.items():
        if found[kind]:
            parts.append(f"### {title}\n\n" + "\n".join(line for _, lines in found[kind] for line in lines))
    return "\n\n".join(parts) + "\n"


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("version", help="the version to release, without the v, e.g. 1.0.0-alpha.1")
    parser.add_argument("--date", default=datetime.date.today().isoformat())
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    found = fragments()
    if not any(found.values()):
        sys.exit("changelog.d/ has no fragments: nothing to release")

    changelog = CHANGELOG.read_text(encoding="utf-8")
    if f"## [{args.version}]" in changelog:
        sys.exit(f"CHANGELOG.md already has a section for {args.version}")

    new = section(args.version, args.date, found)
    if args.dry_run:
        print(new)
        return

    # The new section goes above the newest release, after the file's introduction.
    first = re.search(r"(?m)^## ", changelog)
    at = first.start() if first else len(changelog)
    CHANGELOG.write_text(changelog[:at] + new + "\n" + changelog[at:], encoding="utf-8")

    props = PROPS.read_text(encoding="utf-8-sig")
    props, count = re.subn(r"<Version>[^<]*</Version>", f"<Version>{args.version}</Version>", props)
    if count != 1:
        sys.exit(f"{PROPS}: expected exactly one <Version>")
    PROPS.write_text(props, encoding="utf-8-sig")

    for paths in found.values():
        for path, _ in paths:
            path.unlink()
    print(f"CHANGELOG.md and <Version> set for {args.version}; commit, push, then tag v{args.version}")


if __name__ == "__main__":
    main()
