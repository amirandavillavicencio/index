param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$output = ".\publish-portable"

Write-Host "Publicando Gabriela en modo portable..."
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c $Configuration -r win-x64 --self-contained true /p:PublishSingleFile=false -o $output

Write-Host "\nListo. Carpeta generada: $output"
Write-Host "Recuerda copiar Tesseract portable en: $output\tesseract\ (incluyendo tessdata)."
