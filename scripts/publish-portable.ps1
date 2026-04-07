param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$output = Join-Path $repoRoot "artifact/publish-portable"
if (Test-Path $output) {
    Remove-Item $output -Recurse -Force
}
New-Item -ItemType Directory -Path $output | Out-Null

Write-Host "Publicando Gabriela en modo portable (win-x64, self-contained)..."
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj `
    -c $Configuration `
    -r win-x64 `
    --self-contained true `
    /p:PublishSingleFile=false `
    /p:PublishTrimmed=false `
    -o $output

Write-Host "\nListo. Carpeta generada: $output"

$tesseractExe = Join-Path $output "tesseract/tesseract.exe"
$tessdataDir = Join-Path $output "tesseract/tessdata"
if (!(Test-Path $tesseractExe) -or !(Test-Path $tessdataDir)) {
    Write-Warning "Faltan artefactos OCR. Copia tesseract.exe y tessdata dentro de $output/tesseract/."
}
