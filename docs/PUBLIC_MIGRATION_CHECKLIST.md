# Public Source Migration Checklist

Use this checklist when moving the certified private/local V1 implementation into `ju0o/JuDoctor`.

## Include

- `src/`
- `tests/`
- `installer/`
- solution/project files
- public build scripts
- public documentation
- required open-source notices

## Exclude

- private planning/PM documents
- raw QA evidence containing local machine paths or private metadata
- local SQLite databases
- logs
- `.env` files
- API keys/tokens
- signing certificates/keys
- generated installers/binaries
- screenshots containing private information
- temporary agent/handoff files

The repository `.gitignore` already blocks common forms of these files, but do not rely on `.gitignore` as the only privacy check.

## Branding policy for V1

Prefer a safe visible rebrand instead of a deep internal rename.

Safe V1 candidates:

- product title → `JuDoctor`
- Dashboard brand → `JuDoctor`
- tray tooltip → `JuDoctor`
- installer display name → `JuDoctor`
- executable metadata/description → `JuDoctor`

Avoid renaming stable internal namespaces/data paths solely for cosmetics if that increases V1 regression risk.

In particular, `%APPDATA%\MainPCDoctor\` may remain the V1 storage path until a deliberate data migration is designed.

## Before pushing source

Run searches for sensitive/local-only strings, for example:

```powershell
git grep -n -i "password\|token\|secret\|api[_-]key"
git grep -n "C:\\Users\\"
git status --ignored
```

Review every matched line manually; these searches produce false positives and are not a substitute for inspection.

## Dependency / license audit

Before publishing the binary release:

- inventory all direct NuGet/packages
- record license for each direct dependency
- verify redistribution obligations
- include required notices
- replace the repository's short GPL notice with the canonical full GPL-3.0 license text

## Post-migration certification

After migrating/rebranding the public source:

1. clean restore/build
2. full automated tests
3. production folder publish
4. installer compile
5. manual launch
6. tray close/reopen/exit
7. `--tray` startup
8. single-instance check
9. DB writes continue
10. no premature upgrade recommendation

Do not treat the private/local certification as proof that a changed public/rebranded tree is still identical.

## Release

When all blockers in GitHub Issue #1 are complete, tag `v1.0.0`. The release workflow will test/build the tagged source, generate the installer SHA256, and publish both as GitHub Release assets.
