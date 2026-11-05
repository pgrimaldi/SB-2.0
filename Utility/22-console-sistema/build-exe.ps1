# Crea l'eseguibile Windows in dist\22-console-sistema\ (GUI + headless nella stessa cartella).
#   powershell -ExecutionPolicy Bypass -File build-exe.ps1
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
$Name = "22-console-sistema"

if (-not (Test-Path ".venv")) { throw "Prima eseguire setup.ps1" }
& ".venv\Scripts\python.exe" -m pip install -r requirements-build.txt

Remove-Item -Recurse -Force build, dist -ErrorAction SilentlyContinue

# I sorgenti della Lambda dell'orario (index.py, plan.py) e tzdata servono come file, non solo come moduli.
$Common = @("--collect-all", "tzdata", "--add-data", "app\env\plan.py;app\env",
            "--add-data", "app\env\lambda_src;app\env\lambda_src")
& ".venv\Scripts\pyinstaller.exe" --noconfirm --clean --onedir --windowed @Common --name $Name main.py
& ".venv\Scripts\pyinstaller.exe" --noconfirm --clean --onedir --console @Common --name "$Name-headless" run_headless.py
Copy-Item "dist\$Name-headless\$Name-headless.exe" "dist\$Name\"

New-Item -ItemType Directory -Force "dist\$Name\config", "dist\$Name\logs", "dist\$Name\data" | Out-Null
Copy-Item "config\settings.default.json" "dist\$Name\config\"
Copy-Item "README.md" "dist\$Name\"
Copy-Item "..\manuali\Manuale-utility-22.docx" "dist\$Name\"      # manuale per il committente

Write-Host "Fatto: dist\$Name\$Name.exe (GUI) e $Name-headless.exe"
Write-Host "Da distribuire: l'intera cartella dist\$Name (zip). Mai includere settings.local.json ne' data\."
