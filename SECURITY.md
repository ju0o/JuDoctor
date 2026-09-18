# Security Policy

## Supported versions

The current public `v1.0.x` line receives security/privacy fixes while it remains the active stable release line.

## Reporting a vulnerability

Please do **not** post a working exploit, sensitive local data, or a vulnerability that could expose user information in a public issue.

For now, open a GitHub issue with only a minimal non-sensitive description and clearly mark it as a security report, or contact the maintainer through the private contact method published on the maintainer's GitHub profile if available.

Include only what is necessary to reproduce or assess the issue. Do not attach another person's logs, database, credentials, tokens, or private files.

## Security boundaries in V1

JuDoctor V1 is intended to:

- run in the current Windows user session
- avoid requiring administrator privileges for normal installation/use
- keep monitoring history local
- avoid collecting file/browser/terminal contents
- avoid automatic process killing, registry optimization, overclocking, or driver modification

A report that shows JuDoctor violating one of these boundaries is treated seriously.

## Dependency / release integrity

Official binary releases are distributed through this repository's GitHub Releases page. Each installer should be accompanied by a SHA256 file so users can verify download integrity.
