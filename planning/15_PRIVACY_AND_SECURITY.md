# 15 — Privacy and Security

---

## Privacy Model

MainPC Doctor is **local-first**. All data remains on the user's machine.

| Property | V1 Status |
|---|---|
| Account required | No |
| Internet connection required | No |
| Data uploaded to any server | Never |
| Telemetry or analytics | None |
| Crash reporting to server | None |
| Data shared with third parties | Never |

---

## What Is Collected

| Data Type | Collected | Why |
|---|---|---|
| CPU utilization % | Yes | Diagnosis |
| CPU temperature | Yes (if sensor available) | Thermal detection |
| CPU clock speed | Yes | Thermal throttle detection |
| RAM usage (GB values) | Yes | Diagnosis |
| Available RAM | Yes | Diagnosis |
| Commit charge | Yes | Diagnosis |
| Pagefile activity flag | Yes | Diagnosis |
| Disk utilization % | Yes | Diagnosis |
| Disk latency (ms) | Yes | Diagnosis |
| GPU utilization % | Yes (if GPU detected) | Diagnosis |
| VRAM usage | Yes (if available) | Diagnosis |
| GPU temperature | Yes (if available) | Thermal detection |
| Process names | Yes | Attribution in incidents |
| Process CPU/RAM/Disk values | Yes | Attribution in incidents |
| System uptime | Yes | Context |

---

## What Is Explicitly NOT Collected

| Data Type | Policy | Rationale |
|---|---|---|
| File system contents | Never collected | Not needed for diagnosis |
| File paths or names | Never collected | Not needed |
| Browser history or URLs | Never collected | Private user data |
| Browser tab contents | Never collected | Private user data |
| Terminal / command text | Never collected | Command args not read |
| Process executable path | Not stored (name only) | Names are sufficient |
| Process command-line arguments | Never read | Could contain credentials |
| Clipboard contents | Never collected | Private user data |
| Keyboard or mouse input | Never collected | Not relevant |
| Network packet contents | Never collected | Not relevant |
| User account details | Never collected | No account system |
| Hardware serial numbers | Not stored | Not needed |
| Installed software list | Not collected | Not needed |
| Windows license key | Never collected | Not needed |
| Passwords or credentials | Never collected | Irrelevant |

---

## Process Name Policy

Process names are collected as diagnostic attribution (e.g., `chrome.exe`, `node.exe`).

Rules:
- Name only (`Process.ProcessName`) — no full path
- No command-line arguments (`Process.StartInfo.Arguments` is never read)
- PID is used only transiently (to aggregate samples to the same process); it is not stored long-term
- Process names are stored in the `incident_processes` table and nowhere else

A process name alone reveals the application type but not the user's activity within it.

---

## Local Data Storage

All data is stored at: `%LOCALAPPDATA%\MainPCDoctor\`

This directory is:
- Owned by the current Windows user
- Not accessible to other users on the machine without elevation
- Encrypted by Windows EFS if the user has enabled folder encryption

The application does not add any additional encryption in V1 (not warranted for metric data).

---

## No Elevation Required

MainPC Doctor runs as the current user.

It does NOT require or request:
- Administrator privilege
- UAC elevation
- Windows Service installation with SYSTEM account

All Windows APIs used (PDH, GlobalMemoryStatusEx, WMI limited queries) are accessible at user level.

GPU monitoring may require driver-level access (NVAPI). If unavailable at user level, the GPU section degrades gracefully.

---

## Network Policy

MainPC Doctor makes zero outbound network connections in V1.

There are no:
- Update check calls
- Telemetry endpoints
- License verification servers
- Cloud sync endpoints

Any future connectivity (opt-in update checks) would require:
- Explicit user consent
- Disclosure in settings
- Ability to disable

---

## Future Telemetry Design (V2 Consideration — NOT V1)

If opt-in anonymous telemetry is added in a future version:
- Must be explicitly opt-in (default off)
- Must show exactly what is sent before user enables it
- Must not include process names or any user-identifiable data
- Must be disableable at any time
- Must use a privacy-preserving aggregation approach

This section documents the principle only — it is not implemented in V1.

---

## Security Considerations

### Settings File
`settings.json` is readable by the current user. It contains only configuration preferences — no credentials, no sensitive data.

### SQLite Database
`metrics.db` is readable by the current user. It contains performance metrics and process names. No encryption is applied in V1 — the data is not sensitive in nature. Future versions may offer encryption if user demand warrants it.

### Startup Registry Key
`HKCU\SOFTWARE\Microsoft\Windows\CurrentVersion\Run\MainPCDoctor` points to the executable. This is standard Windows startup mechanism. The application verifies the registered path matches its own location on startup to detect accidental misconfiguration.

### No Privilege Escalation
The application never requests `SeDebugPrivilege` or any elevated Windows privilege. It uses only standard user-level APIs.

### DLL Hijacking Mitigation
The installer places the application in `%LOCALAPPDATA%` where the user has write access. The application directory should not be writable by other users. The installer should set appropriate ACLs. All DLLs are loaded from the application directory (no PATH-based loading).
