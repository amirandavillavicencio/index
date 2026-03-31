# Build & Publish

## Prerrequisitos

- Windows 10/11 x64 (requerido para ejecutar la app WPF publicada).
- .NET SDK 8.0 o superior.
- Git (para clonar repositorio).
- Opcional para fallback OCR: `tesseract` disponible en `PATH`.

## Restore

```bash
dotnet restore AppPortable.sln
```

## Build (Release)

```bash
dotnet build AppPortable.sln -c Release --no-restore
```

## Test

```bash
dotnet test AppPortable.Tests/AppPortable.Tests.csproj -c Release --no-build
```

## Publish (Windows x64 self-contained single-file)

```bash
dotnet publish AppPortable.Desktop/AppPortable.Desktop.csproj -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true
```

## Salida esperada del publish

Carpeta de salida:

- `AppPortable.Desktop/bin/Release/net8.0-windows/win-x64/publish/`

Contenido esperado:

- ejecutable para Windows x64,
- artefacto self-contained,
- empaquetado single-file,
- librerías nativas incluidas para self-extract.

## Ejecución local rápida

```bash
dotnet run --project AppPortable.Desktop/AppPortable.Desktop.csproj
```

## Notas Windows / WPF

- `AppPortable.Desktop` usa `TargetFramework` `net8.0-windows` y `UseWPF=true`.
- Aunque restore/build puede ejecutarse en otros entornos con targeting habilitado, la ejecución de la UI publicada está pensada para Windows.

## Notas OCR / tessdata

- El fallback OCR depende de detectar el ejecutable `tesseract` en `PATH`.
- Si tu instalación de Tesseract requiere datos de idioma (`tessdata`) fuera de rutas por defecto, configura la variable de entorno correspondiente (por ejemplo `TESSDATA_PREFIX`) según tu instalación.
- Estado actual: el servicio OCR existe como fallback, pero no realiza OCR completo por rasterización de página en este repo.
