param([int]$TargetPid)
$ErrorActionPreference='Stop'
$root=Split-Path -Parent (Split-Path -Parent $PSScriptRoot)
$evidence=Join-Path $root 'qa\evidence\v1-governor-final'
$process=Get-Process -Id $TargetPid
$deadline=$process.StartTime.AddSeconds(635)
$manifest=[ordered]@{pid=$TargetPid;started=$process.StartTime.ToString('o');executable=$process.Path;desktopDllHash=(Get-FileHash (Join-Path (Split-Path $process.Path) 'MainPCDoctor.Desktop.dll')).Hash}
$manifest | ConvertTo-Json | Set-Content (Join-Path $evidence 'production-runtime-manifest.json')
do {
    $process.Refresh()
    if($process.HasExited){throw 'Production process exited during observation'}
    $row=python -c "import sqlite3,os,json; c=sqlite3.connect('file:'+os.path.join(os.environ['APPDATA'],'MainPCDoctor','mainpc.db').replace(chr(92),'/')+'?mode=ro',uri=True); c.row_factory=sqlite3.Row; print(json.dumps({'count':c.execute('select count(*) from metrics_samples_1min').fetchone()[0],'last':dict(c.execute('select sampled_at,heartbeat_at,cpu_avg_pct,mem_available_gb_avg,self_private_bytes,monitoring_state from metrics_samples_1min order by id desc limit 1').fetchone())}))"
    if($LASTEXITCODE -ne 0){throw 'DB observation failed'}
    [ordered]@{at=(Get-Date -Format o);pid=$TargetPid;cpuSeconds=$process.TotalProcessorTime.TotalSeconds;privateBytes=$process.PrivateMemorySize64;workingSet=$process.WorkingSet64;db=($row|ConvertFrom-Json)} | ConvertTo-Json -Depth 5 -Compress | Add-Content (Join-Path $evidence 'production-observation.jsonl')
    if((Get-Date) -ge $deadline){break}
    Start-Sleep -Seconds 30
} while($true)
