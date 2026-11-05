# Crea l'eseguibile Windows in dist\14-gestione-utenti\ (GUI + headless nella stessa cartella).
#   powershell -ExecutionPolicy Bypass -File build-exe.ps1
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$Name = "14-gestione-utenti"

if (-not (Test-Path ".venv")) { throw "Prima eseguire setup.ps1" }
& ".venv\Scripts\python.exe" -m pip install -r requirements-build.txt

Remove-Item -Recurse -Force build, dist -ErrorAction SilentlyContinue

# Le definizioni dei ruoli e le policy servono come file, non come moduli.
$Common = @("--add-data", "app\ruoli;app\ruoli")
& ".venv\Scripts\pyinstaller.exe" --noconfirm --clean --onedir --windowed @Common --name $Name main.py
& ".venv\Scripts\pyinstaller.exe" --noconfirm --clean --onedir --console @Common --name "$Name-headless" run_headless.py
Copy-Item "dist\$Name-headless\$Name-headless.exe" "dist\$Name\"

New-Item -ItemType Directory -Force "dist\$Name\config", "dist\$Name\logs", "dist\$Name\data" | Out-Null
Copy-Item "config\settings.default.json" "dist\$Name\config\"
Copy-Item "README.md" "dist\$Name\"
Copy-Item "..\manuali\Manuale-utility-14.docx" "dist\$Name\"      # manuale per il committente

Write-Host "Fatto: dist\$Name\$Name.exe (GUI) e $Name-headless.exe"
Write-Host "Da distribuire: l'intera cartella dist\$Name (zip). Mai includere settings.local.json ne' data\."
