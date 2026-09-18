# JuDoctor Roadmap

This roadmap deliberately prioritizes **finishing, stability, and real-user evidence** over rapid feature expansion.

## V1.0 — Public Windows Release

Goal: make the certified Windows build safely downloadable and usable by other people.

- [x] Create public `ju0o/JuDoctor` repository
- [x] Add public release documentation
- [x] Define GitHub Release artifact structure
- [ ] Migrate sanitized V1 source
- [ ] Complete third-party license audit
- [ ] Apply safe visible-brand rebranding from MainPC Doctor → JuDoctor
- [ ] Re-run the full test suite after public-source migration/rebranding
- [ ] Build the final `JuDoctor-1.0.0-Setup.exe`
- [ ] Publish SHA256 alongside the installer
- [ ] Add real screenshots
- [ ] Tag `v1.0.0`
- [ ] Publish GitHub Release v1.0.0
- [ ] Collect compatibility feedback from 5–20 real external users

## V1.0.x — Stability only

Allowed work:

- crashes
- false-positive diagnoses
- missing telemetry on supported Windows PCs
- installer / reboot / startup defects
- data corruption
- security/privacy defects
- hardware compatibility fixes

Avoid unrelated feature expansion in this release line.

## V1.1 — Only after repeated external-user evidence

Candidate areas, not commitments:

- improved hardware compatibility
- localization polish
- clearer incident explanations
- exportable diagnostic report
- notification grouping
- UX improvements proven necessary by real users

A candidate moves into V1.1 only when real-world feedback shows a repeated problem.

## Commercialization gate

Do not build a large paid tier until a repeated willingness-to-pay signal appears.

Potential paid wedges to validate later:

- longer/advanced history and reports
- shareable technician reports
- multi-PC management
- business/fleet visibility
- deeper recommendation workflows

Choose **one** paid wedge, validate demand, and only then expand.

## Later

Linux may be researched after the Windows product has stable external users. macOS is later still.
