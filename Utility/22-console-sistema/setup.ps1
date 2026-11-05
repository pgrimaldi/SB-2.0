# Crea il venv e installa le dipendenze. Eseguire una volta da questa cartella:
#   powershell -ExecutionPolicy Bypass -File setup.ps1
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot

if (-not (Test-Path ".venv")) {
    Write-Host "Creo il virtualenv .venv ..."
    py -3 -m venv .venv
}
Write-Host "Installo le dipendenze ..."
& ".venv\Scripts\python.exe" -m pip install --upgrade pip
& ".venv\Scripts\python.exe" -m pip install -r requirements.txt

if (-not (Test-Path "config\settings.local.json")) {
    Copy-Item "config\settings.default.json" "config\settings.local.json"
    Write-Host "Creato config\settings.local.json (gitignorato): controllare profilo AWS e percorsi." -ForegroundColor Yellow
}
Write-Host "Fatto. GUI: F5 da VS Code oppure .venv\Scripts\python.exe main.py"
