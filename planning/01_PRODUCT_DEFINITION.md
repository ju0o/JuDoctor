# 01 — Product Definition

## Product Name
**MainPC Doctor**

## Version
V1.0 — Windows MVP

## Tagline
> Your PC's silent guardian. It watches so you don't have to.

---

## Problem Statement

When a PC feels slow, the user's only option today is:
1. Open Task Manager manually
2. Try to catch the culprit in real time
3. Guess whether the slowdown is temporary or a recurring hardware limitation

This workflow fails because:
- The user opens Task Manager *after* the peak passes
- A single snapshot is not evidence of a hardware bottleneck
- There is no historical context, no interpretation, no guidance

**MainPC Doctor solves this** by running continuously in the background, accumulating evidence over time, and answering two distinct questions:

> "Why is my PC slow right now?"

> "Is my hardware the actual limiting factor, or is this a temporary event?"

---

## Target User

### Primary
**Power users on Windows** who work with demanding workloads: developers, designers, content creators, gamers, researchers.

Characteristics:
- Notices PC slowdowns and wants answers
- Does not want to manually monitor Task Manager
- Technically aware but not a hardware engineer
- Values clear explanation over raw numbers

### Secondary
**Non-technical users** who want to understand whether a hardware upgrade is warranted.

---

## Core Value Proposition

| Without MainPC Doctor | With MainPC Doctor |
|---|---|
| Guesses why PC was slow | Gets evidence-backed diagnosis |
| Opens Task Manager manually | Background monitoring is automatic |
| No history of past slowdowns | Incidents tracked over 30 days |
| Buys hardware based on frustration | Gets data-driven upgrade guidance |
| Alarmed by every CPU spike | Understands what is normal vs. problematic |

---

## What MainPC Doctor Is

- A **background hardware monitoring agent** for Windows
- An **incident detection system** that identifies meaningful slowdowns
- A **diagnosis engine** that classifies bottleneck types
- An **upgrade recommendation engine** that requires sustained historical evidence
- A **"Why Was My PC Slow?"** retrospective analysis tool
- A **system tray application** that is silent by default

---

## What MainPC Doctor Is Not

- Not a PC optimizer or registry cleaner
- Not an always-visible performance overlay
- Not a benchmark suite
- Not a remote monitoring server
- Not a cloud service or SaaS product
- Not an AI chatbot or LLM-driven advisor
- Not a hardware shopping assistant
- Not a process killer or automatic optimizer
- Not a Linux or macOS application (V1)

---

## Success Metrics (V1)

| Metric | Target |
|---|---|
| App idle CPU usage | < 1% average |
| App idle RAM footprint | < 100 MB |
| Notification false-positive rate | < 5% of notifications rated "unhelpful" |
| Incident detection recall | Detects > 90% of user-reported slowdowns in test |
| Upgrade recommendation precision | Evidence window ≥ 14 days before recommendation |
| Background service availability | Restarts cleanly after crash/reboot |
| User opens "Why Slow?" and finds useful answer | > 80% satisfaction in early testing |

---

## Platforms

| Platform | V1 | Future |
|---|---|---|
| Windows 11 | ✅ Primary | — |
| Windows 10 | Consider | — |
| Linux | ❌ | Possible V2 |
| macOS | ❌ | Not planned |
