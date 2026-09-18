# MainPC Doctor — Product Planning Pack

**Status: Revised — Awaiting Implementation Authorization**
**Date: 2026-09-17**
**Version: V1 Revision 01 (post Gate Review)**

---

## Documents

| # | Document | Summary |
|---|---|---|
| 01 | [Product Definition](01_PRODUCT_DEFINITION.md) | What the product is, who it's for, success metrics |
| 02 | [Product Principles](02_PRODUCT_PRINCIPLES.md) | 10 governing principles for all design/engineering decisions |
| 03 | [User Flow](03_USER_FLOW.md) | User flows with ASCII diagrams |
| 04 | [Information Architecture](04_INFORMATION_ARCHITECTURE.md) | Navigation structure, screen hierarchy, screen inventory |
| 05 | [System Architecture](05_SYSTEM_ARCHITECTURE.md) ★ | 4-project structure; Background in Desktop; single process |
| 06 | [Monitoring Model](06_MONITORING_MODEL.md) ★ | Metrics, Windows APIs, sampling rates, sensor availability |
| 07 | [Diagnosis Rules](07_DIAGNOSIS_RULES.md) ★ | 7 rules; CPU 300s gate; RAM scaled threshold; all corrected |
| 08 | [Incident Model](08_INCIDENT_MODEL.md) ★ | Lifecycle, data model, notification levels per type |
| 09 | [Upgrade Recommendation Model](09_UPGRADE_RECOMMENDATION_MODEL.md) ★ | CPU + RAM only in V1; scoring model |
| 10 | [Notification Policy](10_NOTIFICATION_POLICY.md) ★ | Level 2 (RAM only) + Level 4 (CPU/RAM upgrade rec only) |
| 11 | [Background Runtime](11_BACKGROUND_RUNTIME.md) ★ | 3-state scheduler; corrected WPF host setup |
| 12 | [Visual Design System](12_VISUAL_DESIGN_SYSTEM.md) | Color palette, typography, components, glitch effects |
| 13 | [Screen Spec](13_SCREEN_SPEC.md) ★ | 6 screens; System Capacity: CPU/RAM recs, GPU/Disk status only |
| 14 | [Data Model](14_DATA_MODEL.md) ★ | Schema without 1-hour aggregate table; CPU/RAM upgrade recs only |
| 15 | [Privacy and Security](15_PRIVACY_AND_SECURITY.md) | What is/isn't collected; local-only policy |
| 16 | [Performance Budget](16_PERFORMANCE_BUDGET.md) ★ | Developer Performance Regression Tests section renamed/clarified |
| 17 | [Windows Technical Plan](17_WINDOWS_TECHNICAL_PLAN.md) ★ | DXGI VRAM fix; PDH lifecycle; WPF host setup corrected |
| 18 | [Test Strategy](18_TEST_STRATEGY.md) ★ | Aligned with all rule changes; removed non-V1 test categories |
| 19 | [WBS](19_WBS.md) ★ | **Reduced: 7 phases, 47 tasks** (was 13 phases, 125 tasks) |
| 20 | [MVP Scope](20_MVP_SCOPE.md) ★ | Corrected scope; acceptance criteria aligned with final rules |

★ = Revised in Gate Review 01 (2026-09-17)

---

## Key Artifacts Embedded in Documents

| Artifact | Location |
|---|---|
| ASCII architecture diagram | [05_SYSTEM_ARCHITECTURE.md](05_SYSTEM_ARCHITECTURE.md) |
| Screen wireframes (6 screens) | [13_SCREEN_SPEC.md](13_SCREEN_SPEC.md) |
| Diagnosis decision table | [07_DIAGNOSIS_RULES.md](07_DIAGNOSIS_RULES.md) |
| Notification severity matrix | [10_NOTIFICATION_POLICY.md](10_NOTIFICATION_POLICY.md) |
| WBS (7 phases, 47 tasks) | [19_WBS.md](19_WBS.md) |
| MVP acceptance criteria | [20_MVP_SCOPE.md](20_MVP_SCOPE.md) |
| Gate Review | [review/FOUNDER_PLANNING_GATE_REVIEW.md](review/FOUNDER_PLANNING_GATE_REVIEW.md) |
| Revision Record | [review/FOUNDER_PLANNING_GATE_REVISION_01.md](review/FOUNDER_PLANNING_GATE_REVISION_01.md) |

---

## Key Decisions (Post-Revision)

| Decision | Choice | Rationale |
|---|---|---|
| V1 upgrade recommendations | CPU and RAM only | GPU/VRAM/Disk require external data or complex disambiguation |
| CPU notification | Silent (Level 0) | 300s gate + no immediate toast; feeds upgrade rec only |
| GPU notification | Always silent | GPU at 99% during gaming is normal, not a problem |
| RAM notification | Level 2 (warning toast) | The only component warranting an immediate user alert |
| CPU confirmation gate | 300 seconds | Eliminates developer build false positives (40s builds) |
| VRAM confirmation gate | 180 seconds | Eliminates level-load false positives in games |
| VRAM total source | DXGI `DedicatedVideoMemory` | WMI `AdapterRAM` overflows on GPUs > 4 GB |
| RAM threshold | max(2.0 GB, totalRam × 5%) | Capacity-aware; 32 GB system gets higher absolute threshold |
| Process ranking | CPU + RAM only | `System.Diagnostics.Process` has no disk I/O properties |
| Project count | 4 source projects | Background merged into Desktop; 5-project was unnecessary |
| Sampling states | 3 (Eco / Normal / Incident) | Watch Mode removed; adds complexity with no measurable benefit |
| 1-hour aggregate table | Removed | No V1 consumer; unnecessary schema complexity |
| Settings live reload | Not in V1 | FileSystemWatcher removed; settings apply on restart |

---

**STOP. Do not implement until Founder reviews and authorizes implementation.**
