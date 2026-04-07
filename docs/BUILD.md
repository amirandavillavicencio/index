# Build & Publish (Portable Windows)

## Prerrequisitos

- Windows 10/11 x64 (para ejecutar la UI WPF publicada).
- .NET SDK 8.0+ (solo para compilar/publicar).
- Tesseract portable (carpeta con `tesseract.exe` y `tessdata`).

## Restore / Build / Test

```bash
dotnet restore AppPortable.sln
dotnet build AppPortable.sln -c Release --no-restore
dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release --no-build
```

## Publish portable (self-contained)

```bash
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=false -o .\publish-portable
```

## Script opcional

```powershell
pwsh ./scripts/publish-portable.ps1
```

## Post-publish

1. Copia `tesseract.exe` y `tessdata` dentro de `publish-portable\tesseract\`.
2. Ejecuta `publish-portable\Gabriela.exe` con doble clic.
3. La app crea `publish-portable\data\` en modo portable (si la carpeta es escribible).

Si no hay permisos de escritura en la carpeta portable, la app usa `%LOCALAPPDATA%\Gabriela` automáticamente.
