# 02 — Product Principles

These principles govern every design and engineering decision in MainPC Doctor.
When two valid approaches conflict, the principle that ranks higher wins.

---

## Principle 1 — Evidence Threshold
**Do NOT treat a temporary resource spike as a hardware problem.**

A single metric crossing a threshold for a brief moment is not evidence of a bottleneck.
Evidence requires:
- Sustained duration (not just a peak)
- Repeated occurrence (not just one event)
- Multiple corroborating signals (not just one metric)
- Absence of obvious alternative explanation (e.g., build, game launch)

**Violation example:**
> CPU 100% for 8 seconds during a compile → NOT a bottleneck signal

**Correct example:**
> CPU 90–100% repeatedly during normal daily workflow, while RAM and Disk have headroom → bottleneck candidate

This principle protects users from anxiety about normal PC behavior.

---

## Principle 2 — Background-First
**The application should be invisible during healthy operation.**

The main window must never appear uninvited.
The tray icon must remain subtle.
No floating overlay. No always-on-top widget. No persistent status bar.

The user should be able to work for days without seeing MainPC Doctor —
and that is the intended, correct behavior.

Visibility is reserved for:
- Explicit user action (open dashboard)
- Meaningful warning (Level 2+)
- Upgrade recommendation (Level 4)

---

## Principle 3 — Explainability
**Every diagnosis must trace back to specific observable metrics.**

There are no "magic scores."
Every conclusion shown to the user must be accompanied by:
- The specific metrics that triggered it
- The time window observed
- The thresholds that were exceeded
- The confidence level

A diagnosis the user cannot understand is a diagnosis they cannot trust.

---

## Principle 4 — Local-First Privacy
**All data stays on the user's machine. No exceptions in V1.**

No account required.
No telemetry upload.
No cloud sync.
No analytics callbacks.
No crash reporting to a server.

Process names and resource metrics are captured.
File contents, browser history, clipboard, and terminal output are never captured.

---

## Principle 5 — Minimal Footprint
**MainPC Doctor must never itself become the performance problem it is monitoring.**

Target:
- < 1% average CPU during normal background operation
- < 100 MB RAM footprint
- Minimal disk writes (aggregated writes, not per-second dumps)

Sampling must be adaptive:
- Healthy system → low-frequency sampling
- Anomaly suspected → temporarily increase sampling
- Incident resolved → return to low-frequency mode

---

## Principle 6 — Safe Language
**MainPC Doctor must not alarm users with aggressive or dismissive language.**

Avoid:
> "Your CPU is bad."
> "You must replace your RAM immediately."
> "Critical hardware failure."

Use:
> "Your CPU appears to be the limiting resource for this workload."
> "Based on repeated memory-pressure events, additional RAM is likely to improve performance."
> "Observation period: 14 days. Confidence: High."

Every recommendation must show its evidence.
Confidence must always be qualified, not absolute.

---

## Principle 7 — Platform Separation
**Windows-specific collection code must be isolated from the diagnosis engine.**

The diagnosis engine must operate on abstract metric types, not Windows API types.
A future Linux collector must be able to plug in without modifying diagnostic rules.

This is not about building Linux support now.
It is about not making Linux support impossible later.

```
Platform.Windows  →  ISystemMetricsCollector  →  Core.Diagnosis
                                                  Core.Incidents
                                                  Core.Recommendations
```

---

## Principle 8 — Deterministic Rules First
**V1 diagnosis is deterministic rule-based logic, not ML or LLM.**

Reasons:
- Reproducible — same inputs always produce same output
- Explainable — rules can be shown to the user
- Low overhead — no model inference required
- Local — no API dependency
- Auditable — thresholds are readable and configurable

LLM integration is explicitly deferred to a future version.

---

## Principle 9 — Rarity of Notification
**Notifications must be rare enough that users pay attention to them.**

Notification spam destroys trust.

A notification system that fires on every CPU spike will be:
1. Dismissed immediately
2. Disabled
3. Uninstalled

Each notification level must have strict preconditions.
Cooldown windows must prevent repetition within a session.
The user's notification preference must be respected absolutely.

---

## Principle 10 — Small V1
**Ship a focused MVP. Resist scope expansion.**

V1 proves the core loop:
monitor → detect → diagnose → notify → explain

Every feature not on the MVP list must be explicitly deferred.
No "while we're at it" additions.
No speculative infrastructure for features not yet designed.
