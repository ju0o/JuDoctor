# Contributing to JuDoctor

Thanks for helping improve JuDoctor.

## Current contribution priority

The `v1.0.x` line is deliberately focused on stability and compatibility.

High-value contributions include:

- Windows hardware compatibility fixes
- crash fixes
- incorrect/false-positive diagnosis fixes
- installer/startup/reboot fixes
- database/recovery fixes
- performance regressions in the background monitor
- privacy/security issues
- deterministic tests for existing behavior
- documentation improvements

Please avoid large feature PRs against the stable line without first opening an issue and discussing the user problem.

## Before opening a PR

1. Open or reference an issue when the change is non-trivial.
2. Keep the change focused.
3. Add or update tests for behavioral changes.
4. Run the full test suite.
5. Avoid weakening an existing diagnosis or recommendation test simply to make the suite green.
6. Do not commit local databases, logs, secrets, signing material, generated installers, or private planning documents.

## Hardware compatibility reports

When reporting a compatibility problem, include Windows version, CPU/GPU model, installed RAM, JuDoctor version, and reproducible behavior.

Do not publish another person's private diagnostic data.

## Architecture principle

JuDoctor should remain quiet and lightweight in the background. A monitoring feature that materially changes CPU, memory, or disk overhead needs evidence that the added cost is justified.

## Licensing

By submitting a contribution, you agree that your contribution is provided under the repository's GPL-3.0 license unless a different written agreement is made before submission.
