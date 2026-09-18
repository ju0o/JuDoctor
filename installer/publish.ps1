# publish.ps1 — builds a self-contained win-x64 release and (optionally) compiles the installer
# Usage:  .\publish.ps1              (publish only)
#         .\publish.ps1 -Installer   (publish + compile installer, requires Inno Setup)

param([switch]$Installer)

$ErrorActionPreference = "Stop"
$root    = Split-Path $PSScriptRoot -Parent
$project = Join-Path $root "src\MainPCDoctor.Desktop\MainPCDoctor.Desktop.csproj"
$outDir  = Join-Path $root "src\MainPCDoctor.Desktop\bin\publish\win-x64"

Write-Host "==> Publishing self-contained win-x64 release..." -ForegroundColor Cyan
dotnet publish $project `
    --configuration Release `
    --runtime win-x64 `
    --self-contained true `
    -p:PublishTrimmed=false `
    --output $outDir

if ($LASTEXITCODE -ne 0) { Write-Error "Publish failed"; exit 1 }
Write-Host "==> Published to: $outDir" -ForegroundColor Green

if ($Installer) {
    $iscc = (Get-Command iscc.exe -ErrorAction SilentlyContinue)?.Source
    if (-not $iscc) {
        # Try the default Inno Setup install path
        $iscc = "C:\Program Files (x86)\Inno Setup 6\ISCC.exe"
    }
    if (-not (Test-Path $iscc)) {
        Write-Warning "Inno Setup (ISCC.exe) not found. Skipping installer build."
        Write-Warning "Install from: https://jrsoftware.org/isinfo.php"
    } else {
        Write-Host "==> Compiling installer..." -ForegroundColor Cyan
        & $iscc (Join-Path $PSScriptRoot "MainPCDoctor.iss")
        if ($LASTEXITCODE -ne 0) { Write-Error "Installer compile failed"; exit 1 }
        Write-Host "==> Installer output: installer\output\" -ForegroundColor Green
    }
}
