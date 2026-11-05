# Crea gli eseguibili delle utility e li impacchetta in dist\<utility>.zip (tracciati con Git LFS).
#   powershell -ExecutionPolicy Bypass -File build-all.ps1            # tutte
#   powershell -ExecutionPolicy Bypass -File build-all.ps1 22         # solo quelle indicate (prefisso)
#
# Lo zip contiene: i due .exe, _internal\, config\settings.default.json, README.md e il manuale Word
# (da manuali\). Mai settings.local.json, data\ o logs\: sono di chi usa la utility.
# Le utility senza .venv si preparano prima con il loro setup.ps1.
param([string[]]$Solo)
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

$Utility = Get-ChildItem -Directory | Where-Object {
    $dir = $_
    (Test-Path (Join-Path $dir.FullName "build-exe.ps1")) -and
        (-not $Solo -or @($Solo | Where-Object { $dir.Name.StartsWith($_) }).Count -gt 0)
}

New-Item -ItemType Directory -Force "dist" | Out-Null
$Esiti = @()
foreach ($u in $Utility) {
    Write-Host "`n=== $($u.Name)" -ForegroundColor Cyan
    try {
        if (-not (Test-Path (Join-Path $u.FullName ".venv"))) {
            & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $u.FullName "setup.ps1")
            if ($LASTEXITCODE -ne 0) { throw "setup.ps1 uscito con $LASTEXITCODE" }
        }
        & powershell -NoProfile -ExecutionPolicy Bypass -File (Join-Path $u.FullName "build-exe.ps1")
        if ($LASTEXITCODE -ne 0) { throw "build-exe.ps1 uscito con $LASTEXITCODE" }
        $src = Join-Path $u.FullName "dist\$($u.Name)"
        if (-not (Test-Path (Join-Path $src "$($u.Name).exe"))) { throw "eseguibile non trovato in $src" }
        foreach ($vietato in "config\settings.local.json") {
            if (Test-Path (Join-Path $src $vietato)) { throw "$vietato nella build: non si impacchetta" }
        }
        $zip = Join-Path $PSScriptRoot "dist\$($u.Name).zip"
        Remove-Item -Force $zip -ErrorAction SilentlyContinue
        # La cartella dentro lo zip si chiama come la utility: si estrae e si avvia. tar.exe (in Windows
        # da 10 1803) e non Compress-Archive: quello di PowerShell 5.1 scrive i percorsi con "\".
        # Percorso completo: con Git per Windows nel PATH "tar" sarebbe GNU tar, che non fa zip.
        & (Join-Path $env:SystemRoot "System32\tar.exe") -a -c -f $zip -C (Join-Path $u.FullName "dist") $u.Name
        if ($LASTEXITCODE -ne 0) { throw "tar uscito con $LASTEXITCODE" }
        $mb = [math]::Round((Get-Item $zip).Length / 1MB, 1)
        $Esiti += [pscustomobject]@{ Utility = $u.Name; Esito = "OK"; Zip = "dist\$($u.Name).zip ($mb MB)" }
    } catch {
        Write-Host "$($u.Name): $_" -ForegroundColor Red
        $Esiti += [pscustomobject]@{ Utility = $u.Name; Esito = "KO: $_"; Zip = "" }
    }
}

$Esiti | Format-Table -AutoSize
if ($Esiti | Where-Object { $_.Esito -ne "OK" }) { exit 1 }
