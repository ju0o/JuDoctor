# Privacy

JuDoctor V1 is designed as a **local-first Windows application**.

## What stays on your PC

JuDoctor stores monitoring history locally so it can compare short-term incidents with longer-term CPU/RAM pressure. V1 does not require a JuDoctor account or a cloud backend.

The application may locally record diagnostic metadata such as:

- timestamps
- CPU utilization / clock-related metrics
- RAM usage / available memory / commit pressure
- GPU utilization and available VRAM metrics
- disk activity / latency metrics
- process names and resource usage summaries used for diagnosis
- incident and recommendation state
- application health / scheduling state

## What JuDoctor does not need to collect

JuDoctor is not designed to collect or inspect:

- file contents
- personal documents
- browser page contents
- terminal or shell command text
- passwords or authentication secrets
- clipboard contents
- keystrokes
- screenshots

## Remote telemetry

V1 does **not** require remote telemetry upload. Monitoring data is intended to remain on the user's PC.

If a future version introduces any optional remote service, cloud sync, crash reporting, or analytics, that behavior must be documented separately and must not be silently added under this V1 policy.

## Local data location

V1 currently stores application data under:

`%APPDATA%\MainPCDoctor\`

The public product name is JuDoctor, but this internal path is intentionally left unchanged in V1 to avoid creating a data migration solely for branding.

## Bug reports

Before attaching logs, databases, or screenshots to a public GitHub issue, review them yourself. Even though JuDoctor minimizes collected content, diagnostic files may still reveal hardware names, process names, usernames embedded in file paths, or other machine-specific metadata.

## Questions

Use the repository's GitHub Issues for privacy-related questions. For a potential security vulnerability, follow [SECURITY.md](SECURITY.md) instead of posting exploit details publicly.
