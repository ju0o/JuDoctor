import datetime as dt
import json
import os
import pathlib
import sqlite3

base = pathlib.Path('qa/evidence/v1-governor-final')
manifest = json.loads((base / 'production-runtime-manifest.json').read_text(encoding='utf-8-sig'))
start = dt.datetime.fromisoformat(manifest['started'])
now = dt.datetime.now(dt.timezone.utc)
path = pathlib.Path(os.environ['APPDATA']) / 'MainPCDoctor' / 'mainpc.db'
c = sqlite3.connect(path.as_uri() + '?mode=ro', uri=True)
c.row_factory = sqlite3.Row
rows = [dict(r) for r in c.execute('SELECT * FROM metrics_samples_1min ORDER BY id')]
rows = [r for r in rows if dt.datetime.fromisoformat(r['sampled_at']) >= start]
heartbeats = [dt.datetime.fromisoformat(r['heartbeat_at']) for r in rows]
sampled = [dt.datetime.fromisoformat(r['sampled_at']) for r in rows]
checks = {
    'at_least_10_minutes': (now-start).total_seconds() >= 600,
    'at_least_10_records': len(rows) >= 10,
    'normal_health': all(r['monitoring_state']=='normal' for r in rows),
    'private_memory_present': all(r['self_private_bytes'] is not None and r['self_private_bytes'] > 0 for r in rows),
    'heartbeat_current': bool(heartbeats) and (now-heartbeats[-1]).total_seconds() < 90,
    'no_duplicate_aggregates': len(sampled) == len(set(sampled)),
    'record_gaps_bounded': bool(sampled) and all(59 <= (b-a).total_seconds() <= 95 for a,b in zip(sampled,sampled[1:])),
    'minimum_fields_present': all(r['mem_total_gb'] > 0 and r['mem_available_gb_avg'] >= 0 and 0 <= r['cpu_avg_pct'] <= 100 for r in rows),
    'integrity_ok': c.execute('PRAGMA quick_check').fetchone()[0] == 'ok',
}
result = {'checked_at':now.isoformat(), 'started':start.isoformat(), 'elapsed_seconds':(now-start).total_seconds(),
          'rows':len(rows), 'first_heartbeat':heartbeats[0].isoformat() if heartbeats else None,
          'last_heartbeat':heartbeats[-1].isoformat() if heartbeats else None, 'checks':checks,
          'pass':all(checks.values())}
(base / 'production-result.json').write_text(json.dumps(result,indent=2))
(base / 'production-rows.json').write_text(json.dumps(rows,indent=2))
print(json.dumps(result))
raise SystemExit(0 if result['pass'] else 1)
