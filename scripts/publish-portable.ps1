param(
    [string]$Configuration = "Release"
)

$ErrorActionPreference = "Stop"

$repoRoot = Split-Path -Parent $PSScriptRoot
Set-Location $repoRoot

$output = Join-Path $repoRoot "artifact/publish-portable"

Write-Host "Publicando Gabriela en modo portable..."
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c $Configuration -r win-x64 --self-contained true /p:PublishSingleFile=false -o $output

$mainExe = Join-Path $output "Gabriela.exe"
$dataDir = Join-Path $output "data"
$tesseractDir = Join-Path $output "tesseract"
$tessdataDir = Join-Path $tesseractDir "tessdata"

$sqliteCandidates = @(
    (Join-Path $output "e_sqlite3.dll"),
    (Join-Path $output "runtimes/win-x64/native/e_sqlite3.dll")
)
$sqliteResolved = $sqliteCandidates | Where-Object { Test-Path $_ }

$requiredChecks = @(
    @{ Name = "exe principal"; Path = $mainExe },
    @{ Name = "carpeta data"; Path = $dataDir },
    @{ Name = "carpeta tesseract"; Path = $tesseractDir },
    @{ Name = "carpeta tessdata"; Path = $tessdataDir }
)

$missing = New-Object System.Collections.Generic.List[string]

Write-Host "\nValidación final portable (artifact/publish-portable):"
foreach ($check in $requiredChecks) {
    if (Test-Path $check.Path) {
        Write-Host "  [OK] $($check.Name): $($check.Path)"
    }
    else {
        Write-Host "  [FALTA] $($check.Name): $($check.Path)"
        $missing.Add($check.Name) | Out-Null
    }
}

if ($sqliteResolved.Count -gt 0) {
    foreach ($path in $sqliteResolved) {
        Write-Host "  [OK] dependencia SQLite nativa: $path"
    }
}
else {
    Write-Host "  [FALTA] dependencia SQLite nativa (e_sqlite3.dll) dentro de artifact/publish-portable"
    $missing.Add("dependencia SQLite nativa") | Out-Null
}

if ($missing.Count -gt 0) {
    Write-Error ("Validación portable fallida. Faltantes: " + ($missing -join ", "))
}

Write-Host "\nListo. Publicación portable verificada en: $output"
