# JuDoctor Documentation

This directory contains the public documentation required to install, build, audit, and release JuDoctor.

## User documentation

- [Installation](INSTALLATION.md) — download, install, SmartScreen/checksum guidance, startup behavior and uninstall.
- [README (English)](../README.md) — product overview and primary download path.
- [README (Korean)](../README_KO.md) — Korean product overview and download path.
- [Known issues](../KNOWN_ISSUES.md) — accepted V1 limitations.
- [Privacy](../PRIVACY.md) — local-first data and privacy behavior.

## Developer / release documentation

- [Releasing JuDoctor](RELEASING.md) — v1.0.x release procedure and GitHub Actions path.
- [Third-party licenses](THIRD_PARTY_LICENSES.md) — shipping dependency/license audit and release security notes.
- [Public migration checklist](PUBLIC_MIGRATION_CHECKLIST.md) — source-publication safety checklist.
- [Screenshots](screenshots/README.md) — public screenshot requirements and expected filenames.

## V1 public source layout

```text
src/                 application source
tests/               automated tests
installer/           self-contained publish + Inno Setup packaging
docs/                public user/release documentation
planning/            product and architecture documentation
qa/                  curated certification reports and QA tools
.github/workflows/    release automation
```

Raw machine-local QA evidence is intentionally excluded from the current public tree.
