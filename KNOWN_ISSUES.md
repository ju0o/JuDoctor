# Known Issues

## PUBLISH_SINGLEFILE_LIMITATION

**Status:** Accepted V1 limitation

The current WPF resource configuration is not reliable with `PublishSingleFile=true` because pack-URI resource lookup can fail in a single-file build.

### V1 production policy

JuDoctor V1 is distributed as a self-contained **folder publish wrapped by the installer**.

This does not affect normal installed operation.

---

## Hardware sensor availability

Some lower-level telemetry, especially temperatures, depends on hardware, drivers, and optional sensor-provider availability.

When a sensor is unavailable, JuDoctor should show it as unavailable rather than inventing `0` or another fake value.

---

## Recommendation warm-up

CPU/RAM upgrade recommendations require at least **14 days of observation**.

Before the minimum observation window is satisfied, System Capacity should remain in a collecting-data state rather than issuing a Level 4 upgrade recommendation.

---

## Unsigned early releases / SmartScreen

Early public installers may not yet have an established Windows publisher reputation and can trigger a SmartScreen warning. This is a distribution trust/reputation issue, not an intentional product behavior.

For official releases, compare the downloaded installer SHA256 with the `.sha256` file attached to the same GitHub Release.
